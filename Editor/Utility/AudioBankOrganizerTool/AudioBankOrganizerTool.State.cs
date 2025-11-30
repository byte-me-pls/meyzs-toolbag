#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - State & Configuration.
    /// Holds constants, runtime state, UI state, collections and regex/static resources.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        // ---------- Constants ----------
        private const int FILE_BATCH = 1;                 // read 1 YAML file per frame to keep UI responsive
        private const int CLIP_BATCH = 50;                // analyze 50 clips per frame
        private const long LARGE_FILE_THRESHOLD = 1_000_000; // ~1 MB
        private const float LONG_DURATION_THRESHOLD = 30f;    // 30 seconds

        // ---------- Scan State ----------
        private static bool dataLoaded = false;
        private static bool isScanning = false;
        private static float scanProgress = 0f;
        private static string scanStatus = "";
        // 0: find clips, 1: clip meta, 2: prepare YAML list, 3: build GUID index, 4: bind usages, 5: complete
        private static int phase = 0;

        // ---------- Data ----------
        private static readonly List<AudioClipInfo> audioClips = new List<AudioClipInfo>();

        // YAML/text asset paths (scanned by lines)
        private static List<string> yamlPaths = new List<string>();
        private static int pathIdx = 0;   // progress over yamlPaths
        private static int clipIdx = 0;   // progress over audioClips (metadata pass)

        // GUID -> files index
        private static readonly Dictionary<string, HashSet<string>> guidToFiles =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // ---------- User Options / UI Flags ----------
        private static FilterMode filterMode = FilterMode.All;
        private static SortMode sortMode = SortMode.Name;
        private static bool sortAscending = true;
        private static string searchFilter = "";
        private static string folderFilter = "";
        private static bool includePackages = false;
        private static bool nameHeuristicSearch = false;
        private static bool autoOptimizeOnScan = false;

        // ---------- UI State ----------
        private static Vector2 scrollPosition;
        private static bool showStatistics = true;

        // ---------- Styles ----------
        private static GUIStyle redStyle, greenStyle, yellowStyle;

        // ---------- Regex & Platforms ----------
        // 32-hex GUID pattern inside YAML lines
        private static readonly Regex kGuidRegex =
            new Regex(@"guid:\s*([a-f0-9]{32})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Known audio platform names used by AudioImporter override API
        private static readonly string[] kPlatforms = new[] { "Standalone", "Android", "iPhone" };
    }
}
#endif
