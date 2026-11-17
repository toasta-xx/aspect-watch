using HarmonyLib;
using Photon.Voice;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(Speaker), nameof(Speaker.OnAudioFrame))]
    public static class SpeakerPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Speaker";

        public static bool enabled;
        public static Speaker targetSpeaker;
        public static FrameOut<float> frameOut;
        public static readonly List<float> SampleQueue = new List<float>();
        public static readonly object locked = new object();

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(SpeakerPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SpeakerPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            targetSpeaker = null;
            frameOut = null;

            lock (locked)
                SampleQueue.Clear();

            harmony = null;
        }

        public static float[] DrainSamples()
        {
            lock (locked)
            {
                if (SampleQueue.Count == 0)
                    return Array.Empty<float>();

                float[] samples = SampleQueue.ToArray();
                SampleQueue.Clear();
                return samples;
            }
        }

        public static float[] PeekSamples()
        {
            lock (locked)
                return SampleQueue.ToArray();
        }

        public static void ClearSamples()
        {
            lock (locked)
                SampleQueue.Clear();
        }

        private static void Postfix(Speaker __instance, FrameOut<float> frame)
        {
            if (!enabled || targetSpeaker == null || __instance != targetSpeaker)
                return;

            float[] source = frame?.Buf;
            if (source == null || source.Length == 0)
                return;

            frameOut = frame;
            lock (locked)
            {
                SampleQueue.AddRange(source);

                int maxSamples = 48000;
                try
                {
                    if (targetSpeaker.RemoteVoiceLink?.Info != null && targetSpeaker.RemoteVoiceLink.Info.SamplingRate > 0)
                        maxSamples = targetSpeaker.RemoteVoiceLink.Info.SamplingRate;
                }
                catch { }

                if (SampleQueue.Count > maxSamples)
                    SampleQueue.RemoveRange(0, SampleQueue.Count - maxSamples);
            }
        }
    }
}
