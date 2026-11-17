using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class MothershipPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Mothership";

        public static bool enabled;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(MothershipPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[MothershipPatches] Failed to patch " + type.Name + ": " + exception);
                }
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            harmony = null;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.EnqueueTelemetryEvent))]
        private static class EnqueueTelemetryEvent
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.FlushMothershipTelemetry))]
        private static class FlushMothershipTelemetry
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.EnqueueZoneEvent))]
        private static class EnqueueZoneEvent
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.PostGameModeEvent))]
        private static class PostGameModeEvent
        {
            private static bool Prefix() => !enabled;
        }
    }
}
