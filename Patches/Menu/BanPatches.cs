/*
 * Name ban bypass and shared ban error decoding. Call BanPatches.Patch() once.
 *
 *   enabled            bypass GorillaServer.CheckForBadName and the local list
 *   CrashGuard         contain ban shaped PlayFab error responses
 *   SuppressAuthError  also stop the authenticator failure path
 *
 * ParseBan(error, out BanInfo) decodes reason, expiry, IP-vs-account and whether
 * the ban is indefinite, so nothing else has to match on error text. Also
 * exposes IsBanError, FormatRemaining, MakeBenignError and ReplaceErrorCallback.
 */


using GorillaNetworking;
using HarmonyLib;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class BanPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Ban";

        public static bool enabled = true;
        public static bool CrashGuard = true;
        public static bool SuppressAuthError;

        public static Action<string> Notify = message => Debug.Log("[BanPatches] " + message);

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public struct BanInfo
        {
            public bool IsBan;
            public bool IsIndefinite;
            public bool IsIpBan;
            public string Reason;
            public DateTime ExpiresUtc;
            public TimeSpan Remaining;
        }

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(BanPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[BanPatches] Failed to patch " + type.Name + ": " + exception);
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

        public static bool IsBanError(PlayFabError error)
        {
            if (error == null)
                return false;

            if (error.Error == PlayFabErrorCode.AccountBanned)
                return true;

            string message = error.ErrorMessage;
            if (string.IsNullOrEmpty(message))
                return false;

            string lower = message.ToLower();
            return lower.Contains("banned") || lower.Contains("ban ") || lower.Contains("suspended") || lower.Contains("suspension");
        }

        public static bool ParseBan(PlayFabError error, out BanInfo info)
        {
            info = default;

            if (!IsBanError(error))
                return false;

            info.IsBan = true;

            string message = error.ErrorMessage ?? string.Empty;
            info.IsIpBan = message.IndexOf("IP", StringComparison.OrdinalIgnoreCase) >= 0 &&
                           message.IndexOf("banned", StringComparison.OrdinalIgnoreCase) >= 0;

            info.ExpiresUtc = DateTime.MaxValue;
            info.IsIndefinite = true;
            info.Reason = "Unknown";

            try
            {
                if (error.ErrorDetails != null)
                {
                    using (Dictionary<string, List<string>>.Enumerator enumerator = error.ErrorDetails.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            KeyValuePair<string, List<string>> entry = enumerator.Current;
                            info.Reason = entry.Key ?? "Unknown";

                            if (entry.Value != null && entry.Value.Count > 0)
                            {
                                string expiry = entry.Value[0];
                                if (!string.IsNullOrEmpty(expiry) && expiry != "Indefinite")
                                {
                                    info.IsIndefinite = false;
                                    info.ExpiresUtc = DateTime.Parse(expiry);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[BanPatches] Failed to parse ban details: " + exception);
            }

            info.Remaining = info.IsIndefinite ? TimeSpan.MaxValue : info.ExpiresUtc - DateTime.UtcNow;
            return true;
        }

        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining == TimeSpan.MaxValue)
                return "Indefinite";

            if (remaining <= TimeSpan.Zero)
                return "Expired";

            var parts = new List<string>();

            int totalDays = remaining.Days;
            int months = totalDays / 30;
            int weeks = totalDays % 30 / 7;
            int days = totalDays % 30 % 7;

            if (months > 0) parts.Add(months + " months");
            if (weeks > 0) parts.Add(weeks + " weeks");
            if (days > 0) parts.Add(days + " days");
            if (remaining.Hours > 0) parts.Add(remaining.Hours + " hours");
            if (remaining.Minutes > 0) parts.Add(remaining.Minutes + " minutes");
            if (remaining.Seconds > 0) parts.Add(remaining.Seconds + " seconds");

            return parts.Count == 0 ? "Expired" : string.Join(" ", parts);
        }

        public static PlayFabError MakeBenignError() =>
            new PlayFabError
            {
                Error = PlayFabErrorCode.UnknownError,
                ErrorMessage = "An unknown error occurred.",
                ErrorDetails = new Dictionary<string, List<string>>()
            };

        public static void ReplaceErrorCallback(ref Action<PlayFabError> callback)
        {
            if (callback == null)
                return;

            Action<PlayFabError> original = callback;
            callback = error =>
            {
                if (CrashGuard && ParseBan(error, out BanInfo info))
                {
                    Notify(info.IsIpBan
                        ? "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> Your IP address is currently banned."
                        : "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> Your account is currently banned.");

                    original.Invoke(MakeBenignError());
                    return;
                }

                original.Invoke(error);
            };
        }

        [HarmonyPatch(typeof(GorillaServer), nameof(GorillaServer.CheckForBadName))]
        private static class AutoBanPlayfabFunction
        {
            private static bool Prefix(CheckForBadNameRequest request, Action<ExecuteFunctionResult> successCallback)
            {
                if (!enabled)
                    return true;

                successCallback?.Invoke(new ExecuteFunctionResult
                {
                    FunctionResult = new PlayFab.Json.JsonObject { { "result", 0 } }
                });

                return false;
            }
        }

        [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.CheckAutoBanListForName))]
        private static class CheckAutoBanListForName
        {
            private static bool Prefix(string nameToCheck, ref bool __result)
            {
                if (!enabled)
                    return true;

                __result = true;
                return false;
            }

            public static bool CheckBanList(string nameToCheck)
            {
                nameToCheck = nameToCheck.ToLower();
                nameToCheck = new string(Array.FindAll(nameToCheck.ToCharArray(), c => char.IsLetterOrDigit(c)));

                GorillaComputer computer = GorillaComputer.instance;
                if (computer == null)
                    return true;

                foreach (string twoWeekName in computer.anywhereTwoWeek)
                    if (nameToCheck.IndexOf(twoWeekName) >= 0)
                        return false;

                foreach (string oneWeekName in computer.anywhereOneWeek)
                    if (nameToCheck.IndexOf(oneWeekName) >= 0 && !nameToCheck.Contains("fagol"))
                        return false;

                string[] exactNames = computer.exactOneWeek;
                for (int i = 0; i < exactNames.Length; i++)
                    if (exactNames[i] == nameToCheck)
                        return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayFabUnityHttp), nameof(PlayFabUnityHttp.MakeApiCall))]
        private static class AntiBanCrash1
        {
            private static bool Prefix(object reqContainerObj)
            {
                if (!CrashGuard || reqContainerObj == null)
                    return true;

                if (reqContainerObj is CallRequestContainer container && container.ErrorCallback != null)
                {
                    Action<PlayFabError> callback = container.ErrorCallback;
                    ReplaceErrorCallback(ref callback);
                    container.ErrorCallback = callback;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayFabWebRequest), nameof(PlayFabWebRequest.MakeApiCall))]
        private static class AntiBanCrash2
        {
            private static bool Prefix(object reqContainerObj)
            {
                if (!CrashGuard || reqContainerObj == null)
                    return true;

                if (reqContainerObj is CallRequestContainer container && container.ErrorCallback != null)
                {
                    Action<PlayFabError> callback = container.ErrorCallback;
                    ReplaceErrorCallback(ref callback);
                    container.ErrorCallback = callback;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayFabAuthenticator), "OnPlayFabError")]
        private static class AntiBanAuthError
        {
            private static bool Prefix(PlayFabError obj)
            {
                if (!CrashGuard)
                    return true;

                if (!ParseBan(obj, out BanInfo info))
                    return true;

                Notify(info.IsIpBan
                    ? "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> IP ban response seen: " + info.Reason
                    : "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> Account ban response seen: " + info.Reason);

                return !SuppressAuthError;
            }
        }
    }
}
