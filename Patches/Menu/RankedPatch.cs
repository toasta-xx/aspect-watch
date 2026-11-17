using GorillaNetworking;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(PhotonNetworkController), nameof(PhotonNetworkController.AttemptToJoinRankedPublicRoom))]
    public static class RankedPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Ranked";

        public static bool enabled;
        public static string targetPlatform;
        public static string targetTier;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(RankedPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[RankedPatch] Failed to patch: " + exception);
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

        private static bool Prefix(GorillaNetworkJoinTrigger triggeredTrigger, JoinType roomJoinType)
        {
            if (!enabled)
                return true;

            PhotonNetworkController controller = PhotonNetworkController.Instance;
            if (controller == null || triggeredTrigger == null)
                return true;

            try
            {
                string tier = targetTier;
                if (string.IsNullOrEmpty(tier) && RankedProgressionManager.Instance != null)
                    tier = RankedProgressionManager.Instance.GetRankedMatchmakingTier().ToString();

                controller.AttemptToJoinRankedPublicRoomAsync(
                    triggeredTrigger,
                    tier,
                    string.IsNullOrEmpty(targetPlatform) ? "PC" : targetPlatform,
                    roomJoinType
                );
            }
            catch (Exception exception)
            {
                Debug.LogError("[RankedPatch] Failed to redirect ranked join: " + exception);
                return true;
            }

            return false;
        }
    }
}
