#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SceneTimeMachineTool
    {
        private const string TOOL_NAME = "🕰️ Scene Time Machine (Non-Intrusive)";

        // --- State ---
        private static SceneTimeMachineSettings settings;
        private static bool initialized;
        private static double nextSnapshotTime;
        private static Vector2 scrollPos, detailScrollPos;
        private static readonly List<SceneSnapshot> snapshots = new List<SceneSnapshot>();
        private static ViewMode viewMode = ViewMode.List;
        private static FilterMode filterMode = FilterMode.All;
        private static SortMode sortMode = SortMode.Newest;
        private static string searchFilter = "";
        private static bool showSettings = false;
        private static bool showStatistics = true;
        private static bool showSnapshotList = true;
        private static float snapshotListHeight = 260f;
        private static SceneSnapshot selectedSnapshot = null;

        // --- Styles ---
        private static GUIStyle redStyle, greenStyle, yellowStyle, boldStyle;

        // --- Stats ---
        private static int totalSnapshots = 0;
        private static long totalStorageUsed = 0;

        // PROJECT ROOT
        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath).Replace("\\", "/");

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            EnsureSettings();
            ApplyEnableStateSubscriptions(settings.toolEnabled);
            ScheduleNextTick();
        }

        public static void Draw()
        {
            try
            {
                InitOnce();
                InitializeStyles();
                DrawHeader();
                DrawQuickActions();
                if (showStatistics) DrawStatistics();
                if (showSettings) DrawSettings();
                DrawViewControls();
                DrawSnapshotList();
                if (selectedSnapshot != null) DrawSnapshotDetails();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
                Debug.LogError($"SceneTimeMachine Error: {e}");
            }
        }

        private static void InitializeStyles()
        {
            if (redStyle != null) return;
            redStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
            greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } };
            yellowStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.85f, 0f) } };
            boldStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        private static void InitOnce()
        {
            if (initialized) return;
            EnsureSettings();
            ApplyEnableStateSubscriptions(settings.toolEnabled);
            ScheduleNextTick();
            RefreshSnapshotsIO();
            initialized = true;
        }

        private static void EnsureSettings()
        {
            if (settings != null) return;

            var existing = AssetDatabase.FindAssets("t:SceneTimeMachineSettings");
            if (existing.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(existing[0]);
                settings = AssetDatabase.LoadAssetAtPath<SceneTimeMachineSettings>(path);
                return;
            }

            string dataFolder = FindNearestDataFolderFromThisScript();
            if (string.IsNullOrEmpty(dataFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                    AssetDatabase.CreateFolder("Assets", "Editor");
                dataFolder = "Assets/Editor";
            }

            string targetAssetPath = dataFolder.TrimEnd('/') + "/SceneTimeMachineSettings.asset";
            var atFolderExisting = AssetDatabase.FindAssets("t:SceneTimeMachineSettings", new[] { dataFolder });
            if (atFolderExisting.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(atFolderExisting[0]);
                settings = AssetDatabase.LoadAssetAtPath<SceneTimeMachineSettings>(path);
                return;
            }

            settings = ScriptableObject.CreateInstance<SceneTimeMachineSettings>();
            AssetDatabase.CreateAsset(settings, targetAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneTimeMachine] Created settings at: {targetAssetPath}");
        }

        private static string FindNearestDataFolderFromThisScript()
        {
            try
            {
                string scriptGuid = AssetDatabase.FindAssets("SceneTimeMachineTool t:Script").FirstOrDefault();
                if (string.IsNullOrEmpty(scriptGuid)) return null;

                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                string dir = Path.GetDirectoryName(scriptPath)?.Replace("\\", "/");

                while (!string.IsNullOrEmpty(dir))
                {
                    string candidate = dir.TrimEnd('/') + "/Data";
                    if (AssetDatabase.IsValidFolder(candidate))
                        return candidate;

                    if (dir == "Assets") break;
                    dir = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                }
            }
            catch { }
            return null;
        }

        private static bool TryGetActiveSceneUnityPath(out string path)
        {
            path = EditorSceneManager.GetActiveScene().path;
            return !string.IsNullOrEmpty(path) && File.Exists(UnityToFull(path));
        }

        private static void RevealInExplorer(string absPath)
        {
            absPath = absPath.Replace("/", "\\");
            if (Directory.Exists(absPath) || File.Exists(absPath))
                EditorUtility.RevealInFinder(absPath);
        }
    }
}
#endif
