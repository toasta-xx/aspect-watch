using GorillaNetworking;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GorillaNetworkJoinTrigger), nameof(GorillaNetworkJoinTrigger.OnBoxTriggered))]
    public static class NetworkTriggerPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.NetworkTrigger";

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
                harmony.CreateClassProcessor(typeof(NetworkTriggerPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[NetworkTriggerPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            harmony = null;
        }

        private static bool Prefix() => !enabled;
    }
}
