// QuickMaterialSwapperTool.Definitions.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        [Serializable]
        public class MaterialPreset
        {
            public string name;
            public string guid;
            public string tags;
            public DateTime created;
            public int usageCount;
            public bool isFavorite;
            public Color thumbnailColor;
        }

        [Serializable]
        public class MaterialOperation
        {
            public string objectName;
            public string objectPath;
            public Material originalMaterial;
            public Material newMaterial;
            public Renderer renderer;
            public int materialIndex;
            public DateTime timestamp;
        }

        [Serializable]
        public class PresetsData
        {
            public List<MaterialPreset> presets = new List<MaterialPreset>();
        }

        public enum ApplyMode { ReplaceAll, ReplaceFirst, ReplaceSpecific, AddToEnd, SmartReplace }
        public enum FilterMode { All, Favorites, RecentlyUsed, ByTag, ByShader, Unused }
        public enum ViewMode { List, Grid, Compact }

        // --- Settings ---
        private static Material chosenMaterial;
        private static ApplyMode applyMode = ApplyMode.ReplaceAll;
        private static bool includeChildren = true;
        private static bool includeInactive = false;
        private static bool showPreview = true;
        private static bool autoCreateBackup = true;   // ayrılmış ama (şimdilik) davranış değişimi yok
        private static bool smartUndo = true;          // ayrılmış ama Undo API zaten bütününü kapsıyor
        private static int specificMaterialIndex = 0;

        // --- UI State ---
        private static FilterMode filterMode = FilterMode.All;
        private static ViewMode viewMode = ViewMode.List;
        private static string searchFilter = "";
        private static string tagFilter = "";
        private static Vector2 scrollPos;
        private static Vector2 presetScrollPos;
        private static Vector2 operationScrollPos;
        private static bool showAdvancedOptions = false;
        private static bool showOperationHistory = false;
        private static bool showStatistics = true;

        // --- Data ---
        private static List<MaterialPreset> presets = new List<MaterialPreset>();
        private static List<MaterialOperation> operationHistory = new List<MaterialOperation>();
        private static MaterialPreset selectedPreset = null;
        private static bool presetsLoaded = false;

        // --- Undo System ---
        private static readonly List<MaterialOperation> lastOperations = new List<MaterialOperation>();
        private static bool canPerformUndo = false;

        // --- Constants ---
        private const string PRESETS_KEY = "QuickMaterialSwapper.Presets";
        private const string HISTORY_KEY = "QuickMaterialSwapper.History"; // yer ayrıldı (hafif kullanım)
        private const int MAX_HISTORY_ITEMS = 100;

        // --- Styles ---
        private static GUIStyle redStyle, greenStyle, yellowStyle, boldStyle;
    }
}
#endif
