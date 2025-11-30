#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - Search & Scanning
    /// Contains optimized and legacy search flows and all file scanning routines.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        // ===================== OPTIMIZED SEARCH (ASYNC) =====================

        private static async void BeginOptimizedSearch()
        {
            var selectedAssets = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            if (selectedAssets.Length == 0) return;

            ClearSearchState();
            processingCancelled = false;

            // Prepare selected GUIDs
            selectedAssetGUIDs = selectedAssets
                .Select(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)))
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            guidToInfo.Clear();

            // Seed info list with metadata
            foreach (var guid in selectedAssetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                var metadata = GetFileMetadata(assetPath);

                var info = new AssetUsageInfo
                {
                    assetPath = assetPath,
                    assetGUID = guid,
                    assetName = Path.GetFileName(assetPath),
                    assetType = asset?.GetType().Name ?? "Unknown",
                    fileSize = metadata.size,
                    lastModified = metadata.lastWrite,
                    isSelected = autoSelectUnused
                };

                guidToInfo[guid] = info;
                assetInfoList.Add(info);
            }

            // Candidate files
            allFilePaths = AssetDatabase.GetAllAssetPaths()
                .Where(IsPathIncluded)
                .ToArray();

            currentFileIndex = 0;

            // Prepare regex chunks if deep search
            if (deepSearch)
                PrepareGuidRegexChunksOptimized(selectedAssetGUIDs);

            isSearching = true;
            scanStatus = "Scanning...";

            try
            {
                await ScanFilesOptimizedAsync();

                foreach (var info in assetInfoList)
                    info.totalReferences = info.usages.Count;

                if (autoSelectUnused)
                    SelectUnusedAssets();

                hasResults = true;
                Debug.Log($"Optimized search complete: Found {assetInfoList.Sum(a => a.totalReferences)} references across {assetInfoList.Count} assets");
            }
            catch (Exception e)
            {
                Debug.LogError($"Optimized search error: {e.Message}\n{e}");
            }
            finally
            {
                isSearching = false;
                scanStatus = "Complete";
                processingCancelled = false;
            }
        }

        private static async Task ScanFilesOptimizedAsync()
        {
            const int BATCH_SIZE = 25;
            const int MAX_PARALLEL = 8;
            int actualParallel = Math.Min(Environment.ProcessorCount, MAX_PARALLEL);

            var semaphore = new System.Threading.SemaphoreSlim(actualParallel);
            var tasks = new List<Task>();

            for (int i = 0; i < allFilePaths.Length; i += BATCH_SIZE)
            {
                if (processingCancelled) break;

                var batch = allFilePaths.Skip(i).Take(BATCH_SIZE).ToArray();
                currentFileIndex = i;
                scanProgress = (float)i / Math.Max(1, allFilePaths.Length);

                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        foreach (var file in batch)
                        {
                            if (processingCancelled) break;
                            ScanFileOptimized(file);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);

                // Throttle task queue
                if (tasks.Count >= MAX_PARALLEL * 2)
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }

                // Keep UI responsive
                if (i % (BATCH_SIZE * 4) == 0)
                    await Task.Delay(1);
            }

            await Task.WhenAll(tasks);
            scanProgress = 1f;
        }

        private static void ScanFileOptimized(string filePath)
        {
            var metadata = GetFileMetadata(filePath);

            // Skip binary files in deep path
            if (metadata.isBinary && deepSearch) return;

            if (!deepSearch)
            {
                // Reverse dependency path (uses AssetDatabase)
                ScanFileReverseDepsOptimized(filePath);
                return;
            }

            // Deep scan (regex over YAML/text)
            ScanFileDeepOptimized(filePath, metadata);
        }

        private static void ScanFileReverseDepsOptimized(string filePath)
        {
            try
            {
                var deps = GetCachedDependencies(filePath);
                var targetGuidSet = new HashSet<string>(selectedAssetGUIDs, StringComparer.OrdinalIgnoreCase);

                foreach (var dep in deps)
                {
                    var guid = AssetDatabase.AssetPathToGUID(dep);
                    if (!string.IsNullOrEmpty(guid) && targetGuidSet.Contains(guid) && guidToInfo.TryGetValue(guid, out var info))
                    {
                        var usage = BuildUsageOptimized(filePath, DetermineReferenceType(filePath, null), 0, null);
                        lock (lockObject)
                        {
                            info.usages.Add(usage);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Reverse deps error for {filePath}: {e.Message}");
            }
        }

        private static void ScanFileDeepOptimized(string filePath, FileMetadata metadata)
        {
            try
            {
                var foundGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (metadata.size < 2 * 1024 * 1024) // small files: read whole
                {
                    ScanFileContentOptimized(filePath, foundGuids);
                }
                else
                {
                    // large files: stream line by line
                    ScanFileStreamingOptimized(filePath, foundGuids);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Deep scan error for {filePath}: {e.Message}");
            }
        }

        private static void ScanFileContentOptimized(string filePath, HashSet<string> foundGuids)
        {
            var absPath = ToAbs(filePath);
            if (!File.Exists(absPath)) return;

            string content = File.ReadAllText(absPath);

            foreach (var regex in guidRegexChunks)
            {
                if (processingCancelled) return;

                var matches = regex.Matches(content);
                foreach (Match match in matches)
                {
                    var guid = match.Value;
                    if (!foundGuids.Contains(guid) && guidToInfo.TryGetValue(guid, out var info))
                    {
                        foundGuids.Add(guid);
                        var (lineNumber, context) = FindLineAndContextOptimized(content, guid);
                        var usage = BuildUsageOptimized(filePath, DetermineReferenceType(filePath, content), lineNumber, context);

                        lock (lockObject)
                        {
                            info.usages.Add(usage);
                        }
                    }
                }
            }
        }

        private static void ScanFileStreamingOptimized(string filePath, HashSet<string> foundGuids)
        {
            int lineNumber = 0;

            foreach (var line in ReadFileStreamingly(filePath))
            {
                if (processingCancelled) return;
                lineNumber++;

                foreach (var regex in guidRegexChunks)
                {
                    var matches = regex.Matches(line);
                    foreach (Match match in matches)
                    {
                        var guid = match.Value;
                        if (!foundGuids.Contains(guid) && guidToInfo.TryGetValue(guid, out var info))
                        {
                            foundGuids.Add(guid);
                            var context = showContext ? TrimContext(line) : null;
                            var usage = BuildUsageOptimized(
                                filePath,
                                DetermineReferenceType(filePath, null),
                                showLineNumbers ? lineNumber : 0,
                                context);

                            lock (lockObject)
                            {
                                info.usages.Add(usage);
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> ReadFileStreamingly(string filePath, int bufferSize = 64 * 1024)
        {
            var absPath = ToAbs(filePath);
            if (!File.Exists(absPath)) yield break;

            using (var stream = new FileStream(absPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, bufferSize))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (processingCancelled) yield break;
                    yield return line;
                }
            }
        }

        private static (int lineNumber, string context) FindLineAndContextOptimized(string content, string guid)
        {
            if (!showLineNumbers && !showContext) return (0, null);

            var lines = content.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var context = showContext ? TrimContext(lines[i]) : null;
                    var lineNum = showLineNumbers ? i + 1 : 0;
                    return (lineNum, context);
                }
            }
            return (0, null);
        }

        private static UsageReference BuildUsageOptimized(string filePath, ReferenceType type, int line, string context)
        {
            return new UsageReference
            {
                filePath = filePath,
                fileName = Path.GetFileName(filePath),
                fileType = Path.GetExtension(filePath),
                folderPath = Path.GetDirectoryName(filePath)?.Replace("\\", "/"),
                referenceType = type,
                lastModified = GetFileMetadata(filePath).lastWrite,
                lineNumber = line,
                context = context
            };
        }

        // ===================== ORIGINAL (LEGACY) SEARCH =====================

        private static void BeginSearch()
        {
            var selectedAssets = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            if (selectedAssets.Length == 0) return;

            assetInfoList.Clear();
            selectedAssetInfo = null;
            hasResults = false;
            isSearching = true;
            cancelRequested = false;
            scanProgress = 0f;

            selectedAssetGUIDs = selectedAssets
                .Select(o => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)))
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            guidToInfo.Clear();
            foreach (var guid in selectedAssetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                var info = new AssetUsageInfo
                {
                    assetPath = assetPath,
                    assetGUID = guid,
                    assetName = Path.GetFileName(assetPath),
                    assetType = asset?.GetType().Name ?? "Unknown",
                    fileSize = GetFileSizeAbs(assetPath),
                    lastModified = GetLastWriteTimeAbs(assetPath),
                    isSelected = autoSelectUnused
                };

                guidToInfo[guid] = info;
                assetInfoList.Add(info);
            }

            allFilePaths = AssetDatabase.GetAllAssetPaths()
                .Where(IsPathIncluded)
                .ToArray();

            currentFileIndex = 0;

            guidRegexChunks.Clear();
            if (deepSearch)
                PrepareGuidRegexChunks(selectedAssetGUIDs);

            scanStatus = "Starting search...";
            EditorApplication.update += SearchStep;
        }

        private static void QuickSearch()
        {
            var selectedAssets = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            if (selectedAssets.Length == 0) return;

            assetInfoList.Clear();
            selectedAssetInfo = null;
            hasResults = true;

            var targetGuidToInfo = new Dictionary<string, AssetUsageInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in selectedAssets)
            {
                var path = AssetDatabase.GetAssetPath(o);
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) continue;

                var info = new AssetUsageInfo
                {
                    assetPath = path,
                    assetGUID = guid,
                    assetName = Path.GetFileName(path),
                    assetType = o.GetType().Name,
                    fileSize = GetFileSizeAbs(path),
                    lastModified = GetLastWriteTimeAbs(path),
                    isSelected = autoSelectUnused
                };

                targetGuidToInfo[guid] = info;
                assetInfoList.Add(info);
            }

            var candidates = AssetDatabase.GetAllAssetPaths().Where(IsPathIncluded).ToArray();

            int processed = 0;
            foreach (var file in candidates)
            {
                processed++;
                if (processed % 200 == 0)
                    EditorUtility.DisplayProgressBar("Quick Search", $"Scanning deps: {Path.GetFileName(file)}",
                        (float)processed / Math.Max(1, candidates.Length));

                try
                {
                    var deps = AssetDatabase.GetDependencies(file, true);
                    foreach (var dep in deps)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(dep);
                        if (guid != null && targetGuidToInfo.TryGetValue(guid, out var info))
                        {
                            var usage = BuildUsage(file, DetermineReferenceType(file, null), 0, null);
                            info.usages.Add(usage);
                        }
                    }
                }
                catch { }
            }

            EditorUtility.ClearProgressBar();

            foreach (var info in assetInfoList)
                info.totalReferences = info.usages.Count;

            if (autoSelectUnused)
                SelectUnusedAssets();

            Debug.Log($"Quick search complete: Analyzed {assetInfoList.Count} assets");
        }

        private static void SearchStep()
        {
            if (cancelRequested)
            {
                EndSearch();
                return;
            }

            try
            {
                int processed = 0;

                while (currentFileIndex < allFilePaths.Length && processed < FILES_PER_FRAME)
                {
                    string filePath = allFilePaths[currentFileIndex];
                    scanStatus = $"Scanning {Path.GetFileName(filePath)}...";

                    if (deepSearch)
                        ScanFileDeep(filePath);
                    else
                        ScanFileReverseDeps(filePath);

                    currentFileIndex++;
                    processed++;

                    scanProgress = (float)currentFileIndex / Math.Max(1, allFilePaths.Length);
                }

                if (currentFileIndex >= allFilePaths.Length)
                {
                    EndSearch();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Search Step Error: {e.Message}");
                EndSearch();
            }
        }

        private static void EndSearch()
        {
            EditorApplication.update -= SearchStep;
            isSearching = false;
            hasResults = true;
            scanProgress = 1f;
            scanStatus = "Complete";

            foreach (var a in assetInfoList)
                a.totalReferences = a.usages.Count;

            if (autoSelectUnused)
                SelectUnusedAssets();

            Debug.Log($"Search complete: Found {assetInfoList.Sum(a => a.totalReferences)} references across {assetInfoList.Count} assets");
        }

        private static void ScanFileDeep(string filePath)
        {
            try
            {
                string abs = ToAbs(filePath);
                if (!File.Exists(abs)) return;

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (IsLikelyBinary(ext)) return;

                var fi = new FileInfo(abs);
                bool lineByLine = fi.Length > (5 * 1024 * 1024);

                if (!lineByLine)
                {
                    string content = File.ReadAllText(abs);
                    MatchGuidRegexChunks(content, filePath);
                }
                else
                {
                    using (var sr = new StreamReader(abs, Encoding.UTF8, true, 64 * 1024))
                    {
                        string line;
                        int lineNo = 0;
                        var foundGuidsForFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        while ((line = sr.ReadLine()) != null)
                        {
                            if (cancelRequested) return;
                            lineNo++;

                            foreach (var rx in guidRegexChunks)
                            {
                                var m = rx.Match(line);
                                while (m.Success)
                                {
                                    var guid = m.Value;
                                    if (guidToInfo.TryGetValue(guid, out var info))
                                    {
                                        if (!foundGuidsForFile.Contains(guid))
                                        {
                                            foundGuidsForFile.Add(guid);
                                            var usage = BuildUsage(filePath, DetermineReferenceType(filePath, null),
                                                showLineNumbers ? lineNo : 0, showContext ? TrimContext(line) : null);
                                            info.usages.Add(usage);
                                        }
                                    }
                                    m = m.NextMatch();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error scanning file {filePath}: {e.Message}");
            }
        }

        private static void ScanFileReverseDeps(string filePath)
        {
            try
            {
                var deps = AssetDatabase.GetDependencies(filePath, true);
                foreach (var dep in deps)
                {
                    var guid = AssetDatabase.AssetPathToGUID(dep);
                    if (!string.IsNullOrEmpty(guid) && guidToInfo.TryGetValue(guid, out var info))
                    {
                        var usage = BuildUsage(filePath, DetermineReferenceType(filePath, null), 0, null);
                        info.usages.Add(usage);
                    }
                }
            }
            catch { }
        }

        private static void MatchGuidRegexChunks(string content, string filePath)
        {
            var lines = SplitLines(content);
            var matchedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rx in guidRegexChunks)
            {
                var m = rx.Match(content);
                while (m.Success)
                {
                    var guid = m.Value;
                    if (!matchedGuids.Contains(guid) && guidToInfo.TryGetValue(guid, out var info))
                    {
                        matchedGuids.Add(guid);

                        int lineNumber = 0;
                        string context = null;
                        if (showLineNumbers || showContext)
                        {
                            (lineNumber, context) = FindLineAndContext(lines, guid);
                        }

                        var usage = BuildUsage(filePath, DetermineReferenceType(filePath, content), lineNumber, context);
                        info.usages.Add(usage);
                    }

                    m = m.NextMatch();
                }
            }
        }

        private static (int lineNumber, string context) FindLineAndContext(string[] lines, string guid)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var ctx = TrimContext(lines[i]);
                    return (i + 1, ctx);
                }
            }
            return (0, null);
        }

        private static UsageReference BuildUsage(string filePath, ReferenceType type, int line, string context)
        {
            return new UsageReference
            {
                filePath = filePath,
                fileName = Path.GetFileName(filePath),
                fileType = Path.GetExtension(filePath),
                folderPath = Path.GetDirectoryName(filePath)?.Replace("\\", "/"),
                referenceType = type,
                lastModified = GetLastWriteTimeAbs(filePath),
                lineNumber = line,
                context = context
            };
        }
    }
}
#endif
