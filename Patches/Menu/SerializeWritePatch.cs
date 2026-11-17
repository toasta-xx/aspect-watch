/*
 * Position spoofing primitive. Call SerializeWritePatch.Patch() once.
 *
 *   positionOverride                       raw override, no expiry. Set it, send, clear it.
 *   PositionOverride                       same field, property alias.
 *   SetOverride(position, seconds)         hold for a duration
 *   SetPose(position, rotation, seconds)   position plus body rotation
 *   ClearOverride()                        end immediately
 *   HasActiveOverride                      whether a write would be rewritten
 *
 * The rig is moved for one serialization pass and restored in the finalizer, so
 * other clients receive the override while your real position is unchanged.
 * Local reads of your transform still see the true position.
 *
 * This is the whole mechanism behind passing the tag distance check. The master
 * validates taggingRig.syncPos against the target, and syncPos only ever comes
 * from the position you serialize, so moving the rig for the write is enough.
 */


using HarmonyLib;
using Photon.Pun;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.OnSerializeWrite))]
    public static class SerializeWritePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.SerializeWrite";

        public static bool enabled = true;

        public static Vector3? positionOverride;
        public static Quaternion? rotationOverride;

        private static float overrideExpiresAt;
        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static bool HasActiveOverride => enabled && positionOverride != null;

        public static Vector3? PositionOverride
        {
            get => positionOverride;
            set => positionOverride = value;
        }

        public static Quaternion? RotationOverride
        {
            get => rotationOverride;
            set => rotationOverride = value;
        }

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(SerializeWritePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SerializeWritePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            ClearOverride();
            harmony = null;
        }

        public static void SetOverride(Vector3 position, float seconds = 0.1f)
        {
            positionOverride = position;
            rotationOverride = null;
            overrideExpiresAt = Time.time + Mathf.Max(0f, seconds);
            enabled = true;
        }

        public static void SetPose(Vector3 position, Quaternion rotation, float seconds = 0.1f)
        {
            positionOverride = position;
            rotationOverride = rotation;
            overrideExpiresAt = Time.time + Mathf.Max(0f, seconds);
            enabled = true;
        }

        public static void ClearOverride()
        {
            positionOverride = null;
            rotationOverride = null;
            overrideExpiresAt = 0f;
        }

        private static bool Prefix(PhotonView view, out SerializeState __state)
        {
            __state = default;

            if (!enabled || positionOverride == null || VRRig.LocalRig == null)
                return true;

            if (overrideExpiresAt > 0f && Time.time > overrideExpiresAt)
                return true;

            if (!IsLocalRigView(view))
                return true;

            Transform rig = VRRig.LocalRig.transform;

            __state.Applied = true;
            __state.Position = rig.position;
            __state.Rotation = rig.rotation;

            rig.position = positionOverride.Value;

            if (rotationOverride != null)
                rig.rotation = rotationOverride.Value;

            return true;
        }

        public static PhotonView GetLocalRigView()
        {
            try
            {
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.myVRRig != null)
                    return GorillaTagger.Instance.myVRRig.GetView;
            }
            catch { }

            return null;
        }

        private static bool IsLocalRigView(PhotonView view)
        {
            if (view == null)
                return false;

            PhotonView localView = GetLocalRigView();
            if (localView != null)
                return view == localView;

            return view.IsMine && VRRig.LocalRig != null && view.GetComponent<VRRig>() == VRRig.LocalRig;
        }

        private static void Finalizer(SerializeState __state)
        {
            if (!__state.Applied || VRRig.LocalRig == null)
                return;

            Transform rig = VRRig.LocalRig.transform;
            rig.position = __state.Position;
            rig.rotation = __state.Rotation;
        }

        private struct SerializeState
        {
            public bool Applied;
            public Vector3 Position;
            public Quaternion Rotation;
        }
    }
}
