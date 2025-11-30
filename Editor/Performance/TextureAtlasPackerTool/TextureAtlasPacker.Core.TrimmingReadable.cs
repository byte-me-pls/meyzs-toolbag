#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static Texture2D CreateTrimmedTexture(TextureInfo info, out bool created)
        {
            created = false;
            var original = info.originalTexture;
            if (!original) return null;

            using (var state = MakeTextureReadableScope(original))
            {
                var trimmed = new Texture2D(info.trimmedSize.x, info.trimmedSize.y, TextureFormat.RGBA32, false);
                var px = original.GetPixels(info.trimOffset.x, info.trimOffset.y, info.trimmedSize.x, info.trimmedSize.y);
                trimmed.SetPixels(px);
                trimmed.Apply();
                created = true;
                return trimmed;
            }
        }

        private static void AnalyzeTextureProperties(TextureInfo info)
        {
            if (!info.originalTexture) return;

            info.hasAlpha = HasAlphaChannel(info.originalTexture);
            info.dominantColor = CalculateDominantColor(info.originalTexture);

            if (trimBorders)
            {
                var r = CalculateTrimBounds(info.originalTexture);
                info.trimmedSize = r.size;
                info.trimOffset = r.offset;
            }
            else
            {
                info.trimmedSize = info.originalSize;
                info.trimOffset = Vector2Int.zero;
            }
        }

        private static bool HasAlphaChannel(Texture2D texture)
        {
            try
            {
                using (var s = MakeTextureReadableScope(texture))
                {
                    var px = texture.GetPixels32();
                    for (int i = 0; i < px.Length; i++)
                        if (px[i].a < 250)
                            return true;
                    return false;
                }
            }
            catch
            {
                var f = texture.format;
                return f == TextureFormat.RGBA32 ||
                       f == TextureFormat.ARGB32 ||
                       f == TextureFormat.DXT5 ||
                       f == TextureFormat.ETC2_RGBA8;
            }
        }

        private static Color CalculateDominantColor(Texture2D texture)
        {
            try
            {
                using (var s = MakeTextureReadableScope(texture))
                {
                    int stepX = Mathf.Max(1, texture.width  / 64);
                    int stepY = Mathf.Max(1, texture.height / 64);

                    double r = 0, g = 0, b = 0;
                    long count = 0;
                    for (int y = 0; y < texture.height; y += stepY)
                        for (int x = 0; x < texture.width; x += stepX)
                        {
                            var c = texture.GetPixel(x, y);
                            r += c.r; g += c.g; b += c.b; count++;
                        }

                    if (count == 0) return Color.white;
                    return new Color((float)(r / count), (float)(g / count), (float)(b / count), 1f);
                }
            }
            catch { return Color.white; }
        }

        private static (Vector2Int size, Vector2Int offset) CalculateTrimBounds(Texture2D texture)
        {
            using (var s = MakeTextureReadableScope(texture))
            {
                int w = texture.width, h = texture.height;
                var px = texture.GetPixels32();

                int minX = w, minY = h, maxX = -1, maxY = -1;

                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var a = px[y * w + x].a / 255f;
                    if (a > trimThreshold)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < minX || maxY < minY) return (new Vector2Int(1, 1), Vector2Int.zero);

                var size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
                var off  = new Vector2Int(minX, minY);
                return (size, off);
            }
        }

        // ====== Importer Readable Scope ======
        private sealed class ReadableScope : IDisposable
        {
            private readonly TextureImporter _importer;
            private readonly bool _prevReadable;
            private readonly bool _changed;

            public ReadableScope(TextureImporter importer, bool prevReadable, bool changed)
            {
                _importer = importer; _prevReadable = prevReadable; _changed = changed;
            }

            public void Dispose()
            {
                if (_changed && _importer != null)
                {
                    _importer.isReadable = _prevReadable;
                    _importer.SaveAndReimport();
                }
            }
        }

        private static ReadableScope MakeTextureReadableScope(Texture2D tex)
        {
            var path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool prev = importer.isReadable;
                if (!prev)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    return new ReadableScope(importer, prev, true);
                }
                return new ReadableScope(importer, prev, false);
            }
            return new ReadableScope(null, false, false);
        }
    }
}
#endif
