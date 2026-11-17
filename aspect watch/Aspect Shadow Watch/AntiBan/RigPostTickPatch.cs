using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(VRRig))]
	[HarmonyPatch("PostTick", 0)]
	public class RigPostTickPatch
	{
		private static bool Prefix(VRRig __instance)
		{
			if (__instance == null)
			{
				return true;
			}
			if (!__instance.isLocal && !__instance.isOfflineVRRig)
			{
				return true;
			}
			return __instance.enabled;
		}
	}
}
