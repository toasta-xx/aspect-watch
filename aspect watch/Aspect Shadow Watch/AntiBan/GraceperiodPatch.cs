using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(GorillaNetworkPublicTestJoin2))]
	[HarmonyPatch("LateUpdate", 0)]
	public class GraceperiodPatch
	{
		private static bool Prefix()
		{
			return false;
		}
	}
}
