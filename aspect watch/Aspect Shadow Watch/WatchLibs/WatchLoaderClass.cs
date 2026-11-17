using System.Reflection;
using AspectWatch.Patches;
using AspectWatch.Patches.Menu;
using AspectWatch.Patches.Safety;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace WatchLibs
{
	[BepInPlugin("aspect.ghost.watch", "Aspect Shadow Watch", "1.0.0")]
	public class WatchLoaderClass : BaseUnityPlugin
	{
		public void Awake()
		{
			new Harmony("aspect.ghost.watch").PatchAll(Assembly.GetExecutingAssembly());
			StartSafetyStack();
		}

		private static void StartSafetyStack()
		{
			PatchHandler.HarmonyId = "aspect.ghost.watch.patches";
			PatchHandler.Log = Debug.Log;
			PatchHandler.LogError = Debug.LogError;

			AntiCheatPatches.Notify = Debug.Log;
			IncrementRPCPatches.Notify = Debug.Log;
			BanContainmentPatches.Notify = Debug.Log;
			BanPatches.Notify = Debug.Log;
			PropertiesPatches.enabled = true;
			DistancePatch.enabled = true;

			AntiCheatPatches.Patch();
			IncrementRPCPatches.Patch();
			TelemetryPatches.Patch();
			PlayFabTelemetryPatches.Patch();
			MothershipPatches.enabled = true;
			MothershipPatches.Patch();
			BanContainmentPatches.Patch();
			BanPatches.Patch();
			CrashPatches.Patch();
			PropertiesPatches.Patch();
			DistancePatch.Patch();
			ReportTagPatch.Patch();
			ReleasePatch.Patch();

			RigPatches.Patch();

			SerializePatch.Patch();
			SerializeWritePatch.Patch();
			PlayerSerializePatch.Patch();

			PostGetData.Patch();
			NetworkTriggerPatch.Patch();
		}

		private const string modGUID = "aspect.ghost.watch";

		private const string modName = "Aspect Shadow Watch";

		private const string modVersion = "1.0.0";
	}
}
