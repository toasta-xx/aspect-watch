using GorillaTagScripts;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class SubscriptionPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Subscription";

        public static bool enabled;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(SubscriptionPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[SubscriptionPatches] Failed to patch " + type.Name + ": " + exception);
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

        [HarmonyPatch(typeof(SubscriptionManager), nameof(SubscriptionManager.IsLocalSubscribed))]
        private static class IsLocalSubscribed
        {
            private static bool Prefix(ref bool __result)
            {
                if (!enabled)
                    return true;

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(SubscriptionManager), nameof(SubscriptionManager.LocalSubscriptionStatus))]
        private static class LocalSubscriptionStatus
        {
            private static bool Prefix(ref SubscriptionManager.SubscriptionStatus __result)
            {
                if (!enabled)
                    return true;

                __result = SubscriptionManager.SubscriptionStatus.Active;
                return false;
            }
        }

        [HarmonyPatch(typeof(SubscriptionManager), nameof(SubscriptionManager.LocalSubscriptionDetails))]
        private static class LocalSubscriptionDetails
        {
            private static bool Prefix(ref SubscriptionManager.SubscriptionDetails __result)
            {
                if (!enabled)
                    return true;

                __result = new SubscriptionManager.SubscriptionDetails
                {
                    active = true,
                    daysAccrued = int.MaxValue,
                    subscriptionFeatureSettings = new[] { true, true },
                    tier = int.MaxValue,
                    subscriptionActiveUntilDate = DateTime.MaxValue,
                    autoRenew = true,
                    autoRenewMonths = int.MaxValue
                };
                return false;
            }
        }
    }
}
