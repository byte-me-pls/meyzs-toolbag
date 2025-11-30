#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Transform
{
    public static partial class PivotChangeTool
    {
        public static void Draw()
        {
            LoadSettings();

            // Header
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🔧 Enhanced Pivot Editor", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);

            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorGUILayout.HelpBox("Select one or more GameObjects to edit pivots.", MessageType.Info);
                return;
            }

            // Selection info
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📦 Selected: {selected.Length} objects", EditorStyles.boldLabel);
            var validCount = selected.Count(go => go.GetComponent<MeshFilter>() != null);
            if (validCount != selected.Length)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"⚠ {validCount} have MeshFilter", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            DrawModeAndSpaceSelection();
            DrawQuickPivotButtons();
            DrawAdvancedOptions();
            DrawPresetSystem();
            DrawManualPivotControl();
            DrawSceneViewIntegration();

            if (selected.Length > 10)
                EditorGUILayout.HelpBox($"Large selection ({selected.Length}). Consider 'Combined' mode for performance.", MessageType.Info);

            SaveSettings();
        }

        private static void DrawModeAndSpaceSelection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("⚙ Mode & Space", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pivot Mode:", GUILayout.Width(80));
            currentPivotMode = (PivotMode)EditorGUILayout.EnumPopup(currentPivotMode);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Space:", GUILayout.Width(80));
            currentCoordinateSpace = (CoordinateSpace)EditorGUILayout.EnumPopup(currentCoordinateSpace);
            EditorGUILayout.EndHorizontal();

            switch (currentPivotMode)
            {
                case PivotMode.Individual: EditorGUILayout.HelpBox("Each object gets its own pivot based on its bounds.", MessageType.None); break;
                case PivotMode.Combined:   EditorGUILayout.HelpBox("All objects share a pivot based on combined bounds.", MessageType.None); break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawQuickPivotButtons()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🧭 Quick Pivot Positions", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Primary Positions:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Center")) ApplyPivotPosition(PivotPosition.Center);
            if (GUILayout.Button("Bottom")) ApplyPivotPosition(PivotPosition.Bottom);
            if (GUILayout.Button("Top"))    ApplyPivotPosition(PivotPosition.Top);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Left"))  ApplyPivotPosition(PivotPosition.Left);
            if (GUILayout.Button("Right")) ApplyPivotPosition(PivotPosition.Right);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Front")) ApplyPivotPosition(PivotPosition.Front);
            if (GUILayout.Button("Back"))  ApplyPivotPosition(PivotPosition.Back);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Combined Positions:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bottom Center")) ApplyPivotPosition(PivotPosition.BottomCenter);
            if (GUILayout.Button("Top Center"))    ApplyPivotPosition(PivotPosition.TopCenter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Left Center"))  ApplyPivotPosition(PivotPosition.LeftCenter);
            if (GUILayout.Button("Right Center")) ApplyPivotPosition(PivotPosition.RightCenter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Front Center")) ApplyPivotPosition(PivotPosition.FrontCenter);
            if (GUILayout.Button("Back Center"))  ApplyPivotPosition(PivotPosition.BackCenter);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Extreme Positions:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Min Corner")) ApplyPivotPosition(PivotPosition.BoundsMin);
            if (GUILayout.Button("Max Corner")) ApplyPivotPosition(PivotPosition.BoundsMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawAdvancedOptions()
        {
            EditorGUILayout.BeginHorizontal();
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "🔧 Advanced Options", true);
            EditorGUILayout.EndHorizontal();

            if (!showAdvancedOptions) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            preserveChildPositions = EditorGUILayout.ToggleLeft("Preserve Child Positions", preserveChildPositions);
            updateColliders        = EditorGUILayout.ToggleLeft("Update Colliders", updateColliders);
            createBackup           = EditorGUILayout.ToggleLeft("Create Undo Backup", createBackup);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Options:", EditorStyles.boldLabel);
            showPivotPreview  = EditorGUILayout.ToggleLeft("Show Pivot Preview", showPivotPreview);
            showBounds        = EditorGUILayout.ToggleLeft("Show Bounds", showBounds);
            showOriginalPivot = EditorGUILayout.ToggleLeft("Show Original Pivot", showOriginalPivot);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Handle Size:", GUILayout.Width(80));
            handleSize = EditorGUILayout.Slider(handleSize, 0.5f, 3f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preview Color:", GUILayout.Width(80));
            pivotPreviewColor = EditorGUILayout.ColorField(pivotPreviewColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawPresetSystem()
        {
            EditorGUILayout.BeginHorizontal();
            showPresets = EditorGUILayout.Foldout(showPresets, "💾 Pivot Presets", true);
            EditorGUILayout.EndHorizontal();

            if (!showPresets) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("Create New Preset:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newPresetName = EditorGUILayout.TextField("Name:", newPresetName);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(newPresetName)))
            {
                if (GUILayout.Button("Save Current", GUILayout.Width(100)))
                {
                    SaveCurrentAsPreset(newPresetName);
                    newPresetName = "";
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (pivotPresets.Count > 0)
            {
                EditorGUILayout.LabelField("Saved Presets:", EditorStyles.boldLabel);
                for (int i = 0; i < pivotPresets.Count; i++)
                {
                    var preset = pivotPresets[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(preset.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"({preset.position:F2})", EditorStyles.miniLabel);

                    if (GUILayout.Button("Apply", GUILayout.Width(50))) ApplyPreset(preset);

                    GUI.color = Color.red;
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        pivotPresets.RemoveAt(i);
                        SavePresets();
                        GUI.color = Color.white;
                        break;
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No presets saved yet.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawManualPivotControl()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🎯 Manual Pivot Control", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (!sceneGUIActive)
            {
                EditorGUILayout.HelpBox("Enable Scene View Integration to use manual pivot editing.", MessageType.Info);
            }
            else
            {
                if (!isPivotEditing)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Start Pivot Editing", GUILayout.Height(30))) StartPivotEditing();
                    if (GUILayout.Button("Reset to Object Center", GUILayout.Height(30))) ResetToObjectCenter();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.green;
                    if (GUILayout.Button("✓ Apply Pivot", GUILayout.Height(35)))
                    {
                        ApplyCustomPivot();
                        isPivotEditing = false;
                        SceneView.RepaintAll();
                    }
                    GUI.color = Color.red;
                    if (GUILayout.Button("✗ Cancel", GUILayout.Height(35)))
                    {
                        isPivotEditing = false;
                        SceneView.RepaintAll();
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    EditorGUI.BeginChangeCheck();
                    customPivotPos = EditorGUILayout.Vector3Field("Pivot Position", customPivotPos);
                    if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

                    EditorGUILayout.LabelField("Quick Snap:", EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Snap to Grid"))
                    {
                        customPivotPos = SnapToGrid(customPivotPos);
                        SceneView.RepaintAll();
                    }
                    if (GUILayout.Button("Snap to Vertices")) SnapToNearestVertex();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.HelpBox("🖱 Drag the handle in Scene View.\n🔧 Use snap buttons for precision.", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private static void DrawSceneViewIntegration()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("👁 Scene View Integration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sceneGUIActive = EditorGUILayout.ToggleLeft("Enable Scene View Integration", sceneGUIActive);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                if (sceneGUIActive) SceneView.duringSceneGui += OnSceneGUI;
                else isPivotEditing = false;
                SceneView.RepaintAll();
            }

            if (sceneGUIActive)
            {
                EditorGUILayout.LabelField("Features:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• Interactive pivot positioning", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• Visual bounds display", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• Real-time preview", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        // ----- UI helpers -----
        private static void StartPivotEditing()
        {
            isPivotEditing = true;
            var selected = Selection.gameObjects;
            if (selected.Length > 0)
            {
                customPivotPos = (currentPivotMode == PivotMode.Combined)
                    ? GetCombinedBounds(selected).center
                    : Selection.activeTransform.position;
            }
            SceneView.RepaintAll();
        }

        private static void ResetToObjectCenter()
        {
            var selected = Selection.gameObjects.Where(go => go.GetComponent<MeshFilter>() != null).ToArray();
            if (selected.Length == 0) return;

            if (createBackup)
                Undo.RecordObjects(selected.Select(go => go.transform).ToArray(), "Reset Pivot to Center");

            foreach (var go in selected)
            {
                var r = go.GetComponent<Renderer>();
                if (!r) continue;
                AdjustPivot(go, r.bounds.center);
            }

            Debug.Log($"Reset pivot to center for {selected.Length} objects");
        }

        private static void SaveCurrentAsPreset(string name)
        {
            if (!isPivotEditing) return;
            var preset = new PivotPreset(name, customPivotPos, PivotPosition.Center, currentCoordinateSpace == CoordinateSpace.World);
            pivotPresets.Add(preset);
            SavePresets();
            Debug.Log($"Saved pivot preset: {name}");
        }

        private static void ApplyPreset(PivotPreset preset)
        {
            if (preset.isWorldSpace)
            {
                customPivotPos = preset.position;
                if (!isPivotEditing) StartPivotEditing(); else SceneView.RepaintAll();
            }
            else
            {
                ApplyPivotPosition(preset.pivotType);
            }
        }
    }
}
#endif
