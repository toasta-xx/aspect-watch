/*
 * RPC flood limit bypass. Call IncrementRPCPatches.Patch() once.
 *
 * Blocks VRRig.IncrementRPC, both MonkeAgent.IncrementRPCCall overloads,
 * IncrementRPCCallLocal and every three parameter IncrementRPCTracker, so the
 * 50-per-second per-function limit never trips on your client.
 *
 *   enabled     toggle without unpatching
 *   RpcCounts   live per-function call counts
 *   Reset()     clear them
 */


using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Safety
{
    public static class IncrementRPCPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Safety.IncrementRPC";

        public static bool enabled = true;

        public static Action<string> Notify = message => Debug.Log("[IncrementRPC] " + message);

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(IncrementRPCPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[IncrementRPCPatches] Failed to patch " + type.Name + ": " + exception);
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

        public static void Reset() => RpcCounts.Clear();

        public static readonly Dictionary<string, int> RpcCounts = new Dictionary<string, int>();

        internal static void Note(string function)
        {
            if (string.IsNullOrEmpty(function))
                return;

            RpcCounts.TryGetValue(function, out int count);
            RpcCounts[function] = count + 1;
        }

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.IncrementRPC), typeof(PhotonMessageInfoWrapped), typeof(string))]
        private static class NoIncrementRPC
        {
            private static bool Prefix(string sourceCall)
            {
                if (!enabled)
                    return true;

                Note(sourceCall);
                return false;
            }
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.IncrementRPCCall), typeof(PhotonMessageInfo), typeof(string))]
        private static class NoIncrementRPCCall
        {
            private static bool Prefix(string callingMethod)
            {
                if (!enabled)
                    return true;

                Note(callingMethod);
                return false;
            }
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.IncrementRPCCall), typeof(PhotonMessageInfoWrapped), typeof(string))]
        private static class NoIncrementRPCCallWrapped
        {
            private static bool Prefix(string callingMethod)
            {
                if (!enabled)
                    return true;

                Note(callingMethod);
                return false;
            }
        }

        [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.IncrementRPCCallLocal))]
        private static class NoIncrementRPCCallLocal
        {
            private static bool Prefix(string rpcFunction)
            {
                if (!enabled)
                    return true;

                Note(rpcFunction);
                return false;
            }
        }

        [HarmonyPatch]
        private static class NoIncrementRPCTracker
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(MonkeAgent)
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(method => method.Name == nameof(MonkeAgent.IncrementRPCTracker) && method.GetParameters().Length == 3);
            }

            private static bool Prefix(ref bool __result)
            {
                if (!enabled)
                    return true;

                __result = true;
                return false;
            }
        }
    }
}
