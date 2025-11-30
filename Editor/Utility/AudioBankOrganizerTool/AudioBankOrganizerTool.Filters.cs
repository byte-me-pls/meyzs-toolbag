#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - Filtering & Sorting.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        private static List<AudioClipInfo> GetFilteredAndSortedClips()
        {
            IEnumerable<AudioClipInfo> q = audioClips;

            // Text search on clip name
            if (!string.IsNullOrEmpty(searchFilter))
                q = q.Where(c => c.clip != null &&
                                 c.clip.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            // Filter by mode
            switch (filterMode)
            {
                case FilterMode.Used:
                    q = q.Where(c => c.isUsed);
                    break;

                case FilterMode.Unused:
                    q = q.Where(c => !c.isUsed);
                    break;

                case FilterMode.RecentlyAdded:
                    q = q.Where(c => c.isImportedRecently);
                    break;

                case FilterMode.LargeFiles:
                    q = q.Where(c => c.fileSize > LARGE_FILE_THRESHOLD);
                    break;

                case FilterMode.LongDuration:
                    q = q.Where(c => c.duration > LONG_DURATION_THRESHOLD);
                    break;

                case FilterMode.UncompressedOnly:
                    q = q.Where(c => c.compressionFormat == AudioCompressionFormat.PCM);
                    break;

                case FilterMode.SpecificFolder:
                    if (!string.IsNullOrEmpty(folderFilter))
                    {
                        string ff = folderFilter.Replace("\\", "/");
                        q = q.Where(c => (c.folderName ?? string.Empty).Replace("\\", "/")
                                .StartsWith(ff, StringComparison.Ordinal));
                    }
                    break;
            }

            // Sorting
            q = sortMode switch
            {
                SortMode.Name            => (sortAscending ? q.OrderBy(c => c.clip?.name)
                                                           : q.OrderByDescending(c => c.clip?.name)),
                SortMode.Size            => (sortAscending ? q.OrderBy(c => c.fileSize)
                                                           : q.OrderByDescending(c => c.fileSize)),
                SortMode.Duration        => (sortAscending ? q.OrderBy(c => c.duration)
                                                           : q.OrderByDescending(c => c.duration)),
                SortMode.Usage           => (sortAscending ? q.OrderBy(c => c.usedInFiles.Count)
                                                           : q.OrderByDescending(c => c.usedInFiles.Count)),
                SortMode.LastModified    => (sortAscending ? q.OrderBy(c => c.lastModified)
                                                           : q.OrderByDescending(c => c.lastModified)),
                SortMode.Frequency       => (sortAscending ? q.OrderBy(c => c.frequency)
                                                           : q.OrderByDescending(c => c.frequency)),
                SortMode.CompressionRatio=> (sortAscending ? q.OrderBy(c => (double)c.fileSize / Math.Max(1.0, c.duration))
                                                           : q.OrderByDescending(c => (double)c.fileSize / Math.Max(1.0, c.duration))),
                _                        => q
            };

            return q.ToList();
        }
    }
}
#endif
