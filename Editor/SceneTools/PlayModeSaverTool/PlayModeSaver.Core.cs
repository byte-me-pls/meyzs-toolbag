#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        private static PlayModeSaverSettings settings;
        private static List<PlayModeSnapshot> snapshots = new List<PlayModeSnapshot>();
        private static List<ComponentDiff> currentDiff = new List<ComponentDiff>();
        private static PlayModeSnapshot selectedSnapshot;

        // UI State
        private static Vector2 scrollPos;
        private static Vector2 diffScrollPos;
        private static int selectedTabIndex = 0;
        private static string[] tabNames = { "Snapshots", "Watch List", "Diff Viewer", "Settings" };
        private static bool isCapturing = false;

        // Auto save
        private static double lastAutoSaveTime;
        private static bool wasInPlayMode;

        // File paths
        private static string settingsPath = "Assets/Meyz'sToolBag/Data/PlayModeSaverSettings.asset";
        private static string snapshotsDirectory = "Library/PlayModeSnapshots/";

        static PlayModeSaverTool()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void EnsureSettings()
        {
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<PlayModeSaverSettings>(settingsPath);
            }
        }

        private static void Update()
        {
            if (!Application.isPlaying) return;

            EnsureSettings();
            if (settings == null) return;

            // Auto save check
            if (settings.autoSaveEnabled && EditorApplication.timeSinceStartup - lastAutoSaveTime > settings.autoSaveInterval)
            {
                CaptureSnapshot($"Auto-save {System.DateTime.Now:HH:mm:ss}");
                lastAutoSaveTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    wasInPlayMode = true;
                    lastAutoSaveTime = EditorApplication.timeSinceStartup;
                    LoadSnapshots();
                    if (settings?.showToastNotifications == true)
                        Debug.Log("Play Mode Saver: Monitoring started");
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    if (wasInPlayMode && snapshots.Count > 0)
                    {
                        ShowExitDialog();
                    }
                    wasInPlayMode = false;
                    break;
            }
        }

        public static void Draw()
        {
            EnsureSettings();

            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🎮 Play Mode Saver Pro", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);

            DrawStatusBar();
            GUILayout.Space(10);

            DrawQuickActions();
            GUILayout.Space(10);

            selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames, GUILayout.Height(25));
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTabIndex)
            {
                case 0: DrawSnapshotsTab(); break;
                case 1: DrawWatchListTab(); break;
                case 2: DrawDiffViewerTab(); break;
                case 3: DrawSettingsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
