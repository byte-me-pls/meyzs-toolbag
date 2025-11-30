#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - Actions & Helpers
    /// Delete/export/report/select/copy/filter operations and list helpers.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        // ---------- State helpers ----------

        private static void ClearSearchState()
        {
            assetInfoList.Clear();
            selectedAssetInfo = null;
            hasResults = false;
        }

        private static void SelectUnusedAssets()
        {
            foreach (var asset in assetInfoList)
                asset.isSelected = asset.totalReferences == 0;
        }

        private static void ClearFilters()
        {
            searchFilter = "";
            filterMode = FilterMode.All;
            minUsageCount = 0;
            maxUsageCount = 999;
        }

        // ---------- Filtering / Sorting ----------

        private static List<AssetUsageInfo> GetFilteredAndSortedAssets()
        {
            var q = assetInfoList.AsEnumerable();

            if (!string.IsNullOrEmpty(searchFilter))
            {
                q = q.Where(a =>
                    a.assetName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.assetPath.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            switch (filterMode)
            {
                case FilterMode.UnusedOnly:       q = q.Where(a => a.totalReferences == 0); break;
                case FilterMode.HighUsage:        q = q.Where(a => a.totalReferences >= Math.Max(5, minUsageCount)); break;
                case FilterMode.RecentlyModified: q = q.Where(a => (DateTime.Now - a.lastModified).TotalDays < 7); break;
                case FilterMode.SceneOnly:        q = q.Where(a => a.usages.Any(u => u.fileType == ".unity")); break;
                case FilterMode.PrefabOnly:       q = q.Where(a => a.usages.Any(u => u.fileType == ".prefab")); break;
                case FilterMode.MaterialOnly:     q = q.Where(a => a.usages.Any(u => u.fileType == ".mat")); break;
                case FilterMode.ScriptOnly:       q = q.Where(a => a.usages.Any(u => u.fileType == ".cs")); break;
            }

            q = q.Where(a => a.totalReferences >= minUsageCount && a.totalReferences <= maxUsageCount);

            switch (sortMode)
            {
                case SortMode.Name:
                    q = sortAscending ? q.OrderBy(a => a.assetName) : q.OrderByDescending(a => a.assetName);
                    break;
                case SortMode.UsageCount:
                    q = sortAscending ? q.OrderBy(a => a.totalReferences) : q.OrderByDescending(a => a.totalReferences);
                    break;
                case SortMode.FileSize:
                    q = sortAscending ? q.OrderBy(a => a.fileSize) : q.OrderByDescending(a => a.fileSize);
                    break;
                case SortMode.LastModified:
                    q = sortAscending ? q.OrderBy(a => a.lastModified) : q.OrderByDescending(a => a.lastModified);
                    break;
                case SortMode.AssetType:
                    q = sortAscending ? q.OrderBy(a => a.assetType) : q.OrderByDescending(a => a.assetType);
                    break;
            }

            return q.ToList();
        }

        private static string GetFileTypeIcon(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".unity":      return "🎬";
                case ".prefab":     return "📦";
                case ".mat":        return "🎨";
                case ".cs":         return "📄";
                case ".shader":     return "✨";
                case ".anim":       return "🎭";
                case ".controller": return "🎮";
                default:            return "📄";
            }
        }

        // ---------- Actions ----------

        private static void DeleteSelectedAssetsSafe()
        {
            var selected = assetInfoList.Where(a => a.isSelected).ToList();
            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No assets selected for deletion.", "OK");
                return;
            }

            var referenced = selected.Where(a => a.totalReferences > 0).ToList();
            var deletable  = selected.Where(a => a.totalReferences == 0).ToList();

            if (deletable.Count == 0)
            {
                EditorUtility.DisplayDialog("Blocked", "Selected assets are referenced. Aborting delete.", "OK");
                return;
            }

            long totalSize = deletable.Sum(a => a.fileSize);

            var msg = new StringBuilder();
            msg.AppendLine($"Delete {deletable.Count} unused assets?");
            msg.AppendLine($"Total size: {FormatBytes(totalSize)}");
            if (referenced.Count > 0)
                msg.AppendLine($"\n({referenced.Count} referenced assets will NOT be deleted.)");

            if (!EditorUtility.DisplayDialog("Delete Selected Assets", msg.ToString(), "Delete", "Cancel"))
                return;

            foreach (var asset in deletable)
            {
                AssetDatabase.DeleteAsset(asset.assetPath);
                assetInfoList.Remove(asset);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Deleted {deletable.Count} assets.");
        }

        private static void GenerateReport()
        {
            string reportPath = EditorUtility.SaveFilePanel("Save Usage Report", "", "AssetUsageReport", "txt");
            if (string.IsNullOrEmpty(reportPath)) return;

            try
            {
                using (var writer = new StreamWriter(reportPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("OPTIMIZED ASSET USAGE REPORT");
                    writer.WriteLine("========================================");
                    writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Total Assets: {assetInfoList.Count}");
                    writer.WriteLine($"Total References: {assetInfoList.Sum(a => a.totalReferences)}");
                    writer.WriteLine($"Unused Assets: {assetInfoList.Count(a => a.totalReferences == 0)}");
                    writer.WriteLine();
                    writer.WriteLine("Groups by usage:");
                    writer.WriteLine(" - Unused (0)");
                    writer.WriteLine(" - Low (1-2)");
                    writer.WriteLine(" - Medium (3-9)");
                    writer.WriteLine(" - High (10+)");
                    writer.WriteLine();

                    var usageGroups = assetInfoList.GroupBy(a => a.totalReferences == 0 ? "Unused" :
                                                             a.totalReferences < 3 ? "Low" :
                                                             a.totalReferences < 10 ? "Medium" : "High");

                    foreach (var group in usageGroups.OrderBy(g => g.Key))
                    {
                        writer.WriteLine($"{group.Key.ToUpper()} ({group.Count()} assets):");
                        writer.WriteLine("----------------------------------------");

                        foreach (var asset in group.OrderBy(a => a.assetName))
                        {
                            writer.WriteLine($"• {asset.assetName} ({asset.assetType})");
                            writer.WriteLine($"  Path: {asset.assetPath}");
                            writer.WriteLine($"  References: {asset.totalReferences}");
                            writer.WriteLine($"  Size: {FormatBytes(asset.fileSize)}");

                            if (asset.usages.Count > 0)
                            {
                                writer.WriteLine("  Used in (first 5):");
                                foreach (var usage in asset.usages.Take(5))
                                    writer.WriteLine($"    - {usage.fileName} ({usage.referenceType})");
                                if (asset.usages.Count > 5)
                                    writer.WriteLine($"    ... and {asset.usages.Count - 5} more");
                            }

                            writer.WriteLine();
                        }

                        writer.WriteLine();
                    }

                    writer.WriteLine("========================================");
                }

                Debug.Log($"Usage report saved to: {reportPath}");
                EditorUtility.DisplayDialog("Report Generated", $"Report saved to:\n{reportPath}", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate report: {e.Message}");
            }
        }

        private static void ExportToCSV()
        {
            string csvPath = EditorUtility.SaveFilePanel("Export Asset Usage Data", "", "AssetUsageData", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            try
            {
                using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("AssetName,AssetPath,AssetType,TotalReferences,FileSize,LastModified,UsageFiles");

                    foreach (var asset in assetInfoList)
                    {
                        // Keep usage file list under control for spreadsheet tools
                        var names = asset.usages.Select(u => u.fileName).Take(30).ToList();
                        string usageFiles = string.Join(";", names);
                        if (asset.usages.Count > names.Count)
                            usageFiles += $";+{asset.usages.Count - names.Count} more";

                        writer.WriteLine(
                            $"\"{asset.assetName}\",\"{asset.assetPath}\",\"{asset.assetType}\",{asset.totalReferences},{asset.fileSize},\"{asset.lastModified:yyyy-MM-dd HH:mm}\",\"{usageFiles}\"");
                    }
                }

                Debug.Log($"Asset usage data exported to: {csvPath}");
                EditorUtility.DisplayDialog("Export Complete", $"Data exported to:\n{csvPath}", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export CSV: {e.Message}");
            }
        }

        private static void CopySelectedPaths()
        {
            var selectedAssets = assetInfoList.Where(a => a.isSelected).ToList();
            if (selectedAssets.Count == 0) return;

            string paths = string.Join("\n", selectedAssets.Select(a => a.assetPath));
            EditorGUIUtility.systemCopyBuffer = paths;
            Debug.Log($"Copied {selectedAssets.Count} asset paths to clipboard");
        }

        private static void SelectInProject()
        {
            var selectedAssets = assetInfoList.Where(a => a.isSelected).ToList();
            if (selectedAssets.Count == 0) return;

            var objects = selectedAssets.Select(a => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(a.assetPath))
                .Where(o => o != null)
                .ToArray();

            if (objects.Length == 0) return;

            Selection.objects = objects;
            EditorGUIUtility.PingObject(objects[0]);
        }

        private static void ShowInExplorer()
        {
            var selectedAssets = assetInfoList.Where(a => a.isSelected).Take(1).ToList();
            if (selectedAssets.Count == 0) return;

            EditorUtility.RevealInFinder(selectedAssets[0].assetPath);
        }
    }
}
#endif
