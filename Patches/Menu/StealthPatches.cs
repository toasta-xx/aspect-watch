using GorillaNetworking;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class StealthPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Stealth";

        public static bool enabled;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(StealthPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[StealthPatches] Failed to patch " + type.Name + ": " + exception);
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

        [HarmonyPatch(typeof(GorillaPlayerScoreboardLine), nameof(GorillaPlayerScoreboardLine.ReportPlayer))]
        private static class BlockScoreboardReport
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaScoreboardTotalUpdater), nameof(GorillaScoreboardTotalUpdater.ReportMute))]
        private static class BlockMuteReport
        {
            private static bool Prefix() => !enabled;
        }
    }
}
