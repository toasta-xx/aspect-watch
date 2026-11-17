using HarmonyLib;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AspectWatch.Patches.Safety
{
    public static class PlayFabTelemetryPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.PlayFabTelemetry";

        public static bool enabled = true;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(PlayFabTelemetryPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[PlayFabTelemetryPatches] Failed to patch " + type.Name + ": " + exception);
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

        public static string RandomString(int length = 4)
        {
            string random = "";
            for (int i = 0; i < length; i++)
            {
                int rand = Random.Range(0, 36);
                char value = rand < 26
                    ? (char)('A' + rand)
                    : (char)('0' + (rand - 26));
                random += value;
            }

            return random;
        }

        [HarmonyPatch(typeof(PlayFabDeviceUtil), nameof(PlayFabDeviceUtil.SendDeviceInfoToPlayFab))]
        private static class PlayfabUtil01
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabClientInstanceAPI), nameof(PlayFabClientInstanceAPI.ReportDeviceInfo))]
        private static class PlayfabUtil02
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), nameof(PlayFabClientAPI.ReportDeviceInfo))]
        private static class PlayfabUtil03
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabDeviceUtil), nameof(PlayFabDeviceUtil.GetAdvertIdFromUnity))]
        private static class PlayfabUtil04
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), nameof(PlayFabClientAPI.AttributeInstall))]
        private static class PlayfabUtil05
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabHttp), nameof(PlayFabHttp.InitializeScreenTimeTracker))]
        private static class PlayfabUtil06
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabClientAPI), nameof(PlayFabClientAPI.UpdateUserTitleDisplayName))]
        private static class DisplayNamePatch
        {
            private static void Prefix(ref UpdateUserTitleDisplayNameRequest request, Action<UpdateUserTitleDisplayNameResult> resultCallback, Action<PlayFabError> errorCallback, object customData = null, Dictionary<string, string> extraHeaders = null)
            {
                if (enabled)
                    request.DisplayName = RandomString(Random.Range(3, 12));
            }
        }
    }
}
