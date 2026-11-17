using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(PropHuntHandFollower), nameof(PropHuntHandFollower.GeoCollisionPoint))]
    public static class PropPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Prop";

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
                harmony.CreateClassProcessor(typeof(PropPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PropPatch] Failed to patch: " + exception);
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

        private static void Postfix(ref Vector3 __result, Vector3 targetPos)
        {
            if (enabled)
                __result = targetPos;
        }
    }
}
