using HarmonyLib;
using Photon.Pun;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(NetworkSystemPUN), nameof(NetworkSystemPUN.InternalDisconnect))]
    public static class SinglePlayerPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.SinglePlayer";

        public static bool enabled;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(SinglePlayerPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SinglePlayerPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            harmony = null;
        }

        private static bool Prefix(NetworkSystemPUN __instance, ref Task __result)
        {
            if (!enabled || __instance == null)
                return true;

            try
            {
                __instance.internalState = NetworkSystemPUN.InternalState.Internal_Disconnecting;
                PhotonNetwork.Disconnect();

                if (__instance.VoiceNetworkObject != null)
                    UnityEngine.Object.Destroy(__instance.VoiceNetworkObject);

                __instance.UpdatePlayers();
                __instance.SinglePlayerStarted();

                __result = FinishDisconnect(__instance);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SinglePlayerPatch] Fast disconnect failed, falling back: " + exception);
                return true;
            }

            return false;
        }

        private static async Task FinishDisconnect(NetworkSystemPUN instance)
        {
            await instance.WaitForStateCheck(NetworkSystemPUN.InternalState.Internal_Disconnected);
            instance.internalState = NetworkSystemPUN.InternalState.Idle;
        }
    }
}
