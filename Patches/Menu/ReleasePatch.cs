using GorillaLocomotion;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(TakeMyHand_HandLink), nameof(TakeMyHand_HandLink.OnRelease))]
    public static class ReleasePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Release";

        public static bool enabled;
        public static float launchSpeed = 20f;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(ReleasePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[ReleasePatch] Failed to patch: " + exception);
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

        private static bool Prefix(TakeMyHand_HandLink __instance, DropZone zoneReleased, GameObject releasingHand)
        {
            if (!enabled || __instance == null || __instance.myRig == null || __instance.myRig.isOfflineVRRig)
                return true;

            try
            {
                GTPlayer player = GTPlayer.Instance;
                if (player == null || EquipmentInteractor.instance == null || releasingHand == null)
                    return true;

                TakeMyHand_HandLink handLink = releasingHand == EquipmentInteractor.instance.leftHand
                    ? VRRig.LocalRig.leftHandLink
                    : VRRig.LocalRig.rightHandLink;

                if (handLink == null)
                    return true;

                bool grounded = false;

                HandLinkAuthorityStatus selfHandLinkAuthority = player.TakeMyHand_GetSelfHandLinkAuthority();
                int steps;
                HandLinkAuthorityStatus selfChainAuthority = handLink.GetChainAuthority(out steps);

                if (selfHandLinkAuthority.type >= HandLinkAuthorityType.ButtGrounded && selfChainAuthority.type < selfHandLinkAuthority.type)
                {
                    grounded = true;
                }
                else if (handLink.myOtherHandLink != null && handLink.myOtherHandLink.grabbedLink != null)
                {
                    HandLinkAuthorityStatus otherChainAuthority = handLink.myOtherHandLink.GetChainAuthority(out steps);
                    if (otherChainAuthority.type >= HandLinkAuthorityType.ButtGrounded && selfChainAuthority.type < otherChainAuthority.type)
                        grounded = true;
                }

                if (grounded && __instance.myRig.netView != null)
                {
                    Vector3 averageVelocity = player.GetHandVelocityTracker(handLink.isLeftHand)
                        .GetAverageVelocity(true, 0.15f, false).normalized * launchSpeed;

                    __instance.myRig.netView.SendRPC("DroppedByPlayer", __instance.myRig.Creator, new object[] { averageVelocity });
                    __instance.myRig.ApplyLocalTrajectoryOverride(averageVelocity);
                }

                handLink.BreakLink();
            }
            catch (Exception exception)
            {
                Debug.LogError("[ReleasePatch] Failed to apply release launch: " + exception);
                return true;
            }

            return false;
        }
    }
}
