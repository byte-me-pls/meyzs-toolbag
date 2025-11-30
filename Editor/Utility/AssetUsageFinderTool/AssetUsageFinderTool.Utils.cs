#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - Utilities
    /// Path, IO, formatting, and simple helper methods.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        /// <summary>
        /// Project-relative path ("Assets/...") to absolute OS path.
        /// If already absolute, returns as is.
        /// </summary>
        private static string ToAbs(string projRelative)
        {
            if (string.IsNullOrEmpty(projRelative)) return projRelative;
            if (Path.IsPathRooted(projRelative)) return projRelative;
            var local = projRelative.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(Directory.GetCurrentDirectory(), local);
        }

        /// <summary>
        /// Returns file size in bytes for a project-relative path.
        /// </summary>
        private static long GetFileSizeAbs(string projPath)
        {
            try
            {
                var abs = ToAbs(projPath);
                return File.Exists(abs) ? new FileInfo(abs).Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns last write time for a project-relative path.
        /// </summary>
        private static DateTime GetLastWriteTimeAbs(string projPath)
        {
            try
            {
                var abs = ToAbs(projPath);
                return File.Exists(abs) ? File.GetLastWriteTime(abs) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Human-readable byte formatter.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_000_000_000) return $"{bytes / 1_000_000_000f:F2} GB";
            if (bytes >= 1_000_000)     return $"{bytes / 1_000_000f:F2} MB";
            if (bytes >= 1_000)         return $"{bytes / 1_000f:F2} KB";
            return $"{bytes} B";
        }

        /// <summary>
        /// Heuristic binary check by extension.
        /// Note: .asset can be text or binary depending on project settings; treat as text unless obviously binary.
        /// </summary>
        private static bool IsLikelyBinary(string ext)
        {
            switch (ext.ToLowerInvariant())
            {
                case ".fbx":
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".exr":
                case ".wav":
                case ".mp3":
                case ".ogg":
                case ".ttf":
                case ".otf":
                case ".dll":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Quick type inference from file extension and optional content.
        /// </summary>
        private static ReferenceType DetermineReferenceType(string filePath, string contentOrNull)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".prefab": return ReferenceType.Prefab;
                case ".mat":    return ReferenceType.Material;
                case ".anim":   return ReferenceType.Animation;
                case ".cs":     return ReferenceType.Script;
                case ".unity":
                    if (!string.IsNullOrEmpty(contentOrNull) && contentOrNull.Contains("m_Component"))
                        return ReferenceType.Component;
                    return ReferenceType.Direct;
                default:
                    return ReferenceType.Unknown;
            }
        }

        /// <summary>
        /// Trim a single line for context display.
        /// </summary>
        private static string TrimContext(string line)
        {
            line = line?.Trim() ?? string.Empty;
            if (line.Length > CONTEXT_MAX) line = line.Substring(0, CONTEXT_MAX) + "...";
            return line;
        }

        /// <summary>
        /// Split text into lines with CRLF tolerance.
        /// </summary>
        private static string[] SplitLines(string text)
        {
            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Inclusion predicate for scanning candidate files based on user toggles.
        /// </summary>
        private static bool IsPathIncluded(string path)
        {
            if (!includePackages && path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return false;

            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (includeScenes      && ext == ".unity")     return true;
            if (includePrefabs     && ext == ".prefab")    return true;
            if (includeMaterials   && ext == ".mat")       return true;
            if (includeAnimations  && ext == ".anim")      return true;
            if (includeControllers && ext == ".controller")return true;
            if (includeScripts     && ext == ".cs")        return true;
            if (includeShaders     && ext == ".shader")    return true;

            if (includeOthers)
            {
                string[] known = { ".unity", ".prefab", ".mat", ".anim", ".controller", ".cs", ".shader" };
                if (!known.Contains(ext)) return true;
            }

            return false;
        }
    }
}
#endif
