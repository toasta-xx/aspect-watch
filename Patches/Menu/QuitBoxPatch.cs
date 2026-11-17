using GorillaLocomotion;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GorillaQuitBox), nameof(GorillaQuitBox.OnBoxTriggered))]
    public static class QuitBoxPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.QuitBox";

        private const string StumpZonePath = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/TreeRoomSpawnForestZone";
        private const string StumpJoinPath = "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab/JoinPublicRoom - Forest, Tree Exit";

        public static bool enabled;
        public static bool teleportToStump;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(QuitBoxPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[QuitBoxPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            teleportToStump = false;
            harmony = null;
        }

        public static void TeleportToStump()
        {
            GameObject zoneObject = GameObject.Find(StumpZonePath);
            if (zoneObject != null)
            {
                GorillaSetZoneTrigger zoneTrigger = zoneObject.GetComponent<GorillaSetZoneTrigger>();
                if (zoneTrigger != null)
                    zoneTrigger.OnBoxTriggered();
            }

            GameObject joinObject = GameObject.Find(StumpJoinPath);
            if (joinObject != null && GTPlayer.Instance != null)
                GTPlayer.Instance.TeleportTo(joinObject.transform, false, false);
        }

        private static bool Prefix()
        {
            if (teleportToStump)
            {
                try
                {
                    TeleportToStump();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[QuitBoxPatch] Failed to teleport to stump: " + exception);
                }

                return false;
            }

            return !enabled;
        }
    }
}
