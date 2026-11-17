/*
 * Serialization hub. Call SerializePatch.Patch() once, Unpatch() to remove.
 *
 *   StopSerialization = true       drop every outgoing view update
 *   SetDelay(seconds)              defer each update, ClearDelay() to stop
 *   OverrideSerialization = () => bool
 *   OnSerialize += ...
 *
 *   SendSerialize(view)            one view now
 *   MassSerialize()                every owned view
 *   ReserializeMyRig()             just your rig
 *   Desync(seconds)                blackout then auto resend
 *   FreezeRig(seconds)             hold the last sent pose
 *   LastSyncPosition               last real synced position
 *   StartSerializationDelay(action, seconds)
 *
 * The delay path runs one coroutine per deferred update, so it accumulates in a
 * full room. Keep delays short and clear them when done.
 */


using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.RunViewUpdate))]
    public static class SerializePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Serialize";

        public static bool Enabled = true;
        public static bool StopSerialization;
        public static float SerializationDelaySeconds;

        public static event Action OnSerialize;

        public static Func<bool> OverrideSerialization;

        public static Vector3 LastSyncPosition;
        public static Vector3 LastSyncLeftHandPosition;
        public static Vector3 LastSyncRightHandPosition;

        private static bool suppressNext;
        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(SerializePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SerializePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            StopSerialization = false;
            SerializationDelaySeconds = 0f;
            OverrideSerialization = null;
            harmony = null;
        }

        public static void SetDelay(float seconds) => SerializationDelaySeconds = Mathf.Max(0f, seconds);

        public static void ClearDelay() => SerializationDelaySeconds = 0f;

        public static void ResetState()
        {
            Enabled = true;
            StopSerialization = false;
            SerializationDelaySeconds = 0f;
            OverrideSerialization = null;
        }

        public static void Desync(float seconds) => StartSerializationDelay(() =>
        {
            StopSerialization = false;
            MassSerialize();
        }, seconds);

        public static void FreezeRig(float seconds)
        {
            if (VRRig.LocalRig == null)
                return;

            VRRig.LocalRig.transform.position = LastSyncPosition;
            Desync(seconds);
        }

        public static void ReserializeMyRig()
        {
            PhotonView view = GetLocalRigView();
            if (view != null)
                SendSerialize(view);
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

        private static bool Prefix()
        {
            if (!Enabled || !PhotonNetwork.InRoom)
                return true;

            if (StopSerialization)
                return false;

            if (suppressNext)
            {
                suppressNext = false;
                return false;
            }

            try
            {
                OnSerialize?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SerializePatch] Error in OnSerialize: " + exception);
            }

            if (SerializationDelaySeconds > 0f)
            {
                float delay = SerializationDelaySeconds;

                StartSerializationDelay(() =>
                {
                    suppressNext = true;
                    SerializationDelaySeconds = 0f;
                    PhotonNetwork.RunViewUpdate();
                    SerializationDelaySeconds = delay;
                }, delay);

                return false;
            }

            if (OverrideSerialization == null)
                return true;

            try
            {
                return OverrideSerialization();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SerializePatch] Error in OverrideSerialization: " + exception);
                return false;
            }
        }

        private static void Postfix()
        {
            if (!Enabled)
                return;

            LastSyncPosition = VRRig.LocalRig != null ? VRRig.LocalRig.transform.position : LastSyncPosition;

            if (VRRig.LocalRig != null)
            {
                if (VRRig.LocalRig.leftHand != null && VRRig.LocalRig.leftHand.rigTarget != null)
                    LastSyncLeftHandPosition = VRRig.LocalRig.leftHand.rigTarget.transform.position;

                if (VRRig.LocalRig.rightHand != null && VRRig.LocalRig.rightHand.rigTarget != null)
                    LastSyncRightHandPosition = VRRig.LocalRig.rightHand.rigTarget.transform.position;
            }
        }

        public static void MassSerialize(bool exclude = false, PhotonView[] viewFilter = null, int timeOffset = 0, float delay = 0f)
        {
            if (!PhotonNetwork.InRoom)
                return;

            viewFilter ??= Array.Empty<PhotonView>();

            var photonViewList = PhotonNetwork.photonViewList;
            if (photonViewList == null)
                return;

            var viewsToSerialize = new List<PhotonView>();
            List<int> filteredViewIds = viewFilter.Select(view => view != null ? view.ViewID : -1).ToList();

            foreach (PhotonView photonView in photonViewList.Values)
            {
                if (photonView == null ||
                    !photonView.IsMine ||
                    photonView.Synchronization == ViewSynchronization.Off ||
                    !photonView.isActiveAndEnabled ||
                    PhotonNetwork.blockedSendingGroups.Contains(photonView.Group))
                    continue;

                if (exclude)
                {
                    if (!filteredViewIds.Contains(photonView.ViewID))
                        viewsToSerialize.Add(photonView);
                }
                else if (filteredViewIds.Contains(photonView.ViewID))
                {
                    viewsToSerialize.Add(photonView);
                }
            }

            foreach (PhotonView view in viewsToSerialize)
                SendSerialize(view, null, timeOffset, delay);
        }

        public static void SendSerialize(PhotonView view, RaiseEventOptions options = null, int timeOffset = 0, float delay = 0f)
        {
            if (!PhotonNetwork.InRoom)
                return;

            if (view == null)
            {
                Debug.LogError("[SerializePatch] PhotonView is null. Cannot serialize.");
                return;
            }

            List<object> serializedData = PhotonNetwork.OnSerializeWrite(view);
            if (serializedData == null)
                return;

            var raiseEventBatch = new PhotonNetwork.RaiseEventBatch
            {
                Reliable = view.Synchronization == ViewSynchronization.ReliableDeltaCompressed || view.mixedModeIsReliable,
                Group = view.Group
            };

            IDictionary batches = PhotonNetwork.serializeViewBatches;
            var serializeViewBatch = new PhotonNetwork.SerializeViewBatch(raiseEventBatch, 2);

            if (!batches.Contains(raiseEventBatch))
                batches[raiseEventBatch] = serializeViewBatch;

            serializeViewBatch.Add(serializedData);

            RaiseEventOptions sendOptions = PhotonNetwork.serializeRaiseEvOptions;
            RaiseEventOptions finalOptions = options != null
                ? new RaiseEventOptions
                {
                    CachingOption = sendOptions.CachingOption,
                    InterestGroup = sendOptions.InterestGroup,
                    TargetActors = options.TargetActors,
                    Receivers = options.Receivers
                }
                : sendOptions;

            bool reliable = serializeViewBatch.Batch.Reliable;
            List<object> objectUpdate = serializeViewBatch.ObjectUpdates;
            byte currentLevelPrefix = PhotonNetwork.currentLevelPrefix;

            objectUpdate[0] = PhotonNetwork.ServerTimestamp + timeOffset;
            objectUpdate[1] = currentLevelPrefix != 0 ? (object)currentLevelPrefix : null;

            byte eventCode = (byte)(reliable ? 206 : 201);
            SendOptions sendFlags = reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;

            if (delay <= 0f)
            {
                PhotonNetwork.NetworkingClient.OpRaiseEvent(eventCode, objectUpdate, finalOptions, sendFlags);
            }
            else
            {
                List<object> copy = new List<object>(objectUpdate);
                StartSerializationDelay(() => PhotonNetwork.NetworkingClient.OpRaiseEvent(eventCode, copy, finalOptions, sendFlags), delay);
            }

            serializeViewBatch.Clear();
        }

        public static IEnumerator SerializationDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        public static Coroutine StartSerializationDelay(Action action, float delay)
        {
            if (delay <= 0f)
            {
                action?.Invoke();
                return null;
            }

            return CoroutineHost.Instance.StartCoroutine(SerializationDelay(action, delay));
        }

        private sealed class CoroutineHost : MonoBehaviour
        {
            private static CoroutineHost instance;

            public static CoroutineHost Instance
            {
                get
                {
                    if (instance == null)
                    {
                        GameObject host = new GameObject("AspectWatch.SerializePatch");
                        DontDestroyOnLoad(host);
                        instance = host.AddComponent<CoroutineHost>();
                    }

                    return instance;
                }
            }
        }
    }
}
