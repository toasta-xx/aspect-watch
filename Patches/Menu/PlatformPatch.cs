using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(SIGadgetPlatformDeployer), nameof(SIGadgetPlatformDeployer.OnUpdateAuthority))]
    public static class PlatformPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Platform";

        public static bool enabled;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(PlatformPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PlatformPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            harmony = null;
        }

        private static void Prefix(SIGadgetPlatformDeployer __instance)
        {
            if (enabled)
                __instance.remainingRechargeTime = 0f;
        }
    }
}
