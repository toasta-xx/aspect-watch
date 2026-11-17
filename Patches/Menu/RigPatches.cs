using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    public static class RigPatches
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Rig";

        public static VRRig GhostRig;
        public static bool ghostException;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            foreach (Type type in typeof(RigPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (type.GetCustomAttribute<HarmonyPatch>() == null)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[RigPatches] Failed to patch " + type.Name + ": " + exception);
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

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.OnDisable))]
        public static class OnDisable
        {
            public static bool Prefix(VRRig __instance) =>
                !__instance.isLocal;
        }

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.Awake))]
        public static class Awake
        {
            public static bool Prefix(VRRig __instance) =>
                !__instance.gameObject.name.StartsWith("Local Gorilla Player(Clone)") && (GhostRig == null || __instance != GhostRig);
        }

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
        public static class PostTick
        {
            public static bool Prefix(VRRig __instance) =>
                !__instance.isLocal || __instance.enabled;
        }
    }
}
