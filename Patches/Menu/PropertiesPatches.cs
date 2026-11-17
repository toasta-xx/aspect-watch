using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Realtime;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class PropertiesPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Properties";

        public static bool enabled;

        public static string[] allowedKeys = { "didTutorial" };

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(PropertiesPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[PropertiesPatches] Failed to patch " + type.Name + ": " + exception);
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

        public static bool IsAllowed(Hashtable properties)
        {
            if (!enabled || properties == null)
                return true;

            foreach (object key in properties.Keys)
            {
                string name = key?.ToString();
                if (name == null || !allowedKeys.Contains(name))
                    return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetCustomProperties))]
        private static class SetCustomPropertiesMethod
        {
            private static bool Prefix(Player __instance, Hashtable propertiesToSet) =>
                __instance == null || !__instance.IsLocal || IsAllowed(propertiesToSet);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.CustomProperties), MethodType.Setter)]
        private static class SetCustomPropertiesField
        {
            private static bool Prefix(Player __instance, Hashtable value) =>
                __instance == null || !__instance.IsLocal || IsAllowed(value);
        }
    }
}
