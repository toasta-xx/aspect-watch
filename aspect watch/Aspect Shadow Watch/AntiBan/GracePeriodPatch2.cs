using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(GorillaNetworkPublicTestsJoin))]
	[HarmonyPatch("PostTick", 0)]
	public class GracePeriodPatch2
	{
		private static bool Prefix()
		{
			return false;
		}
	}
}
