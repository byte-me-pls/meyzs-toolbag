#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        // ====== Settings ======
        private static int atlasSize = 2048;
        private static int padding = 2;
        private static bool trimBorders = true;
        private static bool generateMaterial = true;
        private static bool exportMetadata = true;
        private static bool powerOfTwo = true;
        private static bool allowRotation = false;
        private static PackingMode packingMode = PackingMode.UnityBuiltIn;
        private static AtlasFormat atlasFormat = AtlasFormat.Auto;
        private static FilterMode filterMode = FilterMode.All;
        private static SortMode sortMode = SortMode.Area;
        private static bool sortDescending = true;

        // Advanced
        private static bool showAdvancedSettings = false;
        private static bool enableMipMaps = false;
        private static bool linearColorSpace = false;
        private static int maxTextureSize = 512;
        private static bool onlyPowerOfTwo = false;
        private static bool forceSquare = false;
        private static float trimThreshold = 0.01f;
        private static bool preserveAspectRatio = true;

        // UI
        private static Vector2 scrollPos;
        private static Vector2 previewScrollPos;
        private static bool showPreview = true;
        private static bool showStatistics = true;
        private static bool showTextureDetails = false;
        private static string searchFilter = "";
        private static bool showOutlines = true;
        private static bool showNames = true;
        private static float previewZoom = 1f;

        // Data
        private static readonly List<TextureInfo> textureInfos = new List<TextureInfo>();
        private static AtlasResult currentResult = null;
        private static readonly List<AtlasResult> atlasHistory = new List<AtlasResult>();

        // Packing state
        private static bool isPacking = false;
        private static float packingProgress = 0f;
        private static string packingStatus = "";

        // Styles
        private static GUIStyle redStyle, greenStyle, yellowStyle, boldStyle;
    }
}
#endif
