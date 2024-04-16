using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Tectonic
{
    public class TectonicModSystem : ModSystem
    {
        public static ICoreServerAPI serverApi;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        private Harmony harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;
            serverApi.Logger.Notification("Region size defaults to: " + serverApi.WorldManager.RegionSize);
        }

        [HarmonyPatch(typeof(GenTerra))]
        [HarmonyPatch("initWorldGen")]
        public class PatchGenTerraInitWorldGen
        {
            static void Postfix(GenTerra __instance)
            {
                var distort2dxField = AccessTools.Field(typeof(GenTerra), "distort2dx");
                var distort2dzField = AccessTools.Field(typeof(GenTerra), "distort2dz");
                var noiseScaleField = AccessTools.Field(typeof(GenTerra), "noiseScale");
                var octaves = AccessTools.Field(typeof(GenTerra), "terrainGenOctaves");
                
                octaves.SetValue(__instance,5);
                
                var scaleAdjustedFreqsMethod = AccessTools.Method(typeof(GenTerra), "scaleAdjustedFreqs");
                var geoUpheavalNoiseField = AccessTools.Field(typeof(GenTerra), "geoUpheavalNoise");

                float noiseScale = (float)noiseScaleField.GetValue(__instance);
                double[] zeroMagnitudes = new double[1] { 0.0 };
                double[] frequencies = new double[1] { 0.0 };

                double[] adjustedFrequencies = (double[])scaleAdjustedFreqsMethod.Invoke(__instance, new object[] { frequencies, noiseScale });

                // Set both distort2dx and distort2dz with zeros in magnitudes
                SimplexNoise newDistort2dx = new SimplexNoise(zeroMagnitudes, adjustedFrequencies, serverApi.World.Seed + 9876);
                SimplexNoise newDistort2dz = new SimplexNoise(zeroMagnitudes, adjustedFrequencies, serverApi.World.Seed + 9876 + 2);

                distort2dxField.SetValue(__instance, newDistort2dx);
                distort2dzField.SetValue(__instance, newDistort2dz);
                
                serverApi.Logger.Debug("distort2dx and distort2dz noise arrays set to zero.");
                
                NormalizedSimplexNoise newGeoUpheavalNoise = new NormalizedSimplexNoise(
                    new[] {0.0001d},
                    new[] {0.0001d},
                    serverApi.World.Seed + 9876 + 1);

                // Optionally log the newly created noise instance or other details
                serverApi.Logger.Debug("New geoUpheavalNoise created with seed adjustment.");

                // Set the new geoUpheavalNoise
                geoUpheavalNoiseField.SetValue(__instance, newGeoUpheavalNoise);
            }
        }
    }
}
