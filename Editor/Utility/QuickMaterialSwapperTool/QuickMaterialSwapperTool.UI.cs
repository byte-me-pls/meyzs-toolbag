// QuickMaterialSwapperTool.UI.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        public static void Draw()
        {
            try
            {
                InitializeStyles();
                if (!presetsLoaded) LoadData();

                DrawHeader();
                DrawMaterialSelector();
                DrawApplySettings();
                DrawSelectionInfo();
                DrawActionButtons();

                if (showStatistics) DrawStatistics();
                DrawPresetManager();

                if (showOperationHistory) DrawOperationHistory();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error in Material Swapper: {e.Message}", MessageType.Error);
                Debug.LogError($"QuickMaterialSwapperTool Draw Error: {e}");
            }
        }

        private static void InitializeStyles()
        {
            if (redStyle != null) return;

            redStyle    = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
            greenStyle  = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
            yellowStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } };
            boldStyle   = new GUIStyle(EditorStyles.boldLabel);
        }

        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🎨 Advanced Material Swapper", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);
        }

        private static void DrawMaterialSelector()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🎯 Material Selection:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                chosenMaterial = (Material)EditorGUILayout.ObjectField("Target Material:", chosenMaterial, typeof(Material), false);

                if (chosenMaterial != null)
                {
                    if (GUILayout.Button("📌", GUILayout.Width(30)))
                        EditorGUIUtility.PingObject(chosenMaterial);
                    if (GUILayout.Button("💾", GUILayout.Width(30)))
                        QuickSavePreset(chosenMaterial);
                }
                EditorGUILayout.EndHorizontal();

                if (showPreview && chosenMaterial != null)
                    DrawMaterialPreview(chosenMaterial);

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawMaterialSelector Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawMaterialPreview(Material material)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Shader: {material.shader.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Render Queue: {material.renderQueue}", EditorStyles.miniLabel);

            if (material.HasProperty("_MainTex"))
            {
                var mainTex = material.GetTexture("_MainTex");
                EditorGUILayout.LabelField($"Main Texture: {(mainTex ? mainTex.name : "None")}", EditorStyles.miniLabel);
            }
            if (material.HasProperty("_Color"))
            {
                var color = material.GetColor("_Color");
                EditorGUILayout.LabelField($"Color: {ColorUtility.ToHtmlStringRGBA(color)}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("🔍 Inspect", GUILayout.Width(80)))
                Selection.activeObject = material;

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawApplySettings()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("⚙️ Apply Settings:", EditorStyles.boldLabel);
                if (GUILayout.Button(showAdvancedOptions ? "🔽" : "🔼", GUILayout.Width(30)))
                    showAdvancedOptions = !showAdvancedOptions;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mode:", GUILayout.Width(50));
                applyMode = (ApplyMode)EditorGUILayout.EnumPopup(applyMode, GUILayout.Width(140));

                if (applyMode == ApplyMode.ReplaceSpecific)
                {
                    EditorGUILayout.LabelField("Index:", GUILayout.Width(40));
                    specificMaterialIndex = EditorGUILayout.IntField(specificMaterialIndex, GUILayout.Width(40));
                }
                EditorGUILayout.EndHorizontal();

                if (showAdvancedOptions)
                {
                    EditorGUILayout.BeginHorizontal();
                    includeChildren = EditorGUILayout.ToggleLeft("Include Children", includeChildren, GUILayout.Width(140));
                    includeInactive = EditorGUILayout.ToggleLeft("Include Inactive", includeInactive, GUILayout.Width(140));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    autoCreateBackup = EditorGUILayout.ToggleLeft("Auto Backup", autoCreateBackup, GUILayout.Width(120));
                    smartUndo        = EditorGUILayout.ToggleLeft("Smart Undo", smartUndo, GUILayout.Width(120));
                    showPreview      = EditorGUILayout.ToggleLeft("Show Preview", showPreview, GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawApplySettings Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawSelectionInfo()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📦 Selection Info:", EditorStyles.boldLabel);

                var selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length == 0)
                {
                    EditorGUILayout.HelpBox("Select GameObjects to apply materials.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                var allRenderers = MaterialUtils.CollectRenderersFromSelection(selectedObjects, includeChildren, includeInactive);

                EditorGUILayout.LabelField($"Selected Objects: {selectedObjects.Length}");
                EditorGUILayout.LabelField($"Total Renderers: {allRenderers.Count}");

                var rendererGroups = allRenderers.GroupBy(r => r.GetType().Name).ToList();
                if (rendererGroups.Count > 0)
                {
                    EditorGUILayout.LabelField("Renderer Types:", EditorStyles.boldLabel);
                    foreach (var group in rendererGroups)
                        EditorGUILayout.LabelField($"• {group.Key}: {group.Count()}", EditorStyles.miniLabel);
                }

                var uniqueMaterials = allRenderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToList();
                if (uniqueMaterials.Count > 0)
                {
                    EditorGUILayout.LabelField($"Current Materials ({uniqueMaterials.Count}):", EditorStyles.boldLabel);
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(80));
                    foreach (var mat in uniqueMaterials.Take(10))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(mat.name, GUILayout.Width(150));
                        if (GUILayout.Button("📌", GUILayout.Width(25))) EditorGUIUtility.PingObject(mat);
                        if (GUILayout.Button("🔄", GUILayout.Width(25))) chosenMaterial = mat;
                        EditorGUILayout.EndHorizontal();
                    }
                    if (uniqueMaterials.Count > 10)
                        EditorGUILayout.LabelField($"... and {uniqueMaterials.Count - 10} more", EditorStyles.miniLabel);
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawSelectionInfo Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawActionButtons()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🛠️ Actions:", EditorStyles.boldLabel);

                var selectedObjects = Selection.gameObjects;
                bool canApply = chosenMaterial != null && selectedObjects.Length > 0;

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = canApply;
                GUI.backgroundColor = canApply ? Color.cyan : Color.gray;
                if (GUILayout.Button($"🎨 Apply Material ({applyMode})", GUILayout.Height(30)))
                    ApplyMaterialToSelection();
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;

                if (GUILayout.Button("🔍 Analyze", GUILayout.Height(30), GUILayout.Width(80)))
                    AnalyzeMaterials();
                EditorGUILayout.EndHorizontal();

                if (lastOperations.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button($"↶ Undo Last ({lastOperations.Count} changes)", GUILayout.Height(25)))
                        UndoLastOperation();
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("📋 Show Changes", GUILayout.Height(25), GUILayout.Width(120)))
                        showOperationHistory = !showOperationHistory;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 Swap Materials", GUILayout.Height(25)))
                    ShowMaterialSwapDialog();
                if (GUILayout.Button("🎯 Find Similar", GUILayout.Height(25)))
                    FindSimilarMaterials();
                if (GUILayout.Button("📊 Generate Report", GUILayout.Height(25)))
                    GenerateReport();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawActionButtons Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawStatistics()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📊 Statistics:", EditorStyles.boldLabel);
                if (GUILayout.Button(showStatistics ? "🔽" : "🔼", GUILayout.Width(30)))
                    showStatistics = !showStatistics;
                EditorGUILayout.EndHorizontal();

                if (operationHistory.Count == 0)
                {
                    EditorGUILayout.LabelField("No operations performed yet.", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    return;
                }

                var totalOperations  = operationHistory.Count;
                var uniqueNewMats    = operationHistory.Select(o => o.newMaterial).Where(m => m != null).Distinct().Count();
                var todaysOperations = operationHistory.Count(o => o.timestamp.Date == DateTime.Today);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Total Operations: {totalOperations}");
                EditorGUILayout.LabelField($"Today's Operations: {todaysOperations}");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Unique Materials Used: {uniqueNewMats}");
                EditorGUILayout.LabelField($"Saved Presets: {presets.Count}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                var materialUsage = operationHistory
                    .Where(o => o.newMaterial != null)
                    .GroupBy(o => o.newMaterial.name)
                    .OrderByDescending(g => g.Count())
                    .Take(3);
                if (materialUsage.Any())
                {
                    EditorGUILayout.LabelField("Most Used Materials:", EditorStyles.boldLabel);
                    foreach (var group in materialUsage)
                        EditorGUILayout.LabelField($"• {group.Key}: {group.Count()} times", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawStatistics Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawPresetManager()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("💾 Material Presets:", EditorStyles.boldLabel);
                viewMode = (ViewMode)EditorGUILayout.EnumPopup(viewMode, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(140));
                filterMode   = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(110));
                if (filterMode == FilterMode.ByTag)
                    tagFilter = EditorGUILayout.TextField(tagFilter, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                var filteredPresets = GetFilteredPresets();
                if (filteredPresets.Count == 0)
                {
                    EditorGUILayout.HelpBox(presets.Count == 0 ? "No presets saved." : "No presets match current filter.", MessageType.Info);
                }
                else
                {
                    presetScrollPos = EditorGUILayout.BeginScrollView(presetScrollPos, GUILayout.Height(150));
                    switch (viewMode)
                    {
                        case ViewMode.List:    DrawPresetList(filteredPresets); break;
                        case ViewMode.Grid:    DrawPresetGrid(filteredPresets); break;
                        case ViewMode.Compact: DrawPresetCompact(filteredPresets); break;
                    }
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = chosenMaterial != null;
                if (GUILayout.Button("💾 Save Preset")) ShowSavePresetDialog();
                GUI.enabled = true;

                GUI.enabled = selectedPreset != null;
                if (GUILayout.Button("🗑️ Delete")) DeletePreset(selectedPreset);
                if (GUILayout.Button("⭐ Toggle Favorite")) ToggleFavorite(selectedPreset);
                GUI.enabled = true;

                if (GUILayout.Button("📤 Export Presets")) ExportPresets();
                if (GUILayout.Button("📥 Import Presets")) ImportPresets();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawPresetManager Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawOperationHistory()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📋 Operation History:", EditorStyles.boldLabel);
                if (GUILayout.Button("❌", GUILayout.Width(25))) showOperationHistory = false;
                EditorGUILayout.EndHorizontal();

                if (operationHistory.Count == 0)
                {
                    EditorGUILayout.HelpBox("No operations recorded.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                operationScrollPos = EditorGUILayout.BeginScrollView(operationScrollPos, GUILayout.Height(150));
                foreach (var op in operationHistory.TakeLast(20).Reverse())
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.LabelField(op.timestamp.ToString("HH:mm:ss"), GUILayout.Width(60));
                    EditorGUILayout.LabelField(op.objectName, GUILayout.Width(120));
                    EditorGUILayout.LabelField($"{op.originalMaterial?.name ?? "None"} → {op.newMaterial?.name ?? "None"}", GUILayout.Width(220));
                    if (op.renderer != null && GUILayout.Button("📌", GUILayout.Width(25)))
                        EditorGUIUtility.PingObject(op.renderer.gameObject);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🗑️ Clear History"))
                {
                    operationHistory.Clear();
                    SaveData();
                }
                if (GUILayout.Button("📊 Export History"))
                    ExportHistory();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawOperationHistory Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch { }
            }
        }
    }
}
#endif
