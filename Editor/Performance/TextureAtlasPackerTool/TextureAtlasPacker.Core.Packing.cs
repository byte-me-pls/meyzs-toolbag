#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void StartPacking()
        {
            isPacking = true;
            packingProgress = 0f;
            packingStatus = "Preparing textures...";
            EditorApplication.update += UpdatePacking;
        }

        private static void UpdatePacking()
        {
            try
            {
                packingProgress += 0.12f;
                if (packingProgress >= 1f)
                {
                    EditorApplication.update -= UpdatePacking;
                    isPacking = false;
                    PerformPacking();
                }
                else
                {
                    packingStatus = $"Packing... {(packingProgress * 100f):F0}%";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Packing error: {e.Message}");
                EditorApplication.update -= UpdatePacking;
                isPacking = false;
            }
        }

        private static void PerformPacking()
        {
            var selected = textureInfos.Where(t => t.isSelected && t.originalTexture != null).ToList();
            if (selected.Count == 0) return;

            // Prepare textures (respect trimming) and keep list to dispose later.
            var toPack = new List<Texture2D>();
            var indexToTex = new Dictionary<int, TextureInfo>();
            var tempsToDispose = new List<Texture2D>(); // trimmed temps

            for (int i = 0; i < selected.Count; i++)
            {
                var info = selected[i];
                Texture2D tex = info.originalTexture;

                if (trimBorders && info.trimmedSize != info.originalSize)
                {
                    tex = CreateTrimmedTexture(info, out bool created);
                    if (created && tex != null)
                    {
                        info.processedTexture = tex;
                        tempsToDispose.Add(tex);
                    }
                }

                toPack.Add(tex);
                indexToTex[i] = info;
            }

            // Pack
            Texture2D atlas;
            Rect[] rects;

            switch (packingMode)
            {
                case PackingMode.MaxRects:   (atlas, rects) = PackWithMaxRects(toPack); break;
                case PackingMode.Skyline:    (atlas, rects) = PackWithSkyline(toPack);  break;
                case PackingMode.Custom:     (atlas, rects) = PackWithCustomAlgorithm(toPack); break;
                case PackingMode.UnityBuiltIn:
                default:                     (atlas, rects) = PackWithUnityBuiltIn(toPack); break;
            }

            // Dispose temps no matter success
            foreach (var t in tempsToDispose)
                if (t) UnityEngine.Object.DestroyImmediate(t);

            if (atlas == null || rects == null || rects.Length == 0)
            {
                Debug.LogError("Failed to pack textures. Try fewer textures or a larger atlas.");
                return;
            }

            var result = new AtlasResult
            {
                atlas = atlas,
                entries = new Dictionary<string, AtlasEntry>(),
                size = new Vector2Int(atlas.width, atlas.height),
                totalFileSizeEstimate = EstimateAtlasFileSize(atlas.width, atlas.height),
                efficiency01 = CalculatePackingEfficiency01(rects),
                paddingUsed = padding,
                created = DateTime.Now
            };

            for (int i = 0; i < rects.Length; i++)
            {
                var info = indexToTex[i];
                var entry = new AtlasEntry
                {
                    guid = info.guid,
                    name = info.name,
                    uvRect = rects[i],
                    originalSize = info.originalSize,
                    atlasSize = new Vector2Int(
                        Mathf.RoundToInt(rects[i].width  * atlas.width),
                        Mathf.RoundToInt(rects[i].height * atlas.height)
                    ),
                    trimOffset = info.trimOffset,
                    wasTrimmed = trimBorders && info.trimmedSize != info.originalSize
                };

                result.entries[info.guid] = entry; // GUID-unique
            }

            if (currentResult != null) atlasHistory.Add(currentResult);
            currentResult = result;

            Debug.Log($"Atlas packed using {packingMode}. Count={selected.Count}, Eff={result.efficiency01:P1}");
        }

        private static TextureFormat GetTextureFormat()
        {
            switch (atlasFormat)
            {
                case AtlasFormat.RGBA32:     return TextureFormat.RGBA32;
                case AtlasFormat.RGB24:      return TextureFormat.RGB24;
                case AtlasFormat.DXT5:       return TextureFormat.DXT5;
                case AtlasFormat.DXT1:       return TextureFormat.DXT1;
                case AtlasFormat.ETC2_RGBA8: return TextureFormat.ETC2_RGBA8;
                case AtlasFormat.ASTC_4x4:   return TextureFormat.ASTC_4x4;
                case AtlasFormat.Auto:
                default:
                    bool anyAlpha = textureInfos.Any(t => t.isSelected && t.hasAlpha);
                    return anyAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            }
        }

        private static float CalculatePackingEfficiency01(Rect[] rects)
        {
            float used = rects.Sum(r => r.width * r.height); // UV area sum (0..1)
            return Mathf.Clamp01(used);
        }

        private static int BytesPerBlock(TextureFormat fmt)
        {
            switch (fmt)
            {
                case TextureFormat.DXT1:       return 8;
                case TextureFormat.DXT5:       return 16;
                case TextureFormat.ASTC_4x4:   return 16;
                case TextureFormat.ETC2_RGBA8: return 16;
                default: return 0;
            }
        }

        private static long EstimateAtlasFileSize(int w, int h)
        {
            var fmt = GetTextureFormat();
            int bpb = BytesPerBlock(fmt);
            if (bpb > 0)
            {
                int bw = Mathf.CeilToInt(w / 4f);
                int bh = Mathf.CeilToInt(h / 4f);
                return (long)bw * bh * bpb;
            }

            int bpp = (fmt == TextureFormat.RGB24) ? 3 : 4;
            long baseBytes = (long)w * h * bpp;
            if (enableMipMaps) baseBytes = (long)(baseBytes * 1.3333f);
            return baseBytes;
        }
    }
}
#endif
