using GorillaTagScripts;
using HarmonyLib;
using System;
using UnityEngine;

namespace AspectWatch.Patches.Menu
{
    [HarmonyPatch(typeof(BuilderTable), nameof(BuilderTable.RequestPlacePiece))]
    public static class PlacePatch
    {
        public const string HarmonyId = "AspectWatch.Patches.Menu.Place";

        public static BuilderPiece _piece;
        public static BuilderPiece _attachPiece;
        public static BuilderPiece _parentPiece;

        public static sbyte _bumpOffsetX;
        public static sbyte _bumpOffsetZ;
        public static byte _twist;
        public static int _attachIndex;
        public static int _parentAttachIndex;

        private static Harmony harmony;

        public static bool IsPatched => harmony != null;

        public static void Patch()
        {
            if (harmony != null)
                return;

            harmony = new Harmony(HarmonyId);

            try
            {
                harmony.CreateClassProcessor(typeof(PlacePatch)).Patch();
            }
            catch (Exception exception)
            {
                Debug.LogError("[PlacePatch] Failed to patch: " + exception);
            }
        }

        public static void Unpatch()
        {
            if (harmony == null)
                return;

            harmony.UnpatchSelf();
            Clear();
            harmony = null;
        }

        public static void Clear()
        {
            _piece = null;
            _attachPiece = null;
            _parentPiece = null;
            _bumpOffsetX = 0;
            _bumpOffsetZ = 0;
            _twist = 0;
            _attachIndex = 0;
            _parentAttachIndex = 0;
        }

        public static bool HasPlacement => _piece != null;

        public static BuilderTable GetTable()
        {
            if (BuilderTable.TryGetBuilderTableForZone(GTZone.forest, out BuilderTable table) && table != null)
                return table;

            for (int zone = 0; zone < 32; zone++)
                if (BuilderTable.TryGetBuilderTableForZone((GTZone)zone, out table) && table != null)
                    return table;

            return null;
        }

        public static void Replay()
        {
            if (_piece == null)
                return;

            BuilderTable table = GetTable();
            if (table == null)
                return;

            table.RequestPlacePiece(_piece, _attachPiece, _bumpOffsetX, _bumpOffsetZ, _twist, _parentPiece, _attachIndex, _parentAttachIndex);
        }

        private static void Postfix(BuilderPiece piece, BuilderPiece attachPiece, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, BuilderPiece parentPiece, int attachIndex, int parentAttachIndex)
        {
            _piece = piece;
            _attachPiece = attachPiece;
            _parentPiece = parentPiece;

            _bumpOffsetX = bumpOffsetX;
            _bumpOffsetZ = bumpOffsetZ;
            _twist = twist;

            _attachIndex = attachIndex;
            _parentAttachIndex = parentAttachIndex;
        }
    }
}
