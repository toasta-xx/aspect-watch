using GorillaNetworking;
using GorillaNetworking.Store;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(BundleManager), nameof(BundleManager.CheckIfBundlesOwned))]
    public static class PostGetData
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.PostGetData";

        public static bool CosmeticsInitialized;
        public static string CosmeticsAllowed = string.Empty;

        public static event Action<string> OnCosmeticsSnapshot;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(PostGetData)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PostGetData] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            CosmeticsInitialized = false;
            CosmeticsAllowed = string.Empty;
            harmony = null;
        }

        public static bool IsCosmeticAllowed(string cosmeticName) =>
            string.IsNullOrEmpty(CosmeticsAllowed) || CosmeticsAllowed.Contains(cosmeticName);

        private static void Postfix()
        {
            CosmeticsController cosmetics = CosmeticsController.instance;

            if (cosmetics == null)
            {
                Debug.Log("[PostGetData] Bundle check ran before the cosmetics were initialized");
                return;
            }

            CosmeticsInitialized = true;
            CosmeticsAllowed = cosmetics.concatStringCosmeticsAllowed ?? string.Empty;

            try
            {
                OnCosmeticsSnapshot?.Invoke(CosmeticsAllowed);
            }
            catch (Exception exception)
            {
                Debug.LogError("[PostGetData] OnCosmeticsSnapshot subscriber failed: " + exception);
            }
        }
    }
}
