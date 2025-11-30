#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class RandomAreaDuplicationTool
    {
        // Settings
        private static Vector3 center = Vector3.zero;
        private static float radius = 5f;
        private static int count = 20;
        private static bool avoidOverlap = true;
        private static float minDistance = 1f;
        private static bool snapToSurface = true;
        private static LayerMask snapLayers = 1;
        private static bool alignToSlope = true;
        private static float maxSlopeAngle = 45f;
        private static bool randomRotateOnSlope = true;

        // Advanced Options
        private static bool showAdvanced = false;
        private static bool randomRotation = false;
        private static Vector3 randomRotationRange = new Vector3(0, 360, 0);
        private static bool randomScale = false;
        private static Vector2 randomScaleRange = new Vector2(0.8f, 1.2f);
        private static bool createParentGroup = true;
        private static string parentName = "Random Area";
        private static string namingPattern = "{name}_{index}";
        private static bool alignToSurfaceNormal = false;

        // Scene View Integration
        private static bool sceneGUIActive = false;
        private static Vector2 scroll;
        private static int seed = 12345;

        // Source objects management
        private static GameObject[] sourceObjects = null;
        private static string[] sourceObjectNames = null;
        private static bool useLockedSources = false;

        // Track if center has been manually set
        private static bool centerManuallySet = false;

        // Preview positions cache
        private static List<Vector3> previewPositions = new List<Vector3>();
        private static int lastSeed = -1;
        private static float lastRadius = -1;
        private static int lastCount = -1;
        private static float lastMinDistance = -1;
        private static bool lastAvoidOverlap = false;

        public static void ResetTool()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGUIActive = false;

            sourceObjects = null;
            sourceObjectNames = null;
            useLockedSources = false;

            scroll = Vector2.zero;
            showAdvanced = false;
            centerManuallySet = false;

            previewPositions.Clear();
            lastSeed = -1;

            Debug.Log("Random Area Duplication Tool reset");
        }

        // Set current selection as source objects
        private static void CaptureCurrentSelection()
        {
            var currentSelection = Selection.gameObjects;
            if (currentSelection.Length > 0 && !IsGeneratedObject(currentSelection[0]))
            {
                sourceObjects = currentSelection.ToArray();
                sourceObjectNames = currentSelection.Select(go => go.name).ToArray();
                Debug.Log($"Source objects captured: {string.Join(", ", sourceObjectNames)}");
            }
        }

        // Update center based on source objects
        private static void UpdateCenterFromSources()
        {
            Vector3 newCenter = Vector3.zero;
            bool found = false;

            if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0)
            {
                newCenter = CalculateAveragePosition(sourceObjects);
                found = true;
            }
            else
            {
                var currentSelection = Selection.gameObjects;
                var validSelection = currentSelection.Where(go => !IsGeneratedObject(go)).ToArray();
                if (validSelection.Length > 0)
                {
                    newCenter = CalculateAveragePosition(validSelection);
                    found = true;
                }
            }

            if (found)
            {
                center = newCenter;
                centerManuallySet = false;
                Debug.Log($"Center updated to: {center}");
            }
        }

        private static Vector3 CalculateAveragePosition(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int validCount = 0;

            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    sum += obj.transform.position;
                    validCount++;
                }
            }

            return validCount > 0 ? sum / validCount : Vector3.zero;
        }

        // Better generated object detection
        private static bool IsGeneratedObject(GameObject obj)
        {
            if (obj == null) return false;

            string name = obj.name;
            if (name.EndsWith("(Clone)")) return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"_\d{2}$")) return true;

            var parent = obj.transform.parent;
            if (parent != null)
            {
                string parentName = parent.name;
                if (parentName.Contains("Array") || parentName.Contains("Random") ||
                    parentName.Contains("Linear") || parentName.Contains("Circular") ||
                    parentName.Contains("Grid") || parentName.Contains("Spline"))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetUniqueParentName(string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;
            while (GameObject.Find(uniqueName) != null)
            {
                uniqueName = $"{baseName} ({counter})";
                counter++;
            }
            return uniqueName;
        }

        private static int LayerMaskToLayer(LayerMask mask)
        {
            int v = mask.value, l = 0;
            while (v > 1) { v >>= 1; l++; }
            return l;
        }

        // Update preview positions when parameters change
        private static void UpdatePreviewPositions()
        {
            bool needsUpdate = (seed != lastSeed ||
                                radius != lastRadius ||
                                count != lastCount ||
                                minDistance != lastMinDistance ||
                                avoidOverlap != lastAvoidOverlap);

            if (needsUpdate)
            {
                previewPositions = GeneratePositions(center, radius, count, minDistance, avoidOverlap, seed);
                lastSeed = seed;
                lastRadius = radius;
                lastCount = count;
                lastMinDistance = minDistance;
                lastAvoidOverlap = avoidOverlap;
            }
        }
    }
}
#endif
