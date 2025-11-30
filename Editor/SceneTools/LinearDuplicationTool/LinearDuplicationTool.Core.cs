#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class LinearDuplicationTool
    {
        private static string _dataAssetPath;
        private static LinearDataHolder dataHolder;

        // Data holder proxy
        private static int     count         => dataHolder.count;
        private static Vector3 offset        => dataHolder.offset;
        private static bool    useLocalSpace => dataHolder.useLocalSpace;

        // Surface snapping
        private static bool      snapToSurface        = false;
        private static LayerMask snapLayers           = 1;
        private static bool      alignToSlope         = false;
        private static float     maxSlopeAngle        = 45f;
        private static bool      alignToSurfaceNormal = false;

        // Advanced
        private static bool    showAdvanced          = false;
        private static bool    randomRotation        = false;
        private static Vector3  randomRotationRange  = new Vector3(0, 360, 0);
        private static bool    randomScale           = false;
        private static Vector2  randomScaleRange     = new Vector2(0.8f, 1.2f);
        private static bool    createParentGroup     = true;
        private static string  parentName            = "Linear Array";
        private static string  namingPattern         = "{name}_{index}";
        private static Vector2 scroll;

        // Scene
        private static bool sceneGUIActive  = false;
        private static bool showOffsetHandle = true;

        // Sources
        private static GameObject[] sourceObjects     = null;
        private static string[]     sourceObjectNames = null;
        private static bool         useLockedSources  = false;

        private static string GetDataAssetPath()
        {
            if (!string.IsNullOrEmpty(_dataAssetPath))
                return _dataAssetPath;

            // Try existing asset
            string[] guids = AssetDatabase.FindAssets("t:LinearDataHolder");
            if (guids.Length > 0)
            {
                _dataAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return _dataAssetPath;
            }

            // Infer from script
            string[] scriptGuids = AssetDatabase.FindAssets("LinearDuplicationTool t:Script");
            if (scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string dir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");

                while (dir != "Assets" && !string.IsNullOrEmpty(dir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName.Contains("Toolbag") || dirName.Contains("ToolBag"))
                    {
                        _dataAssetPath = $"{dir}/Data/LinearDataHolder.asset";
                        return _dataAssetPath;
                    }
                    dir = Path.GetDirectoryName(dir)?.Replace("\\", "/");
                }
            }

            // Fallback
            _dataAssetPath = "Assets/Meyz'sToolBag/Data/LinearDataHolder.asset";
            return _dataAssetPath;
        }

        public static void ResetTool()
        {
            sourceObjects = null;
            sourceObjectNames = null;
            useLockedSources = false;

            SceneView.duringSceneGui -= OnSceneGUI;
            sceneGUIActive = false;

            scroll = Vector2.zero;
            showAdvanced = false;

            Debug.Log("Linear Duplication Tool reset");
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

            // Load/Create
            dataHolder = AssetDatabase.LoadAssetAtPath<LinearDataHolder>(dataAssetPath);
            if (dataHolder == null)
            {
                dataHolder = ScriptableObject.CreateInstance<LinearDataHolder>();
                AssetDatabase.CreateAsset(dataHolder, dataAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created LinearDataHolder asset at: {dataAssetPath}");
            }
        }

        private static void CaptureCurrentSelection()
        {
            var current = Selection.gameObjects;
            var valid = current.Where(go => go != null && !IsGeneratedObject(go)).ToArray();

            if (valid.Length > 0)
            {
                sourceObjects = valid;
                sourceObjectNames = valid.Select(go => go.name).ToArray();
                Debug.Log($"Source objects captured: {string.Join(", ", sourceObjectNames)}");
            }
            else
            {
                Debug.LogWarning("No valid source objects selected. Generated objects are ignored.");
            }
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
                string parentName = parent.name;
                if (parentName.Contains("Array") || parentName.Contains("Spline") ||
                    parentName.Contains("Linear") || parentName.Contains("Circular") ||
                    parentName.Contains("Grid") || parentName.Contains("Random"))
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
    }
}
#endif
