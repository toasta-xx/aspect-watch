using GorillaTagScripts;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(BuilderAttachGridPlane), nameof(BuilderAttachGridPlane.IsConnected))]
    public static class OverlapPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Overlap";

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
                harmony.CreateClassProcessor(typeof(OverlapPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[OverlapPatch] Failed to patch: " + exception);
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
                __result = false;
        }
    }
}
