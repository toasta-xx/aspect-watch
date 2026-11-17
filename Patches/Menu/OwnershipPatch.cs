using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(RequestableOwnershipGuard), nameof(RequestableOwnershipGuard.OwnershipRequested))]
    public static class OwnershipPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Ownership";

        public static bool enabled;
        public static readonly List<RequestableOwnershipGuard> blacklistedGuards = new List<RequestableOwnershipGuard>();

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(OwnershipPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[OwnershipPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            blacklistedGuards.Clear();
            harmony = null;
        }

        private static bool Prefix(RequestableOwnershipGuard __instance) =>
            !enabled || (__instance.photonView != null && __instance.photonView.IsMine && !blacklistedGuards.Contains(__instance));
    }
}
