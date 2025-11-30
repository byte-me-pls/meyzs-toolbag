#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SceneTimeMachineTool
    {
        private static List<SceneSnapshot> GetFilteredAndSortedSnapshots()
        {
            IEnumerable<SceneSnapshot> f = snapshots;

            if (!string.IsNullOrEmpty(searchFilter))
            {
                f = f.Where(s =>
                    (s.sceneName?.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (s.description?.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (s.tags?.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            switch (filterMode)
            {
                case FilterMode.Manual: f = f.Where(s => !s.isAutoSnapshot); break;
                case FilterMode.Auto:   f = f.Where(s => s.isAutoSnapshot);  break;
                case FilterMode.Pinned: f = f.Where(s => s.isPinned);        break;
                case FilterMode.Recent: f = f.Where(s => (DateTime.Now - s.timestamp) < TimeSpan.FromHours(24)); break;
                case FilterMode.Large:  f = f.Where(s => s.fileSize > 1024 * 1024); break;
            }

            switch (sortMode)
            {
                case SortMode.Newest:       f = f.OrderByDescending(s => s.timestamp); break;
                case SortMode.Oldest:       f = f.OrderBy(s => s.timestamp); break;
                case SortMode.Largest:      f = f.OrderByDescending(s => s.fileSize); break;
                case SortMode.Smallest:     f = f.OrderBy(s => s.fileSize); break;
                case SortMode.Alphabetical: f = f.OrderBy(s => s.sceneName); break;
            }

            return f.ToList();
        }

        private static void UpdateStatistics()
        {
            totalSnapshots = snapshots.Count;
            totalStorageUsed = snapshots.Sum(s => s.fileSize);
        }

        private static string GetRelativeTimeString(DateTime t)
        {
            var ts = DateTime.Now - t;
            if (ts.TotalMinutes < 1) return "Just now";
            if (ts.TotalMinutes < 60) return $"{ts.Minutes}m ago";
            if (ts.TotalHours < 24) return $"{ts.Hours}h ago";
            if (ts.TotalDays < 7) return $"{ts.Days}d ago";
            if (ts.TotalDays < 365) return t.ToString("MM/dd HH:mm");
            return t.ToString("MM/dd/yy HH:mm");
        }

        private static string GetSnapshotTypeIcon(SnapshotType type) => type switch
        {
            SnapshotType.Manual      => "📸",
            SnapshotType.AutoSave    => "💾",
            SnapshotType.AutoInterval=> "⏰",
            SnapshotType.PlayMode    => "▶️",
            SnapshotType.Milestone   => "⭐",
            SnapshotType.Backup      => "🗄️",
            _                        => "📄"
        };

        private static string FormatBytes(long b)
        {
            if (b >= 1_000_000_000) return $"{b / 1_000_000_000f:F2} GB";
            if (b >= 1_000_000)     return $"{b / 1_000_000f:F2} MB";
            if (b >= 1_000)         return $"{b / 1_000f:F2} KB";
            return $"{b} B";
        }
    }
}
#endif
