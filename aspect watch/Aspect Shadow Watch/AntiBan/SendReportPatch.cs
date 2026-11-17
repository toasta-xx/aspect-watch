using HarmonyLib;

namespace AntiBan
{
	[HarmonyPatch(typeof(MonkeAgent))]
	[HarmonyPatch("SendReport", 0)]
	public class SendReportPatch
	{
		private static bool Prefix(MonkeAgent __instance)
		{
			SendReportPatch.Clear(__instance);
			return false;
		}

		internal static void Clear(MonkeAgent agent)
		{
			if (agent == null)
			{
				return;
			}
			agent._sendReport = false;
			agent._suspiciousPlayerId = "";
			agent._suspiciousPlayerName = "";
			agent._suspiciousReason = "";
			agent.testAssault = false;
		}
	}
}
