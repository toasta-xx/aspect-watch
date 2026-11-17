using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(MonkeAgent))]
	[HarmonyPatch("DispatchReport", 0)]
	public class DispatchReportPatch
	{
		private static bool Prefix(MonkeAgent __instance)
		{
			SendReportPatch.Clear(__instance);
			return false;
		}
	}
}
