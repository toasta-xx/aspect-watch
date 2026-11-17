using UnityEngine;
using UnityEngine.XR;

namespace WatchLibs
{
	public static class Inputs
	{
		public static bool RG;

		public static bool LG;

		public static bool RT;

		public static bool LT;

		public static bool A;

		public static bool B;

		public static bool X;

		public static bool Y;

		public static void Poll()
		{
			ControllerInputPoller poller = ControllerInputPoller.instance;
			if (poller == null)
			{
				return;
			}
			Inputs.RT = poller.rightControllerIndexFloat > 0.5f;
			Inputs.LT = poller.leftControllerIndexFloat > 0.5f;
			Inputs.RG = poller.rightGrab;
			Inputs.LG = poller.leftGrab;
			Inputs.A = poller.rightControllerPrimaryButton;
			Inputs.B = poller.rightControllerSecondaryButton;
			Inputs.X = poller.leftControllerPrimaryButton;
			Inputs.Y = poller.leftControllerSecondaryButton;
		}
	}
}
