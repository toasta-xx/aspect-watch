namespace WatchModHandler
{
	public class ModificationsHandler
	{
		public static void ModActiveHandler()
		{
			var on = WatchLibs.WatchUpdateClass.modBools;

			if (on[0]) ModificationLoader.ProcessDisconnect();
			if (on[1]) ModificationLoader.ProcessIronMonke();
			if (on[2])
			{
				ModificationLoader.ProcessPlatforms();
				ModificationLoader.UpdatePlatformRGB();
			}
			if (on[3]) ModificationLoader.ProcessNoClip();
			if (on[4]) ModificationLoader.ProcessESP();
			else if (ModificationLoader.stopEsp)
				ModificationLoader.StopESP();

			if (on[5]) ModificationLoader.ProcessTagAura();
			if (on[6]) ModificationLoader.ProcessTagGun();
			if (on[7]) ModificationLoader.ProcessTagAll();
			if (on[8]) ModificationLoader.ProcessRGB();
			if (on[9]) ModificationLoader.ProcessStrobe();

			if (on[10]) ModificationLoader.ProcessMenuRGB();
			else ModificationLoader.StopMenuRGB();

			if (on[11])
			{
				ModificationLoader.ProcessBoardRGB();
				WatchTutorialBoard.BoardMain.Refresh();
			}
			else ModificationLoader.StopBoardRGB();

			if (on[12]) ModificationLoader.ProcessInvisibility();
			if (on[13]) ModificationLoader.ProcessGhostCam();
			if (on[14]) ModificationLoader.ProcessTeleportGun();
			if (on[15]) ModificationLoader.ProcessFly();
			if (on[16])
			{
				ModificationLoader.ProcessStickyPlatforms();
				ModificationLoader.UpdatePlatformRGB();
			}

			if (on[17]) ModificationLoader.ProcessPlatformRGB();
			else ModificationLoader.StopPlatformRGB();

			if (on[18]) ModificationLoader.ProcessInstaTaggedGun();
			if (on[19]) ModificationLoader.ProcessInstaTaggedAll();
			if (on[20]) ModificationLoader.ProcessSoundSpammer();

			if (on[21]) ModificationLoader.ProcessAntiBan();
			else ModificationLoader.StopAntiBan();

			if (on[22]) ModificationLoader.ProcessAntiReport();
			else ModificationLoader.StopAntiReport();

			if (on[23]) ModificationLoader.ProcessReportLog();
			else ModificationLoader.StopReportLog();

			if (on[24]) ModificationLoader.ProcessDayNight();
			else ModificationLoader.StopDayNight();

			if (!on[5] && !on[6] && !on[7])
				AspectWatch.Spoof.TagSpoofer.EndSpoof();
		}
	}
}
