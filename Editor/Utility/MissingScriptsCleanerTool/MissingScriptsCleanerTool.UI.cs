#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// UI pieces: header, controls, progress, stats, filters, getting started, styles.
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        private static void InitializeStyles()
        {
            if (redStyle == null)
            {
                redStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                yellowStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.85f, 0f) } };
            }
        }

        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🗑️ Advanced Missing Scripts Cleaner (Optimized)", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);
        }

        private static void DrawScanControls()
        {
            Color prev = GUI.backgroundColor;
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🔍 Scan Settings:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scan Mode:", GUILayout.Width(90));
                scanMode = (ScanMode)EditorGUILayout.EnumPopup(scanMode, GUILayout.Width(180));

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(showAdvancedOptions ? "🔽 Advanced" : "🔼 Advanced", GUILayout.Width(110)))
                    showAdvancedOptions = !showAdvancedOptions;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (showAdvancedOptions)
                {
                    autoSelectAll =
                        EditorGUILayout.ToggleLeft("Auto-select all found", autoSelectAll, GUILayout.Width(180));
                    createBackup = EditorGUILayout.ToggleLeft("Create backup before cleanup", createBackup,
                        GUILayout.Width(230));
                }
                else
                {
                    EditorGUILayout.LabelField("Advanced options hidden", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(6);
                EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);

                EditorGUILayout.BeginHorizontal();
                if (!isScanning)
                {
                    GUI.backgroundColor = new Color(0.4f, 1f, 1f);
                    if (GUILayout.Button($"🚀 {GetScanButtonText()}", GUILayout.Height(30)))
                        StartScan();

                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("🔄 Quick Scene Scan", GUILayout.Height(30), GUILayout.Width(160)))
                        QuickScan();
                }
                else
                {
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("❌ Cancel Scan", GUILayout.Height(30)))
                        CancelScan();
                }

                GUI.backgroundColor = prev;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawScanControls Error: {e}");
                GUI.backgroundColor = prev;
            }
        }

        private static void DrawScanProgress()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📊 Scanning for Missing Scripts...", EditorStyles.boldLabel);
                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(rect, scanProgress, $"{scanStatus} ({(int)(scanProgress * 100)}%)");
                EditorGUILayout.LabelField($"Found: {foundObjects.Count} objects with missing scripts",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawScanProgress Error: {e}");
            }
        }

        private static void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            try
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);

                if (GUILayout.Button(showStatistics ? "🔽" : "🔼", GUILayout.Width(30)))
                    showStatistics = !showStatistics;
                EditorGUILayout.EndHorizontal();

                if (!showStatistics)
                {
                    EditorGUILayout.LabelField("Hidden (click to expand)", EditorStyles.miniLabel);
                    return;
                }

                if (foundObjects.Count == 0)
                {
                    EditorGUILayout.LabelField("✅ No missing scripts found!", greenStyle);
                    return;
                }

                int sceneObjects = 0, prefabObjects = 0, totalMissing = 0, selectedCount = 0;
                for (int i = 0; i < foundObjects.Count; i++)
                {
                    var fo = foundObjects[i];
                    if (fo.isPrefab) prefabObjects++; else sceneObjects++;
                    totalMissing += fo.missingCount;
                    if (fo.isSelected) selectedCount++;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"📁 Total Objects: {foundObjects.Count}");
                EditorGUILayout.LabelField($"🎬 Scene Objects: {sceneObjects}");
                EditorGUILayout.LabelField($"📦 Prefab Objects: {prefabObjects}");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"❌ Total Missing Scripts: {totalMissing}", redStyle);
                EditorGUILayout.LabelField($"✅ Selected for Cleanup: {selectedCount}",
                    selectedCount > 0 ? greenStyle : redStyle);
                float avg = foundObjects.Count > 0 ? (float)totalMissing / foundObjects.Count : 0f;
                EditorGUILayout.LabelField($"📊 Avg per Object: {avg:F1}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private static void DrawFiltersAndSearch()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Filters & Search", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            scanMode = (ScanMode)EditorGUILayout.EnumPopup("Scan Mode", scanMode);
            filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter", filterMode);
            createBackup = GUILayout.Toggle(createBackup, "Create Backup Before Cleanup", GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchFilter = GUILayout.TextField(searchFilter, GUILayout.MinWidth(200));
            if (GUILayout.Button("✖", GUILayout.Width(24))) searchFilter = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 Start Scan", GUILayout.Height(24))) StartScan();
            if (GUILayout.Button("🛑 Cancel", GUILayout.Height(24))) CancelScan();
            if (GUILayout.Button("♻ Refresh", GUILayout.Height(24))) QuickScan();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawGettingStarted()
        {
            EditorGUILayout.HelpBox(
                "🗑️ Missing Scripts Cleaner (Optimized)\n\n" +
                "Bu araç sahneler ve prefab'lerdeki eksik script referanslarını hızla bulur ve temizler.\n" +
                "• Fast-path GUID ön eleme ile büyük projelerde 10–50x daha hızlı tarama\n" +
                "• Prefab temizlemede LoadPrefabContents + recursive remove\n" +
                "• ProgressBar ve iptal desteği\n\n" +
                "‘Start Scan’ ile başla.",
                MessageType.Info);
        }
    }
}
#endif
