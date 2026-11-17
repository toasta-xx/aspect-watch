/*
 * Optional central entry point. Every patch file has its own Patch() and
 * Unpatch(), so this is only for applying all of them at once.
 *
 *   PatchAll()                 every [HarmonyPatch] type
 *   PatchAll(awake: true)      only [PatchOnAwake] types
 *   UnpatchAll()
 *   ApplyPatch(type, method, prefix, postfix)
 *   RemovePatch(type, method)
 *
 * Set HarmonyId if you need a distinct Harmony instance. Route output with Log
 * and LogError. A failing [SecurityPatch] sets CriticalPatchFailed.
 */


using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AspectWatch.Patches
{
    public static class PatchHandler
    {
        public static string HarmonyId = "AspectWatch.Patches";

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class SecurityPatch : Attribute { }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class PatchOnAwake : Attribute { }

        public static bool IsPatched { get; private set; }
        public static int PatchErrors { get; private set; }
        public static bool CriticalPatchFailed { get; private set; }

        public static Action<object> Log = message => Debug.Log(message);
        public static Action<object> LogError = message => Debug.LogError(message);

        private static Harmony instance;

        public static void PatchAll(bool awake = false)
        {
            PatchAll(Assembly.GetExecutingAssembly(), awake);
        }

        public static void PatchAll(Assembly assembly, bool awake = false)
        {
            if (assembly == null)
                return;

            if (IsPatched)
                return;

            instance ??= new Harmony(HarmonyId);
            PatchErrors = 0;
            CriticalPatchFailed = false;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types?.Where(type => type != null).ToArray() ?? Array.Empty<Type>();
                LogError?.Invoke($"Some types failed to load: {exception}");
            }

            foreach (var type in types.Where(type => type.IsClass
                                                     && type.GetCustomAttribute<HarmonyPatch>() != null
                                                     && (type.GetCustomAttribute<PatchOnAwake>() != null) == awake))
            {
                try
                {
                    instance.CreateClassProcessor(type).Patch();
                }
                catch (Exception exception)
                {
                    PatchErrors++;

                    if (type.GetCustomAttribute<SecurityPatch>() != null)
                        CriticalPatchFailed = true;

                    LogError?.Invoke($"Failed to patch {type.FullName}: {exception}");
                }
            }

            Log?.Invoke($"Patched with {PatchErrors} errors");

            if (!awake)
                IsPatched = true;
        }

        public static void UnpatchAll()
        {
            if (instance == null)
                return;

            instance.UnpatchSelf();
            IsPatched = false;
            instance = null;
        }

        public static void ApplyPatch(Type targetClass, string methodName, MethodInfo prefix = null, MethodInfo postfix = null, Type[] parameterTypes = null)
        {
            if (targetClass == null)
                throw new ArgumentNullException(nameof(targetClass));

            instance ??= new Harmony(HarmonyId);

            MethodInfo original = FindMethod(targetClass, methodName, parameterTypes)
                ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");

            instance.Patch(original,
                prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
        }

        public static void RemovePatch(Type targetClass, string methodName, Type[] parameterTypes = null)
        {
            if (instance == null || targetClass == null)
                return;

            MethodInfo original = FindMethod(targetClass, methodName, parameterTypes)
                ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");

            instance.Unpatch(original, HarmonyPatchType.All, instance.Id);
        }

        private static MethodInfo FindMethod(Type targetClass, string methodName, Type[] parameterTypes)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            return parameterTypes == null
                ? targetClass.GetMethod(methodName, flags)
                : targetClass.GetMethod(methodName, flags, null, parameterTypes, null);
        }
    }
}
