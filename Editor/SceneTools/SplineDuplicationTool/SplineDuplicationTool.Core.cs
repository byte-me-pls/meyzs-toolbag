#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SplineDuplicationTool
    {
        private static string _dataAssetPath;
        private static SplineDataHolder dataHolder;
        private static System.Collections.Generic.List<Vector3> splinePoints => dataHolder.points;

        // Basic settings
        private static float spacing = 1f;
        private static bool alignToPath = true;
        private static bool loop = false;
        private static bool snapToSurface = true;
        private static bool alignToSurfaceNormal = false;
        private static AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
        private static LayerMask snapLayers = 1;

        // Slope support
        private static bool alignToSlope = false;
        private static float maxSlopeAngle = 45f;

        // Advanced options
        private static bool showAdvanced = false;
        private static bool randomRotation = false;
        private static Vector3 randomRotationRange = new Vector3(0, 360, 0);
        private static bool randomScale = false;
        private static Vector2 randomScaleRange = new Vector2(0.8f, 1.2f);
        private static bool createParentGroup = true;
        private static string parentName = "Spline Array";
        private static string namingPattern = "{name}_{index}";

        // Scene GUI state
        private static bool sceneGUIActive = false;
        private static bool editMode = false;
        private static int selectedPoint = -1;
        private static Vector2 scroll;

        // Source objects management
        private static GameObject[] sourceObjects = null;
        private static string[] sourceObjectNames = null;
        private static bool useLockedSources = false;

        /// Path for data asset
        private static string GetDataAssetPath()
        {
            if (!string.IsNullOrEmpty(_dataAssetPath))
                return _dataAssetPath;

            // Existing asset?
            string[] guids = AssetDatabase.FindAssets("t:SplineDataHolder");
            if (guids.Length > 0)
            {
                _dataAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return _dataAssetPath;
            }

            // Based on this script
            string[] scriptGuids = AssetDatabase.FindAssets("SplineDuplicationTool t:Script");
            if (scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string dir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                while (dir != "Assets" && !string.IsNullOrEmpty(dir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName.Contains("Toolbag") || dirName.Contains("ToolBag"))
                    {
                        _dataAssetPath = $"{dir}/Data/SplineDataHolder.asset";
                        return _dataAssetPath;
                    }
                    dir = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                }
            }

            _dataAssetPath = "Assets/Meyz'sToolBag/Data/SplineDataHolder.asset";
            return _dataAssetPath;
        }

        private static void EnsureDataHolder()
        {
            if (dataHolder != null) return;

            string dataAssetPath = GetDataAssetPath();

            // Ensure folders
            string folderPath = Path.GetDirectoryName(dataAssetPath).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                var parts = folderPath.Split('/');
                string cur = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{cur}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }

            // Load or create
            dataHolder = AssetDatabase.LoadAssetAtPath<SplineDataHolder>(dataAssetPath);
            if (dataHolder == null)
            {
                dataHolder = ScriptableObject.CreateInstance<SplineDataHolder>();
                AssetDatabase.CreateAsset(dataHolder, dataAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created SplineDataHolder asset at: {dataAssetPath}");
            }
        }

        // Reset tool
        public static void ResetTool()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGUIActive = false;
            editMode = false;
            selectedPoint = -1;

            sourceObjects = null;
            sourceObjectNames = null;
            useLockedSources = false;

            scroll = Vector2.zero;
            showAdvanced = false;

            Debug.Log("Spline Duplication Tool reset");
        }

        // Source capture
        private static void CaptureCurrentSelection()
        {
            var currentSelection = Selection.gameObjects;
            var validSelection = currentSelection.Where(go => go != null && !IsGeneratedObject(go)).ToArray();

            if (validSelection.Length > 0)
            {
                sourceObjects = validSelection;
                sourceObjectNames = validSelection.Select(go => go.name).ToArray();
                Debug.Log($"Source objects captured: {string.Join(", ", sourceObjectNames)}");
            }
            else
            {
                Debug.LogWarning("No valid source objects selected. Generated objects are ignored.");
            }
        }

        // Helpers
        private static bool IsGeneratedObject(GameObject obj)
        {
            if (obj == null) return false;
            string name = obj.name;
            if (name.EndsWith("(Clone)")) return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"_\d{2}$")) return true;

            var parent = obj.transform.parent;
            if (parent != null)
            {
                string pn = parent.name;
                if (pn.Contains("Array") || pn.Contains("Spline") || pn.Contains("Linear") ||
                    pn.Contains("Circular") || pn.Contains("Grid") || pn.Contains("Random"))
                    return true;
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

        private static string FormatName(string original, int idx)
            => namingPattern.Replace("{name}", original).Replace("{index}", (idx + 1).ToString("D2"));
    }
}
#endif
