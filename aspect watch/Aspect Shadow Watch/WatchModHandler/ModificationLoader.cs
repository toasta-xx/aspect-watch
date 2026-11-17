using System;
using System.Collections.Generic;
using AspectWatch.Mods;
using AspectWatch.Spoof;
using AspectWatch.Patches.Menu;
using AspectWatch.Patches.Safety;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.XR;
using WatchLibs;

namespace WatchModHandler
{
	public class ModificationLoader
	{
		private const string TaggedShader = "GUI/Text Shader";

		public static void ProcessDisconnect()
		{
			if (Inputs.RG && PhotonNetwork.InRoom)
			{
				PhotonNetwork.Disconnect();
			}
		}

		public static void ProcessIronMonke()
		{
			Rigidbody body = GTPlayer.Instance.playerRigidBody;
			if (body == null)
			{
				return;
			}
			Transform right = GTPlayer.Instance.RightHand.controllerTransform;
			Transform left = GTPlayer.Instance.LeftHand.controllerTransform;
			if (right != null && Inputs.RT)
			{
				body.AddForce(25f * right.right, ForceMode.VelocityChange);
			}
			if (left != null && Inputs.LT)
			{
				body.AddForce(-25f * left.right, ForceMode.VelocityChange);
			}
		}

		public static void ProcessPlatforms()
		{
			Transform right = GTPlayer.Instance.RightHand.controllerTransform;
			Transform left = GTPlayer.Instance.LeftHand.controllerTransform;
			ModificationLoader.TickPlatform(Inputs.RG, right, ref ModificationLoader.RightToggle, ref ModificationLoader.RightToggleBool, 0.025f, true);
			ModificationLoader.TickPlatform(Inputs.LG, left, ref ModificationLoader.LeftToggle, ref ModificationLoader.LeftToggleBool, 0.034f, false);
		}

		private static void TickPlatform(bool held, Transform hand, ref GameObject platform, ref bool ready, float drop, bool box)
		{
			if (held && ready && hand != null)
			{
				platform = GameObject.CreatePrimitive(box ? PrimitiveType.Cube : PrimitiveType.Sphere);
				Renderer renderer = platform.GetComponent<Renderer>();
				renderer.material.shader = Shader.Find("GorillaTag/UberShader");
				renderer.material.color = WatchUpdateClass.platformColor;
				platform.transform.localScale = box ? new Vector3(0.01f, 0.2f, 0.3f) : new Vector3(0.01f, 0.2f, 0.2f);
				platform.transform.position = hand.position - new Vector3(0f, drop, 0f);
				platform.transform.rotation = hand.rotation;
				ready = false;
			}
			if (!held)
			{
				UnityEngine.Object.Destroy(platform);
				ready = true;
			}
		}

		public static void ProcessStickyPlatforms()
		{
			Transform right = GTPlayer.Instance.RightHand.controllerTransform;
			Transform left = GTPlayer.Instance.LeftHand.controllerTransform;
			ModificationLoader.TickSticky(Inputs.RG, right, ref ModificationLoader.RightToggle, ref ModificationLoader.RightToggleBool);
			ModificationLoader.TickSticky(Inputs.LG, left, ref ModificationLoader.LeftToggle, ref ModificationLoader.LeftToggleBool);
		}

		private static void TickSticky(bool held, Transform hand, ref GameObject platform, ref bool ready)
		{
			if (held)
			{
				if (platform == null && hand != null)
				{
					platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
					platform.transform.localScale = new Vector3(0.025f, 0.23f, 0.32f);
					platform.transform.position = hand.position + hand.up * -0.06f;
					platform.transform.rotation = hand.rotation;
					Renderer renderer = platform.GetComponent<Renderer>();
					renderer.material.shader = Shader.Find("GorillaTag/UberShader");
					renderer.material.color = WatchUpdateClass.platformColor;
				}
				if (platform != null && hand != null)
				{
					platform.transform.position = hand.position + hand.up * -0.06f;
					platform.transform.rotation = hand.rotation;
				}
				ready = true;
				return;
			}
			if (ready && platform != null)
			{
				UnityEngine.Object.Destroy(platform);
				platform = null;
				ready = false;
			}
		}

		public static void ProcessNoClip()
		{
			bool trigger = Inputs.LT;
			if (trigger == ModificationLoader.NoclipDebounce)
			{
				return;
			}
			MeshCollider[] colliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = !trigger;
			}
			ModificationLoader.NoclipDebounce = trigger;
		}

		public static void ProcessESP()
		{
			if (!PhotonNetwork.InRoom)
			{
				return;
			}
			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.isOfflineVRRig || rig.mainSkin == null || rig.mainSkin.material == null)
				{
					continue;
				}
				if (rig.mainSkin.material.shader != null && rig.mainSkin.material.shader.name == ModificationLoader.TaggedShader)
				{
					continue;
				}
				rig.mainSkin.material.shader = Shader.Find(ModificationLoader.TaggedShader);
				rig.mainSkin.material.color = ModificationLoader.IsTagged(rig) ? ModificationLoader.taggedColor : ModificationLoader.untaggedColor;
			}
			ModificationLoader.stopEsp = true;
		}

		private static bool IsTagged(VRRig rig)
		{
			NetPlayer owner = rig.Creator;
			if (owner == null)
			{
				return false;
			}
			GorillaTagManager tagManager = GorillaGameManager.instance as GorillaTagManager;
			if (tagManager != null)
			{
				if (tagManager.currentIt != null && tagManager.currentIt == owner)
				{
					return true;
				}
				return tagManager.currentInfected.Contains(owner);
			}
			GorillaHuntManager huntManager = GorillaGameManager.instance as GorillaHuntManager;
			return huntManager != null && huntManager.currentHunted.Contains(owner);
		}

		public static void StopESP()
		{
			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.isOfflineVRRig || rig.mainSkin == null || rig.mainSkin.material == null)
				{
					continue;
				}
				rig.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
				rig.mainSkin.material.color = Color.white;
			}
			ModificationLoader.stopEsp = false;
		}

		public static VRRig FindVRRigForPlayer(NetPlayer player)
		{
			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig != null && !rig.isOfflineVRRig && rig.Creator == player)
				{
					return rig;
				}
			}
			return null;
		}

		public static void ProcessTagAura()
		{
			if (!TagSpoofer.InRoom)
			{
				return;
			}
			TagSpoofer.SpoofAura(ModificationLoader.tagAuraRadius);
		}

		public static void ProcessAntiReport()
		{
			AntiCheatPatches.AntiACReport = true;
		}

		public static void StopAntiReport()
		{
			AntiCheatPatches.AntiACReport = false;
		}

		public static void ProcessReportLog()
		{
			AntiCheatPatches.AntiCheatSelf = true;
			AntiCheatPatches.AntiCheatAll = true;
		}

		public static void StopReportLog()
		{
			AntiCheatPatches.AntiCheatSelf = false;
			AntiCheatPatches.AntiCheatAll = false;
		}

		public static void ProcessDayNight()
		{
			if (ModificationLoader.dayNightApplied)
			{
				return;
			}
			ModificationLoader.dayNightApplied = true;
			DayNightChanger.SetDay();
		}

		public static void StopDayNight()
		{
			if (!ModificationLoader.dayNightApplied)
			{
				return;
			}
			ModificationLoader.dayNightApplied = false;
			DayNightChanger.Restore();
		}

		public static void ProcessAntiBan()
		{
			BanPatches.enabled = true;
			BanPatches.CrashGuard = true;
			BanContainmentPatches.ContainmentEnabled = true;
		}

		public static void StopAntiBan()
		{
			BanPatches.enabled = false;
		}

		public static void ProcessTagGun()
		{
			ModificationLoader.RunGun(delegate (VRRig target)
			{
				TagSpoofer.SpoofTag(target);
			}, true);
		}

		public static void ProcessTagAll()
		{
			if (!TagSpoofer.InRoom)
			{
				return;
			}
			TagSpoofer.SpoofTagAll(null);
		}

		public static void ProcessRGB()
		{
			float num = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.4f));
			float num2 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.5f));
			float num3 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.6f));
			ModificationLoader.colorTimer += Time.deltaTime;
			if (Time.time > ModificationLoader.time)
			{
				ModificationLoader.time = Time.time + 0.1f;
				GorillaTagger.Instance.UpdateColor(num, num2, num3);
			}
		}

		public static void ProcessStrobe()
		{
			if (Time.time > ModificationLoader.time)
			{
				ModificationLoader.color = UnityEngine.Random.ColorHSV(0f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f);
				ModificationLoader.time = Time.time + 0.25f;
				GorillaTagger.Instance.UpdateColor(ModificationLoader.color.r, ModificationLoader.color.g, ModificationLoader.color.b);
			}
		}

		public static void ProcessMenuRGB()
		{
			float num = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.4f));
			float num2 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.5f));
			float num3 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.6f));
			ModificationLoader.colorTimer += Time.deltaTime;
			WatchUpdateClass.menuColor = new Color(num, num2, num3);
		}

		public static void StopMenuRGB()
		{
			WatchUpdateClass.menuColor = Color.black;
		}

		public static void ProcessBoardRGB()
		{
			float num = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.4f));
			float num2 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.5f));
			float num3 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.6f));
			ModificationLoader.colorTimer += Time.deltaTime;
			WatchUpdateClass.boardColor = new Color(num, num2, num3);
		}

		public static void StopBoardRGB()
		{
			WatchUpdateClass.boardColor = Color.black;
		}

		public static void ProcessPlatformRGB()
		{
			float num = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.4f));
			float num2 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.5f));
			float num3 = Mathf.Abs(Mathf.Sin(ModificationLoader.colorTimer * 0.6f));
			ModificationLoader.colorTimer += Time.deltaTime;
			WatchUpdateClass.platformColor = new Color(num, num2, num3);
		}

		public static void StopPlatformRGB()
		{
			WatchUpdateClass.platformColor = Color.black;
		}

		public static void UpdatePlatformRGB()
		{
			if (!ModificationLoader.RightToggleBool && ModificationLoader.RightToggle != null)
			{
				ModificationLoader.RightToggle.GetComponent<Renderer>().material.color = WatchUpdateClass.platformColor;
			}
			if (!ModificationLoader.LeftToggleBool && ModificationLoader.LeftToggle != null)
			{
				ModificationLoader.LeftToggle.GetComponent<Renderer>().material.color = WatchUpdateClass.platformColor;
			}
		}

		public static void ProcessInvisibility()
		{
			if (!Inputs.A)
			{
				ModificationLoader.antiRepeatInvis = false;
				return;
			}
			if (ModificationLoader.antiRepeatInvis)
			{
				return;
			}
			VRRig rig = VRRig.LocalRig;
			if (rig == null || GTPlayer.Instance == null)
			{
				return;
			}
			ModificationLoader.toggleInvis = !ModificationLoader.toggleInvis;
			rig.enabled = !ModificationLoader.toggleInvis;
			if (ModificationLoader.toggleInvis)
			{
				Vector3 pos = GTPlayer.Instance.transform.position;
				rig.transform.position = new Vector3(pos.x, 0f, pos.z);
			}
			ModificationLoader.antiRepeatInvis = true;
		}

		public static void ProcessGhostCam()
		{
			if (!Inputs.B)
			{
				ModificationLoader.antiRepeatGhostCam = false;
				return;
			}
			if (ModificationLoader.antiRepeatGhostCam)
			{
				return;
			}
			ModificationLoader.antiRepeatGhostCam = true;
			VRRig rig = VRRig.LocalRig;
			if (rig == null)
			{
				return;
			}
			ModificationLoader.toggleGhostCam = !ModificationLoader.toggleGhostCam;
			rig.enabled = !ModificationLoader.toggleGhostCam;
		}

		public static void ProcessTeleportGun()
		{
			if (!Inputs.RG)
			{
				ModificationLoader.ClearPointer();
				return;
			}
			Transform hand = GTPlayer.Instance.RightHand.controllerTransform;
			if (hand == null)
			{
				return;
			}
			RaycastHit hit;
			if (!Physics.Raycast(hand.position - hand.up, -hand.up, out hit, 512f, ModificationLoader.GunMask))
			{
				return;
			}
			ModificationLoader.ShowPointer(hit.point);
			if (!Inputs.RT)
			{
				ModificationLoader.teleportReady = true;
				return;
			}
			if (!ModificationLoader.teleportReady)
			{
				return;
			}
			ModificationLoader.teleportReady = false;
			MeshCollider[] colliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = false;
			}
			GTPlayer.Instance.transform.position = hit.point + Vector3.up * 0.2f;
			if (GTPlayer.Instance.playerRigidBody != null)
			{
				GTPlayer.Instance.playerRigidBody.linearVelocity = Vector3.zero;
			}
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = true;
			}
		}

		public static void ProcessFly()
		{
			if (!Inputs.B || GTPlayer.Instance == null)
			{
				return;
			}
			GTPlayer.Instance.transform.position += GTPlayer.Instance.headCollider.transform.forward * Time.deltaTime * 8f;
			if (GTPlayer.Instance.playerRigidBody != null)
			{
				GTPlayer.Instance.playerRigidBody.linearVelocity = Vector3.zero;
			}
		}

		public static void ProcessInstaTaggedGun()
		{
			ModificationLoader.RunGun(delegate (VRRig target)
			{
				if (PhotonNetwork.LocalPlayer.IsMasterClient)
				{
					GameMode.BroadcastTag(target.Creator, PhotonNetwork.LocalPlayer);
				}
			}, true);
		}

		public static void ProcessInstaTaggedAll()
		{
			if (!PhotonNetwork.InRoom || !PhotonNetwork.LocalPlayer.IsMasterClient)
			{
				return;
			}
			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.isOfflineVRRig || rig.Creator == null)
				{
					continue;
				}
				GameMode.BroadcastTag(rig.Creator, PhotonNetwork.LocalPlayer);
			}
		}

		public static void ProcessSoundSpammer()
		{
			if (!PhotonNetwork.InRoom || !PhotonNetwork.LocalPlayer.IsMasterClient)
			{
				return;
			}
			foreach (VRRig rig in VRRigCache.ActiveRigs)
			{
				if (rig == null || rig.netView == null)
				{
					continue;
				}
				rig.netView.SendRPC("RPC_PlayHandTap", RpcTarget.All, new object[] { 2, true, 90f });
			}
		}

		private static readonly float[] tagAuraDistances = new float[] { 3f, 5f, 8f, 12f, 20f };

		private static int tagAuraRangeIndex = 2;

		private static float tagAuraRadius = 8f;

		private static void RunGun(Action<VRRig> action, bool needTarget)
		{
			if (!Inputs.RG)
			{
				ModificationLoader.ClearPointer();
				return;
			}
			RaycastHit hit;
			if (!Physics.Raycast(GTPlayer.Instance.RightHand.controllerTransform.position - GTPlayer.Instance.RightHand.controllerTransform.up, -GTPlayer.Instance.RightHand.controllerTransform.up, out hit, 512f, ModificationLoader.GunMask))
			{
				return;
			}
			ModificationLoader.ShowPointer(hit.point);
			VRRig target = ModificationLoader.RigFromCollider(hit.collider);
			if (needTarget && target == null)
			{
				return;
			}
			if (Inputs.RT && target != null)
			{
				action(target);
			}
		}

		private static VRRig RigFromCollider(Collider collider)
		{
			if (collider == null)
			{
				return null;
			}
			VRRig rig = collider.GetComponentInParent<VRRig>();
			if (rig == null || rig.isOfflineVRRig || rig.Creator == null)
			{
				return null;
			}
			return rig;
		}

		private static void ShowPointer(Vector3 point)
		{
			if (ModificationLoader.pointer == null)
			{
				ModificationLoader.pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				UnityEngine.Object.Destroy(ModificationLoader.pointer.GetComponent<Rigidbody>());
				UnityEngine.Object.Destroy(ModificationLoader.pointer.GetComponent<SphereCollider>());
				ModificationLoader.pointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
				ModificationLoader.pointer.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
			}
			ModificationLoader.pointer.transform.position = point;
			ModificationLoader.pointer.GetComponent<Renderer>().material.color = Inputs.RT ? Color.green : Color.magenta;
		}

		private static void ClearPointer()
		{
			if (ModificationLoader.pointer != null)
			{
				UnityEngine.Object.Destroy(ModificationLoader.pointer);
				ModificationLoader.pointer = null;
			}
		}

		public static bool stopEsp;

		private static bool dayNightApplied;

		private static GameObject LeftToggle;

		private static bool LeftToggleBool = true;

		private static GameObject RightToggle;

		private static bool RightToggleBool = true;

		private static GameObject pointer;

		private static Color taggedColor = new Color(0.85f, 0.1f, 0.1f, 0.55f);

		private static Color untaggedColor = new Color(0.1f, 0.85f, 0.1f, 0.55f);

		private static Color color;

		private static float colorTimer = 0f;

		private static float time;

		private static bool toggleGhostCam;

		private static bool antiRepeatGhostCam;

		private static bool toggleInvis;

		private static bool antiRepeatInvis;

		private static bool TeleportPatch;

		private static bool teleportReady = true;

		private static bool NoclipDebounce;

		private static readonly LayerMask GunMask = ~LayerMask.GetMask(new string[] { "Gorilla Trigger", "Gorilla Boundary", "GorillaHand", "GorillaObject", "Zone", "Water", "GorillaCosmetics" });
	}
}
