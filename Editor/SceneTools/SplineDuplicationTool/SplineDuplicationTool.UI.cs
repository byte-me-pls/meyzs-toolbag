#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SplineDuplicationTool
    {
        public static void Draw()
        {
            EnsureDataHolder();

            GUILayout.Space(10);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🛤️ Spline Duplication Tool", header);
            EditorGUILayout.LabelField(GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(6);

            // Auto-capture sources if none
            if (!useLockedSources && sourceObjects == null) CaptureCurrentSelection();

            // Validate locked sources
            if (useLockedSources && sourceObjects != null && sourceObjects.Any(t => t == null))
            {
                EditorGUILayout.HelpBox("⚠️ Some source objects were deleted. Please select new source objects.", MessageType.Warning);
                sourceObjects = null; sourceObjectNames = null; useLockedSources = false;
                return;
            }

            var currentSelection = Selection.gameObjects;
            var validCurrentSelection = currentSelection.Where(go => go != null && !IsGeneratedObject(go)).ToArray();

            // Source block
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
                    var validSelection = validCurrentSelection.Where(go => go != null && !IsGeneratedObject(go)).ToArray();
                    if (validSelection.Length > 0)
                    {
                        sourceObjects = validSelection;
                        sourceObjectNames = validSelection.Select(go => go.name).ToArray();
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
                    sourceObjects = null; sourceObjectNames = null;
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
                else EditorGUILayout.LabelField("  No valid objects selected", EditorStyles.miniLabel);

                if (currentSelection.Length > validCurrentSelection.Length)
                {
                    int invalidCount = currentSelection.Length - validCurrentSelection.Length;
                    EditorGUILayout.LabelField($"  ({invalidCount} generated objects ignored)", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            sceneGUIActive = EditorGUILayout.ToggleLeft("Enable Scene View Editing & Preview", sceneGUIActive);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                if (sceneGUIActive) SceneView.duringSceneGui += OnSceneGUI;
            }
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (editMode)
            {
                EditorGUILayout.HelpBox("Editing Mode: use Scene View to modify points. Other controls are disabled.", MessageType.Info);
                GUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Finish Editing", GUILayout.Height(30))) { editMode = false; selectedPoint = -1; }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⤺ Undo Last", GUILayout.Height(30)))
                {
                    EditorApplication.delayCall += () => Undo.PerformUndo();
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                return;
            }

            // Basic parameters
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
            alignToPath = EditorGUILayout.ToggleLeft("Align to Path Direction", alignToPath);
            loop = EditorGUILayout.ToggleLeft("Loop Path", loop);
            GUILayout.Space(8);

            // Surface snapping
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
            GUILayout.Space(8);

            // Scale curve
            EditorGUILayout.LabelField("Scale Along Path:", EditorStyles.boldLabel);
            scaleCurve = EditorGUILayout.CurveField("Scale Curve", scaleCurve);
            GUILayout.Space(8);

            // Edit / Clear
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = sceneGUIActive;
            if (!editMode)
            {
                if (GUILayout.Button("Start Editing")) { editMode = true; selectedPoint = -1; }
            }
            else
            {
                if (GUILayout.Button("Finish Editing")) { editMode = false; selectedPoint = -1; }
            }
            if (GUILayout.Button("Clear Path"))
            {
                Undo.RecordObject(dataHolder, "Clear Spline Points");
                splinePoints.Clear();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Points: {splinePoints.Count}", EditorStyles.miniLabel);
            GUILayout.Space(6);

            // Quick presets
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Straight Line"))
            {
                Undo.RecordObject(dataHolder, "Create Straight Line");
                splinePoints.Clear();
                CreateStraightLine();
            }
            if (GUILayout.Button("Circle Path"))
            {
                Undo.RecordObject(dataHolder, "Create Circle Path");
                splinePoints.Clear();
                CreateCirclePath();
            }
            if (GUILayout.Button("S-Curve"))
            {
                Undo.RecordObject(dataHolder, "Create S-Curve");
                splinePoints.Clear();
                CreateSCurve();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.HelpBox(
                splinePoints.Count < 2
                    ? "Add at least two points to generate an array"
                    : $"Path Length: {CalculateLength():F2} units, Estimated: {Mathf.FloorToInt(CalculateLength()/spacing)+1} items",
                MessageType.Info
            );

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

            // Generate & Undo
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            bool canGenerate = useLockedSources && sourceObjects != null && sourceObjects.Length > 0 && splinePoints.Count >= 2;

            GUI.enabled = canGenerate;
            GUI.backgroundColor = canGenerate ? Color.green : Color.gray;

            string buttonText;
            if (!useLockedSources) buttonText = "🔒 Lock Sources First";
            else if (sourceObjects == null || sourceObjects.Length == 0) buttonText = "❌ No Sources";
            else if (splinePoints.Count < 2) buttonText = "❌ Need Points";
            else buttonText = "🚀 Generate";

            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0 && splinePoints.Count >= 2)
                {
                    Undo.IncrementCurrentGroup();
                    string undoName = $"Generate Spline Array";
                    Undo.SetCurrentGroupName(undoName);
                    int undoGroup = Undo.GetCurrentGroup();

                    try
                    {
                        var generated = GenerateSplineArray(sourceObjects);
                        Debug.Log($"Generated {generated.Count} objects along spline from locked sources: {string.Join(", ", sourceObjectNames)}");

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
                            else Selection.objects = generated.ToArray();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error generating spline array: {e.Message}");
                        Undo.RevertAllDownToGroup(undoGroup);
                    }
                    finally
                    {
                        Undo.CollapseUndoOperations(undoGroup);
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
