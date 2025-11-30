#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Missing Scripts Cleaner (Optimized) – Core: types, state, entry, common utils, menu.
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        #region Types & State

        [Serializable]
        public class MissingScriptInfo
        {
            public GameObject gameObject;
            public string path;
            public string sceneName;
            public bool isPrefab;
            public int missingCount;
            public readonly List<string> componentNames = new List<string>();
            public bool isSelected;
            public DateTime lastModified;
            public string displayLocationCached;
        }

        public enum ScanMode
        {
            CurrentScene,
            AllOpenScenes,
            AllScenesInBuild,
            ProjectPrefabs,
            Everything
        }

        public enum FilterMode
        {
            All,
            SceneOnly,
            PrefabsOnly,
            RecentlyModified,
            HighMissingCount
        }

        // --- UI State ---
        private static ScanMode scanMode = ScanMode.CurrentScene;
        private static FilterMode filterMode = FilterMode.All;
        private static Vector2 scrollPos;
        private static readonly List<MissingScriptInfo> foundObjects = new List<MissingScriptInfo>();
        private static bool hasScanned;
        private static bool isScanning;
        private static float scanProgress;
        private static string scanStatus = "";
        private static bool autoSelectAll = true;
        private static bool showAdvancedOptions;
        private static bool createBackup = true;
        private static bool showStatistics = true;
        private static string searchFilter = "";

        // --- Scanning State ---
        private static int currentScanIndex;
        private static readonly List<string> filesToScan = new List<string>(); // .unity & .prefab
        private static int processedWorkUnits;
        private static int totalWorkUnits;

        // Dinamik iş yükü
        private static int itemsPerFrame = 8;
        private static double lastFrameTime;

        // Fast-path cache (script GUID set)
        private static HashSet<string> validScriptGuids;

        // --- Styles ---
        private static GUIStyle redStyle, greenStyle, yellowStyle;

        // Path yardımcıları
        private static readonly string ProjectRoot =
            Directory.GetParent(Application.dataPath).FullName;

        private static string ToFullPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(ProjectRoot, assetPath.Replace('\\', '/')));

        #endregion

        #region Public Entry

        public static void Draw()
        {
            try
            {
                InitializeStyles();

                DrawHeader();
                DrawScanControls();

                if (isScanning)
                {
                    DrawScanProgress();
                    return;
                }

                if (hasScanned)
                {
                    DrawStatistics();
                    DrawFiltersAndSearch();
                    DrawResults();
                    DrawActions();
                }
                else
                {
                    DrawGettingStarted();
                }
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error in Missing Scripts Cleaner: {e.Message}", MessageType.Error);
                Debug.LogError($"MissingScriptsCleanerTool Draw Error: {e}");
            }
        }

        #endregion

        #region Fast-Path Helpers (GUID index + YAML tarama)

        // m_Script: {fileID: 11500000, guid: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx, type: 3}
        private static readonly Regex ScriptGuidRegex =
            new Regex(@"m_Script:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})[^}]*\}", RegexOptions.Compiled);

        private static HashSet<string> BuildValidScriptGuidSet()
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            return new HashSet<string>(guids, StringComparer.OrdinalIgnoreCase);
        }

        private static bool FastPathFileHasMissingScripts(string assetPath)
        {
            try
            {
                string full = ToFullPath(assetPath);
                if (!File.Exists(full)) return false;

                string text = File.ReadAllText(full);
                var matches = ScriptGuidRegex.Matches(text);
                if (matches.Count == 0) return false;

                for (int i = 0; i < matches.Count; i++)
                {
                    string guid = matches[i].Groups[1].Value;
                    if (!validScriptGuids.Contains(guid))
                        return true; // şüpheli
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"FastPath fail ({assetPath}): {e.Message} → including in detailed scan.");
                return true;
            }

            return false;
        }

        #endregion

        #region Utilities (paths, timestamps, remove, select, backup helpers)

        private static DateTime TryGetAssetWriteTime(string assetPath)
        {
            try
            {
                string full = ToFullPath(assetPath);
                if (File.Exists(full)) return File.GetLastWriteTime(full);
            }
            catch { }
            return DateTime.Now;
        }

        private static string GetScanButtonText()
        {
            switch (scanMode)
            {
                case ScanMode.CurrentScene: return "Scan Current Scene";
                case ScanMode.AllOpenScenes: return "Scan All Open Scenes";
                case ScanMode.AllScenesInBuild: return "Scan Build Scenes";
                case ScanMode.ProjectPrefabs: return "Scan Project Prefabs";
                case ScanMode.Everything: return "Scan Everything";
                default: return "Start Scan";
            }
        }

        private static string GetGameObjectPath(UnityEngine.Transform transform)
        {
            var sb = new StringBuilder(128);
            sb.Insert(0, transform.name);
            var t = transform.parent;
            while (t != null)
            {
                sb.Insert(0, '/');
                sb.Insert(0, t.name);
                t = t.parent;
            }
            return sb.ToString();
        }

        private static void RemoveMissingScriptsRecursive(GameObject root)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            var t = root.transform;
            for (int i = 0, c = t.childCount; i < c; i++)
                RemoveMissingScriptsRecursive(t.GetChild(i).gameObject);
        }

        private static void SelectAll(bool select)
        {
            for (int i = 0; i < foundObjects.Count; i++)
                foundObjects[i].isSelected = select;
        }

        private static void ToggleSelection()
        {
            for (int i = 0; i < foundObjects.Count; i++)
                foundObjects[i].isSelected = !foundObjects[i].isSelected;
        }

        #endregion

        #region Menu Items

        [MenuItem("MeyzToolbag/Utility/Missing Scripts Cleaner/Quick Scene Scan")]
        public static void QuickScanMenuItem() => QuickScan();

        [MenuItem("MeyzToolbag/Utility/Missing Scripts Cleaner/Scan All Open Scenes")]
        public static void ScanAllOpenScenesMenuItem()
        {
            scanMode = ScanMode.AllOpenScenes;
            StartScan();
        }

        [MenuItem("MeyzToolbag/Utility/Missing Scripts Cleaner/Scan Project Prefabs")]
        public static void ScanProjectPrefabsMenuItem()
        {
            scanMode = ScanMode.ProjectPrefabs;
            StartScan();
        }

        #endregion
    }
}
#endif
