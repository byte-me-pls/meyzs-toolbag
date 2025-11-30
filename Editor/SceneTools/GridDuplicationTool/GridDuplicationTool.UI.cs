#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class GridDuplicationTool
    {
        public static void Draw()
        {
            EnsureDataHolder();

            GUILayout.Space(10);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🔲 Grid Duplication Tool", header);
            EditorGUILayout.LabelField(GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(6);

            // first use: capture
            if (!useLockedSources && sourceObjects == null)
                CaptureCurrentSelection();

            // validate locked set
            if (useLockedSources && sourceObjects != null && sourceObjects.Any(t => t == null))
            {
                EditorGUILayout.HelpBox("⚠️ Some source objects were deleted. Please select new source objects.", MessageType.Warning);
                sourceObjects = null; sourceObjectNames = null; useLockedSources = false;
                return;
            }

            var currentSelection = Selection.gameObjects;
            var validCurrentSelection = currentSelection.Where(go => go && !IsGeneratedObject(go)).ToArray();

            // source management
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
                    foreach (var n in sourceObjectNames)
                        EditorGUILayout.LabelField($"  • {n}", EditorStyles.miniLabel);
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
                showGridHandle = EditorGUILayout.ToggleLeft("  Show Grid Handle", showGridHandle);
            }
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // basic params
            EditorGUI.BeginChangeCheck();
            Vector2Int newGridCount = EditorGUILayout.Vector2IntField("Grid Count (X, Z)", dataHolder.gridCount);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Grid Count"); dataHolder.gridCount = newGridCount; }

            EditorGUI.BeginChangeCheck();
            Vector2 newGridSpacing = EditorGUILayout.Vector2Field("Spacing (X, Z)", dataHolder.gridSpacing);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Grid Spacing"); dataHolder.gridSpacing = newGridSpacing; }

            EditorGUI.BeginChangeCheck();
            bool newAlternateRows = EditorGUILayout.ToggleLeft("Alternate Rows (Brick Pattern)", dataHolder.alternateRows);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataHolder, "Change Alternate Rows"); dataHolder.alternateRows = newAlternateRows; }

            GUILayout.Space(8);

            // snapping
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

            // quick presets
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("3x3"))  { Undo.RecordObject(dataHolder, "Set 3x3 Grid");  dataHolder.gridCount = new Vector2Int(3, 3); }
            if (GUILayout.Button("5x5"))  { Undo.RecordObject(dataHolder, "Set 5x5 Grid");  dataHolder.gridCount = new Vector2Int(5, 5); }
            if (GUILayout.Button("10x10")){ Undo.RecordObject(dataHolder, "Set 10x10 Grid");dataHolder.gridCount = new Vector2Int(10, 10); }
            EditorGUILayout.EndHorizontal();

            int total = dataHolder.gridCount.x * dataHolder.gridCount.y;
            EditorGUILayout.HelpBox($"Will create {total} objects in {dataHolder.gridCount.x}x{dataHolder.gridCount.y} grid", MessageType.Info);

            // advanced
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

            // generate / undo
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
                    string undoName = "Generate Grid Array";
                    Undo.SetCurrentGroupName(undoName);
                    int group = Undo.GetCurrentGroup();

                    try
                    {
                        var generated = GenerateGridArray(sourceObjects);
                        Debug.Log($"Generated {generated.Count} objects in grid array from locked sources: {string.Join(", ", sourceObjectNames)}");

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
                        Debug.LogError($"Error generating grid array: {e.Message}");
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
