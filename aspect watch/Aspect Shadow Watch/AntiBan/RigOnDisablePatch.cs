using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(VRRig))]
	[HarmonyPatch("OnDisable", 0)]
	public class RigOnDisablePatch
	{
		private static bool Prefix(VRRig __instance)
		{
			if (__instance == null)
			{
				return true;
			}
			return !__instance.isLocal && !__instance.isOfflineVRRig;
		}
	}
}
