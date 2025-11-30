#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    // Çekirdek durum + tipler + yardımcılar
    public static partial class NullSafetyAnalyzerTool
    {
        private const int BATCH_SIZE = 15;
        private const int PROJECT_SCAN_CHUNK_SIZE = 5;

        // Asset
        private static string _dataAssetPath;
        private static NullSafetyIgnoredComponentsData dataHolder;

        // UI state
        private static Vector2 scrollPos;
        private static Vector2 ignoredScrollPos;
        private static bool showIgnoredDrawer;
        private static bool showAdvancedFilters;
        private static bool showStatistics;
        private static bool showProjectScanSettings;

        // Scan state
        private static List<NullSafetyIssue> report;
        private static bool isScanning;
        private static bool dataLoaded;
        private static float scanProgress;
        private static List<MonoBehaviour> targets;
        private static int scanIndex;
        private static string scanStatus = "";

        // Project-wide scan state
        private static ProjectScanState projectScanState;
        private static List<string> projectAssetPaths;
        private static int projectAssetIndex;
        private static string currentlyLoadedScene;
        private static bool originalSceneWasDirty;

        // Filters
        private static string componentTypeFilter = "";
        private static string gameObjectFilter = "";
        private static bool includeInactiveObjects = true;
        private static SeverityLevel minSeverityLevel = SeverityLevel.Low;
        private static ScanScope currentScanScope = ScanScope.ActiveScene;

        // Project scan settings
        private static bool scanPrefabs = true;
        private static bool scanScenes = true;
        private static bool scanScriptableObjects = true;
        private static bool scanAnimationControllers = false;
        private static List<string> excludedFolders = new List<string> { "Packages/", "Library/" };

        // Auto-fix
        private static bool enableAutoFix = false;
        private static Dictionary<string, UnityEngine.Object> autoFixMappings = new Dictionary<string, UnityEngine.Object>();

        // Statistics
        private static Dictionary<string, int> componentTypeStats = new Dictionary<string, int>();
        private static DateTime lastScanTime;
        private static int totalScannedObjects;
        private static ProjectScanStats projectStats = new ProjectScanStats();

        public enum SeverityLevel { Low, Medium, High, Critical }
        public enum ScanScope { ActiveScene, AllOpenScenes, EntireProject }
        private enum ProjectScanState { None, CollectingAssets, ScanningPrefabs, ScanningScenes, ScanningScriptableObjects, ScanningAnimationControllers, Complete }

        [Serializable]
        private class ProjectScanStats
        {
            public int totalAssets;
            public int scannedAssets;
            public int prefabsScanned;
            public int scenesScanned;
            public int scriptableObjectsScanned;
            public int animationControllersScanned;
            public long memoryUsageBytes;
            public float elapsedSeconds;
        }

        [Serializable]
        public class NullSafetyIssue
        {
            public MonoBehaviour component;
            public List<string> propertyPaths;
            public List<string> propertyNames;
            public SeverityLevel severity;
            public string description;
            public bool canAutoFix;
            public string assetPath; // Project scan için
            public virtual UnityEngine.Object TargetObject => component;
            public virtual string ObjectName => component?.gameObject.name ?? "Unknown";
            public virtual string ObjectTypeName => component?.GetType().Name ?? "Unknown";

            public NullSafetyIssue(MonoBehaviour mb, List<string> paths, List<string> names, string assetPath = "")
            {
                component = mb;
                propertyPaths = paths;
                propertyNames = names;
                severity = CalculateSeverity(mb?.GetType(), paths);
                description = GenerateDescription(mb?.GetType(), paths);
                canAutoFix = CanBeAutoFixed(mb?.GetType(), paths);
                this.assetPath = assetPath;
            }

            protected virtual SeverityLevel CalculateSeverity(Type objectType, List<string> paths)
            {
                if (IsEssentialType(objectType) && paths.Count > 3) return SeverityLevel.Critical;
                if (IsEssentialType(objectType) || paths.Count > 2) return SeverityLevel.High;
                if (paths.Count > 1) return SeverityLevel.Medium;
                return SeverityLevel.Low;
            }

            protected virtual bool IsEssentialType(Type objectType)
            {
                if (objectType == null) return false;
                return objectType.Name.Contains("Controller") ||
                       objectType.Name.Contains("Manager") ||
                       objectType.Name.Contains("Handler") ||
                       objectType.Namespace?.Contains("UnityEngine.UI") == true;
            }

            protected virtual string GenerateDescription(Type objectType, List<string> paths)
            {
                var typeName = objectType?.Name ?? "Unknown";
                return paths.Count == 1 ? $"{typeName} missing 1 reference" : $"{typeName} missing {paths.Count} references";
            }

            protected virtual bool CanBeAutoFixed(Type objectType, List<string> paths)
            {
                if (objectType == null) return false;
                var typeName = objectType.Name;
                return typeName.Contains("Image") || typeName.Contains("Text") || typeName.Contains("Button");
            }
        }

        [Serializable]
        public class ScriptableObjectNullIssue : NullSafetyIssue
        {
            public ScriptableObject scriptableObject;
            public override UnityEngine.Object TargetObject => scriptableObject;
            public override string ObjectName => scriptableObject?.name ?? "Unknown";
            public override string ObjectTypeName => scriptableObject?.GetType().Name ?? "Unknown";

            public ScriptableObjectNullIssue(ScriptableObject so, List<string> paths, List<string> names, string assetPath = "")
                : base(null, paths, names, assetPath)
            {
                scriptableObject = so;
                severity = CalculateSeverity(so?.GetType(), paths);
                description = GenerateDescription(so?.GetType(), paths);
                canAutoFix = CanBeAutoFixed(so?.GetType(), paths);
            }
        }

        [Serializable]
        public class StateMachineBehaviourNullIssue : NullSafetyIssue
        {
            public StateMachineBehaviour stateMachineBehaviour;
            public override UnityEngine.Object TargetObject => stateMachineBehaviour;
            public override string ObjectName => stateMachineBehaviour?.GetType().Name ?? "Unknown";
            public override string ObjectTypeName => stateMachineBehaviour?.GetType().Name ?? "Unknown";

            public StateMachineBehaviourNullIssue(StateMachineBehaviour smb, List<string> paths, List<string> names, string assetPath = "")
                : base(null, paths, names, assetPath)
            {
                stateMachineBehaviour = smb;
                severity = CalculateSeverity(smb?.GetType(), paths);
                description = GenerateDescription(smb?.GetType(), paths);
                canAutoFix = CanBeAutoFixed(smb?.GetType(), paths);
            }
        }

        // ===== Asset path & data holder =====
        private static string GetDataAssetPath()
        {
            if (!string.IsNullOrEmpty(_dataAssetPath)) return _dataAssetPath;

            string[] guids = AssetDatabase.FindAssets("t:NullSafetyIgnoredComponentsData");
            if (guids.Length > 0)
            {
                _dataAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return _dataAssetPath;
            }

            _dataAssetPath = "Assets/MeyzsToolBag/Data/NullSafetyIgnoredComponents.asset";
            return _dataAssetPath;
        }

        private static void EnsureDataHolder()
        {
            if (dataHolder != null)
            {
                if (AssetDatabase.Contains(dataHolder)) return;
                dataHolder = null;
            }

            string dataAssetPath = GetDataAssetPath();
            dataHolder = AssetDatabase.LoadAssetAtPath<NullSafetyIgnoredComponentsData>(dataAssetPath);
            if (dataHolder != null) return;

            string folderPath = Path.GetDirectoryName(dataAssetPath).Replace("\\", "/");
            CreateFoldersIfNeeded(folderPath);

            dataHolder = ScriptableObject.CreateInstance<NullSafetyIgnoredComponentsData>();
            try
            {
                AssetDatabase.CreateAsset(dataHolder, dataAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                dataHolder = AssetDatabase.LoadAssetAtPath<NullSafetyIgnoredComponentsData>(dataAssetPath);
                if (dataHolder == null) Debug.LogError($"Failed to load newly created asset at: {dataAssetPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create data asset: {ex.Message}");
            }
        }

        private static void CreateFoldersIfNeeded(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            var parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string result = AssetDatabase.CreateFolder(currentPath, parts[i]);
                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError($"Failed to create folder: {nextPath}");
                        return;
                    }
                }
                currentPath = nextPath;
            }
        }

        // ===== Filters & helpers used by UI =====
        private static List<NullSafetyIssue> GetFilteredReport()
        {
            var filtered = report.Where(issue =>
                issue.TargetObject != null &&
                (issue.component == null || !dataHolder.IsIgnored(issue.component.GetInstanceID())) &&
                issue.severity >= minSeverityLevel);

            if (!string.IsNullOrEmpty(componentTypeFilter))
                filtered = filtered.Where(issue => issue.ObjectTypeName.IndexOf(componentTypeFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(gameObjectFilter))
                filtered = filtered.Where(issue => issue.ObjectName.IndexOf(gameObjectFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            return filtered.ToList();
        }

        private static Color GetSeverityColor(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Critical: return Color.red;
                case SeverityLevel.High:     return new Color(1f, 0.5f, 0f);
                case SeverityLevel.Medium:   return Color.yellow;
                case SeverityLevel.Low:      return Color.green;
                default: return Color.white;
            }
        }

        private static string GetSeverityIcon(SeverityLevel severity)
        {
            switch (severity)
            {
                case SeverityLevel.Critical: return "💥";
                case SeverityLevel.High:     return "🔴";
                case SeverityLevel.Medium:   return "🟡";
                case SeverityLevel.Low:      return "🟢";
                default: return "⚪";
            }
        }

        // ===== Public helpers =====
        public static void StartQuickScan() { currentScanScope = ScanScope.ActiveScene; includeInactiveObjects = false; StartScan(); }
        public static void StartDeepScan()  { currentScanScope = ScanScope.AllOpenScenes; includeInactiveObjects = true; StartScan(); }
        public static void StartProjectScan(){ currentScanScope = ScanScope.EntireProject; includeInactiveObjects = true; StartScan(); }
        public static List<NullSafetyIssue> GetCurrentIssues() => report?.ToList() ?? new List<NullSafetyIssue>();
        public static void IgnoreComponent(MonoBehaviour component, string reason = "") { EnsureDataHolder(); dataHolder.AddIgnoredComponent(component, reason); }

        private static void PerformAutoFix(List<NullSafetyIssue> issues)
        {
            int fixedCount = 0;
            foreach (var issue in issues.Where(i => i.canAutoFix))
            {
                Debug.Log($"Auto-fixing {issue.component.GetType().Name} on {issue.component.gameObject.name}");
                fixedCount++;
            }

            if (fixedCount > 0)
            {
                Debug.Log($"Auto-fixed {fixedCount} issues. Please rescan to verify.");
                StartScan();
            }
        }

        public static void ExportReport()
        {
            if (report == null || report.Count == 0)
            {
                Debug.LogWarning("No report data to export. Please run a scan first.");
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"NullSafetyReport_{timestamp}.csv";
            string path = EditorUtility.SaveFilePanel("Export Null Safety Report", "", fileName, "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("GameObject,Component,Severity,Issue Count,Missing Properties,Scene,Asset Path");

                    foreach (var issue in report)
                    {
                        if (issue.component == null) continue;

                        var sceneName  = issue.component.gameObject.scene.name;
                        var properties = string.Join("; ", issue.propertyNames);

                        writer.WriteLine($"\"{issue.component.gameObject.name}\"," +
                                         $"\"{issue.component.GetType().Name}\"," +
                                         $"{issue.severity}," +
                                         $"{issue.propertyPaths.Count}," +
                                         $"\"{properties}\"," +
                                         $"\"{sceneName}\"," +
                                         $"\"{issue.assetPath}\"");
                    }
                }

                Debug.Log($"Null Safety Report exported to: {path}");
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export report: {ex.Message}");
            }
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= UpdateScan;
            EditorApplication.update -= UpdateProjectScan;
        }
    }
}
#endif
