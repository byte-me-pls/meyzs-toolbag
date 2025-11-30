#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - State & Configuration
    /// Contains all configuration flags, constants, runtime state and UI state variables.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        // --------- Search Settings ---------
        private static bool includeScenes = true;
        private static bool includePrefabs = true;
        private static bool includeMaterials = true;
        private static bool includeAnimations = true;
        private static bool includeControllers = true;
        private static bool includeScripts = true;
        private static bool includeShaders = true;
        private static bool includeOthers = false;
        private static bool includePackages = false;

        private static bool deepSearch = false;
        private static bool showLineNumbers = true;
        private static bool showContext = true;
        private static bool autoSelectUnused = false;

        private static GroupBy groupBy = GroupBy.ByAsset;
        private static FilterMode filterMode = FilterMode.All;
        private static SortMode sortMode = SortMode.UsageCount;
        private static bool sortAscending = false;
        private static string searchFilter = "";
        private static int minUsageCount = 0;
        private static int maxUsageCount = 999;

        // --------- Performance Constants ---------
        private const int FILES_PER_FRAME = 15;
        private const int REGEX_GUID_CHUNK = 50;
        private const int CONTEXT_MAX = 120;
        private const int CACHE_VALIDITY_HOURS = 2;
        private const int MAX_CACHE_ENTRIES = 10000;

        // --------- Runtime State ---------
        private static bool isSearching = false;
        private static bool cancelRequested = false;
        private static bool hasResults = false;
        private static float scanProgress = 0f;
        private static string scanStatus = "";

        private static Vector2 scrollPos;
        private static Vector2 detailScrollPos;

        private static List<AssetUsageInfo> assetInfoList = new List<AssetUsageInfo>();
        private static AssetUsageInfo selectedAssetInfo = null;

        private static string[] allFilePaths;
        private static int currentFileIndex;
        private static List<string> selectedAssetGUIDs = new List<string>();
        private static Dictionary<string, AssetUsageInfo> guidToInfo = new Dictionary<string, AssetUsageInfo>(StringComparer.OrdinalIgnoreCase);
        private static List<System.Text.RegularExpressions.Regex> guidRegexChunks = new List<System.Text.RegularExpressions.Regex>();

        // --------- Cache Storage ---------
        private static Dictionary<string, string[]> dependencyCache = new Dictionary<string, string[]>();
        private static Dictionary<string, DateTime> cacheTimestamps = new Dictionary<string, DateTime>();
        private static Dictionary<string, long> cacheFileSizes = new Dictionary<string, long>();
        private static Dictionary<string, System.Text.RegularExpressions.Regex> compiledRegexCache = new Dictionary<string, System.Text.RegularExpressions.Regex>();
        private static Dictionary<string, List<System.Text.RegularExpressions.Regex>> guidRegexBatches = new Dictionary<string, List<System.Text.RegularExpressions.Regex>>();
        private static Dictionary<string, FileMetadata> fileMetadataCache = new Dictionary<string, FileMetadata>();

        private struct FileMetadata
        {
            public long size;
            public DateTime lastWrite;
            public bool isBinary;
            public string extension;
        }

        // --------- Thread Safety ---------
        private static readonly object lockObject = new object();
        private static volatile bool processingCancelled = false;

        // --------- Memory Pools ---------
        private static readonly Queue<System.Text.StringBuilder> stringBuilderPool = new Queue<System.Text.StringBuilder>();
        private static readonly Queue<List<string>> stringListPool = new Queue<List<string>>();

        // --------- UI Styles ---------
        private static GUIStyle redStyle, greenStyle, yellowStyle, boldStyle;
    }
}
#endif
