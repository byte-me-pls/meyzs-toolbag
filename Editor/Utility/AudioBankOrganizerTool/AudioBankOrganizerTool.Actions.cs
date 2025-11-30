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
    /// Audio Bank Organizer - Actions (trash/delete, optimize, export, backup, suggestions).
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        // ---------- Trash / Delete ----------

        private static void TrashClip(AudioClipInfo info)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                AssetDatabase.MoveAssetToTrash(info.path);
                audioClips.Remove(info);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static void TrashAllUnused()
        {
            var unused = audioClips.Where(c => !c.isUsed).ToList();
            if (unused.Count == 0)
            {
                EditorUtility.DisplayDialog("No Unused", "No unused audio clips found.", "OK");
                return;
            }

            long freed = unused.Sum(c => c.fileSize);
            if (!EditorUtility.DisplayDialog(
                    "Move All Unused to Trash",
                    $"Move {unused.Count} unused clips to Trash?\nPotential space: {FmtBytes(freed)}",
                    "Move", "Cancel"))
                return;

            EditorUtility.DisplayProgressBar("Move to Trash", "Processing...", 0f);
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < unused.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Move to Trash", unused[i].clip.name, (float)i / Math.Max(1, unused.Count));
                    AssetDatabase.MoveAssetToTrash(unused[i].path);
                    audioClips.Remove(unused[i]);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        // ---------- Optimization ----------

        private static void OptimizeClip(AudioClipInfo c)
        {
            var importer = AssetImporter.GetAtPath(c.path) as AudioImporter;
            if (importer == null) return;

            Undo.RecordObject(importer, "Optimize Audio Clip");
            var s = importer.defaultSampleSettings;

            if (c.duration > 30f)
            {
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
                s.loadType = AudioClipLoadType.Streaming;
            }
            else if (c.duration > 5f)
            {
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.8f;
                s.loadType = AudioClipLoadType.CompressedInMemory;
            }
            else
            {
                s.compressionFormat = AudioCompressionFormat.ADPCM;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
            }

            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
            AnalyzeAudioClip(c);
        }

        private static void OptimizeAllLargeFiles()
        {
            var targets = audioClips.Where(c =>
                c.fileSize > LARGE_FILE_THRESHOLD || c.compressionFormat == AudioCompressionFormat.PCM).ToList();

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Optimize", "No large/uncompressed clips found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Optimize Clips", $"Optimize {targets.Count} clips?", "Optimize", "Cancel"))
                return;

            EditorUtility.DisplayProgressBar("Optimizing", "Processing...", 0f);
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Optimizing", targets[i].clip.name, (float)i / Math.Max(1, targets.Count));
                    OptimizeClip(targets[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ---------- Suggestions (UI helper) ----------

        private static void ShowOptimizationSuggestions(AudioClipInfo c)
        {
            var suggestions = new List<string>();

            var importer = AssetImporter.GetAtPath(c.path) as AudioImporter;
            string platform = GetAudioPlatformName(EditorUserBuildSettings.activeBuildTarget);
            var s = importer != null ? GetEffectiveSampleSettings(importer, platform) : default;

            if (importer == null)
            {
                if (c.compressionFormat == AudioCompressionFormat.PCM)
                    suggestions.Add("• The file is uncompressed (PCM). Consider Vorbis or ADPCM.");
                if (c.duration > 30f)
                    suggestions.Add("• Long duration (>30s). Streaming Load Type is recommended.");
                if (!c.isUsed)
                    suggestions.Add("• This clip is unused. Consider removing it from the project.");
            }
            else
            {
                if (s.compressionFormat == AudioCompressionFormat.PCM)
                    suggestions.Add("• Compression: PCM → Vorbis/ADPCM to reduce file size.");

                if (c.duration > 30f && s.loadType != AudioClipLoadType.Streaming)
                    suggestions.Add("• Long track. Set Load Type to Streaming for lower memory usage.");

                if (c.duration <= 2.0f && s.loadType != AudioClipLoadType.DecompressOnLoad)
                    suggestions.Add("• Very short SFX. Use Decompress On Load to minimize latency.");

                if (c.duration > 5.0f && s.loadType == AudioClipLoadType.DecompressOnLoad)
                    suggestions.Add("• Medium/long clip. Consider Compressed In Memory or Streaming.");

                if (c.frequency > 44100 && c.duration > 5.0f)
                    suggestions.Add("• High sample rate. 44.1 kHz (or 22.05 kHz) may be sufficient.");

                if (c.clip != null && c.clip.channels > 1 && c.duration <= 5.0f && !importer.forceToMono)
                    suggestions.Add("• Short SFX can be mono. Enable 'Force To Mono'.");

                if (s.compressionFormat == AudioCompressionFormat.Vorbis && c.duration > 10.0f && s.quality > 0.9f)
                    suggestions.Add("• Vorbis quality is very high (Q>0.9). 0.7–0.8 is usually enough.");

                if (!s.preloadAudioData && c.duration <= 2.0f)
                    suggestions.Add("• For short SFX, enable 'Preload Audio Data' to avoid first-play stutter.");

                if (!importer.loadInBackground && c.duration > 2.0f)
                    suggestions.Add("• Enable 'Load In Background' for medium/long clips.");

                if (!HasPlatformOverride(importer, "Android"))
                    suggestions.Add("• No Android override. Consider platform-specific overrides.");
                if (!HasPlatformOverride(importer, "iPhone"))
                    suggestions.Add("• No iOS override. Consider platform-specific overrides.");
            }

            if (!c.isUsed)
                suggestions.Add("• This clip is not referenced in the project. It may be safe to remove.");

            string message = suggestions.Count == 0
                ? $"'{c.clip?.name ?? "<unnamed>"}' looks well optimized. No further changes needed."
                : $"Optimization suggestions for '{c.clip?.name ?? "<unnamed>"}':\n\n{string.Join("\n", suggestions)}";

            EditorUtility.DisplayDialog("Optimization Suggestions", message, "OK");
        }

        // ---------- Export / Backup ----------

        private static void ExportReport()
        {
            string path = EditorUtility.SaveFilePanel("Export Audio Bank Report", "", "AudioBankReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using var w = new StreamWriter(path);
                w.WriteLine("Name,Path,SizeB,Size,DurationS,Freq,Channels,Compression,Quality,LoadType,Used,Refs,LastModified,Folder");
                foreach (var c in audioClips)
                {
                    w.WriteLine($"\"{c.clip.name}\",\"{c.path}\",{c.fileSize},\"{FmtBytes(c.fileSize)}\"," +
                                $"{c.duration:F2},{c.frequency},{c.channels},\"{c.compressionFormat}\",{c.quality:F2}," +
                                $"\"{c.loadType}\",{c.isUsed},{c.usedInFiles.Count},\"{c.lastModified:yyyy-MM-dd HH:mm}\",\"{c.folderName}\"");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Export failed: {e.Message}");
            }
        }

        private static void BackupAudioBank()
        {
            string dest = EditorUtility.SaveFolderPanel("Backup Audio Bank", "", "AudioBankBackup");
            if (string.IsNullOrEmpty(dest)) return;

            EditorUtility.DisplayProgressBar("Backup", "Copying...", 0f);
            try
            {
                var root = Directory.GetParent(Application.dataPath).FullName;
                for (int i = 0; i < audioClips.Count; i++)
                {
                    var c = audioClips[i];
                    var src = Path.Combine(root, c.path);
                    var dst = Path.Combine(dest, c.clip.name + Path.GetExtension(src));
                    try { File.Copy(src, dst, true); } catch { /* ignore per-file errors */ }
                    EditorUtility.DisplayProgressBar("Backup", c.clip.name, (float)i / Math.Max(1, audioClips.Count));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
#endif
