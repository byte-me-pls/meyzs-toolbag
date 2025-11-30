#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - Scan workflow and phases.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        // ---------- Entry points ----------

        private static void StartScan()
        {
            if (isScanning) return;
            ResetState();

            // Phase 0: find clips (immediately fills list and switches to phase 1)
            BuildClipList(folderFilter, filterMode == FilterMode.SpecificFolder);
            phase = 1;

            EditorApplication.update += UpdateScan;
        }

        private static void StartFolderScan()
        {
            string folderAbs = EditorUtility.OpenFolderPanel("Select Audio Folder", Application.dataPath, "");
            if (string.IsNullOrEmpty(folderAbs)) return;

            if (!folderAbs.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Invalid Folder",
                    "Please choose a folder under the project's Assets/.", "OK");
                return;
            }

            // Convert to Assets-relative
            folderFilter = "Assets" + folderAbs.Substring(Application.dataPath.Length);
            filterMode = FilterMode.SpecificFolder;

            StartScan();
        }

        private static void ResetState()
        {
            dataLoaded   = false;
            isScanning   = true;
            scanProgress = 0f;
            scanStatus   = "Initializing...";
            phase        = 0;

            clipIdx = 0;
            pathIdx = 0;

            audioClips.Clear();
            yamlPaths.Clear();
            guidToFiles.Clear();
        }

        private static void CancelScan()
        {
            if (!isScanning) return;
            EditorApplication.update -= UpdateScan;
            isScanning = false;
            scanStatus = "Cancelled";
        }

        // ---------- Main update loop ----------

        private static void UpdateScan()
        {
            try
            {
                switch (phase)
                {
                    case 1: Phase_AnalyzeClipMeta(); break;
                    case 2: Phase_PrepareYamlPathsAndIndex(); break;
                    case 3: Phase_BuildGuidIndexStreaming(); break;
                    case 4: Phase_BindUsages(); break;
                    default: CompleteScan(); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"AudioBankOrganizerTool scan failed: {e}");
                CancelScan();
            }
        }

        // ---------- Phase 0: Build clip list ----------

        private static void BuildClipList(string folder, bool useFolder)
        {
            scanStatus = "Finding AudioClips...";

            string[] folders = null;
            if (useFolder && !string.IsNullOrEmpty(folder))
                folders = new[] { folder };

            var guids = AssetDatabase.FindAssets("t:AudioClip", folders);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                audioClips.Add(new AudioClipInfo
                {
                    clip = clip,
                    path = path,
                    guid = g,
                    folderName = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets",
                    lastModified = SafeLastWriteTime(path),
                    isImportedRecently = (DateTime.Now - SafeLastWriteTime(path)).TotalDays < 7
                });
            }

            phase = 1;
        }

        // ---------- Phase 1: Analyze clip metadata ----------

        private static void Phase_AnalyzeClipMeta()
        {
            scanStatus = "Analyzing clip properties...";

            if (audioClips.Count == 0)
            {
                phase = 2;
                return;
            }

            int end = Mathf.Min(clipIdx + CLIP_BATCH, audioClips.Count);
            for (; clipIdx < end; clipIdx++)
                AnalyzeAudioClip(audioClips[clipIdx]);

            scanProgress = Mathf.Lerp(0.05f, 0.25f,
                audioClips.Count == 0 ? 1f : (float)clipIdx / audioClips.Count);

            if (clipIdx >= audioClips.Count)
                phase = 2;
        }

        // ---------- Phase 2: Prepare YAML/text file list ----------

        private static void Phase_PrepareYamlPathsAndIndex()
        {
            scanStatus = "Collecting project files...";

            IEnumerable<string> all = AssetDatabase.GetAllAssetPaths()
                .Where(p =>
                    p.StartsWith("Assets/", StringComparison.Ordinal) ||
                    (includePackages && p.StartsWith("Packages/", StringComparison.Ordinal)))
                .Where(p =>
                {
                    string ext = Path.GetExtension(p).ToLowerInvariant();
                    switch (ext)
                    {
                        case ".unity":
                        case ".prefab":
                        case ".mat":
                        case ".anim":
                        case ".controller":
                        case ".asset":
                            return true;
                        default:
                            return false;
                    }
                });

            if (filterMode == FilterMode.SpecificFolder && !string.IsNullOrEmpty(folderFilter))
                all = all.Where(p => p.Replace("\\", "/").StartsWith(folderFilter, StringComparison.Ordinal));

            yamlPaths = all.ToList();
            pathIdx = 0;
            phase = 3;
        }

        // ---------- Phase 3: Build GUID index (streaming) ----------

        private static void Phase_BuildGuidIndexStreaming()
        {
            scanStatus = "Building GUID index (streaming)...";

            int steps = 0;
            while (pathIdx < yamlPaths.Count && steps < FILE_BATCH)
            {
                string p = yamlPaths[pathIdx];

                try
                {
                    // Use Application.dataPath to resolve to absolute when opening with StreamReader
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    var abs = Path.Combine(projectRoot, p).Replace("\\", "/");

                    using (var sr = new StreamReader(abs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            // quick precheck
                            if (line.IndexOf("guid:", StringComparison.OrdinalIgnoreCase) < 0) continue;

                            var m = kGuidRegex.Match(line);
                            if (!m.Success) continue;
                            string g = m.Groups[1].Value;
                            if (string.IsNullOrEmpty(g)) continue;

                            if (!guidToFiles.TryGetValue(g, out var set))
                            {
                                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                guidToFiles[g] = set;
                            }
                            set.Add(p);
                        }
                    }
                }
                catch
                {
                    // Ignore unreadable/binary/missing files
                }

                pathIdx++;
                steps++;
            }

            float denom = Mathf.Max(1, yamlPaths.Count);
            scanProgress = Mathf.Lerp(0.25f, 0.8f, pathIdx / denom);

            if (pathIdx >= yamlPaths.Count)
                phase = 4;
        }

        // ---------- Phase 4: Bind usages to clips ----------

        private static void Phase_BindUsages()
        {
            scanStatus = "Binding usages...";
            int processedThisFrame = 0;

            foreach (var c in audioClips)
            {
                // Skip already bound
                if (c.usedInFiles.Count > 0) continue;

                if (guidToFiles.TryGetValue(c.guid, out var files))
                    c.usedInFiles = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Optional name-based heuristic if no direct GUID hits
                if (nameHeuristicSearch && c.usedInFiles.Count == 0)
                {
                    foreach (var p in yamlPaths)
                    {
                        try
                        {
                            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                            var abs = Path.Combine(projectRoot, p).Replace("\\", "/");

                            using var sr = new StreamReader(abs);
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (line.IndexOf(c.clip.name, StringComparison.Ordinal) >= 0)
                                {
                                    c.usedInFiles.Add(p + " (name-heuristic)");
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // ignore file errors
                        }
                    }
                }

                c.isUsed = c.usedInFiles.Count > 0;

                processedThisFrame++;
                if (processedThisFrame > 25) break; // throttle per frame
            }

            scanProgress = 0.95f;

            // Done?
            bool allBound = audioClips.All(a =>
                a.isUsed || a.usedInFiles.Count > 0 || !guidToFiles.ContainsKey(a.guid) || !nameHeuristicSearch);

            if (allBound)
                CompleteScan();
        }

        // ---------- Finish ----------

        private static void CompleteScan()
        {
            EditorApplication.update -= UpdateScan;
            isScanning   = false;
            dataLoaded   = true;
            scanProgress = 1f;
            scanStatus   = "Complete";

            if (autoOptimizeOnScan)
                OptimizeAllLargeFiles();

            Debug.Log($"Audio Bank Scan Complete: {audioClips.Count} clips, index size {guidToFiles.Count} GUID(s).");
        }

        // ---------- Analysis ----------

        private static void AnalyzeAudioClip(AudioClipInfo clipInfo)
        {
            // File size (absolute)
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath    = Path.Combine(projectRoot, clipInfo.path).Replace("\\", "/");
            try
            {
                clipInfo.fileSize = File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
            }
            catch
            {
                clipInfo.fileSize = 0;
            }

            clipInfo.duration  = clipInfo.clip.length;
            clipInfo.frequency = clipInfo.clip.frequency;
            clipInfo.channels  = clipInfo.clip.channels;

            var importer = AssetImporter.GetAtPath(clipInfo.path) as AudioImporter;
            if (importer != null)
            {
                var s = importer.defaultSampleSettings;
                clipInfo.defaultSampleSettings = s;
                clipInfo.loadType         = s.loadType;
                clipInfo.compressionFormat= s.compressionFormat;
                clipInfo.quality          = s.quality;

                // Platform overrides
                clipInfo.platformSettings.Clear();
                foreach (var plat in kPlatforms)
                {
                    if (importer.ContainsSampleSettingsOverride(plat))
                    {
                        var ov = importer.GetOverrideSampleSettings(plat);
                        clipInfo.platformSettings[plat] = ov;
                    }
                }
            }
        }
    }
}
#endif
