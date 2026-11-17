using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GorillaTagManager), nameof(GorillaTagManager.ReportTag))]
    public static class ReportTagPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.ReportTag";

        public static readonly List<NetPlayer> blacklistedPlayers = new List<NetPlayer>();
        public static readonly List<NetPlayer> invinciblePlayers = new List<NetPlayer>();

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(ReportTagPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[ReportTagPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            blacklistedPlayers.Clear();
            invinciblePlayers.Clear();
            harmony = null;
        }

        private static bool Prefix(NetPlayer taggedPlayer, NetPlayer taggingPlayer)
        {
            if (taggingPlayer != null && blacklistedPlayers.Contains(taggingPlayer))
                return false;

            return taggedPlayer == null || !invinciblePlayers.Contains(taggedPlayer);
        }
    }
}
