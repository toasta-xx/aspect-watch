using HarmonyLib;
using Photon.Pun;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.IsPositionInRange))]
    public static class DistancePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Distance";

        public static bool enabled;

        public static Func<NetPlayer, bool> ShouldBypassChecks = player => player == NetworkSystem.Instance.LocalPlayer;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(DistancePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[DistancePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            harmony = null;
        }

        public static void Postfix(VRRig __instance, ref bool __result, Vector3 position, float range)
        {
            NetPlayer player = GetPlayerFromVRRig(__instance) ?? null;
            if ((enabled && __instance.isLocal) || (player != null && ShouldBypassChecks?.Invoke(player) == true))
                __result = true;
        }

        public static NetPlayer GetPlayerFromVRRig(VRRig rig)
        {
            if (rig == null || NetworkSystem.Instance == null || !PhotonNetwork.InRoom)
                return null;

            try
            {
                if (rig.Creator != null)
                {
                    foreach (NetPlayer candidate in NetworkSystem.Instance.AllNetPlayers)
                        if (candidate != null && candidate.ActorNumber == rig.Creator.ActorNumber)
                            return candidate;
                }

                if (rig.rigSerializer == null || rig.rigSerializer.gameObject == null)
                    return null;

                int ownerId = NetworkSystem.Instance.GetOwningPlayerID(rig.rigSerializer.gameObject);
                foreach (NetPlayer candidate in NetworkSystem.Instance.AllNetPlayers)
                    if (candidate != null && candidate.ActorNumber == ownerId)
                        return candidate;
            }
            catch { }

            return null;
        }
    }
}
