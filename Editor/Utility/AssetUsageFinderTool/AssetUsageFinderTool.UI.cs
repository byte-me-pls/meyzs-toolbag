#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - UI Rendering
    /// All IMGUI drawing logic lives here.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        public static void Draw()
        {
            try
            {
                InitializeStyles();

                DrawHeader();
                DrawSerializationWarningIfAny();
                DrawSelectionInfo();
                DrawScanControls();

                if (isSearching)
                {
                    DrawScanProgress();
                    return;
                }

                if (hasResults)
                {
                    DrawStatistics();
                    DrawFiltersAndSort();
                    DrawResultsPanel();
                }
                else
                {
                    DrawGettingStarted();
                }
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error in Asset Usage Finder: {e.Message}", MessageType.Error);
                Debug.LogError($"AssetUsageFinderTool Draw Error: {e}");
            }
        }

        private static void InitializeStyles()
        {
            if (redStyle == null)
            {
                redStyle = new GUIStyle(EditorStyles.label)   { normal = { textColor = Color.red } };
                greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } };
                yellowStyle = new GUIStyle(EditorStyles.label){ normal = { textColor = new Color(0.95f, 0.8f, 0.2f) } };
                boldStyle = new GUIStyle(EditorStyles.boldLabel);
            }
        }

        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🚀 Optimized Asset Usage Finder", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);
        }

        private static void DrawSerializationWarningIfAny()
        {
            if (deepSearch && EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox(
                    "Deep Search works best with Text Serialization. " +
                    "Set Project Settings > Editor > Asset Serialization to Force Text for more accurate results.",
                    MessageType.Warning);
            }
        }

        private static void DrawSelectionInfo()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📦 Selected Assets:", EditorStyles.boldLabel);

                var selectedAssets = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);

                if (selectedAssets.Length == 0)
                {
                    EditorGUILayout.HelpBox("Select assets in the Project window to analyze their usage.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.LabelField($"Count: {selectedAssets.Length} assets selected");

                var typeGroups = selectedAssets.GroupBy(a => a.GetType().Name).ToList();
                EditorGUILayout.BeginHorizontal();
                foreach (var group in typeGroups.Take(4))
                    EditorGUILayout.LabelField($"{group.Key}: {group.Count()}", EditorStyles.miniLabel);
                if (typeGroups.Count > 4)
                    EditorGUILayout.LabelField($"+{typeGroups.Count - 4} more types", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawSelectionInfo Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawScanControls()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🔧 Search Settings:", EditorStyles.boldLabel);

                // File Types
                EditorGUILayout.LabelField("File Types:", EditorStyles.boldLabel);
                includeScenes      = EditorGUILayout.ToggleLeft("Scenes (.unity)", includeScenes);
                includePrefabs     = EditorGUILayout.ToggleLeft("Prefabs (.prefab)", includePrefabs);
                includeMaterials   = EditorGUILayout.ToggleLeft("Materials (.mat)", includeMaterials);
                includeAnimations  = EditorGUILayout.ToggleLeft("Animations (.anim)", includeAnimations);

                GUILayout.Space(6);

                // Advanced
                EditorGUILayout.LabelField("Advanced:", EditorStyles.boldLabel);
                includeControllers = EditorGUILayout.ToggleLeft("Controllers (.controller)", includeControllers);
                includeScripts     = EditorGUILayout.ToggleLeft("Scripts (.cs)", includeScripts);
                includeShaders     = EditorGUILayout.ToggleLeft("Shaders (.shader)", includeShaders);
                includeOthers      = EditorGUILayout.ToggleLeft("Others (.asset etc.)", includeOthers);
                includePackages    = EditorGUILayout.ToggleLeft("Include Packages/", includePackages);

                GUILayout.Space(6);

                // Options
                EditorGUILayout.BeginHorizontal();
                deepSearch       = EditorGUILayout.ToggleLeft("Deep Search (Regex)", deepSearch);
                showLineNumbers  = EditorGUILayout.ToggleLeft("Line numbers", showLineNumbers);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                showContext      = EditorGUILayout.ToggleLeft("Show context", showContext);
                autoSelectUnused = EditorGUILayout.ToggleLeft("Auto-select unused", autoSelectUnused);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Cache controls
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔥 Warmup Cache", GUILayout.Height(22)))
                    WarmupCache();
                if (GUILayout.Button("🧹 Clear Cache", GUILayout.Height(22)))
                    ClearAllCaches();
                if (GUILayout.Button("📊 Cache Stats", GUILayout.Height(22)))
                    LogPerformanceStats();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Main buttons
                var selectedAssets = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);

                using (new EditorGUI.DisabledScope(selectedAssets.Length == 0 || isSearching))
                {
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.9f);
                    if (GUILayout.Button($"🚀 Optimized Search ({selectedAssets.Length} assets)", GUILayout.Height(28)))
                        BeginOptimizedSearch();
                    GUI.backgroundColor = prev;
                }

                EditorGUILayout.BeginHorizontal();
                if (!isSearching)
                {
                    if (GUILayout.Button("⚡ Quick Search (Original)", GUILayout.Height(28)))
                        QuickSearch();
                    if (GUILayout.Button("🔍 Deep Search (Original)", GUILayout.Height(28)))
                        BeginSearch();
                }
                else
                {
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("❌ Cancel Search", GUILayout.Height(28)))
                    {
                        cancelRequested = true;
                        processingCancelled = true;
                    }
                    GUI.backgroundColor = prev;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawScanControls Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawScanProgress()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📊 Searching for Asset Usage...", EditorStyles.boldLabel);

                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(rect, scanProgress, $"{scanStatus} ({(int)(scanProgress * 100)}%)");

                EditorGUILayout.LabelField($"Files scanned: {currentFileIndex}/{(allFilePaths?.Length ?? 0)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"References found: {assetInfoList.Sum(a => a.totalReferences)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Cache entries: {dependencyCache.Count}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawScanProgress Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawStatistics()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📈 Statistics:", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                if (assetInfoList.Count == 0)
                {
                    EditorGUILayout.EndVertical();
                    return;
                }

                int totalAssets     = assetInfoList.Count;
                int usedAssets      = assetInfoList.Count(a => a.totalReferences > 0);
                int unusedAssets    = totalAssets - usedAssets;
                int totalReferences = assetInfoList.Sum(a => a.totalReferences);
                long totalSize      = assetInfoList.Sum(a => a.fileSize);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"📁 Total Assets: {totalAssets}");
                EditorGUILayout.LabelField($"✅ Used Assets: {usedAssets}", usedAssets > 0 ? greenStyle : redStyle);
                EditorGUILayout.LabelField($"❌ Unused Assets: {unusedAssets}", unusedAssets > 0 ? yellowStyle : greenStyle);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"🔗 Total References: {totalReferences}");
                EditorGUILayout.LabelField($"📊 Avg References (used): {(float)totalReferences / Math.Max(usedAssets, 1):F1}");
                EditorGUILayout.LabelField($"💾 Total Size: {FormatBytes(totalSize)}");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"🏎️ Dependency Cache: {dependencyCache.Count}");
                EditorGUILayout.LabelField($"🧠 Regex Cache: {compiledRegexCache.Count}");
                EditorGUILayout.LabelField($"📁 Metadata Cache: {fileMetadataCache.Count}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                var topUsed = assetInfoList.OrderByDescending(a => a.totalReferences).Take(3).ToList();
                if (topUsed.Count > 0 && topUsed[0].totalReferences > 0)
                {
                    EditorGUILayout.LabelField("🏆 Most Referenced:", EditorStyles.boldLabel);
                    foreach (var asset in topUsed)
                        EditorGUILayout.LabelField($"• {asset.assetName}: {asset.totalReferences} refs", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawStatistics Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawFiltersAndSort()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🔧 Filters & Sorting:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(180));
                filterMode   = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(140));
                groupBy      = (GroupBy)EditorGUILayout.EnumPopup("Group:", groupBy, GUILayout.Width(220));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(120));
                if (GUILayout.Button(sortAscending ? "🔼" : "🔽", GUILayout.Width(30)))
                    sortAscending = !sortAscending;

                EditorGUILayout.LabelField("Usage:", GUILayout.Width(45));
                minUsageCount = EditorGUILayout.IntField(minUsageCount, GUILayout.Width(40));
                EditorGUILayout.LabelField("-", GUILayout.Width(10));
                maxUsageCount = EditorGUILayout.IntField(maxUsageCount, GUILayout.Width(40));

                if (GUILayout.Button("❌ Unused Only", GUILayout.Width(110)))
                    filterMode = FilterMode.UnusedOnly;
                if (GUILayout.Button("🔥 High Usage", GUILayout.Width(110)))
                {
                    filterMode = FilterMode.HighUsage;
                    minUsageCount = Math.Max(minUsageCount, 5);
                }

                if (GUILayout.Button("🗑️ Clear Filters", GUILayout.Width(110)))
                    ClearFilters();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawFiltersAndSort Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawResultsPanel()
        {
            try
            {
                var filteredAssets = GetFilteredAndSortedAssets();

                EditorGUILayout.BeginHorizontal();

                // Left list
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width((selectedAssetInfo != null) ? 360 : -1));
                EditorGUILayout.LabelField($"🎯 Assets ({filteredAssets.Count}):", EditorStyles.boldLabel);

                if (filteredAssets.Count == 0)
                {
                    EditorGUILayout.HelpBox("No assets match the current filter.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.Label("☑", GUILayout.Width(25));
                    GUILayout.Label("Asset", GUILayout.Width(170));
                    GUILayout.Label("Type", GUILayout.Width(80));
                    GUILayout.Label("Refs", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(310));
                    foreach (var asset in filteredAssets)
                        DrawAssetItem(asset);
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.EndVertical();

                // Right detail
                if (selectedAssetInfo != null)
                    DrawDetailPanel();

                EditorGUILayout.EndHorizontal();

                DrawActionButtons();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawResultsPanel Error: {e}");
                try
                {
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                catch { }
            }
        }

        private static void DrawAssetItem(AssetUsageInfo asset)
        {
            try
            {
                EditorGUILayout.BeginHorizontal(asset == selectedAssetInfo ? EditorStyles.helpBox : GUI.skin.box);

                asset.isSelected = EditorGUILayout.Toggle(asset.isSelected, GUILayout.Width(25));

                var nameStyle = asset.totalReferences == 0 ? redStyle : EditorStyles.label;
                if (GUILayout.Button(asset.assetName, nameStyle, GUILayout.Width(170)))
                {
                    selectedAssetInfo = asset;
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }

                GUILayout.Label(asset.assetType, GUILayout.Width(80));

                var refStyle = asset.totalReferences == 0 ? redStyle :
                    asset.totalReferences >= 10 ? greenStyle : EditorStyles.label;
                GUILayout.Label(asset.totalReferences.ToString(), refStyle, GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawAssetItem Error: {e}");
                try { EditorGUILayout.EndHorizontal(); } catch { }
            }
        }

        private static void DrawDetailPanel()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(520));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📋 {selectedAssetInfo.assetName}", EditorStyles.boldLabel);
                if (GUILayout.Button("❌", GUILayout.Width(25)))
                    selectedAssetInfo = null;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Path: {selectedAssetInfo.assetPath}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Type: {selectedAssetInfo.assetType}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Size: {FormatBytes(selectedAssetInfo.fileSize)}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"References: {selectedAssetInfo.totalReferences}", EditorStyles.miniLabel);

                GUILayout.Space(5);

                if (selectedAssetInfo.usages.Count == 0)
                {
                    EditorGUILayout.HelpBox("This asset is not referenced anywhere.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("📍 Usage Locations:", EditorStyles.boldLabel);

                    detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos, GUILayout.Height(260));
                    foreach (var usage in selectedAssetInfo.usages)
                        DrawUsageReference(usage);
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawDetailPanel Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawUsageReference(UsageReference usage)
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();

                string icon = GetFileTypeIcon(usage.fileType);
                EditorGUILayout.LabelField($"{icon} {usage.fileName}", EditorStyles.boldLabel);

                if (GUILayout.Button("📂", GUILayout.Width(25)))
                {
                    var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(usage.filePath);
                    if (o != null) EditorGUIUtility.PingObject(o);
                }

                if (usage.fileType == ".unity" && GUILayout.Button("🎬", GUILayout.Width(25)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(usage.filePath, OpenSceneMode.Single);
                }
                else if (usage.fileType == ".prefab" && GUILayout.Button("📦", GUILayout.Width(25)))
                {
                    var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(usage.filePath);
                    if (o != null) AssetDatabase.OpenAsset(o);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Folder: {usage.folderPath}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Type: {usage.referenceType}", EditorStyles.miniLabel);

                if (showLineNumbers && usage.lineNumber > 0)
                    EditorGUILayout.LabelField($"Line: {usage.lineNumber}", EditorStyles.miniLabel);

                if (showContext && !string.IsNullOrEmpty(usage.context))
                    EditorGUILayout.LabelField($"Context: {usage.context}", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawUsageReference Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawActionButtons()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🛠️ Actions:", EditorStyles.boldLabel);

                int selectedCount = assetInfoList.Count(a => a.isSelected);

                EditorGUILayout.BeginHorizontal();

                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button($"🗑️ Delete Selected ({selectedCount})", GUILayout.Height(25)))
                    DeleteSelectedAssetsSafe();
                GUI.backgroundColor = prev;

                if (GUILayout.Button("📊 Generate Report", GUILayout.Height(25)))
                    GenerateReport();

                if (GUILayout.Button("💾 Export CSV", GUILayout.Height(25)))
                    ExportToCSV();

                if (GUILayout.Button("🔄 Refresh", GUILayout.Height(25)))
                    BeginOptimizedSearch();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📋 Copy Paths", GUILayout.Height(25)))
                    CopySelectedPaths();

                if (GUILayout.Button("🎯 Select in Project", GUILayout.Height(25)))
                    SelectInProject();

                if (GUILayout.Button("📁 Show in Explorer", GUILayout.Height(25)))
                    ShowInExplorer();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawActionButtons Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawGettingStarted()
        {
            EditorGUILayout.HelpBox(
                "Optimized Asset Usage Finder\n\n" +
                "Find where your assets are used throughout the project:\n" +
                "• Select assets in the Project window\n" +
                "• Configure search settings\n" +
                "• Click 'Optimized Search' for best performance\n" +
                "• Use 'Warmup Cache' for faster subsequent searches\n\n" +
                "Performance Features:\n" +
                "• Dependency caching\n" +
                "• Compiled regex cache\n" +
                "• Parallel processing (I/O-safe paths)\n" +
                "• Memory pooling\n" +
                "• File metadata cache",
                MessageType.Info);
        }
    }
}
#endif
