#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class CircularDuplicationTool
    {
        private static string _dataAssetPath;
        private static CircularDataHolder dataHolder;

        // Basic settings (proxy from data holder)
        private static int    count      => dataHolder.count;
        private static float  radius     => dataHolder.radius;
        private static Vector3 center    => dataHolder.center;
        private static float  startAngle => dataHolder.startAngle;
        private static float  endAngle   => dataHolder.endAngle;
        private static bool   faceCenter => dataHolder.faceCenter;

        // Surface snapping
        private static bool      snapToSurface        = false;
        private static LayerMask snapLayers           = 1;
        private static bool      alignToSlope         = false;
        private static float     maxSlopeAngle        = 45f;
        private static bool      alignToSurfaceNormal = false;

        // Advanced options
        private static bool   showAdvanced         = false;
        private static bool   randomRotation       = false;
        private static Vector3 randomRotationRange = new Vector3(0, 360, 0);
        private static bool   randomScale          = false;
        private static Vector2 randomScaleRange    = new Vector2(0.8f, 1.2f);
        private static bool   createParentGroup    = true;
        private static string parentName           = "Circular Array";
        private static string namingPattern        = "{name}_{index}";

        // Scene GUI
        private static bool     sceneGUIActive   = false;
        private static bool     showRadiusHandle = true;
        private static bool     showCenterHandle = true;
        private static Vector2  scroll;

        // Sources
        private static GameObject[] sourceObjects     = null;
        private static string[]     sourceObjectNames = null;
        private static bool         useLockedSources  = false;

        // Center tracking
        private static bool centerManuallySet = false;

        private static string GetDataAssetPath()
        {
            if (!string.IsNullOrEmpty(_dataAssetPath))
                return _dataAssetPath;

            // existing asset?
            string[] guids = AssetDatabase.FindAssets("t:CircularDataHolder");
            if (guids.Length > 0)
            {
                _dataAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return _dataAssetPath;
            }

            // infer from script location
            string[] scriptGuids = AssetDatabase.FindAssets("CircularDuplicationTool t:Script");
            if (scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string dir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
                while (dir != "Assets" && !string.IsNullOrEmpty(dir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName.Contains("Toolbag") || dirName.Contains("ToolBag"))
                    {
                        _dataAssetPath = $"{dir}/Data/CircularDataHolder.asset";
                        return _dataAssetPath;
                    }
                    dir = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                }
            }

            // fallback
            _dataAssetPath = "Assets/Meyz'sToolBag/Data/CircularDataHolder.asset";
            return _dataAssetPath;
        }

        public static void ResetTool()
        {
            // sources
            sourceObjects = null;
            sourceObjectNames = null;
            useLockedSources = false;

            // scene gui
            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGUIActive = false;

            // ui state
            scroll = Vector2.zero;
            showAdvanced = false;
            centerManuallySet = false;

            Debug.Log("Circular Duplication Tool reset");
        }

        private static void EnsureDataHolder()
        {
            if (dataHolder != null) return;

            string dataAssetPath = GetDataAssetPath();

            // ensure folders
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

            // load or create
            dataHolder = AssetDatabase.LoadAssetAtPath<CircularDataHolder>(dataAssetPath);
            if (dataHolder == null)
            {
                dataHolder = ScriptableObject.CreateInstance<CircularDataHolder>();
                AssetDatabase.CreateAsset(dataHolder, dataAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created CircularDataHolder asset at: {dataAssetPath}");
            }
        }

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
        }

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
                Undo.RecordObject(dataHolder, "Update Center from Sources");
                dataHolder.center = newCenter;
                centerManuallySet = false;
                Debug.Log($"Center updated to: {newCenter}");
            }
        }

        private static Vector3 CalculateAveragePosition(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int valid = 0;
            foreach (var obj in objects)
            {
                if (!obj) continue;
                sum += obj.transform.position;
                valid++;
            }
            return valid > 0 ? sum / valid : Vector3.zero;
        }

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
            string unique = baseName;
            int i = 1;
            while (GameObject.Find(unique) != null)
            {
                unique = $"{baseName} ({i})";
                i++;
            }
            return unique;
        }

        private static int LayerMaskToLayer(LayerMask mask)
        {
            int v = mask.value, l = 0;
            while (v > 1) { v >>= 1; l++; }
            return l;
        }
    }
}
#endif
