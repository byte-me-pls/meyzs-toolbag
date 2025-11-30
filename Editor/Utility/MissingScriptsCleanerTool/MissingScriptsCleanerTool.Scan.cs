#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Scanning pipeline: start/cancel/update, prepare targets, live/asset passes.
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        private static void StartScan()
        {
            if (isScanning) return;

            try
            {
                EditorUtility.DisplayProgressBar("Missing Scripts Cleaner", "Preparing...", 0f);

                foundObjects.Clear();
                isScanning = true;
                hasScanned = false;
                scanProgress = 0f;
                scanStatus = "Preparing scan targets...";
                currentScanIndex = 0;
                processedWorkUnits = 0;
                filesToScan.Clear();

                validScriptGuids = BuildValidScriptGuidSet();

                PrepareScanTargets();
                ComputeTotalWorkUnits();

                lastFrameTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += UpdateScan;
            }
            catch (Exception e)
            {
                Debug.LogError($"StartScan Error: {e}");
                isScanning = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private static void QuickScan()
        {
            foundObjects.Clear();
            hasScanned = true;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                    ScanGameObjectRecursively(root, scene.name, false, scene.path);
            }

            if (autoSelectAll) SelectAll(true);
            Debug.Log($"Quick scan complete: Found {foundObjects.Count} objects with missing scripts");
        }

        private static void PrepareScanTargets()
        {
            scanStatus = "Preparing scan targets...";
            List<string> candidateAssets = null;

            switch (scanMode)
            {
                case ScanMode.CurrentScene:
                case ScanMode.AllOpenScenes:
                    break;

                case ScanMode.AllScenesInBuild:
                {
                    candidateAssets = EditorBuildSettings.scenes
                        .Where(s => s.enabled)
                        .Select(s => s.path)
                        .Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    break;
                }

                case ScanMode.ProjectPrefabs:
                {
                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    candidateAssets = new List<string>(prefabGuids.Length);
                    for (int i = 0; i < prefabGuids.Length; i++)
                        candidateAssets.Add(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
                    break;
                }

                case ScanMode.Everything:
                {
                    var buildScenes = EditorBuildSettings.scenes
                        .Where(s => s.enabled)
                        .Select(s => s.path)
                        .Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));

                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    var prefabs = new List<string>(prefabGuids.Length);
                    for (int i = 0; i < prefabGuids.Length; i++)
                        prefabs.Add(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));

                    candidateAssets = new List<string>();
                    candidateAssets.AddRange(buildScenes);
                    candidateAssets.AddRange(prefabs);
                    break;
                }
            }

            if (candidateAssets != null && candidateAssets.Count > 0)
            {
                int flagged = 0;
                for (int i = 0; i < candidateAssets.Count; i++)
                {
                    string assetPath = candidateAssets[i];
                    if (FastPathFileHasMissingScripts(assetPath))
                    {
                        filesToScan.Add(assetPath);
                        flagged++;
                    }
                }

                Debug.Log($"Fast-path: {flagged}/{candidateAssets.Count} asset flaglendi (yalnızca bunlar detay taranacak).");
            }
        }

        private static void ComputeTotalWorkUnits()
        {
            int liveScenePass = (scanMode == ScanMode.CurrentScene || scanMode == ScanMode.AllOpenScenes) ? 1 : 0;
            totalWorkUnits = filesToScan.Count + liveScenePass;
            if (totalWorkUnits == 0) totalWorkUnits = 1;
        }

        private static void UpdateScan()
        {
            try
            {
                double now = EditorApplication.timeSinceStartup;
                double delta = now - lastFrameTime;
                lastFrameTime = now;

                if (delta > 0.020) itemsPerFrame = Math.Max(4, itemsPerFrame - 1);
                else if (delta < 0.006) itemsPerFrame = Math.Min(32, itemsPerFrame + 1);

                int processedThisFrame = 0;

                bool hasLivePass = (scanMode == ScanMode.CurrentScene || scanMode == ScanMode.AllOpenScenes);
                if (hasLivePass && currentScanIndex == 0)
                {
                    ScanCurrentScenesLive();
                    processedWorkUnits++;
                    currentScanIndex = 1;
                    UpdateProgressUI("Scanning open scenes...");
                }

                int AssetIndex() => (hasLivePass ? currentScanIndex - 1 : currentScanIndex);

                while (AssetIndex() < filesToScan.Count && processedThisFrame < itemsPerFrame)
                {
                    int idx = AssetIndex();
                    string path = filesToScan[idx];

                    scanStatus = $"Scanning {Path.GetFileName(path)}";

                    if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                        ScanSceneFile(path);
                    else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                        ScanPrefabFile(path);

                    processedWorkUnits++;
                    currentScanIndex++;
                    processedThisFrame++;

                    UpdateProgressUI($"Scanning {Path.GetFileName(path)}");
                }

                if (processedWorkUnits >= totalWorkUnits)
                {
                    CompleteScan();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Scan Error: {e}");
                CancelScan();
            }
        }

        private static void UpdateProgressUI(string status)
        {
            scanStatus = status;
            scanProgress = Mathf.Clamp01(totalWorkUnits == 0 ? 1f : (float)processedWorkUnits / totalWorkUnits);
            EditorUtility.DisplayProgressBar("Missing Scripts Cleaner", scanStatus, scanProgress);
        }

        private static void ScanCurrentScenesLive()
        {
            if (scanMode == ScanMode.CurrentScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded)
                {
                    foreach (var root in scene.GetRootGameObjects())
                        ScanGameObjectRecursively(root, scene.name, false, scene.path);
                }
            }
            else if (scanMode == ScanMode.AllOpenScenes)
            {
                int sceneCount = EditorSceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    foreach (var root in scene.GetRootGameObjects())
                        ScanGameObjectRecursively(root, scene.name, false, scene.path);
                }
            }
        }

        private static void ScanSceneFile(string scenePath)
        {
            Scene opened = default;
            try
            {
                opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                if (opened.IsValid() && opened.isLoaded)
                {
                    foreach (var root in opened.GetRootGameObjects())
                        ScanGameObjectRecursively(root, opened.name, false, scenePath);
                }
            }
            finally
            {
                if (opened.IsValid())
                    EditorSceneManager.CloseScene(opened, true);
            }
        }

        private static void ScanPrefabFile(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            ScanGameObjectRecursively(prefab, prefabPath, true, prefabPath);
        }

        private static void ScanGameObjectRecursively(GameObject go, string sceneOrAssetName, bool isPrefab, string assetOrScenePath)
        {
            var comps = ListPool<Component>.Get();
            try
            {
                go.GetComponents(comps);
                int nullCount = 0;
                for (int i = 0; i < comps.Count; i++)
                    if (comps[i] == null)
                        nullCount++;

                if (nullCount > 0)
                {
                    var info = new MissingScriptInfo
                    {
                        gameObject = go,
                        path = isPrefab ? assetOrScenePath : GetGameObjectPath(go.transform),
                        sceneName = sceneOrAssetName,
                        isPrefab = isPrefab,
                        missingCount = nullCount,
                        isSelected = autoSelectAll,
                        lastModified = TryGetAssetWriteTime(assetOrScenePath),
                        displayLocationCached = isPrefab ? Path.GetFileName(assetOrScenePath) : sceneOrAssetName
                    };

                    for (int i = 0; i < comps.Count; i++)
                        if (comps[i] == null)
                            info.componentNames.Add($"Missing Script {i}");

                    foundObjects.Add(info);
                }
            }
            finally
            {
                ListPool<Component>.Release(comps);
            }

            var t = go.transform;
            for (int i = 0, c = t.childCount; i < c; i++)
                ScanGameObjectRecursively(t.GetChild(i).gameObject, sceneOrAssetName, isPrefab, assetOrScenePath);
        }

        private static void CompleteScan()
        {
            EditorApplication.update -= UpdateScan;
            isScanning = false;
            hasScanned = true;
            scanProgress = 1f;
            scanStatus = "Complete";
            EditorUtility.ClearProgressBar();
            Debug.Log($"Scan complete: Found {foundObjects.Count} objects with missing scripts");
        }

        private static void CancelScan()
        {
            if (!isScanning) return;
            EditorApplication.update -= UpdateScan;
            isScanning = false;
            scanStatus = "Cancelled";
            EditorUtility.ClearProgressBar();
            Debug.LogWarning("Scan cancelled. Partial results kept.");
        }
    }
}
#endif
