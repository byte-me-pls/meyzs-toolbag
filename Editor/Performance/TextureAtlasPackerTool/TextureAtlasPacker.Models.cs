#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    [Serializable]
    public class TextureInfo
    {
        public string guid;
        public string path;
        public Texture2D originalTexture;
        public Texture2D processedTexture; // temporary trimmed
        public string name;
        public Vector2Int originalSize;
        public Vector2Int trimmedSize;
        public Vector2Int trimOffset;
        public long fileSize;
        public TextureFormat format;
        public bool isSelected;
        public bool hasAlpha;
        public Color dominantColor;
    }

    [Serializable]
    public class AtlasEntry
    {
        public string guid; // key
        public string name; // display only
        public Rect uvRect;
        public Vector2Int originalSize;
        public Vector2Int atlasSize;
        public Vector2Int trimOffset;
        public bool wasTrimmed;
    }

    [Serializable]
    public class AtlasResult
    {
        public Texture2D atlas;
        public Dictionary<string, AtlasEntry> entries; // key: guid
        public Vector2Int size;
        public long totalFileSizeEstimate;
        public float efficiency01; // 0..1
        public int paddingUsed;
        public DateTime created;
    }

    public enum PackingMode { UnityBuiltIn, MaxRects, Skyline, Custom }
    public enum AtlasFormat { Auto, RGBA32, RGB24, DXT5, DXT1, ETC2_RGBA8, ASTC_4x4 }
    public enum FilterMode { All, Selected, HasAlpha, NoAlpha, LargeTextures, SmallTextures, RecentlyAdded }
    public enum SortMode { Name, Size, Area, AspectRatio, Format, DateAdded }

    [Serializable]
    public class AtlasMetadata
    {
        public Vector2Int atlasSize;
        public AtlasEntry[] entries;
        public PackingSettings settings;
    }

    [Serializable]
    public class PackingSettings
    {
        public int padding;
        public bool trimBorders;
        public string atlasFormat;
        public DateTime created;
    }
}
#endif
