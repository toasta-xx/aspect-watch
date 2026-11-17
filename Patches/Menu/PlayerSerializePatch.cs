/*
 * Incoming rig serialization control. Call PlayerSerializePatch.Patch() once.
 *
 *   stopSerialization = true        drop every incoming update
 *   SetDelay(0.25f)                 defer each update, ClearDelay() to stop
 *   OnPlayerSerialize += rig => ...  fires after an update is processed
 *   IgnoreRig(rig) / ClearIgnoredRigs()
 *
 * The delay path starts a coroutine per incoming packet, so a full room builds
 * them up fast. Use short delays and clear them.
 */


using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(VRRig), "SerializeReadShared")]
    public static class PlayerSerializePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.PlayerSerialize";

        public static bool stopSerialization;
        public static float? delay;

        public static event Action<VRRig> OnPlayerSerialize;

        public static readonly HashSet<VRRig> IgnoredRigs = new HashSet<VRRig>();

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(PlayerSerializePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PlayerSerializePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            stopSerialization = false;
            delay = null;
            IgnoredRigs.Clear();
            harmony = null;
        }

        public static void SetDelay(float? seconds) => delay = seconds;

        public static void ClearDelay() => delay = null;

        public static void IgnoreRig(VRRig rig)
        {
            if (rig != null)
                IgnoredRigs.Add(rig);
        }

        public static void ClearIgnoredRigs() => IgnoredRigs.Clear();

        public static void ResetState()
        {
            stopSerialization = false;
            delay = null;
            IgnoredRigs.Clear();
        }

        private static bool Prefix(VRRig __instance, InputStruct data)
        {
            if (__instance == null)
                return false;

            if (IgnoredRigs.Contains(__instance))
                return false;

            if (stopSerialization)
                return false;

            if (delay == null)
                return true;

            float currentDelay = delay.Value;
            float appliedDelay = currentDelay;

            CoroutineHost.Instance.StartCoroutine(SerializationDelay(() =>
            {
                float previousDelay = delay ?? appliedDelay;
                delay = null;
                try
                {
                    __instance.SerializeReadShared(data);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[PlayerSerializePatch] Delayed read failed: " + exception);
                }
                delay = previousDelay;
            }, currentDelay));

            return false;
        }

        private static void Postfix(VRRig __instance)
        {
            if (__instance == null)
                return;

            try
            {
                OnPlayerSerialize?.Invoke(__instance);
            }
            catch (Exception exception)
            {
                Debug.LogError("[PlayerSerializePatch] OnPlayerSerialize subscriber failed: " + exception);
            }
        }

        public static IEnumerator SerializationDelay(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
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
                        GameObject host = new GameObject("AspectWatch.PlayerSerializePatch");
                        DontDestroyOnLoad(host);
                        instance = host.AddComponent<CoroutineHost>();
                    }

                    return instance;
                }
            }
        }
    }
}
