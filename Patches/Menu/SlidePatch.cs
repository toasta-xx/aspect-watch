using GorillaLocomotion;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GTPlayer), nameof(GTPlayer.GetSlidePercentage))]
    public static class SlidePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Slide";

        public static bool everythingSlippery;
        public static bool everythingGrippy;
        public static bool minimalSlip;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(SlidePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SlidePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            everythingSlippery = false;
            everythingGrippy = false;
            minimalSlip = false;
            harmony = null;
        }

        private static void Postfix(ref float __result)
        {
            if (everythingSlippery)
                __result = 1f;

            if (everythingGrippy)
                __result = 0f;

            if (minimalSlip)
                __result *= 0.75f;
        }
    }
}
