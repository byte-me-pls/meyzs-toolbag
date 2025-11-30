#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - Models & Enums.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        [Serializable]
        public class AudioClipInfo
        {
            public AudioClip clip;
            public string path;
            public string guid;
            public long fileSize;
            public float duration;
            public int frequency;
            public int channels;
            public AudioClipLoadType loadType;
            public AudioCompressionFormat compressionFormat;
            public float quality;
            public bool isUsed;
            public List<string> usedInFiles = new List<string>();
            public DateTime lastModified;
            public bool isImportedRecently;
            public string folderName;

            // Platform overrides (key: BuildTargetGroup-like name, e.g., "Standalone", "Android", "iPhone")
            public Dictionary<string, AudioImporterSampleSettings> platformSettings =
                new Dictionary<string, AudioImporterSampleSettings>();

            // Cached defaults
            public AudioImporterSampleSettings defaultSampleSettings;
        }

        public enum SortMode
        {
            Name,
            Size,
            Duration,
            Usage,
            LastModified,
            Frequency,
            CompressionRatio
        }

        public enum FilterMode
        {
            All,
            Used,
            Unused,
            RecentlyAdded,
            LargeFiles,
            LongDuration,
            UncompressedOnly,
            SpecificFolder
        }
    }
}
#endif