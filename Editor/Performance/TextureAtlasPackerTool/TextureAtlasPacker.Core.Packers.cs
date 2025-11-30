#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static (Texture2D atlas, Rect[] rects) PackWithUnityBuiltIn(List<Texture2D> textures)
        {
            var atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, enableMipMaps);
            var rects = atlas.PackTextures(textures.ToArray(), padding, atlasSize, false); // makeNoLongerReadable=false
            return (atlas, rects);
        }

        private static (Texture2D atlas, Rect[] rects) PackWithMaxRects(List<Texture2D> textures)
        {
            var packer = new MaxRectsPacker(atlasSize, atlasSize, allowRotation);
            var rectsPx = new Rect[textures.Count];
            var placements = new List<(Rect rect, Texture2D texture, int index)>();

            var sorted = textures
                .Select((t, i) => new { t, i, area = t.width * t.height })
                .OrderByDescending(x => x.area)
                .ToList();

            foreach (var x in sorted)
            {
                var placed = packer.Insert(x.t.width + padding * 2, x.t.height + padding * 2);
                if (placed.width <= 0 || placed.height <= 0) return (null, null);

                rectsPx[x.i] = new Rect(
                    (placed.x + padding) / (float)atlasSize,
                    (placed.y + padding) / (float)atlasSize,
                    (placed.width - padding * 2) / (float)atlasSize,
                    (placed.height - padding * 2) / (float)atlasSize
                );
                placements.Add((placed, x.t, x.i));
            }

            var atlas = CreateAtlasFromPlacements(placements, atlasSize);
            return (atlas, rectsPx);
        }

        private static (Texture2D atlas, Rect[] rects) PackWithSkyline(List<Texture2D> textures)
        {
            var packer = new SkylinePacker(atlasSize, atlasSize);
            var rectsPx = new Rect[textures.Count];
            var placements = new List<(Rect rect, Texture2D texture, int index)>();

            var sorted = textures
                .Select((t, i) => new { t, i })
                .OrderByDescending(x => x.t.height)
                .ThenByDescending(x => x.t.width)
                .ToList();

            foreach (var x in sorted)
            {
                var placed = packer.Insert(x.t.width + padding * 2, x.t.height + padding * 2);
                if (placed.width <= 0 || placed.height <= 0) return (null, null);

                rectsPx[x.i] = new Rect(
                    (placed.x + padding) / (float)atlasSize,
                    (placed.y + padding) / (float)atlasSize,
                    (placed.width - padding * 2) / (float)atlasSize,
                    (placed.height - padding * 2) / (float)atlasSize
                );
                placements.Add((placed, x.t, x.i));
            }

            var atlas = CreateAtlasFromPlacements(placements, atlasSize);
            return (atlas, rectsPx);
        }

        private static (Texture2D atlas, Rect[] rects) PackWithCustomAlgorithm(List<Texture2D> textures)
        {
            var rectsPx = new Rect[textures.Count];
            var placements = new List<(Rect rect, Texture2D texture, int index)>();
            var occupied = new List<Rect>();

            var sorted = textures
                .Select((t, i) => new { t, i, area = t.width * t.height })
                .OrderByDescending(x => x.area)
                .ToList();

            foreach (var x in sorted)
            {
                var pos = FindBestPosition(x.t.width + padding * 2, x.t.height + padding * 2, occupied, atlasSize);
                if (!pos.HasValue) return (null, null);

                var placed = new Rect(pos.Value.x, pos.Value.y, x.t.width + padding * 2, x.t.height + padding * 2);
                occupied.Add(placed);

                rectsPx[x.i] = new Rect(
                    (placed.x + padding) / (float)atlasSize,
                    (placed.y + padding) / (float)atlasSize,
                    (placed.width - padding * 2) / (float)atlasSize,
                    (placed.height - padding * 2) / (float)atlasSize
                );
                placements.Add((placed, x.t, x.i));
            }

            var atlas = CreateAtlasFromPlacements(placements, atlasSize);
            return (atlas, rectsPx);
        }

        private static Vector2Int? FindBestPosition(int w, int h, List<Rect> occupied, int size)
        {
            for (int y = 0; y <= size - h; y++)
                for (int x = 0; x <= size - w; x++)
                {
                    var r = new Rect(x, y, w, h);
                    if (!occupied.Any(o => r.Overlaps(o))) return new Vector2Int(x, y);
                }
            return null;
        }

        // Replace this whole method
        private static Texture2D CreateAtlasFromPlacements(List<(Rect rect, Texture2D texture, int index)> placements, int size)
        {
            var atlas = new Texture2D(size, size, TextureFormat.RGBA32, enableMipMaps);

            // clear transparent
            var clearRow = Enumerable.Repeat(Color.clear, size).ToArray();
            for (int y = 0; y < size; y++) atlas.SetPixels(0, y, size, 1, clearRow);

            foreach (var p in placements)
            {
                using (var state = MakeTextureReadableScope(p.texture))
                {
                    int startX = Mathf.RoundToInt(p.rect.x) + padding;
                    int startY = Mathf.RoundToInt(p.rect.y) + padding;
                    int w = p.texture.width;
                    int h = p.texture.height;

                    var src = p.texture.GetPixels(0, 0, w, h);
                    atlas.SetPixels(startX, startY, w, h, src);
                }
            }

            atlas.Apply();
            return atlas;
        }
    }
}
#endif
