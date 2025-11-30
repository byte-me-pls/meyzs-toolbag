#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.Performance
{
    // Tüm tarama (scan) akışı
    public static partial class NullSafetyAnalyzerTool
    {
        private static void StartScan()
        {
            CancelScan();
            report = new List<NullSafetyIssue>();
            componentTypeStats = new Dictionary<string, int>();

            if (currentScanScope == ScanScope.EntireProject) StartProjectWideScan();
            else StartRegularScan();
        }

        private static void StartRegularScan()
        {
            targets = GetTargetsBasedOnScope();
            scanIndex = 0;
            scanProgress = 0f;
            isScanning = true;
            dataLoaded = false;
            scanStatus = "Initializing scan...";
            lastScanTime = DateTime.Now;
            totalScannedObjects = targets.Count;

            EditorApplication.update += UpdateScan;
        }

        private static List<MonoBehaviour> GetTargetsBasedOnScope()
        {
            List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

            switch (currentScanScope)
            {
                case ScanScope.ActiveScene:
                    allTargets.AddRange(UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(includeInactiveObjects)
                        .Where(mb => mb != null && mb.hideFlags == HideFlags.None));
                    break;

                case ScanScope.AllOpenScenes:
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded)
                        {
                            var sceneObjects = scene.GetRootGameObjects()
                                .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(includeInactiveObjects))
                                .Where(mb => mb != null && mb.hideFlags == HideFlags.None);
                            allTargets.AddRange(sceneObjects);
                        }
                    }
                    break;
            }

            return allTargets;
        }

        private static void UpdateScan()
        {
            int total = targets.Count;
            int end = Math.Min(scanIndex + BATCH_SIZE, total);

            for (; scanIndex < end; scanIndex++)
            {
                var mb = targets[scanIndex];
                if (mb == null) continue;

                scanStatus = $"Scanning {mb.GetType().Name} on {mb.gameObject.name}";
                ScanComponent(mb);
            }

            scanProgress = total > 0 ? (float)scanIndex / total : 1f;

            if (scanIndex >= total)
            {
                EditorApplication.update -= UpdateScan;
                isScanning = false;
                dataLoaded = true;
                scanStatus = $"Scan completed! Found {report.Count} issues in {targets.Count} components.";

                var severityCounts = report.GroupBy(i => i.severity).ToDictionary(g => g.Key, g => g.Count());
                Debug.Log($"Null Safety Scan Complete: {report.Count} total issues " +
                         $"(Critical: {severityCounts.GetValueOrDefault(SeverityLevel.Critical, 0)}, " +
                         $"High: {severityCounts.GetValueOrDefault(SeverityLevel.High, 0)}, " +
                         $"Medium: {severityCounts.GetValueOrDefault(SeverityLevel.Medium, 0)}, " +
                         $"Low: {severityCounts.GetValueOrDefault(SeverityLevel.Low, 0)})");
            }
        }

        private static void ScanComponent(MonoBehaviour component, string assetPath = "")
        {
            if (component == null) return;

            var so = new SerializedObject(component);
            var sp = so.GetIterator();
            var paths = new List<string>();
            var names = new List<string>();

            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference
                    && sp.objectReferenceValue == null
                    && !sp.hasVisibleChildren
                    && sp.name != "m_Script")
                {
                    paths.Add(sp.propertyPath);
                    names.Add(sp.displayName);
                }
            }

            if (paths.Count > 0)
            {
                var issue = new NullSafetyIssue(component, paths, names, assetPath);
                report.Add(issue);

                var typeName = component.GetType().Name;
                if (componentTypeStats.ContainsKey(typeName)) componentTypeStats[typeName]++;
                else componentTypeStats[typeName] = 1;
            }
        }

        // ===== Entire Project scan =====

        private static void StartProjectWideScan()
        {
            projectScanState = ProjectScanState.CollectingAssets;
            projectAssetPaths = new List<string>();
            projectAssetIndex = 0;
            projectStats = new ProjectScanStats();

            var currentScene = EditorSceneManager.GetActiveScene();
            currentlyLoadedScene = currentScene.path;
            originalSceneWasDirty = currentScene.isDirty;

            isScanning = true;
            dataLoaded = false;
            scanProgress = 0f;
            scanStatus = "Collecting project assets...";
            lastScanTime = DateTime.Now;
            projectStats.elapsedSeconds = 0f;

            EditorApplication.update += UpdateProjectScan;
        }

        private static void UpdateProjectScan()
        {
            projectStats.elapsedSeconds = (float)(DateTime.Now - lastScanTime).TotalSeconds;

            switch (projectScanState)
            {
                case ProjectScanState.CollectingAssets:            CollectProjectAssets(); break;
                case ProjectScanState.ScanningPrefabs:             UpdateProjectScanChunk("prefab"); break;
                case ProjectScanState.ScanningScenes:              UpdateProjectScanChunk("scene"); break;
                case ProjectScanState.ScanningScriptableObjects:   UpdateProjectScanChunk("scriptableobject"); break;
                case ProjectScanState.ScanningAnimationControllers:UpdateProjectScanChunk("animationcontroller"); break;
                case ProjectScanState.Complete:                    CompleteProjectScan(); break;
            }
        }

        private static void CollectProjectAssets()
        {
            projectAssetPaths.Clear();

            if (scanPrefabs)
            {
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!IsPathExcluded(path)) projectAssetPaths.Add($"prefab:{path}");
                }
            }

            if (scanScenes)
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                foreach (var guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!IsPathExcluded(path)) projectAssetPaths.Add($"scene:{path}");
                }
            }

            if (scanScriptableObjects)
            {
                var soGuids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var guid in soGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!IsPathExcluded(path)) projectAssetPaths.Add($"scriptableobject:{path}");
                }
            }

            if (scanAnimationControllers)
            {
                var animGuids = AssetDatabase.FindAssets("t:AnimatorController");
                foreach (var guid in animGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!IsPathExcluded(path)) projectAssetPaths.Add($"animationcontroller:{path}");
                }
            }

            projectStats.totalAssets = projectAssetPaths.Count;
            projectAssetIndex = 0;

            // Group by type
            var prefabs = projectAssetPaths.Where(p => p.StartsWith("prefab:")).ToList();
            var scenes = projectAssetPaths.Where(p => p.StartsWith("scene:")).ToList();
            var scriptableObjects = projectAssetPaths.Where(p => p.StartsWith("scriptableobject:")).ToList();
            var animControllers = projectAssetPaths.Where(p => p.StartsWith("animationcontroller:")).ToList();

            projectAssetPaths.Clear();
            projectAssetPaths.AddRange(prefabs);
            projectAssetPaths.AddRange(scenes);
            projectAssetPaths.AddRange(scriptableObjects);
            projectAssetPaths.AddRange(animControllers);

            scanStatus = $"Found {projectStats.totalAssets} assets to scan";
            projectScanState = ProjectScanState.ScanningPrefabs;
            totalScannedObjects = projectStats.totalAssets;
        }

        private static void UpdateProjectScanChunk(string assetType)
        {
            int chunkEnd = Mathf.Min(projectAssetIndex + PROJECT_SCAN_CHUNK_SIZE, projectAssetPaths.Count);

            for (; projectAssetIndex < chunkEnd; projectAssetIndex++)
            {
                string assetInfo = projectAssetPaths[projectAssetIndex];
                string[] parts = assetInfo.Split(':');
                string type = parts[0];
                string path = parts[1];

                if (!type.Equals(assetType)) continue;

                scanStatus = $"Scanning {type}: {Path.GetFileName(path)}";
                projectStats.scannedAssets++;

                try { ScanProjectAsset(type, path); }
                catch (Exception ex) { Debug.LogWarning($"Error scanning {path}: {ex.Message}"); }

                if (projectAssetIndex % 50 == 0) GC.Collect();
            }

            scanProgress = projectStats.totalAssets > 0 ? (float)projectStats.scannedAssets / projectStats.totalAssets : 1f;

            bool currentTypeComplete = projectAssetIndex >= projectAssetPaths.Count ||
                                       !projectAssetPaths[projectAssetIndex].StartsWith(assetType + ":");

            if (currentTypeComplete)
            {
                switch (assetType)
                {
                    case "prefab":             projectScanState = ProjectScanState.ScanningScenes; break;
                    case "scene":              projectScanState = ProjectScanState.ScanningScriptableObjects; break;
                    case "scriptableobject":   projectScanState = ProjectScanState.ScanningAnimationControllers; break;
                    case "animationcontroller":projectScanState = ProjectScanState.Complete; break;
                }
            }
        }

        private static void ScanProjectAsset(string assetType, string assetPath)
        {
            switch (assetType)
            {
                case "prefab":             ScanPrefab(assetPath);             projectStats.prefabsScanned++; break;
                case "scene":              ScanSceneFile(assetPath);          projectStats.scenesScanned++; break;
                case "scriptableobject":   ScanScriptableObject(assetPath);   projectStats.scriptableObjectsScanned++; break;
                case "animationcontroller":ScanAnimationController(assetPath);projectStats.animationControllersScanned++; break;
            }
        }

        private static void ScanPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(mb => mb != null && mb.hideFlags == HideFlags.None);

            foreach (var component in components) ScanComponent(component, prefabPath);
            Resources.UnloadAsset(prefab);
        }

        private static void ScanSceneFile(string scenePath)
        {
            if (scenePath == currentlyLoadedScene) return;

            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                if (scene.isLoaded)
                {
                    var sceneObjects = scene.GetRootGameObjects()
                        .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
                        .Where(mb => mb != null && mb.hideFlags == HideFlags.None);

                    foreach (var component in sceneObjects) ScanComponent(component, scenePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not scan scene {scenePath}: {ex.Message}");
            }
        }

        private static void ScanScriptableObject(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null) return;

            ScanScriptableObjectFields(asset, assetPath);
            Resources.UnloadAsset(asset);
        }

        private static void ScanScriptableObjectFields(ScriptableObject scriptableObject, string assetPath)
        {
            if (scriptableObject == null) return;

            var so = new SerializedObject(scriptableObject);
            var sp = so.GetIterator();
            var paths = new List<string>();
            var names = new List<string>();

            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference
                    && sp.objectReferenceValue == null
                    && !sp.hasVisibleChildren
                    && sp.name != "m_Script")
                {
                    paths.Add(sp.propertyPath);
                    names.Add(sp.displayName);
                }
            }

            if (paths.Count > 0)
            {
                var issue = new ScriptableObjectNullIssue(scriptableObject, paths, names, assetPath);
                report.Add(issue);

                var typeName = scriptableObject.GetType().Name;
                if (componentTypeStats.ContainsKey(typeName)) componentTypeStats[typeName]++;
                else componentTypeStats[typeName] = 1;
            }
        }

        private static void ScanAnimationController(string assetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(assetPath);
            if (controller == null) return;

            for (int i = 0; i < controller.layers.Length; i++)
            {
                var layer = controller.layers[i];
                var stateMachine = layer.stateMachine;

                foreach (var state in stateMachine.states)
                {
                    foreach (var behaviour in state.state.behaviours)
                    {
                        if (behaviour != null) ScanStateMachineBehaviour(behaviour, assetPath);
                    }
                }
            }

            Resources.UnloadAsset(controller);
        }

        private static void ScanStateMachineBehaviour(StateMachineBehaviour behaviour, string assetPath)
        {
            if (behaviour == null) return;

            var so = new SerializedObject(behaviour);
            var sp = so.GetIterator();
            var paths = new List<string>();
            var names = new List<string>();

            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference
                    && sp.objectReferenceValue == null
                    && !sp.hasVisibleChildren
                    && sp.name != "m_Script")
                {
                    paths.Add(sp.propertyPath);
                    names.Add(sp.displayName);
                }
            }

            if (paths.Count > 0)
            {
                var issue = new StateMachineBehaviourNullIssue(behaviour, paths, names, assetPath);
                report.Add(issue);

                var typeName = behaviour.GetType().Name;
                if (componentTypeStats.ContainsKey(typeName)) componentTypeStats[typeName]++;
                else componentTypeStats[typeName] = 1;
            }
        }

        private static void CompleteProjectScan()
        {
            if (!string.IsNullOrEmpty(currentlyLoadedScene))
            {
                try
                {
                    EditorSceneManager.OpenScene(currentlyLoadedScene, OpenSceneMode.Single);
                    if (originalSceneWasDirty) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not restore original scene: {ex.Message}");
                }
            }

            EditorApplication.update -= UpdateProjectScan;
            isScanning = false;
            dataLoaded = true;
            projectStats.elapsedSeconds = (float)(DateTime.Now - lastScanTime).TotalSeconds;
            scanStatus = $"Project scan completed! Found {report.Count} issues in {projectStats.scannedAssets} assets ({projectStats.elapsedSeconds:F1}s)";

            GC.Collect();

            var severityCounts = report.GroupBy(i => i.severity).ToDictionary(g => g.Key, g => g.Count());
            Debug.Log($"Project-wide Null Safety Scan Complete: {report.Count} total issues " +
                     $"(Critical: {severityCounts.GetValueOrDefault(SeverityLevel.Critical, 0)}, " +
                     $"High: {severityCounts.GetValueOrDefault(SeverityLevel.High, 0)}, " +
                     $"Medium: {severityCounts.GetValueOrDefault(SeverityLevel.Medium, 0)}, " +
                     $"Low: {severityCounts.GetValueOrDefault(SeverityLevel.Low, 0)}) " +
                     $"across {projectStats.scannedAssets} assets in {projectStats.elapsedSeconds:F1} seconds");
        }

        private static bool IsPathExcluded(string path) => excludedFolders.Any(folder => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase));

        private static void CancelScan()
        {
            if (isScanning)
            {
                EditorApplication.update -= UpdateScan;
                EditorApplication.update -= UpdateProjectScan;

                if (currentScanScope == ScanScope.EntireProject && !string.IsNullOrEmpty(currentlyLoadedScene))
                {
                    try
                    {
                        EditorSceneManager.OpenScene(currentlyLoadedScene, OpenSceneMode.Single);
                        if (originalSceneWasDirty) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not restore original scene after cancel: {ex.Message}");
                    }
                }
            }

            isScanning = false;
            scanStatus = "Scan cancelled";
            GC.Collect();
        }
    }
}
#endif
