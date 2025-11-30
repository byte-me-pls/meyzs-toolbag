#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - Utility helpers
    /// Formatting, safe IO, importer/platform utilities.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        // ---------- Formatting ----------

        private static string FmtBytes(long b)
        {
            if (b >= 1_000_000_000) return $"{b / 1_000_000_000f:F2} GB";
            if (b >= 1_000_000)     return $"{b / 1_000_000f:F2} MB";
            if (b >= 1_000)         return $"{b / 1_000f:F2} KB";
            return $"{b} B";
        }

        private static string FmtTime(float s)
        {
            if (s >= 3600f)
            {
                int h = (int)(s / 3600f), m = (int)((s % 3600f) / 60f), sec = (int)(s % 60f);
                return $"{h}h {m}m {sec}s";
            }
            if (s >= 60f)
            {
                int m = (int)(s / 60f), sec = (int)(s % 60f);
                return $"{m}m {sec}s";
            }
            return $"{s:F1}s";
        }

        // ---------- Safe IO ----------

        private static DateTime SafeLastWriteTime(string assetPath)
        {
            try
            {
                var root = Directory.GetParent(Application.dataPath).FullName;
                var full = Path.Combine(root, assetPath);
                return File.Exists(full) ? File.GetLastWriteTime(full) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        // ---------- Importer / Platform Utilities ----------

        /// <summary>
        /// Maps BuildTarget to the AudioImporter override platform name.
        /// </summary>
        private static string GetAudioPlatformName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android: return "Android";
                case BuildTarget.iOS:     return "iPhone";
                case BuildTarget.WebGL:   return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return "Standalone";
                default:
                    return "Standalone";
            }
        }

        /// <summary>
        /// Gets effective sample settings: override if present, otherwise defaults.
        /// </summary>
        private static AudioImporterSampleSettings GetEffectiveSampleSettings(AudioImporter importer, string platformName)
        {
            if (importer == null) return default;
            try
            {
                return importer.GetOverrideSampleSettings(platformName);
            }
            catch
            {
                return importer.defaultSampleSettings;
            }
        }

        /// <summary>
        /// Returns true if a platform override exists and differs from defaults.
        /// </summary>
        private static bool HasPlatformOverride(AudioImporter importer, string platformName)
        {
            if (importer == null) return false;
            try
            {
                var o = importer.GetOverrideSampleSettings(platformName);
                var d = importer.defaultSampleSettings;
                return o.loadType != d.loadType ||
                       o.compressionFormat != d.compressionFormat ||
                       Mathf.Abs(o.quality - d.quality) > 0.0001f ||
                       o.preloadAudioData != d.preloadAudioData;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
