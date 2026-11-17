using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.CreatePrimitive))]
    public static class ShaderFix
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.ShaderFix";

        public enum Mode
        {
            Unlit,
            Lit,
            Transparent,
            Crystal
        }

        public static bool enabled;
        public static Mode CurrentMode = Mode.Unlit;
        public static Color TintColor = Color.white;
        public static Material CrystalMaterial;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(ShaderFix)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[ShaderFix] Failed to patch: " + exception);
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

        private static void Postfix(GameObject __result)
        {
            if (!enabled || __result == null)
                return;

            Renderer renderer = __result.GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (CurrentMode == Mode.Crystal && CrystalMaterial != null)
            {
                renderer.sharedMaterial = CrystalMaterial;
                return;
            }

            Material material = renderer.material;

            if (CurrentMode == Mode.Transparent)
            {
                material.shader = Shader.Find("Universal Render Pipeline/Unlit");

                material.SetFloat("_Surface", 1);
                material.SetFloat("_Blend", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (CurrentMode == Mode.Lit)
                material.shader = Shader.Find("Universal Render Pipeline/Lit");
            else
                material.shader = Shader.Find("Universal Render Pipeline/Unlit");

            material.color = TintColor;
        }

        public static void SetMode(Mode mode)
        {
            CurrentMode = mode;
            enabled = true;
        }

        public static void SetTint(Color color)
        {
            TintColor = color;
            enabled = true;
        }
    }
}
