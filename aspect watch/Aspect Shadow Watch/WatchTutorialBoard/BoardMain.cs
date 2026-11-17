using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using WatchLibs;

namespace WatchTutorialBoard
{
	public class BoardMain
	{
		private static readonly List<Renderer> Boards = new List<Renderer>();

		private static Material boardMaterial;

		private static string[] boardPaths = new string[]
		{
			"Environment Objects/LocalObjects_Prefab/TreeRoom/motd",
			"Environment Objects/LocalObjects_Prefab/TreeRoom/motdscreen",
			"Environment Objects/LocalObjects_Prefab/TreeRoom/StaticUnlit/motdscreen",
			"Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData",
			"Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText",
			"Environment Objects/LocalObjects_Prefab/City_WorkingPrefab/CosmeticsScoreboardAnchor/GorillaScoreBoard",
			"Environment Objects/LocalObjects_Prefab/Forest/ForestScoreboardAnchor/GorillaScoreBoard",
			"Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/GorillaComputerObject/ComputerUI/monitor/monitorScreen"
		};

		public static void Refresh()
		{
			if (BoardMain.boardMaterial == null)
			{
				BoardMain.boardMaterial = new Material(Shader.Find("GorillaTag/UberShader"));
			}
			BoardMain.boardMaterial.color = WatchUpdateClass.boardColor;
			if (BoardMain.Boards.Count == 0)
			{
				BoardMain.FindBoards();
			}
			for (int i = 0; i < BoardMain.Boards.Count; i++)
			{
				Renderer renderer = BoardMain.Boards[i];
				if (renderer != null && renderer.material != null)
				{
					renderer.material.color = WatchUpdateClass.boardColor;
				}
			}
		}

		private static void FindBoards()
		{
			for (int i = 0; i < BoardMain.boardPaths.Length; i++)
			{
				GameObject found = GameObject.Find(BoardMain.boardPaths[i]);
				if (found == null)
				{
					continue;
				}
				Renderer renderer = found.GetComponent<Renderer>();
				if (renderer != null && !BoardMain.Boards.Contains(renderer))
				{
					BoardMain.Boards.Add(renderer);
				}
			}
			GorillaScoreBoard[] scoreboards = Object.FindObjectsOfType<GorillaScoreBoard>();
			for (int j = 0; j < scoreboards.Length; j++)
			{
				Renderer renderer2 = scoreboards[j].GetComponent<Renderer>();
				if (renderer2 != null && !BoardMain.Boards.Contains(renderer2))
				{
					BoardMain.Boards.Add(renderer2);
				}
			}
		}
	}

	[HarmonyPatch(typeof(GorillaLocomotion.GTPlayer))]
	[HarmonyPatch("LateUpdate", 0)]
	public class BoardTitle
	{
		private static bool applied;

		private static void Postfix()
		{
			if (BoardTitle.applied)
			{
				return;
			}
			BoardTitle.applied = BoardTitle.ApplyText();
		}

		private static bool ApplyText()
		{
			bool title = BoardTitle.SetText("Environment Objects/LocalObjects_Prefab/TreeRoom/CodeOfConductHeadingText", "[<color=magenta>ASPECT SHADOW WATCH</color>]");
			bool body = BoardTitle.SetText("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData", "How to use:\nUse your left joystick to slide through the mods, click your left joystick to select the mods\n\n\nDeveloper: Aspect#5555");
			return title && body;
		}

		private static bool SetText(string path, string value)
		{
			GameObject target = GameObject.Find(path);
			if (target == null)
			{
				return false;
			}
			TMP_Text text = target.GetComponent<TMP_Text>();
			if (text == null)
			{
				return false;
			}
			text.richText = true;
			text.text = value;
			return true;
		}
	}
}
