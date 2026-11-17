using GorillaLocomotion;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(GorillaTagger), "LateUpdate")]
    public static class NiceMirrorPatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.NiceMirror";

        private const string MirrorBackdropPath = "City_Pretty/CosmeticsRoomAnchor/nicegorillastore_prefab/DressingRoom_Mirrors_Prefab/Mirror Backdrop";
        private const string MirrorCameraPath = "City_Pretty/CosmeticsRoomAnchor/nicegorillastore_prefab/DressingRoom_Mirrors_Prefab/CameraC";
        private const int TargetResolution = 2048;

        public static bool enabled;

        private static bool isEnhanced;
        private static int originalWidth = 512;
        private static int originalHeight = 512;
        private static int originalCullingMask;
        private static float originalFarClip;
        private static float nextCheckTime;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(NiceMirrorPatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[NiceMirrorPatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            enabled = false;
            DowngradeMirror();
            harmony = null;
        }

        public static void Enable()
        {
            enabled = true;
            nextCheckTime = 0f;
        }

        public static void Disable()
        {
            enabled = false;
            DowngradeMirror();
        }

        private static void Postfix()
        {
            if (enabled && !isEnhanced)
            {
                if (Time.time > nextCheckTime)
                {
                    nextCheckTime = Time.time + 2f;
                    EnhanceMirror();
                }
            }
            else if (!enabled && isEnhanced)
                DowngradeMirror();
        }

        private static void EnhanceMirror()
        {
            GameObject cameraObj = GameObject.Find(MirrorCameraPath);
            if (cameraObj == null)
                return;

            Camera camera = cameraObj.GetComponent<Camera>();
            if (camera == null)
                return;

            GameObject backdrop = GameObject.Find(MirrorBackdropPath);
            if (backdrop != null)
            {
                Renderer backdropRenderer = backdrop.GetComponent<Renderer>();
                if (backdropRenderer != null)
                    backdropRenderer.enabled = false;
            }

            originalFarClip = camera.farClipPlane;
            originalCullingMask = camera.cullingMask;

            camera.farClipPlane = 40f;
            camera.cullingMask = -1;
            camera.pixelRect = new Rect(0, 0, TargetResolution, TargetResolution);

            RenderTexture renderTexture = camera.targetTexture;
            if (renderTexture == null)
                return;

            originalWidth = renderTexture.width;
            originalHeight = renderTexture.height;

            bool wasActiveTexture = RenderTexture.active == renderTexture;
            bool wasCameraTexture = camera.targetTexture == renderTexture;

            renderTexture.Release();
            renderTexture.width = TargetResolution;
            renderTexture.height = TargetResolution;
            renderTexture.Create();

            if (wasCameraTexture)
                camera.targetTexture = renderTexture;

            if (wasActiveTexture)
                RenderTexture.active = renderTexture;

            isEnhanced = true;
        }

        private static void DowngradeMirror()
        {
            if (!isEnhanced)
                return;

            GameObject cameraObj = GameObject.Find(MirrorCameraPath);
            if (cameraObj != null)
            {
                Camera camera = cameraObj.GetComponent<Camera>();
                if (camera != null)
                {
                    if (originalFarClip > 0f)
                        camera.farClipPlane = originalFarClip;

                    if (originalCullingMask != 0)
                        camera.cullingMask = originalCullingMask;

                    RenderTexture renderTexture = camera.targetTexture;
                    if (renderTexture != null)
                    {
                        bool wasActiveTexture = RenderTexture.active == renderTexture;
                        bool wasCameraTexture = camera.targetTexture == renderTexture;

                        renderTexture.Release();
                        renderTexture.width = originalWidth > 0 ? originalWidth : 512;
                        renderTexture.height = originalHeight > 0 ? originalHeight : 512;
                        renderTexture.Create();

                        if (wasCameraTexture)
                            camera.targetTexture = renderTexture;

                        if (wasActiveTexture)
                            RenderTexture.active = renderTexture;
                    }
                }
            }

            GameObject backdrop = GameObject.Find(MirrorBackdropPath);
            if (backdrop != null)
            {
                Renderer backdropRenderer = backdrop.GetComponent<Renderer>();
                if (backdropRenderer != null)
                    backdropRenderer.enabled = true;
            }

            isEnhanced = false;
        }
    }
}
