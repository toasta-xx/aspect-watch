using HarmonyLib;
using UnityEngine;

namespace AntiBan
{
	[HarmonyPatch(typeof(VRRig))]
	[HarmonyPatch("InitializeNoobMaterial", 0)]
	public class NoobMaterialPatch
	{
		private static bool Prefix(VRRig __instance, float red, float green, float blue, PhotonMessageInfoWrapped info)
		{
			if (__instance == null || __instance.rigSerializer == null)
			{
				return true;
			}
			if (info.senderID != NetworkSystem.Instance.GetOwningPlayerID(__instance.rigSerializer.gameObject))
			{
				return true;
			}
			__instance.initialized = true;
			__instance.InitializeNoobMaterialLocal(Mathf.Clamp(red, 0f, 1f), Mathf.Clamp(green, 0f, 1f), Mathf.Clamp(blue, 0f, 1f));
			return false;
		}
	}
}
