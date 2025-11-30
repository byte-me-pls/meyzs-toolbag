#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class CircularDuplicationTool
    {
        public static void Draw()
        {
            EnsureDataHolder();

            GUILayout.Space(10);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("⭕ Circular Duplication Tool", header);
            EditorGUILayout.LabelField(GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(6);

            // First-use: capture + center
            if (!useLockedSources && sourceObjects == null)
            {
                CaptureCurrentSelection();
                if (!centerManuallySet) UpdateCenterFromSources();
            }

            // Validate locked
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
                    if (!centerManuallySet) UpdateCenterFromSources();
                }
                EditorWindow.focusedWindow?.Repaint();
            }

            if (useLockedSources)
            {
                if (sourceObjects != null && sourceObjects.Length > 0)
                {
                    EditorGUILayout.LabelField("Locked Sources:", EditorStyles.miniLabel);
                    foreach (var n in sourceObjectNames) EditorGUILayout.LabelField($"  • {n}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("No locked sources - will capture current selection", EditorStyles.miniLabel);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📸 Capture Current Selection"))
                {
                    var valid = validCurrentSelection.Where(go => go != null && !IsGeneratedObject(go)).ToArray();
                    if (valid.Length > 0)
                    {
                        sourceObjects = valid;
                        sourceObjectNames = valid.Select(go => go.name).ToArray();
                        Debug.Log($"Source objects updated: {string.Join(", ", sourceObjectNames)}");
                        if (!centerManuallySet) UpdateCenterFromSources();
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
                    int invalid = currentSelection.Length - validCurrentSelection.Length;
                    EditorGUILayout.LabelField($"  ({invalid} generated objects ignored)", EditorStyles.miniLabel);
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
                showRadiusHandle = EditorGUILayout.ToggleLeft("  Show Radius Handle", showRadiusHandle);
                showCenterHandle = EditorGUILayout.ToggleLeft("  Show Center Handle", showCenterHandle);
            }
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Basic parameters (Undo-friendly)
            EditorGUI.BeginChangeCheck();
            int newCount = EditorGUILayout.IntSlider("Count", dataHolder.count, 2, 50);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Count"); dataHolder.count = newCount; }

            EditorGUI.BeginChangeCheck();
            float newRadius = EditorGUILayout.FloatField("Radius", dataHolder.radius);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Radius"); dataHolder.radius = newRadius; }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = EditorGUILayout.Vector3Field("Center", dataHolder.center);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataHolder, "Change Center");
                dataHolder.center = newCenter;
                centerManuallySet = true;
            }
            if (GUILayout.Button("📍 Center on Selection", GUILayout.Width(140)))
            {
                UpdateCenterFromSources();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            float newStartAngle = EditorGUILayout.Slider("Start Angle", dataHolder.startAngle, 0f, 360f);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Start Angle"); dataHolder.startAngle = newStartAngle; }

            EditorGUI.BeginChangeCheck();
            float newEndAngle = EditorGUILayout.Slider("End Angle", dataHolder.endAngle, 0f, 360f);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change End Angle"); dataHolder.endAngle = newEndAngle; }

            EditorGUI.BeginChangeCheck();
            bool newFaceCenter = EditorGUILayout.ToggleLeft("Face Center", dataHolder.faceCenter);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Face Center"); dataHolder.faceCenter = newFaceCenter; }

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

            // Quick presets
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected Center"))
            {
                var objectsToUse = useLockedSources && sourceObjects != null && sourceObjects.Length > 0
                    ? sourceObjects : validCurrentSelection;
                if (objectsToUse != null && objectsToUse.Length > 0)
                {
                    Undo.RecordObject(dataHolder, "Set Selected Center");
                    dataHolder.center = objectsToUse[0].transform.position;
                    centerManuallySet = false;
                }
            }
            if (GUILayout.Button("Use Scene Center"))
            {
                var objectsToUse = useLockedSources && sourceObjects != null && sourceObjects.Length > 0
                    ? sourceObjects : validCurrentSelection;
                if (objectsToUse != null && objectsToUse.Length > 0)
                {
                    Undo.RecordObject(dataHolder, "Set Scene Center");
                    Vector3 sum = Vector3.zero;
                    foreach (var obj in objectsToUse) sum += obj.transform.position;
                    dataHolder.center = sum / objectsToUse.Length;
                    centerManuallySet = false;
                }
                else
                {
                    Undo.RecordObject(dataHolder, "Set World Center");
                    dataHolder.center = Vector3.zero;
                    centerManuallySet = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Full Circle"))
            { Undo.RecordObject(dataHolder, "Set Full Circle"); dataHolder.startAngle = 0; dataHolder.endAngle = 360; }
            if (GUILayout.Button("Half Circle"))
            { Undo.RecordObject(dataHolder, "Set Half Circle"); dataHolder.startAngle = 0; dataHolder.endAngle = 180; }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                $"Creating {dataHolder.count} duplicates from {dataHolder.startAngle}° to {dataHolder.endAngle}° on radius {dataHolder.radius}.",
                MessageType.Info);

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

            string btn;
            if (!useLockedSources) btn = "🔒 Lock Sources First";
            else if (sourceObjects == null || sourceObjects.Length == 0) btn = "❌ No Sources";
            else btn = "🚀 Generate";

            if (GUILayout.Button(btn, GUILayout.Height(30)))
            {
                if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0)
                {
                    Undo.IncrementCurrentGroup();
                    string undoName = "Generate Circular Array";
                    Undo.SetCurrentGroupName(undoName);
                    int group = Undo.GetCurrentGroup();

                    try
                    {
                        var generated = GenerateCircularArray(sourceObjects);
                        Debug.Log($"Generated {generated.Count} objects in circular array from locked sources: {string.Join(", ", sourceObjectNames)}");

                        if (generated.Count > 0)
                        {
                            if (createParentGroup)
                            {
                                string unique = GetUniqueParentName(parentName);
                                var parent = new GameObject(unique);
                                Undo.RegisterCreatedObjectUndo(parent, undoName);
                                foreach (var go in generated) go.transform.SetParent(parent.transform);
                                Selection.activeGameObject = parent;
                            }
                            else Selection.objects = generated.ToArray();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error generating circular array: {e.Message}");
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
