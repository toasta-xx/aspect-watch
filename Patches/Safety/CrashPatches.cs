/*
 * Crash containment against malformed packets. Call CrashPatches.Patch() once.
 *
 * Validates rope indices, bounds checks GetBone, rejects self-referential and
 * cyclic critters grabs, caps parent walks at 30, gates ownership transfers and
 * rate limits incoming critters grab and release RPCs.
 *
 * AntiCrashCritterCore is public and stays usable on its own.
 */


using GorillaExtensions;
using GorillaLocomotion.Gameplay;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Safety
{
    public static class CrashPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.Crash";

        public static bool enabled = true;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            var targets = new List<Type>
            {
                typeof(UpdateRopeDataPatch),
                typeof(GetBonePatch),
                typeof(RemoteCrittersActorGrabbedbyPatch),
                typeof(CrittersActorGrabbedByPatch),
                typeof(CrittersActorGetRootActorPatch),
                typeof(Patch_TransferOwnership_Player),
                typeof(Patch_TransferOwnership_Id),
                typeof(Patch_RPC_Grabbed),
                typeof(Patch_RPC_Released)
            };

            foreach (Type type in targets)
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CrashPatches] Failed to patch " + type.Name + ": " + exception);
                }
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            harmony = null;
        }

        [HarmonyPatch(typeof(VRRig), "UpdateRopeData")]
        private static class UpdateRopeDataPatch
        {
            private static void Prefix(VRRig __instance, ref int ___grabbedRopeIndex, ref int ___grabbedRopeBoneIndex, bool ___grabbedRopeIsPhotonView)
            {
                if (!enabled || ___grabbedRopeIndex == -1)
                    return;

                if (___grabbedRopeIsPhotonView)
                {
                    PhotonView photonView = PhotonView.Find(___grabbedRopeIndex);
                    if (photonView == null)
                        ___grabbedRopeIndex = -1;
                }
                else
                {
                    if (RopeSwingManager.instance == null || !RopeSwingManager.instance.TryGetRope(___grabbedRopeIndex, out var result) || result == null)
                        ___grabbedRopeIndex = -1;
                    else
                        ___grabbedRopeBoneIndex = Mathf.Clamp(___grabbedRopeBoneIndex, 0, int.MaxValue);
                }
            }
        }

        [HarmonyPatch(typeof(GorillaRopeSwing), "GetBone")]
        private static class GetBonePatch
        {
            private static bool Prefix(GorillaRopeSwing __instance, ref Transform __result, int index, Transform[] ___nodes)
            {
                if (!enabled)
                    return true;

                if (___nodes == null || ___nodes.Length == 0)
                {
                    __result = __instance.transform;
                    return false;
                }

                if (index < 0)
                {
                    __result = ___nodes[0];
                    return false;
                }

                if (index >= ___nodes.Length)
                {
                    __result = ___nodes[___nodes.Length - 1];
                    return false;
                }

                __result = ___nodes[index];
                return false;
            }
        }

        [HarmonyPatch(typeof(CrittersManager), "RemoteCrittersActorGrabbedby")]
        private static class RemoteCrittersActorGrabbedbyPatch
        {
            private static bool Prefix(CrittersManager __instance, int grabbedActorID, int grabberActorID)
            {
                if (!enabled)
                    return true;

                if (grabbedActorID == grabberActorID || grabbedActorID < 0 || grabberActorID < 0) return false;
                if (__instance == null || __instance.actorById == null) return true;
                if (!__instance.actorById.TryGetValue(grabbedActorID, out _) || !__instance.actorById.TryGetValue(grabberActorID, out CrittersActor grabber)) return false;
                int current = grabber.parentActorId;
                int depth = 0;
                while (current != -1 && depth < 30)
                {
                    if (current == grabbedActorID) return false;
                    if (__instance.actorById.TryGetValue(current, out CrittersActor parent))
                        current = parent.parentActorId;
                    else
                        break;
                    depth++;
                }
                return depth < 30;
            }
        }

        [HarmonyPatch(typeof(CrittersActor), "GrabbedBy")]
        private static class CrittersActorGrabbedByPatch
        {
            private static bool Prefix(CrittersActor __instance, CrittersActor grabbingActor)
            {
                if (!enabled)
                    return true;

                if (__instance == null || grabbingActor == null || __instance == grabbingActor) return false;
                CrittersActor current = grabbingActor;
                int depth = 0;
                while (current != null && depth < 30)
                {
                    if (current == __instance) return false;
                    if (CrittersManager.instance != null && CrittersManager.instance.actorById != null && current.parentActorId != -1 && CrittersManager.instance.actorById.TryGetValue(current.parentActorId, out CrittersActor parent))
                        current = parent;
                    else
                        break;
                    depth++;
                }
                return depth < 30;
            }
        }

        [HarmonyPatch(typeof(CrittersActor), "GetRootActor", typeof(int))]
        private static class CrittersActorGetRootActorPatch
        {
            private static bool Prefix(int actorId, ref CrittersActor __result)
            {
                if (!enabled)
                    return true;

                if (CrittersManager.instance == null || CrittersManager.instance.actorById == null)
                {
                    __result = null;
                    return false;
                }
                int currentId = actorId;
                CrittersActor currentActor = null;
                int depth = 0;
                while (currentId != -1 && depth < 30)
                {
                    if (!CrittersManager.instance.actorById.TryGetValue(currentId, out CrittersActor next) || next == null)
                        break;
                    currentActor = next;
                    if (currentActor.parentActorId == currentId)
                    {
                        currentActor.parentActorId = -1;
                        break;
                    }
                    currentId = currentActor.parentActorId;
                    depth++;
                }
                if (depth >= 30 && currentActor != null)
                    currentActor.parentActorId = -1;
                __result = currentActor;
                return false;
            }
        }
    }

    public static class AntiCrashCritterCore
    {
        private static readonly Dictionary<int, float> LastRpc = new Dictionary<int, float>();
        private const float MinInterval = 0.05f;

        public static bool LocalTransferInProgress { get; private set; }

        public static void BeginLocalTransfer() => LocalTransferInProgress = true;

        public static void EndLocalTransfer() => LocalTransferInProgress = false;

        public static bool AllowTransfer(PhotonView view, Player newOwner)
        {
            if (!PhotonNetwork.IsMasterClient) return true;
            if (view == null || newOwner == null) return true;
            if (view.Owner != null && view.Owner.IsLocal && !LocalTransferInProgress && !newOwner.IsLocal)
                return false;
            return true;
        }

        public static bool ValidateIncoming(CrittersManager manager, PhotonMessageInfo info)
        {
            if (manager == null) return false;

            Player sender = null;
            try { sender = info.Sender; } catch { }
            if (sender == null) return true;

            float now = Time.realtimeSinceStartup;
            if (LastRpc.TryGetValue(sender.ActorNumber, out float last) && now - last < MinInterval)
                return false;
            LastRpc[sender.ActorNumber] = now;

            if (!PhotonNetwork.IsMasterClient) return true;
            if (sender.IsLocal) return true;

            Player guardOwner = GetGuardOwner(manager);
            if (guardOwner == null) return true;
            return guardOwner.ActorNumber == sender.ActorNumber;
        }

        public static Player GetGuardOwner(CrittersManager manager)
        {
            object guard = null;
            try
            {
                var type = typeof(CrittersManager);
                var field = type.GetField("guard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) guard = field.GetValue(manager);
                if (guard == null)
                {
                    var property = type.GetProperty("guard", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null) guard = property.GetValue(manager);
                }
            }
            catch { return null; }
            if (guard == null) return null;

            var guardType = guard.GetType();
            foreach (var name in new[] { "currentOwner", "owner", "Owner" })
            {
                var property = guardType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    if (typeof(Player).IsAssignableFrom(property.PropertyType))
                    {
                        try { return (Player)property.GetValue(guard); } catch { }
                    }
                    try
                    {
                        object value = property.GetValue(guard);
                        if (value is NetPlayer netPlayer)
                            return PhotonNetwork.CurrentRoom?.GetPlayer(netPlayer.ActorNumber);
                    }
                    catch { }
                }
                var field = guardType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    if (typeof(Player).IsAssignableFrom(field.FieldType))
                    {
                        try { return (Player)field.GetValue(guard); } catch { }
                    }
                    try
                    {
                        object value = field.GetValue(guard);
                        if (value is NetPlayer netPlayer)
                            return PhotonNetwork.CurrentRoom?.GetPlayer(netPlayer.ActorNumber);
                    }
                    catch { }
                }
            }
            if (guard is Component component)
            {
                var view = component.GetComponentInParent<PhotonView>();
                if (view != null) return view.Owner;
            }
            return null;
        }
    }

    [HarmonyPatch]
    public static class Patch_TransferOwnership_Player
    {
        [HarmonyPrepare]
        public static bool Prepare() =>
            AccessTools.Method(typeof(PhotonView), nameof(PhotonView.TransferOwnership), new[] { typeof(Player) }) != null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonView), nameof(PhotonView.TransferOwnership), new[] { typeof(Player) })]
        public static bool Prefix(PhotonView __instance, Player newOwner) =>
            !CrashPatches.enabled || AntiCrashCritterCore.AllowTransfer(__instance, newOwner);
    }

    [HarmonyPatch]
    public static class Patch_TransferOwnership_Id
    {
        [HarmonyPrepare]
        public static bool Prepare() =>
            AccessTools.Method(typeof(PhotonView), nameof(PhotonView.TransferOwnership), new[] { typeof(int) }) != null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhotonView), nameof(PhotonView.TransferOwnership), new[] { typeof(int) })]
        public static bool Prefix(PhotonView __instance, int newOwnerId)
        {
            if (!CrashPatches.enabled)
                return true;

            Player player = PhotonNetwork.CurrentRoom?.GetPlayer(newOwnerId);
            return AntiCrashCritterCore.AllowTransfer(__instance, player);
        }
    }

    [HarmonyPatch]
    public static class Patch_RPC_Grabbed
    {
        [HarmonyPrepare]
        public static bool Prepare() => FindMethod() != null;

        [HarmonyTargetMethod]
        public static MethodBase FindMethod()
        {
            try
            {
                return AccessTools.GetDeclaredMethods(typeof(CrittersManager))
                    .FirstOrDefault(method => method.Name == "RemoteCrittersActorGrabbedby");
            }
            catch { return null; }
        }

        [HarmonyPrefix]
        public static bool Prefix(object[] __args)
        {
            if (!CrashPatches.enabled)
                return true;

            PhotonMessageInfo info = default;
            if (__args != null)
                for (int i = __args.Length - 1; i >= 0; i--)
                    if (__args[i] is PhotonMessageInfo messageInfo) { info = messageInfo; break; }
            return AntiCrashCritterCore.ValidateIncoming(CrittersManager.instance, info);
        }
    }

    [HarmonyPatch]
    public static class Patch_RPC_Released
    {
        [HarmonyPrepare]
        public static bool Prepare() => FindMethod() != null;

        [HarmonyTargetMethod]
        public static MethodBase FindMethod()
        {
            try
            {
                return AccessTools.GetDeclaredMethods(typeof(CrittersManager))
                    .FirstOrDefault(method => method.Name == "RemoteCritterActorReleased");
            }
            catch { return null; }
        }

        [HarmonyPrefix]
        public static bool Prefix(object[] __args)
        {
            if (!CrashPatches.enabled)
                return true;

            PhotonMessageInfo info = default;
            if (__args != null)
                for (int i = __args.Length - 1; i >= 0; i--)
                    if (__args[i] is PhotonMessageInfo messageInfo) { info = messageInfo; break; }
            return AntiCrashCritterCore.ValidateIncoming(CrittersManager.instance, info);
        }
    }
}
