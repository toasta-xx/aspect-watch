using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(KIDManager), nameof(KIDManager.HasPermissionToUseFeature))]
    public static class PermissionPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Permission";

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
                harmony.CreateClassProcessor(typeof(PermissionPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PermissionPatch] Failed to patch: " + exception);
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

        private static void Postfix(ref bool __result)
        {
            if (enabled)
                __result = true;
        }
    }
}
