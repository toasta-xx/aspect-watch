using System;
using AspectWatch.Patches.Menu;
using GorillaGameModes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AspectWatch.Spoof
{
	public static class TagSpoofer
	{
		public static bool Enabled = true;
		public static bool RequireInfected = true;
		public static bool SendToMasterOnly = true;
		public static float DropBelowTarget = 3f;
		public static Action<string> Notify = Debug.Log;

		public static bool InRoom =>
			PhotonNetwork.InRoom && GorillaGameManager.instance != null && PhotonNetwork.LocalPlayer != null;

		public static bool IsMaster =>
			PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.IsMasterClient;

		public static bool CanSpoof =>
			InRoom && SerializePatch.IsPatched && SerializeWritePatch.IsPatched && GetLocalRigView() != null;

		public static PhotonView GetLocalRigView()
		{
			try
			{
				return GorillaTagger.Instance != null && GorillaTagger.Instance.myVRRig != null
					? GorillaTagger.Instance.myVRRig.GetView
					: null;
			}
			catch { return null; }
		}

		public static RaiseEventOptions MasterOnly =>
			SendToMasterOnly && PhotonNetwork.MasterClient != null
				? new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } }
				: null;

		public static bool IsTagged(VRRig rig)
		{
			if (rig == null || rig.Creator == null)
				return false;

			try
			{
				if (GorillaGameManager.instance is GorillaTagManager manager)
					return manager.isCurrentlyTag
						? manager.currentIt == rig.Creator
						: manager.currentInfected != null && manager.currentInfected.Contains(rig.Creator);
			}
			catch { }

			return false;
		}

		public static bool LocalIsTagged
		{
			get
			{
				try { return VRRig.LocalRig != null && IsTagged(VRRig.LocalRig); }
				catch { return false; }
			}
		}

		public static bool CanTagFrom(VRRig rig) => IsMaster || !RequireInfected || LocalIsTagged;

		public static Vector3 SpoofPositionFor(VRRig rig) =>
			rig != null ? rig.transform.position - new Vector3(0f, DropBelowTarget, 0f) : Vector3.zero;

		public static Vector3 SelfPosition =>
			GorillaTagger.Instance != null && GorillaTagger.Instance.bodyCollider != null
				? GorillaTagger.Instance.bodyCollider.transform.position
				: VRRig.LocalRig != null ? VRRig.LocalRig.transform.position : Vector3.zero;

		public static void EndSpoof() => SerializeWritePatch.ClearOverride();

		public static void Push(Vector3 position, RaiseEventOptions options = null)
		{
			PhotonView view = GetLocalRigView();
			if (view == null)
				return;

			SerializeWritePatch.positionOverride = position;

			try { SerializePatch.SendSerialize(view, options ?? MasterOnly); }
			finally { SerializeWritePatch.ClearOverride(); }
		}

		public static bool SpoofTag(VRRig rig, Vector3? position = null)
		{
			if (rig == null || rig.Creator == null || !Enabled)
				return false;

			if (!CanSpoof)
			{
				Report(rig);
				return true;
			}

			if (!CanTagFrom(rig))
				return false;

			Push(position ?? SpoofPositionFor(rig));
			Report(rig);
			return true;
		}

		public static int SpoofTagAll(Vector3? position = null)
		{
			if (!InRoom)
				return 0;

			int tagged = 0;

			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.isOfflineVRRig || rig.Creator == null || IsTagged(rig))
					continue;

				if (!IsMaster && RequireInfected && !LocalIsTagged)
					continue;

				if (SpoofTag(rig, position ?? SpoofPositionFor(rig)))
					tagged++;
			}

			return tagged;
		}

		public static int SpoofAura(float radius)
		{
			if (!CanSpoof || !CanTagFrom(VRRig.LocalRig))
				return 0;

			Vector3 origin = SelfPosition;
			int tagged = 0;

			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.isOfflineVRRig || rig.Creator == null || IsTagged(rig))
					continue;

				if (Vector3.Distance(rig.transform.position, origin) > radius)
					continue;

				if (SpoofTag(rig))
					tagged++;
			}

			return tagged;
		}

		public static void Report(VRRig rig)
		{
			if (rig == null || rig.Creator == null)
				return;

			try { GameMode.ReportTag(rig.Creator); }
			catch (Exception e) { Notify?.Invoke("ReportTag failed: " + e.Message); }
		}
	}
}
