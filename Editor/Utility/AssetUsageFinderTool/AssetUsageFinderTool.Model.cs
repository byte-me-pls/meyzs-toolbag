#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Asset Usage Finder - Models & Enums
    /// Contains all data structures used by the tool.
    /// </summary>
    public static partial class AssetUsageFinderTool
    {
        [Serializable]
        public class AssetUsageInfo
        {
            public string assetPath;
            public string assetGUID;
            public string assetName;
            public string assetType;
            public List<UsageReference> usages = new List<UsageReference>();
            public long fileSize;
            public DateTime lastModified;
            public bool isSelected;
            public int totalReferences;
        }

        [Serializable]
        public class UsageReference
        {
            public string filePath;
            public string fileName;
            public string fileType;
            public string folderPath;
            public int lineNumber;
            public string context;
            public ReferenceType referenceType;
            public DateTime lastModified;
        }

        public enum ReferenceType
        {
            Direct, Component, Material, Animation, Script, Prefab, Unknown
        }

        public enum GroupBy
        {
            None, ByAsset, ByFolder, ByFileType, ByReferenceType, ByUsageCount, ByLastModified
        }

        public enum FilterMode
        {
            All, UnusedOnly, HighUsage, RecentlyModified, SceneOnly, PrefabOnly, MaterialOnly, ScriptOnly
        }

        public enum SortMode
        {
            Name, UsageCount, FileSize, LastModified, AssetType
        }
    }
}
#endif