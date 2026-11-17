/*
 * Telemetry suppression. Call TelemetryPatches.Patch() once.
 *
 * Blocks GorillaTelemetry.EnqueueTelemetryEvent and FlushMothershipTelemetry,
 * LckTelemetryClient.SendTelemetry and PlayFabEventsAPI.WriteTelemetryEvents.
 * Set enabled to false to restore them without unpatching.
 */


using HarmonyLib;
using Liv.Lck.Telemetry;
using PlayFab;
using PlayFab.EventsModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Safety
{
    public static class TelemetryPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.Telemetry";

        public static bool enabled = true;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(TelemetryPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[TelemetryPatches] Failed to patch " + type.Name + ": " + exception);
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

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.EnqueueTelemetryEvent))]
        private static class EnqueueTelemetryEvent
        {
            private static bool Prefix(string eventName, object content, string[] customTags = null) => !enabled;
        }

        [HarmonyPatch(typeof(GorillaTelemetry), nameof(GorillaTelemetry.FlushMothershipTelemetry))]
        private static class FlushMothershipTelemetry
        {
            private static bool Prefix() => !enabled;
        }

        [HarmonyPatch(typeof(LckTelemetryClient), nameof(LckTelemetryClient.SendTelemetry))]
        private static class SendTelemetry
        {
            private static bool Prefix(LckTelemetryEvent lckTelemetryEvent) => !enabled;
        }

        [HarmonyPatch(typeof(PlayFabEventsAPI), nameof(PlayFabEventsAPI.WriteTelemetryEvents))]
        private static class WriteTelemetryEvents
        {
            private static bool Prefix(WriteEventsRequest request, Action<WriteEventsResponse> resultCallback, Action<PlayFabError> errorCallback, object customData = null, Dictionary<string, string> extraHeaders = null) => !enabled;
        }
    }
}
