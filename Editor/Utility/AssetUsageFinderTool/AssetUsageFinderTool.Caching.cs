#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - Caching Layer
    /// Contains dependency cache, file metadata cache, compiled regex cache, GUID batch regexes,
    /// memory pools and cache management helpers.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        /// <summary>
        /// Get dependencies for a path with strong caching and staleness checks.
        /// Only call this on the main thread (AssetDatabase is not thread-safe).
        /// </summary>
        private static string[] GetCachedDependencies(string assetPath)
        {
            var metadata = GetFileMetadata(assetPath);

            if (dependencyCache.TryGetValue(assetPath, out var cached) &&
                cacheTimestamps.TryGetValue(assetPath, out var cacheTime) &&
                cacheFileSizes.TryGetValue(assetPath, out var cacheSize) &&
                (DateTime.Now - cacheTime).TotalHours < CACHE_VALIDITY_HOURS &&
                metadata.lastWrite <= cacheTime &&
                metadata.size == cacheSize)
            {
                return cached;
            }

            try
            {
                var deps = AssetDatabase.GetDependencies(assetPath, true);

                if (dependencyCache.Count >= MAX_CACHE_ENTRIES)
                    ClearOldestCacheEntries();

                dependencyCache[assetPath] = deps;
                cacheTimestamps[assetPath] = DateTime.Now;
                cacheFileSizes[assetPath] = metadata.size;
                return deps;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Trim 25% of the oldest dependency cache entries to control memory growth.
        /// </summary>
        private static void ClearOldestCacheEntries()
        {
            var oldKeys = cacheTimestamps
                .OrderBy(kvp => kvp.Value)
                .Take(Math.Max(1, MAX_CACHE_ENTRIES / 4))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldKeys)
            {
                dependencyCache.Remove(key);
                cacheTimestamps.Remove(key);
                cacheFileSizes.Remove(key);
            }
        }

        /// <summary>
        /// Get or compute file metadata (size, last write, extension, binary heuristic).
        /// Safe to call from any thread (pure IO).
        /// </summary>
        private static FileMetadata GetFileMetadata(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return default;

            if (fileMetadataCache.TryGetValue(assetPath, out var cached))
                return cached;

            var absPath = ToAbs(assetPath);
            var meta = new FileMetadata
            {
                size = File.Exists(absPath) ? new FileInfo(absPath).Length : 0,
                lastWrite = File.Exists(absPath) ? File.GetLastWriteTime(absPath) : DateTime.MinValue,
                extension = Path.GetExtension(assetPath).ToLowerInvariant(),
                isBinary = IsLikelyBinary(Path.GetExtension(assetPath))
            };

            fileMetadataCache[assetPath] = meta;
            return meta;
        }

        /// <summary>
        /// Get or compile a regex instance for a given pattern.
        /// </summary>
        private static Regex GetOrCreateRegex(string pattern)
        {
            if (!compiledRegexCache.TryGetValue(pattern, out var rx))
            {
                rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
                compiledRegexCache[pattern] = rx;
            }
            return rx;
        }

        /// <summary>
        /// Build batched GUID regex list with caching keyed by the ordered GUID set.
        /// Optimized to reuse compiled regex batches across searches.
        /// </summary>
        private static void PrepareGuidRegexChunksOptimized(List<string> guids)
        {
            guidRegexChunks.Clear();
            if (guids == null || guids.Count == 0) return;

            var batchKey = string.Join("|", guids.OrderBy(g => g, StringComparer.Ordinal));
            if (guidRegexBatches.TryGetValue(batchKey, out var cachedList))
            {
                guidRegexChunks.AddRange(cachedList);
                return;
            }

            var newList = new List<Regex>();
            var chunk = new List<string>(REGEX_GUID_CHUNK);

            foreach (var g in guids)
            {
                if (string.IsNullOrEmpty(g)) continue;
                chunk.Add(Regex.Escape(g));
                if (chunk.Count >= REGEX_GUID_CHUNK)
                {
                    var pattern = "(" + string.Join("|", chunk) + ")";
                    var rx = GetOrCreateRegex(pattern);
                    newList.Add(rx);
                    guidRegexChunks.Add(rx);
                    chunk.Clear();
                }
            }

            if (chunk.Count > 0)
            {
                var pattern = "(" + string.Join("|", chunk) + ")";
                var rx = GetOrCreateRegex(pattern);
                newList.Add(rx);
                guidRegexChunks.Add(rx);
            }

            guidRegexBatches[batchKey] = newList;
        }

        /// <summary>
        /// Simple non-cached GUID regex batching (legacy/original path).
        /// </summary>
        private static void PrepareGuidRegexChunks(List<string> guids)
        {
            guidRegexChunks.Clear();
            if (guids == null || guids.Count == 0) return;

            var chunk = new List<string>(REGEX_GUID_CHUNK);
            foreach (var g in guids)
            {
                if (string.IsNullOrEmpty(g)) continue;
                chunk.Add(Regex.Escape(g));
                if (chunk.Count >= REGEX_GUID_CHUNK)
                {
                    guidRegexChunks.Add(
                        new Regex("(" + string.Join("|", chunk) + ")", RegexOptions.Compiled | RegexOptions.CultureInvariant));
                    chunk.Clear();
                }
            }

            if (chunk.Count > 0)
            {
                guidRegexChunks.Add(
                    new Regex("(" + string.Join("|", chunk) + ")", RegexOptions.Compiled | RegexOptions.CultureInvariant));
            }
        }

        /// <summary>
        /// Clear all caches and memory pools.
        /// </summary>
        public static void ClearAllCaches()
        {
            dependencyCache.Clear();
            cacheTimestamps.Clear();
            cacheFileSizes.Clear();

            fileMetadataCache.Clear();

            compiledRegexCache.Clear();
            guidRegexBatches.Clear();
            guidRegexChunks.Clear();

            lock (stringBuilderPool) stringBuilderPool.Clear();
            lock (stringListPool) stringListPool.Clear();

            Debug.Log("Asset Usage Finder: all caches cleared.");
        }

        /// <summary>
        /// Preload dependency cache for a subset of assets to speed up upcoming searches.
        /// </summary>
        public static void WarmupCache()
        {
            var allAssets = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                .Take(1000)
                .ToArray();

            EditorUtility.DisplayProgressBar("Warming Cache", "Pre-loading dependencies...", 0f);
            for (int i = 0; i < allAssets.Length; i++)
            {
                GetCachedDependencies(allAssets[i]);

                if (i % 50 == 0)
                {
                    float p = (float)i / Math.Max(1, allAssets.Length);
                    EditorUtility.DisplayProgressBar("Warming Cache", $"Loaded {i}/{allAssets.Length}", p);
                }
            }
            EditorUtility.ClearProgressBar();

            Debug.Log($"Asset Usage Finder: cache warmed with {dependencyCache.Count} dependency entries.");
        }

        /// <summary>
        /// Log current cache stats to the console for diagnostics.
        /// </summary>
        public static void LogPerformanceStats()
        {
            Debug.Log(
                "🚀 Asset Usage Finder - Performance Stats\n" +
                $"• Dependency Cache: {dependencyCache.Count} entries\n" +
                $"• Regex Cache: {compiledRegexCache.Count} compiled patterns\n" +
                $"• Regex Batches: {guidRegexBatches.Count} batch sets\n" +
                $"• File Metadata Cache: {fileMetadataCache.Count} files\n" +
                $"• StringBuilder Pool: {stringBuilderPool.Count} available\n" +
                $"• String List Pool: {stringListPool.Count} available\n" +
                $"• Memory usage estimate (rough): ~{(dependencyCache.Count * 0.5f + fileMetadataCache.Count * 0.1f):F1} MB"
            );
        }

        // ---------------- Memory pool helpers (optional future use) ----------------

        private static StringBuilder RentStringBuilder(int capacity = 256)
        {
            lock (stringBuilderPool)
            {
                if (stringBuilderPool.Count > 0)
                {
                    var sb = stringBuilderPool.Dequeue();
                    sb.Clear();
                    if (sb.Capacity < capacity) sb.EnsureCapacity(capacity);
                    return sb;
                }
            }
            return new StringBuilder(capacity);
        }

        private static void ReturnStringBuilder(StringBuilder sb)
        {
            if (sb == null) return;
            lock (stringBuilderPool)
            {
                // keep a reasonable cap to avoid unbounded growth
                if (stringBuilderPool.Count < 64)
                    stringBuilderPool.Enqueue(sb);
            }
        }

        private static List<string> RentStringList()
        {
            lock (stringListPool)
            {
                if (stringListPool.Count > 0)
                    return stringListPool.Dequeue();
            }
            return new List<string>(32);
        }

        private static void ReturnStringList(List<string> list)
        {
            if (list == null) return;
            list.Clear();
            lock (stringListPool)
            {
                if (stringListPool.Count < 64)
                    stringListPool.Enqueue(list);
            }
        }
    }
}
#endif
