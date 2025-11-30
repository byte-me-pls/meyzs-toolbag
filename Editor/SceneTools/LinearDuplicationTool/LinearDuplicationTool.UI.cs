#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class LinearDuplicationTool
    {
        public static void Draw()
        {
            EnsureDataHolder();

            GUILayout.Space(10);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("📏 Linear Duplication Tool", header);
            EditorGUILayout.LabelField(GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(6);

            if (!useLockedSources && sourceObjects == null)
                CaptureCurrentSelection();

            if (useLockedSources && sourceObjects != null && sourceObjects.Any(t => t == null))
            {
                EditorGUILayout.HelpBox("⚠️ Some source objects were deleted. Please select new source objects.", MessageType.Warning);
                sourceObjects = null; sourceObjectNames = null; useLockedSources = false;
                return;
            }

            var currentSelection = Selection.gameObjects;
            var validCurrentSelection = currentSelection.Where(go => go != null && !IsGeneratedObject(go)).ToArray();

            // Sources UI
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("📦 Source Objects:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            useLockedSources = EditorGUILayout.ToggleLeft("🔒 Use Locked Sources", useLockedSources);
            if (EditorGUI.EndChangeCheck())
            {
                if (useLockedSources && validCurrentSelection.Length > 0)
                {
                    sourceObjects = validCurrentSelection;
                    sourceObjectNames = validCurrentSelection.Select(go => go.name).ToArray();
                    Debug.Log($"Sources locked: {string.Join(", ", sourceObjectNames)}");
                }
                EditorWindow.focusedWindow?.Repaint();
            }

            if (useLockedSources)
            {
                if (sourceObjects != null && sourceObjects.Length > 0)
                {
                    EditorGUILayout.LabelField("Locked Sources:", EditorStyles.miniLabel);
                    foreach (var name in sourceObjectNames)
                        EditorGUILayout.LabelField($"  • {name}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("No locked sources - will capture current selection", EditorStyles.miniLabel);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📸 Capture Current Selection"))
                {
                    var valid = validCurrentSelection.Where(go => go && !IsGeneratedObject(go)).ToArray();
                    if (valid.Length > 0)
                    {
                        sourceObjects = valid;
                        sourceObjectNames = valid.Select(go => go.name).ToArray();
                        Debug.Log($"Source objects updated: {string.Join(", ", sourceObjectNames)}");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Selection",
                            "Please select valid source objects (not generated objects like clones or arrays).", "OK");
                    }
                }
                if (GUILayout.Button("🗑️ Clear"))
                {
                    sourceObjects = null;
                    sourceObjectNames = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Using Current Selection (Live):", EditorStyles.miniLabel);
                if (validCurrentSelection.Length > 0)
                {
                    EditorGUILayout.LabelField($"  Count: {validCurrentSelection.Length}", EditorStyles.miniLabel);
                    foreach (var go in validCurrentSelection.Take(5))
                        EditorGUILayout.LabelField($"  • {go.name}", EditorStyles.miniLabel);
                    if (validCurrentSelection.Length > 5)
                        EditorGUILayout.LabelField($"  ... and {validCurrentSelection.Length - 5} more", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("  No valid objects selected", EditorStyles.miniLabel);
                }

                if (currentSelection.Length > validCurrentSelection.Length)
                {
                    int invalidCount = currentSelection.Length - validCurrentSelection.Length;
                    EditorGUILayout.LabelField($"  ({invalidCount} generated objects ignored)", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            sceneGUIActive = EditorGUILayout.ToggleLeft("Enable Scene View Preview", sceneGUIActive);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                if (sceneGUIActive) SceneView.duringSceneGui += OnSceneGUI;
            }
            if (sceneGUIActive)
            {
                showOffsetHandle = EditorGUILayout.ToggleLeft("  Show Offset Handle", showOffsetHandle);
            }
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Basic params
            EditorGUI.BeginChangeCheck();
            int newCount = EditorGUILayout.IntSlider("Count", dataHolder.count, 2, 50);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Count"); dataHolder.count = newCount; }

            EditorGUI.BeginChangeCheck();
            Vector3 newOffset = EditorGUILayout.Vector3Field("Offset", dataHolder.offset);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Offset"); dataHolder.offset = newOffset; }

            EditorGUI.BeginChangeCheck();
            bool newUseLocal = EditorGUILayout.ToggleLeft("Use Local Space", dataHolder.useLocalSpace);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Local Space"); dataHolder.useLocalSpace = newUseLocal; }

            GUILayout.Space(8);

            // Snapping
            EditorGUILayout.LabelField("Surface Snapping:", EditorStyles.boldLabel);
            snapToSurface = EditorGUILayout.ToggleLeft("Snap to Surface", snapToSurface);
            if (snapToSurface)
            {
                alignToSurfaceNormal = EditorGUILayout.ToggleLeft("Align to Surface Normal", alignToSurfaceNormal);
                EditorGUI.BeginChangeCheck();
                int layer = LayerMaskToLayer(snapLayers);
                layer = EditorGUILayout.LayerField("Snap Layers", layer);
                if (EditorGUI.EndChangeCheck()) snapLayers = 1 << layer;

                alignToSlope = EditorGUILayout.ToggleLeft("Align To Slope", alignToSlope);
                if (alignToSlope) maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", maxSlopeAngle, 0f, 90f);
            }
            GUILayout.Space(6);

            // Presets
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X Axis")) { Undo.RecordObject(dataHolder, "Set X Axis Offset"); dataHolder.offset = new Vector3(2, 0, 0); }
            if (GUILayout.Button("Y Axis")) { Undo.RecordObject(dataHolder, "Set Y Axis Offset"); dataHolder.offset = new Vector3(0, 2, 0); }
            if (GUILayout.Button("Z Axis")) { Undo.RecordObject(dataHolder, "Set Z Axis Offset"); dataHolder.offset = new Vector3(0, 0, 2); }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Advanced
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "⚙ Advanced");
            if (showAdvanced)
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Randomization:", EditorStyles.boldLabel);
                randomRotation = EditorGUILayout.ToggleLeft("Random Rotation", randomRotation);
                if (randomRotation) randomRotationRange = EditorGUILayout.Vector3Field("Rotation Range", randomRotationRange);

                randomScale = EditorGUILayout.ToggleLeft("Random Scale", randomScale);
                if (randomScale) randomScaleRange = EditorGUILayout.Vector2Field("Scale Range", randomScaleRange);

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Organization:", EditorStyles.boldLabel);
                createParentGroup = EditorGUILayout.ToggleLeft("Create Parent Group", createParentGroup);
                if (createParentGroup) parentName = EditorGUILayout.TextField("Parent Name", parentName);

                namingPattern = EditorGUILayout.TextField("Naming Pattern", namingPattern);
                EditorGUILayout.LabelField("{name} = original, {index} = sequence number", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();

            // Generate / Undo
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            bool canGenerate = useLockedSources && sourceObjects != null && sourceObjects.Length > 0;
            GUI.enabled = canGenerate;
            GUI.backgroundColor = canGenerate ? Color.green : Color.gray;

            string buttonText = !useLockedSources ? "🔒 Lock Sources First"
                : (sourceObjects == null || sourceObjects.Length == 0) ? "❌ No Sources"
                : "🚀 Generate";

            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0)
                {
                    Undo.IncrementCurrentGroup();
                    string undoName = "Generate Linear Array";
                    Undo.SetCurrentGroupName(undoName);
                    int group = Undo.GetCurrentGroup();

                    try
                    {
                        var generated = GenerateLinearArray(sourceObjects);
                        Debug.Log($"Generated {generated.Count} objects in linear array from locked sources: {string.Join(", ", sourceObjectNames)}");

                        if (generated.Count > 0)
                        {
                            if (createParentGroup)
                            {
                                string uniqueParentName = GetUniqueParentName(parentName);
                                var parent = new GameObject(uniqueParentName);
                                Undo.RegisterCreatedObjectUndo(parent, undoName);
                                foreach (var go in generated) go.transform.SetParent(parent.transform);
                                Selection.activeGameObject = parent;
                            }
                            else
                            {
                                Selection.objects = generated.ToArray();
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error generating linear array: {e.Message}");
                        Undo.RevertAllDownToGroup(group);
                    }
                    finally
                    {
                        Undo.CollapseUndoOperations(group);
                    }
                }
            }
            GUI.enabled = true;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("⤺ Undo Last", GUILayout.Height(30)))
            {
                EditorApplication.delayCall += () => Undo.PerformUndo();
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
