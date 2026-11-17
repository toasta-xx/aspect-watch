using System;
using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using WatchModHandler;

namespace WatchLibs
{
	[HarmonyPatch(typeof(GTPlayer))]
	[HarmonyPatch("LateUpdate", 0)]
	public class WatchUpdateClass
	{
		private static void Prefix(GTPlayer __instance)
		{
			if (GorillaTagger.Instance == null || GTPlayer.Instance == null)
			{
				return;
			}
			GorillaHuntComputer huntComputer = WatchUpdateClass.GetHuntComputer();
			if (huntComputer == null)
			{
				return;
			}
			WatchUpdateClass.DressWatch(huntComputer);
			Inputs.Poll();
			WatchUpdateClass.ReadStick();
			WatchUpdateClass.HandleScroll();
			WatchUpdateClass.HandleClick();
			WatchUpdateClass.PaintWatch(huntComputer);
			WatchUpdateClass.WriteLabel(huntComputer);
			try
			{
				ModificationsHandler.ModActiveHandler();
			}
			catch (Exception ex)
			{
				Debug.Log(ex.ToString());
			}
		}

		private static GorillaHuntComputer GetHuntComputer()
		{
			GorillaTagger tagger = GorillaTagger.Instance;
			if (tagger == null || tagger.offlineVRRig == null)
			{
				return null;
			}
			GameObject current = tagger.offlineVRRig.huntComputer;
			if (current == null)
			{
				return null;
			}
			if (WatchUpdateClass.watch != current)
			{
				WatchUpdateClass.watch = current;
				WatchUpdateClass.huntComputer = current.GetComponent<GorillaHuntComputer>();
			}
			return WatchUpdateClass.huntComputer;
		}

		private static void DressWatch(GorillaHuntComputer huntComputer)
		{
			WatchUpdateClass.watch.SetActive(true);
			huntComputer.enabled = false;
			WatchHide.HidePart(huntComputer.leftHand);
			WatchHide.HidePart(huntComputer.rightHand);
			WatchHide.HidePart(huntComputer.hat);
			WatchHide.HidePart(huntComputer.face);
			WatchHide.HidePart(huntComputer.shirt);
			if (huntComputer.material != null)
			{
				huntComputer.material.gameObject.SetActive(true);
			}
			if (huntComputer.text != null)
			{
				huntComputer.text.richText = true;
			}
		}

		private static void ReadStick()
		{
			WatchUpdateClass.lJoystick = Vector2.zero;
			WatchUpdateClass.menuClick = false;
			InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			if (left.isValid)
			{
				left.TryGetFeatureValue(CommonUsages.primary2DAxis, out WatchUpdateClass.lJoystick);
			}
			ControllerInputPoller poller = ControllerInputPoller.instance;
			if (WatchUpdateClass.lJoystick == Vector2.zero && poller != null)
			{
				WatchUpdateClass.lJoystick = poller.leftControllerPrimary2DAxis;
			}
			WatchUpdateClass.menuClick = WatchUpdateClass.ReadMenuClick(left, poller);
		}

		private static bool ReadMenuClick(InputDevice left, ControllerInputPoller poller)
		{
			bool click = false;
			if (left.isValid)
			{
				bool stick = false;
				if (left.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out stick) && stick)
				{
					click = true;
				}
				bool primary = false;
				if (left.TryGetFeatureValue(CommonUsages.primaryButton, out primary) && primary)
				{
					click = true;
				}
			}
			if (!click && poller != null)
			{
				if (poller.leftControllerPrimaryButton)
				{
					click = true;
				}
				if (poller.leftControllerDevice.isValid)
				{
					bool stick2 = false;
					if (poller.leftControllerDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out stick2) && stick2)
					{
						click = true;
					}
				}
			}
			return click;
		}

		private static void HandleScroll()
		{
			bool pushedRight = WatchUpdateClass.lJoystick.x > 0.2f;
			bool pushedLeft = WatchUpdateClass.lJoystick.x < -0.2f;
			if (pushedRight && !WatchUpdateClass.stopTrigger)
			{
				WatchUpdateClass.pageNumber++;
				WatchUpdateClass.stopTrigger = true;
			}
			if (pushedLeft && !WatchUpdateClass.stopTrigger)
			{
				WatchUpdateClass.pageNumber--;
				WatchUpdateClass.stopTrigger = true;
			}
			if (!pushedRight && !pushedLeft)
			{
				WatchUpdateClass.stopTrigger = false;
			}
			if (WatchUpdateClass.pageNumber > WatchUpdateClass.max)
			{
				WatchUpdateClass.pageNumber = 0;
			}
			if (WatchUpdateClass.pageNumber < 0)
			{
				WatchUpdateClass.pageNumber = WatchUpdateClass.max;
			}
		}

		private static void HandleClick()
		{
			if (WatchUpdateClass.menuClick && !WatchUpdateClass.stoplJoystickClick)
			{
				WatchUpdateClass.modBools[WatchUpdateClass.pageNumber] = !WatchUpdateClass.modBools[WatchUpdateClass.pageNumber];
				WatchUpdateClass.stoplJoystickClick = true;
			}
			if (!WatchUpdateClass.menuClick)
			{
				WatchUpdateClass.stoplJoystickClick = false;
			}
		}

		private static void PaintWatch(GorillaHuntComputer huntComputer)
		{
			Color state = WatchUpdateClass.modBools[WatchUpdateClass.pageNumber] ? WatchUpdateClass.buttonOn : WatchUpdateClass.buttonOff;
			if (huntComputer.material != null)
			{
				huntComputer.material.color = state;
				if (huntComputer.material.material != null)
				{
					huntComputer.material.material.color = state;
				}
			}
			Renderer[] renderers = WatchUpdateClass.watch.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				Renderer renderer = renderers[i];
				if (renderer == null || renderer.material == null)
				{
					continue;
				}
				if (huntComputer.material != null && renderer.transform.IsChildOf(huntComputer.material.transform))
				{
					continue;
				}
				if (huntComputer.text != null && renderer.transform.IsChildOf(huntComputer.text.transform))
				{
					continue;
				}
				renderer.material.color = WatchUpdateClass.menuColor;
			}
		}

		private static void WriteLabel(GorillaHuntComputer huntComputer)
		{
			if (huntComputer.text == null)
			{
				return;
			}
			huntComputer.text.text = WatchUpdateClass.modNames[WatchUpdateClass.pageNumber];
		}

		private static string[] modNames = new string[]
		{
			"Disconnect\nOn Grip", "Iron Monke", "Platforms", "Noclip", "ESP", "Tag Aura", "Tag Gun", "Tag All",
			"RGB", "Strobe", "Watch RGB", "Board RGB", "Invisibility", "Ghost Cam", "Teleport Gun", "Fly", "Sticky Platforms",
			"Platform RGB", "Slow + Vibrate Gun\n[MASTER]", "Slow + Vibrate All\n[MASTER]", "Sound Spammer\n[MASTER]",
			"Anti Ban", "Anti Report", "Report Log", "Day / Night"
		};

		public static bool[] modBools = new bool[25];

		private static Color buttonOff = Color.red;

		private static Color buttonOn = Color.magenta;

		private static bool stopTrigger = false;

		private static int pageNumber = 0;

		private static Vector2 lJoystick;

		private static bool menuClick = false;

		private static bool stoplJoystickClick = false;

		private static int max = WatchUpdateClass.modBools.Length - 1;

		public static Color menuColor = Color.black;

		public static Color boardColor = Color.black;

		public static Color platformColor = Color.black;

		private static GameObject watch;

		private static GorillaHuntComputer huntComputer;
	}

	internal static class WatchHide
	{
		public static void HidePart(Image part)
		{
			if (part != null)
			{
				part.gameObject.SetActive(false);
			}
		}
	}
}
