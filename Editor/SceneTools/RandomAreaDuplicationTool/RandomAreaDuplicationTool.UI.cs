#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class RandomAreaDuplicationTool
    {
        public static void Draw()
        {
            GUILayout.Space(10);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🌳 Random Area Spawn Tool", header);
            EditorGUILayout.LabelField(GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(6);

            if (!useLockedSources && sourceObjects == null)
            {
                CaptureCurrentSelection();
                if (!centerManuallySet) UpdateCenterFromSources();
            }

            if (useLockedSources && sourceObjects != null && sourceObjects.Any(t => t == null))
            {
                EditorGUILayout.HelpBox("⚠️ Some source objects were deleted. Please select new source objects.", MessageType.Warning);
                sourceObjects = null; sourceObjectNames = null; useLockedSources = false;
                return;
            }

            var currentSelection = Selection.gameObjects;
            var validCurrentSelection = currentSelection.Where(go => !IsGeneratedObject(go)).ToArray();

            GameObject[] objectsToUse = null;
            string[] objectNamesToShow = null;

            if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0)
            {
                objectsToUse = sourceObjects;
                objectNamesToShow = sourceObjectNames;
            }
            else if (validCurrentSelection.Length > 0)
            {
                objectsToUse = validCurrentSelection;
                objectNamesToShow = validCurrentSelection.Select(go => go.name).ToArray();
            }

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
                    if (validCurrentSelection.Length > 0)
                    {
                        sourceObjects = validCurrentSelection;
                        sourceObjectNames = validCurrentSelection.Select(go => go.name).ToArray();
                        Debug.Log($"Source objects updated: {string.Join(", ", sourceObjectNames)}");
                        if (!centerManuallySet) UpdateCenterFromSources();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Selection", "Please select valid source objects (not generated objects).", "OK");
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
                    foreach (var go in validCurrentSelection.Take(5)) EditorGUILayout.LabelField($"  • {go.name}", EditorStyles.miniLabel);
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
            sceneGUIActive = EditorGUILayout.ToggleLeft("Enable Scene View Preview/Editing", sceneGUIActive);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                if (sceneGUIActive) SceneView.duringSceneGui += OnSceneGUI;
            }
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Center + button
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            center = EditorGUILayout.Vector3Field("Center", center);
            if (EditorGUI.EndChangeCheck()) centerManuallySet = true;
            if (GUILayout.Button("📍 Center on Selection", GUILayout.Width(140))) UpdateCenterFromSources();
            EditorGUILayout.EndHorizontal();

            // Radius & Count
            EditorGUI.BeginChangeCheck();
            radius = EditorGUILayout.FloatField("Radius", radius);
            count = EditorGUILayout.IntSlider("Count", count, 1, 100);
            if (EditorGUI.EndChangeCheck()) lastRadius = -1;

            // Distribution
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distribution:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            avoidOverlap = EditorGUILayout.ToggleLeft("Avoid Overlap", avoidOverlap);
            if (avoidOverlap)
            {
                minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
                EditorGUILayout.LabelField($"  Effective density: ~{Mathf.FloorToInt(Mathf.PI * radius * radius / (Mathf.PI * minDistance * minDistance))} max objects", EditorStyles.miniLabel);
            }
            if (EditorGUI.EndChangeCheck()) lastMinDistance = -1;

            // Snapping
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Surface Snapping:", EditorStyles.boldLabel);
            snapToSurface = EditorGUILayout.ToggleLeft("Snap to Surface", snapToSurface);
            if (snapToSurface)
            {
                EditorGUI.BeginChangeCheck();
                int layerIndex = 0;
                if (snapLayers.value > 0) layerIndex = Mathf.RoundToInt(Mathf.Log(snapLayers.value, 2));
                layerIndex = EditorGUILayout.LayerField("Snap Layers", layerIndex);
                if (EditorGUI.EndChangeCheck()) snapLayers = 1 << layerIndex;

                alignToSlope = EditorGUILayout.ToggleLeft("Align To Slope", alignToSlope);
                if (alignToSlope) maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", maxSlopeAngle, 0f, 90f);
                randomRotateOnSlope = EditorGUILayout.ToggleLeft("Random Y Rotation on Slope", randomRotateOnSlope);
            }

            // Seed
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            seed = EditorGUILayout.IntField("Random Seed", seed);
            if (EditorGUI.EndChangeCheck()) lastSeed = -1;

            // Presets
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Forest (50)")) { count = 50; radius = 10f; minDistance = 2f; alignToSlope = true; maxSlopeAngle = 30f; avoidOverlap = true; }
            if (GUILayout.Button("Rocks (30)")) { count = 30; radius = 8f; minDistance = 1.5f; alignToSlope = true; maxSlopeAngle = 60f; avoidOverlap = true; }
            if (GUILayout.Button("Dense Grass (100)")) { count = 100; radius = 5f; minDistance = 0.5f; alignToSlope = false; avoidOverlap = true; }
            EditorGUILayout.EndHorizontal();

            // Advanced
            GUILayout.Space(8);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "⚙ Advanced");
            if (showAdvanced)
            {
                GUILayout.Space(4);
                randomRotation = EditorGUILayout.ToggleLeft("Random Rotation", randomRotation);
                if (randomRotation) randomRotationRange = EditorGUILayout.Vector3Field("Rotation Range", randomRotationRange);
                randomScale = EditorGUILayout.ToggleLeft("Random Scale", randomScale);
                if (randomScale) randomScaleRange = EditorGUILayout.Vector2Field("Scale Range", randomScaleRange);

                GUILayout.Space(6);
                createParentGroup = EditorGUILayout.ToggleLeft("Create Parent Group", createParentGroup);
                if (createParentGroup) parentName = EditorGUILayout.TextField("Parent Name", parentName);
                namingPattern = EditorGUILayout.TextField("Naming Pattern", namingPattern);
                EditorGUILayout.LabelField("{name} = original, {index} = sequence number", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();

            // Generate / Undo
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            bool canGenerate = objectsToUse != null && objectsToUse.Length > 0;
            GUI.enabled = canGenerate;
            GUI.backgroundColor = canGenerate ? Color.green : Color.gray;

            if (GUILayout.Button(canGenerate ? "🚀 Generate" : "❌ No Sources", GUILayout.Height(30)))
            {
                Undo.IncrementCurrentGroup();
                string undoName = "Generate Random Area";
                Undo.SetCurrentGroupName(undoName);
                int undoGroup = Undo.GetCurrentGroup();

                try
                {
                    GenerateRandomArea(objectsToUse);
                    Debug.Log($"Generated random area with {objectsToUse.Length} source types: {string.Join(", ", objectNamesToShow)}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error generating random area: {e.Message}");
                    Undo.RevertAllDownToGroup(undoGroup);
                }
                finally
                {
                    Undo.CollapseUndoOperations(undoGroup);
                }
            }
            GUI.enabled = true;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("⤺ Undo Last", GUILayout.Height(30)))
            {
                EditorApplication.delayCall += () => Undo.PerformUndo();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif
