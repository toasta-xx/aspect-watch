using GorillaNetworking;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(CosmeticsController), nameof(CosmeticsController.PurchaseItem))]
    public static class PurchasePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Purchase";

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
                harmony.CreateClassProcessor(typeof(PurchasePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PurchasePatch] Failed to patch: " + exception);
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

        private static bool Prefix()
        {
            if (!enabled)
                return true;

            CosmeticsController controller = CosmeticsController.instance;
            if (controller == null || string.IsNullOrEmpty(controller.itemToBuy.itemName))
                return true;

            string itemName = controller.itemToBuy.itemName;
            CosmeticsController.CosmeticItem item = controller.GetItemFromDict(itemName);

            bool isSet = item.itemCategory == CosmeticsController.CosmeticCategory.Set && item.bundledItems != null;

            if (isSet)
            {
                controller.UnlockItem(itemName);
                foreach (string bundledItem in item.bundledItems)
                    controller.UnlockItem(bundledItem);
            }
            else
                controller.UnlockItem(itemName);

            controller.UpdateMyCosmetics();
            controller.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Success;
            controller.UpdateShoppingCart();
            controller.ProcessPurchaseItemState(null, controller.isLastHandTouchedLeft);

            return false;
        }
    }
}
