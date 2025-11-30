#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    // Tüm Editor GUI çizimleri
    public static partial class NullSafetyAnalyzerTool
    {
        public static void Draw()
        {
            EnsureDataHolder();

            if (report == null) report = new System.Collections.Generic.List<NullSafetyIssue>();

            DrawHeader();
            DrawMainControls();
            DrawScanScope();
            DrawProjectScanSettings();
            DrawAdvancedFilters();
            DrawDataAssetInfo();

            if (!dataLoaded)
            {
                DrawScanPrompt();
                return;
            }

            DrawStatistics();
            DrawResults();
            DrawIgnoredComponents();
        }

        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🧯 Null Safety Analyzer Pro", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);
        }

        private static void DrawMainControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (!isScanning)
            {
                GUI.color = Color.green;
                string label = dataLoaded ? "🔄 Rescan" : "🔍 Start Scan";
                if (GUILayout.Button(label, GUILayout.Height(32))) StartScan();
                GUI.color = Color.white;
            }
            else
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
                EditorGUI.ProgressBar(rect, scanProgress, $"{scanStatus} ({Mathf.RoundToInt(scanProgress * 100)}%)");

                GUI.color = Color.red;
                if (GUILayout.Button("✖ Cancel", GUILayout.Height(32), GUILayout.Width(80))) CancelScan();
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🧹 Cleanup Missing", GUILayout.Height(32))) dataHolder.CleanupMissingReferences();

            GUI.color = showIgnoredDrawer ? Color.yellow : Color.white;
            if (GUILayout.Button($"👁 Ignored ({dataHolder.IgnoredComponents.Count})", GUILayout.Height(32)))
                showIgnoredDrawer = !showIgnoredDrawer;
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        private static void DrawScanScope()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🎯 Scan Configuration", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scope:", GUILayout.Width(50));
            currentScanScope = (ScanScope)EditorGUILayout.EnumPopup(currentScanScope);
            EditorGUILayout.EndHorizontal();

            includeInactiveObjects = EditorGUILayout.ToggleLeft("Include Inactive GameObjects", includeInactiveObjects);
            enableAutoFix = EditorGUILayout.ToggleLeft("Enable Auto-Fix Suggestions", enableAutoFix);

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawProjectScanSettings()
        {
            if (currentScanScope != ScanScope.EntireProject) return;

            EditorGUILayout.BeginHorizontal();
            showProjectScanSettings = EditorGUILayout.Foldout(showProjectScanSettings, "🌐 Project Scan Settings", true);
            EditorGUILayout.EndHorizontal();

            if (showProjectScanSettings)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.LabelField("Asset Types to Scan:", EditorStyles.boldLabel);
                scanPrefabs = EditorGUILayout.ToggleLeft("📦 Prefabs", scanPrefabs);
                scanScenes = EditorGUILayout.ToggleLeft("🏞️ Scenes", scanScenes);
                scanScriptableObjects = EditorGUILayout.ToggleLeft("📄 ScriptableObjects", scanScriptableObjects);
                scanAnimationControllers = EditorGUILayout.ToggleLeft("🎬 Animation Controllers", scanAnimationControllers);

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Performance Info:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"• Streaming scan - low memory usage\n" +
                    $"• Chunk size: {PROJECT_SCAN_CHUNK_SIZE} assets per frame\n" +
                    $"• Scenes loaded temporarily and unloaded",
                    MessageType.Info);

                if (projectStats.totalAssets > 0)
                {
                    EditorGUILayout.LabelField($"Last scan: {projectStats.scannedAssets}/{projectStats.totalAssets} assets in {projectStats.elapsedSeconds:F1}s", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private static void DrawAdvancedFilters()
        {
            EditorGUILayout.BeginHorizontal();
            showAdvancedFilters = EditorGUILayout.Foldout(showAdvancedFilters, "🔧 Advanced Filters", true);
            EditorGUILayout.EndHorizontal();

            if (showAdvancedFilters)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Component Type:", GUILayout.Width(100));
                componentTypeFilter = EditorGUILayout.TextField(componentTypeFilter);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("GameObject Name:", GUILayout.Width(100));
                gameObjectFilter = EditorGUILayout.TextField(gameObjectFilter);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Min Severity:", GUILayout.Width(100));
                minSeverityLevel = (SeverityLevel)EditorGUILayout.EnumPopup(minSeverityLevel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private static void DrawDataAssetInfo()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📊 Data Asset: {dataHolder.IgnoredComponents.Count} ignored | Path: {System.IO.Path.GetFileName(GetDataAssetPath())}", EditorStyles.miniLabel);
            if (GUILayout.Button("📌 Ping", GUILayout.Width(50))) EditorGUIUtility.PingObject(dataHolder);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawScanPrompt()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (isScanning)
            {
                EditorGUILayout.LabelField("🔍 Scanning in progress...", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(scanStatus, EditorStyles.miniLabel);

                if (currentScanScope == ScanScope.EntireProject && projectStats.totalAssets > 0)
                {
                    EditorGUILayout.LabelField($"Progress: {projectStats.scannedAssets}/{projectStats.totalAssets} assets", EditorStyles.miniLabel);
                    if (projectStats.elapsedSeconds > 0)
                    {
                        float eta = (projectStats.elapsedSeconds / Mathf.Max(1, projectStats.scannedAssets)) * (projectStats.totalAssets - projectStats.scannedAssets);
                        EditorGUILayout.LabelField($"ETA: {eta:F0} seconds", EditorStyles.miniLabel);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("📋 Ready to scan", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Click 'Start Scan' to analyze your project for null references.", EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawStatistics()
        {
            EditorGUILayout.BeginHorizontal();
            showStatistics = EditorGUILayout.Foldout(showStatistics, "📊 Statistics", true);
            EditorGUILayout.EndHorizontal();

            if (showStatistics)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                var filteredReport = GetFilteredReport();
                var severityCounts = filteredReport.GroupBy(i => i.severity).ToDictionary(g => g.Key, g => g.Count());

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🕐 Last Scan: {lastScanTime:HH:mm:ss}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"📦 Scanned Objects: {totalScannedObjects}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                foreach (SeverityLevel severity in System.Enum.GetValues(typeof(SeverityLevel)))
                {
                    int count = severityCounts.GetValueOrDefault(severity, 0);
                    var color = GetSeverityColor(severity);
                    GUI.color = color;
                    EditorGUILayout.LabelField($"{severity}: {count}", EditorStyles.boldLabel, GUILayout.Width(80));
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                if (componentTypeStats.Count > 0)
                {
                    EditorGUILayout.LabelField("Top Problematic Components:", EditorStyles.boldLabel);
                    var topTypes = componentTypeStats.OrderByDescending(kvp => kvp.Value).Take(3);
                    foreach (var kvp in topTypes)
                        EditorGUILayout.LabelField($"• {kvp.Key}: {kvp.Value} issues", EditorStyles.miniLabel);
                }

                if (currentScanScope == ScanScope.EntireProject && projectStats.totalAssets > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Project Scan Stats:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"• Prefabs: {projectStats.prefabsScanned}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Scenes: {projectStats.scenesScanned}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• ScriptableObjects: {projectStats.scriptableObjectsScanned}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Animation Controllers: {projectStats.animationControllersScanned}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Total Time: {projectStats.elapsedSeconds:F1}s", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private static void DrawResults()
        {
            var filteredReport = GetFilteredReport();

            if (filteredReport.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ No null reference issues found with current filters!", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🚨 Found {filteredReport.Count} issues", EditorStyles.boldLabel);

            if (enableAutoFix && filteredReport.Any(i => i.canAutoFix))
            {
                GUI.color = Color.green;
                if (GUILayout.Button($"🔧 Auto-Fix ({filteredReport.Count(i => i.canAutoFix)})", GUILayout.Width(120)))
                    PerformAutoFix(filteredReport);
                GUI.color = Color.white;
            }

            if (GUILayout.Button("📤 Export", GUILayout.Width(80))) ExportReport();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var issue in filteredReport.OrderByDescending(i => i.severity))
                DrawIssueEntry(issue);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawIssueEntry(NullSafetyIssue issue)
        {
            if (issue.TargetObject == null) return;

            var severityColor = GetSeverityColor(issue.severity);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();
            GUI.color = severityColor;
            EditorGUILayout.LabelField(GetSeverityIcon(issue.severity), GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"{issue.ObjectTypeName} on {issue.ObjectName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (issue.canAutoFix && enableAutoFix)
            {
                GUI.color = Color.cyan;
                if (GUILayout.Button("🔧 Fix", GUILayout.Width(50))) PerformAutoFix(new System.Collections.Generic.List<NullSafetyIssue> { issue });
                GUI.color = Color.white;
            }

            if (issue.component != null)
            {
                if (GUILayout.Button("🚫 Ignore", GUILayout.Width(60)))
                {
                    string reason = $"User ignored: {issue.severity} severity, {issue.propertyPaths.Count} issues";
                    dataHolder.AddIgnoredComponent(issue.component, reason);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Description
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📋 {issue.description}", EditorStyles.miniLabel);
            GUI.color = severityColor;
            EditorGUILayout.LabelField($"[{issue.severity}]", EditorStyles.miniLabel, GUILayout.Width(60));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // Asset path
            if (!string.IsNullOrEmpty(issue.assetPath))
                EditorGUILayout.LabelField($"📁 {issue.assetPath}", EditorStyles.miniLabel);

            // Properties
            if (issue.TargetObject != null)
            {
                try
                {
                    SerializedObject so = new SerializedObject(issue.TargetObject);
                    so.Update();

                    for (int i = 0; i < issue.propertyPaths.Count; i++)
                    {
                        var path = issue.propertyPaths[i];
                        var displayName = issue.propertyNames[i];

                        SerializedProperty prop = so.FindProperty(path);
                        if (prop == null) continue;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(prop, new GUIContent(displayName));
                        if (EditorGUI.EndChangeCheck()) so.ApplyModifiedProperties();

                        if (GUILayout.Button("📌", GUILayout.Width(25)))
                        {
                            EditorGUIUtility.PingObject(issue.TargetObject);
                            Selection.activeObject = issue.TargetObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                catch (System.Exception ex)
                {
                    EditorGUILayout.LabelField($"⚠️ Cannot edit properties: {ex.Message}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private static void DrawIgnoredComponents()
        {
            if (!showIgnoredDrawer) return;

            GUILayout.Space(6);
            EditorGUILayout.LabelField("🚫 Ignored Components", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (dataHolder.IgnoredComponents.Count == 0)
            {
                EditorGUILayout.LabelField("No ignored components.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                ignoredScrollPos = EditorGUILayout.BeginScrollView(ignoredScrollPos, GUILayout.MaxHeight(200));

                for (int i = dataHolder.IgnoredComponents.Count - 1; i >= 0; i--)
                {
                    var ignoredComp = dataHolder.IgnoredComponents[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    var comp = EditorUtility.InstanceIDToObject(ignoredComp.instanceID) as MonoBehaviour;
                    if (comp != null)
                    {
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"{ignoredComp.componentTypeName} on {comp.gameObject.name}", EditorStyles.boldLabel);
                        if (!string.IsNullOrEmpty(ignoredComp.reason))
                            EditorGUILayout.LabelField($"Reason: {ignoredComp.reason}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Ignored: {ignoredComp.ignoredDate:yyyy-MM-dd HH:mm}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();

                        if (GUILayout.Button("📌", GUILayout.Width(25))) EditorGUIUtility.PingObject(comp);
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"<Missing> {ignoredComp.componentTypeName} on {ignoredComp.gameObjectName}",
                            new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic });
                        EditorGUILayout.LabelField($"Scene: {System.IO.Path.GetFileName(ignoredComp.scenePath)}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                    }

                    GUI.color = Color.red;
                    if (GUILayout.Button("✖", GUILayout.Width(25)))
                    {
                        dataHolder.RemoveIgnoredComponent(ignoredComp.instanceID);
                        GUIUtility.ExitGUI();
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
