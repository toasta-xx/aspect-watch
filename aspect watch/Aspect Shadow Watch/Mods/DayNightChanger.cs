using System;
using UnityEngine;

namespace AspectWatch.Mods
{
	public static class DayNightChanger
	{
		public const int DayIndex = 3;

		public static Action<string> Notify = Debug.Log;

		private static bool applied;
		private static int previousIndex = -1;
		private static TimeSettings previousSetting = TimeSettings.Normal;

		private static BetterDayNightManager Manager => BetterDayNightManager.instance;

		public static bool SetDay()
		{
			BetterDayNightManager manager = Manager;
			if (manager == null)
			{
				Notify?.Invoke("Day setter unavailable - no BetterDayNightManager in this scene.");
				return false;
			}

			try
			{
				int count = manager.timeOfDayRange != null ? manager.timeOfDayRange.Length : 0;
				if (count <= 0)
					return false;

				if (!applied)
				{
					previousIndex = manager.currentTimeIndex;
					previousSetting = manager.currentSetting;
					applied = true;
				}

				int target = ((DayIndex % count) + count) % count;
				manager.SetTimeOfDay(target, false);
				manager.SetOverrideIndex(target);
				return true;
			}
			catch (Exception e)
			{
				Notify?.Invoke("Day change failed: " + e.Message);
				return false;
			}
		}

		public static bool Restore()
		{
			BetterDayNightManager manager = Manager;
			if (manager == null || !applied)
				return false;

			try
			{
				manager.ClearTimeOfDay(true);

				if (previousIndex >= 0 && previousSetting == TimeSettings.Static)
				{
					manager.SetTimeOfDay(previousIndex, false);
					manager.SetOverrideIndex(previousIndex);
				}

				applied = false;
				previousIndex = -1;
				previousSetting = TimeSettings.Normal;
				return true;
			}
			catch (Exception e)
			{
				Notify?.Invoke("Day restore failed: " + e.Message);
				return false;
			}
		}
	}
}
