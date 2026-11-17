using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(FriendCard), nameof(FriendCard.Populate), new[] { typeof(FriendBackendController.Friend), typeof(bool) })]
    public static class PopulatePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Populate";

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
                harmony.CreateClassProcessor(typeof(PopulatePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PopulatePatch] Failed to patch: " + exception);
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

        private static void Postfix(FriendCard __instance, FriendBackendController.Friend friend)
        {
            if (!enabled || __instance == null || friend == null || friend.Presence == null)
                return;

            string roomId = friend.Presence.RoomId;
            if (string.IsNullOrEmpty(roomId))
                return;

            bool isCustom = roomId[0] == '@';
            string zone = friend.Presence.Zone ?? string.Empty;

            __instance.SetRoom((isCustom ? roomId.Substring(1) : roomId).ToUpper());
            __instance.SetZone((isCustom ? "CUSTOM" : zone).ToUpper());
            __instance.joinable = true;

            __instance.UpdateComponentStates();
        }
    }
}
