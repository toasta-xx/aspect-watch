/*
 * Teardown containment after a ban verdict. Call Patch() once.
 *
 *   ContainmentEnabled     master switch
 *   BlockAnalytics         stop Gorillanalytics uploads
 *   BlockBacktrace         stop crash reports
 *   BlockBanScreens        suppress ban shaped failure and auth screens
 *   BlockVRConstraintLatch stop the VR constraint latching
 *
 * IsBanShaped(message), IsBanError(error) and DescribeBan(error) are public so
 * other code can test and format a ban the same way this file does.
 *
 * This is containment, not prevention: it runs after the game has decided you
 * are banned and stops the local teardown that follows.
 */


using Backtrace.Unity;
using Backtrace.Unity.Model;
using GorillaNetworking;
using HarmonyLib;
using PlayFab;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Safety
{
    public static class BanContainmentPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.BanContainment";

        public static bool ContainmentEnabled = true;
        public static bool BlockAnalytics = true;
        public static bool BlockBacktrace = true;
        public static bool BlockBanScreens = true;
        public static bool BlockVRConstraintLatch = true;

        public static Action<string> Notify = message => Debug.Log("[BanContainment] " + message);

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(BanContainmentPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[BanContainment] Failed to patch " + type.Name + ": " + exception);
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

        public static bool IsBanShaped(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            string lower = message.ToLower();
            return lower.Contains("banned") ||
                   lower.Contains("ban expires") ||
                   lower.Contains("unban date") ||
                   lower.Contains("unable to authenticate with mothership");
        }

        public static bool IsBanError(PlayFabError error)
        {
            if (error == null)
                return false;

            if (error.Error == PlayFabErrorCode.AccountBanned)
                return true;

            return IsBanShaped(error.ErrorMessage);
        }

        public static string DescribeBan(PlayFabError error)
        {
            if (error == null)
                return "unknown";

            try
            {
                if (error.ErrorDetails != null)
                {
                    using (Dictionary<string, List<string>>.Enumerator enumerator = error.ErrorDetails.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            KeyValuePair<string, List<string>> entry = enumerator.Current;
                            string expiry = entry.Value != null && entry.Value.Count > 0 ? entry.Value[0] : "Indefinite";
                            return entry.Key + " (" + expiry + ")";
                        }
                    }
                }
            }
            catch { }

            return error.ErrorMessage ?? "unknown";
        }

        [HarmonyPatch(typeof(Gorillanalytics), nameof(Gorillanalytics.UploadGorillanalytics))]
        private static class NoGorillanalyticsUpload
        {
            private static bool Prefix() => !(BlockAnalytics && ContainmentEnabled);
        }

        [HarmonyPatch(typeof(GorillaServer), nameof(GorillaServer.UploadGorillanalytics))]
        private static class NoGorillanalyticsCloudScript
        {
            private static bool Prefix() => !(BlockAnalytics && ContainmentEnabled);
        }

        [HarmonyPatch(typeof(BacktraceManager), nameof(BacktraceManager.Awake))]
        private static class NoBacktraceReports
        {
            private static void Postfix(BacktraceManager __instance)
            {
                if (!BlockBacktrace || !ContainmentEnabled || __instance == null)
                    return;

                try
                {
                    __instance.backtraceSampleRate = 0.0;
                    BacktraceClient client = __instance.GetComponent<BacktraceClient>();
                    if (client != null)
                        client.BeforeSend = (BacktraceData data) => null;
                }
                catch (Exception exception)
                {
                    Debug.LogError("[BanContainment] Failed to disable Backtrace: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(CosmeticsController), nameof(CosmeticsController.ReauthOrBan))]
        private static class NoReauthOrBanNuke
        {
            private static bool Prefix(PlayFabError error)
            {
                if (!(ContainmentEnabled && IsBanError(error)))
                    return true;

                Notify("<color=grey>[</color><color=orange>CONTAINMENT</color><color=grey>]</color> Blocked cosmetics ban teardown: " + DescribeBan(error));
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayFabAuthenticator), nameof(PlayFabAuthenticator.ShowMothershipAuthErrorMessage))]
        private static class NoMothershipAuthErrorScreen
        {
            private static bool Prefix(string errorMessage, string errorCode)
            {
                if (!(ContainmentEnabled && BlockBanScreens) || !IsBanShaped(errorMessage))
                    return true;

                Notify("<color=grey>[</color><color=orange>CONTAINMENT</color><color=grey>]</color> Ban auth screen suppressed (" + errorCode + ").");
                return false;
            }
        }

        [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.GeneralFailureMessage))]
        private static class NoBanFailureScreen
        {
            private static bool Prefix(string failMessage)
            {
                if (!(ContainmentEnabled && BlockBanScreens) || !IsBanShaped(failMessage))
                    return true;

                Notify("<color=grey>[</color><color=orange>CONTAINMENT</color><color=grey>]</color> Ban screen suppressed - remaining in the session.");
                return false;
            }
        }

        [HarmonyPatch(typeof(GorillaVRConstraint), nameof(GorillaVRConstraint.Tick))]
        private static class NoVRConstraintLatch
        {
            private static bool Prefix() => !(BlockVRConstraintLatch && ContainmentEnabled);
        }
    }
}
