/*
 * The local report pipeline. Call AntiCheatPatches.Patch() once.
 *
 * Covers the whole path, not just the entry point: SendReport, DispatchReport
 * (the method that actually raises report event 8), CheckReports and
 * RefreshRPCs (the per-second self audit), LogErrorCount, IncrementRPCTracker,
 * GetRPCCallTracker, ShouldDisconnectFromRoom, QuitDelay, CloseInvalidRoom,
 * ForceStopGame_DisconnectAndDestroy and both public join GracePeriods.
 *
 *   enabled              master switch
 *   AntiCheatSelf/All    notify when a report is attempted
 *   AntiCheatReasonHide  hide the reason text in that notice
 *   AntiACReport         clear the report and notify
 *   ClearStateOnBlock    wipe the pending reason/id/name fields
 *   BlockedReports       count of blocks
 *
 * All of it runs on your own client. Reports other players raise about you are
 * generated on their machines and the master's, so this cannot suppress those.
 */


using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Safety
{
    public static class AntiCheatPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.AntiCheat";

        public static bool enabled = true;

        public static bool AntiCheatSelf;
        public static bool AntiCheatAll;
        public static bool AntiCheatReasonHide;
        public static bool AntiACReport;
        public static bool ClearStateOnBlock = true;

        public static VRRig ReportRig;

        public static Action<string> Notify = message => Debug.Log("[AntiCheat] " + message);
        public static Action OnClearNotifications;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static int BlockedReports { get; private set; }

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(AntiCheatPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[AntiCheatPatches] Failed to patch " + type.Name + ": " + exception);
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

        public static void ResetBlockedReports() => BlockedReports = 0;

        public static void AntiReportFRT(Player subject)
        {
            if (subject == null || subject.IsLocal)
                return;

            ReportRig = GetRigFromPlayer(subject);
        }

        public static VRRig GetRigFromPlayer(Player player)
        {
            if (player == null || VRRigCache.ActiveRigs == null)
                return null;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isOfflineVRRig)
                    continue;

                if (rig.Creator != null && rig.Creator.ActorNumber == player.ActorNumber)
                    return rig;
            }

            return null;
        }

        public static void ClearPendingReport(MonkeAgent agent)
        {
            if (agent == null)
                return;

            try
            {
                MonkeState.Clear(agent);
            }
            catch (Exception exception)
            {
                Debug.LogError("[AntiCheatPatches] Failed to clear pending report: " + exception);
            }
        }

        private static void Announce(string susReason, string susId, string susNick)
        {
            BlockedReports++;

            if (AntiCheatSelf || AntiCheatAll)
            {
                string shown = AntiCheatReasonHide ? "hidden reason" : susReason;

                if (susId == PhotonNetwork.LocalPlayer.UserId)
                    Notify($"<color=grey>[</color><color=green>ANTI-CHEAT</color><color=grey>]</color> You have been reported for {shown}.");
                else if (AntiCheatAll)
                    Notify($"<color=grey>[</color><color=green>ANTI-CHEAT</color><color=grey>]</color> {susNick} was reported for {shown}.");
            }

            if (!AntiACReport)
                return;

            AntiReportFRT(PhotonNetwork.LocalPlayer);
            OnClearNotifications?.Invoke();
            Notify("<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> The anti cheat attempted to report you, you have been disconnected.");
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
        private static class SendReportPatch
        {
            private static bool Prefix(MonkeAgent __instance, string susReason, string susId, string susNick)
            {
                if (!enabled)
                    return true;

                if (!string.IsNullOrEmpty(susReason) && susReason.ToLower() == "empty rig")
                    return false;

                Announce(susReason, susId, susNick);

                if (ClearStateOnBlock)
                    ClearPendingReport(__instance);

                return false;
            }
        }

        [HarmonyPatch(typeof(MonkeAgent), "DispatchReport")]
        private static class NoDispatchReport
        {
            private static bool Prefix(MonkeAgent __instance)
            {
                if (!enabled)
                    return true;

                if (ClearStateOnBlock)
                    ClearPendingReport(__instance);

                return false;
            }
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.CheckReports))]
        private static class NoCheckReports
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), "RefreshRPCs")]
        private static class NoRefreshRPCs
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.CloseInvalidRoom))]
        private static class NoCloseInvalidRoom
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.GetRPCCallTracker))]
        private static class NoGetRPCCallTracker
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.LogErrorCount))]
        private static class NoLogErrorCount
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.QuitDelay), MethodType.Enumerator)]
        private static class NoQuitDelay
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaGameManager), nameof(GorillaGameManager.ForceStopGame_DisconnectAndDestroy))]
        private static class NoQuitOnBan
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.ShouldDisconnectFromRoom))]
        private static class NoShouldDisconnectFromRoom
        {
            private static bool Prefix(ref bool __result)
            {
                if (!enabled)
                    return true;

                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(RoomSystem), "OnApplicationPause")]
        private static class NoRoomSystemPause
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(RoomSystem), "TimerDC")]
        private static class NoRoomSystemTimerDC
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaNetworking.PhotonNetworkController), "OnApplicationPause")]
        private static class NoPhotonPause
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaNetworkPublicTestsJoin), nameof(GorillaNetworkPublicTestsJoin.GracePeriod), MethodType.Enumerator)]
        private static class GracePeriodPatch1
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(GorillaNetworkPublicTestJoin2), nameof(GorillaNetworkPublicTestJoin2.GracePeriod), MethodType.Enumerator)]
        private static class GracePeriodPatch2
        {
            private static bool Prefix() => !enabled;
        }
    }

    internal static class MonkeState
    {
        private static readonly FieldInfo SendReportField = AccessTools.Field(typeof(MonkeAgent), "_sendReport");
        private static readonly FieldInfo SuspiciousPlayerIdField = AccessTools.Field(typeof(MonkeAgent), "_suspiciousPlayerId");
        private static readonly FieldInfo SuspiciousPlayerNameField = AccessTools.Field(typeof(MonkeAgent), "_suspiciousPlayerName");
        private static readonly FieldInfo SuspiciousReasonField = AccessTools.Field(typeof(MonkeAgent), "_suspiciousReason");
        private static readonly FieldInfo TestAssaultField = AccessTools.Field(typeof(MonkeAgent), "testAssault");
        private static readonly FieldInfo ReportedPlayersField = AccessTools.Field(typeof(MonkeAgent), "reportedPlayers");

        public static void Clear(MonkeAgent agent)
        {
            SendReportField?.SetValue(agent, false);
            SuspiciousPlayerIdField?.SetValue(agent, string.Empty);
            SuspiciousPlayerNameField?.SetValue(agent, string.Empty);
            SuspiciousReasonField?.SetValue(agent, string.Empty);
        }

        public static void ClearTestAssault(MonkeAgent agent)
        {
            TestAssaultField?.SetValue(agent, false);
        }

        public static void ForgetReportedPlayers(MonkeAgent agent)
        {
            if (ReportedPlayersField?.GetValue(agent) is System.Collections.IList reported)
                reported.Clear();
        }
    }
}
