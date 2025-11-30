#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        // ====== List / CRUD / Analysis ======
        private static List<TextureInfo> GetFilteredAndSortedTextures()
        {
            IEnumerable<TextureInfo> q = textureInfos;

            if (!string.IsNullOrEmpty(searchFilter))
                q = q.Where(t => t.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                              || t.path.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            switch (filterMode)
            {
                case FilterMode.Selected:     q = q.Where(t => t.isSelected); break;
                case FilterMode.HasAlpha:     q = q.Where(t => t.hasAlpha);   break;
                case FilterMode.NoAlpha:      q = q.Where(t => !t.hasAlpha);  break;
                case FilterMode.LargeTextures:q = q.Where(t => t.originalSize.x > 512 || t.originalSize.y > 512); break;
                case FilterMode.SmallTextures:q = q.Where(t => t.originalSize.x <= 128 && t.originalSize.y <= 128); break;
            }

            Func<TextureInfo, object> keySelector = t => t.name;
            switch (sortMode)
            {
                case SortMode.Name:        keySelector = t => t.name; break;
                case SortMode.Size:        keySelector = t => t.originalSize.x * t.originalSize.y; break;
                case SortMode.Area:        keySelector = t => t.trimmedSize.x  * t.trimmedSize.y;  break;
                case SortMode.Format:      keySelector = t => t.format.ToString(); break;
                case SortMode.AspectRatio: keySelector = t => (float)t.originalSize.x / Mathf.Max(1, t.originalSize.y); break;
                default:                   keySelector = t => t.name; break;
            }

            q = sortDescending ? q.OrderByDescending(keySelector) : q.OrderBy(keySelector);
            return q.ToList();
        }

        private static void ClearAllTextures()
        {
            if (EditorUtility.DisplayDialog("Clear All Textures", "Remove all textures from the list?", "Clear", "Cancel"))
            {
                textureInfos.Clear();
                currentResult = null;
            }
        }

        private static void RemoveSelectedTextures()
        {
            textureInfos.RemoveAll(t => t.isSelected);
            currentResult = null;
        }

        private static void RemoveTexture(TextureInfo t)
        {
            textureInfos.Remove(t);
            currentResult = null;
        }

        private static void SelectAllTextures(bool v)
        {
            foreach (var t in textureInfos) t.isSelected = v;
        }

        private static void RefreshTextureInfo()
        {
            foreach (var t in textureInfos)
                if (t.originalTexture) AnalyzeTextureProperties(t);
        }

        private static void AnalyzeTextures()
        {
            var sel = textureInfos.Where(t => t.isSelected).ToList();
            if (sel.Count == 0) return;

            string report = "🔍 TEXTURE ANALYSIS REPORT 🔍\n";
            report += "═══════════════════════════════════════\n";
            report += $"Selected: {sel.Count}\n";
            report += $"Total Original Size: {FormatBytes(sel.Sum(t => t.fileSize))}\n";
            report += $"Avg Px: {sel.Average(t => (double)t.originalSize.x * t.originalSize.y):F0}\n";
            report += $"With Alpha: {sel.Count(t => t.hasAlpha)}\n\n";

            foreach (var g in sel.GroupBy(t => t.format).OrderByDescending(g => g.Count()))
                report += $"• {g.Key}: {g.Count()} textures\n";

            report += "\nSize Distribution:\n";
            foreach (var g in sel.GroupBy(t => GetSizeCategory(t.originalSize)).OrderBy(g => g.Key))
                report += $"• {g.Key}: {g.Count()}\n";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Texture Analysis", "Analysis complete. Check Console for details.", "OK");
        }

        private static string GetSizeCategory(Vector2Int s)
        {
            int m = Mathf.Max(s.x, s.y);
            if (m <= 64) return "Tiny (≤64)";
            if (m <= 128) return "Small (≤128)";
            if (m <= 256) return "Medium (≤256)";
            if (m <= 512) return "Large (≤512)";
            if (m <= 1024) return "Very Large (≤1024)";
            return "Huge (>1024)";
        }

        private static void EstimatePackingResult()
        {
            var sel = textureInfos.Where(t => t.isSelected && t.originalTexture != null).ToList();
            if (sel.Count == 0) return;

            long totalPx = sel.Sum(t => (long)t.trimmedSize.x * t.trimmedSize.y);
            long atlasPx = (long)atlasSize * atlasSize;
            float eff = Mathf.Min(1f, (float)totalPx / atlasPx);

            string msg = $"Estimated Packing Result:\n\n" +
                         $"Count: {sel.Count}\n" +
                         $"Atlas: {atlasSize}x{atlasSize}\n" +
                         $"Total Px: {totalPx:N0}\n" +
                         $"Atlas Px: {atlasPx:N0}\n" +
                         $"Estimated Efficiency: {eff:P1}\n\n";

            if (eff > 0.9f) msg += "⚠️ Very high fill. Consider larger atlas.\n";
            else if (eff < 0.5f) msg += "💡 Consider smaller atlas for memory.\n";
            else msg += "✅ Looks reasonable.\n";

            EditorUtility.DisplayDialog("Packing Estimate", msg, "OK");
        }

        private static void AddSelectedTextures()
        {
            int added = 0;
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D tex && !textureInfos.Any(t => t.originalTexture == tex))
                {
                    var info = CreateTextureInfo(tex);
                    textureInfos.Add(info);
                    added++;
                }
            }
            Debug.Log($"Added {added} textures from selection.");
        }

        private static void AddFromFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");
            if (string.IsNullOrEmpty(folder)) return;

            if (folder.StartsWith(Application.dataPath))
                folder = "Assets" + folder.Substring(Application.dataPath.Length);

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int added = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex && !textureInfos.Any(t => t.originalTexture == tex))
                {
                    var info = CreateTextureInfo(tex);
                    textureInfos.Add(info);
                    added++;
                }
            }
            Debug.Log($"Added {added} textures from folder.");
        }

        private static TextureInfo CreateTextureInfo(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            string guid = AssetDatabase.AssetPathToGUID(path);

            long fileSize = 0;
            try
            {
                var abs = Path.GetFullPath(path);
                var fi = new FileInfo(abs);
                if (fi.Exists) fileSize = fi.Length;
                else
                {
                    var abs2 = Path.Combine(Directory.GetCurrentDirectory(), path);
                    var fi2 = new FileInfo(abs2);
                    if (fi2.Exists) fileSize = fi2.Length;
                }
            }
            catch { /* ignore */ }

            var info = new TextureInfo
            {
                guid = guid,
                path = path,
                originalTexture = tex,
                processedTexture = null,
                name = tex.name,
                originalSize = new Vector2Int(tex.width, tex.height),
                trimmedSize = new Vector2Int(tex.width, tex.height),
                trimOffset = Vector2Int.zero,
                fileSize = fileSize,
                format = tex.format,
                isSelected = true
            };

            AnalyzeTextureProperties(info);
            return info;
        }

        private static void ShowTextureDetails(TextureInfo t)
        {
            string details =
                $"🖼️ TEXTURE DETAILS 🖼️\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"Name: {t.name}\n" +
                $"GUID: {t.guid}\n" +
                $"Path: {t.path}\n" +
                $"Original Size: {t.originalSize.x}x{t.originalSize.y}\n" +
                $"Trimmed Size: {t.trimmedSize.x}x{t.trimmedSize.y}\n" +
                $"Trim Offset: {t.trimOffset.x}, {t.trimOffset.y}\n" +
                $"Format: {t.format}\n" +
                $"File Size: {FormatBytes(t.fileSize)}\n" +
                $"Has Alpha: {(t.hasAlpha ? "Yes" : "No")}\n" +
                $"Dominant Color: #{ColorUtility.ToHtmlStringRGB(t.dominantColor)}\n";
            EditorUtility.DisplayDialog("Texture Details", details, "OK");
        }

        private static void RestoreFromHistory(AtlasResult r)
        {
            if (currentResult != null) atlasHistory.Add(currentResult);
            atlasHistory.Remove(r);
            currentResult = r;
        }

        private static string FormatBytes(long b)
        {
            if (b >= 1_000_000_000) return $"{b / 1_000_000_000f:F2} GB";
            if (b >= 1_000_000)     return $"{b / 1_000_000f:F2} MB";
            if (b >= 1_000)         return $"{b / 1_000f:F2} KB";
            return $"{b} B";
        }

        private static void DestroyAtlas(Texture2D tex)
        {
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
        }

        // ====== Save / Export / Optimize ======

        private static void SaveAtlas()
        {
            if (currentResult?.atlas == null) return;
            string path = EditorUtility.SaveFilePanelInProject("Save Atlas", "TextureAtlas", "png", "Choose save location");
            if (string.IsNullOrEmpty(path)) return;
            SaveAtlasTexture(path, currentResult.atlas);
        }

        private static void ExportAll()
        {
            if (currentResult?.atlas == null) return;

            string basePath = EditorUtility.SaveFilePanelInProject("Export Atlas Package", "TextureAtlas", "", "Choose base name");
            if (string.IsNullOrEmpty(basePath)) return;

            string atlasPath = basePath + ".png";
            SaveAtlasTexture(atlasPath, currentResult.atlas);

            if (generateMaterial)
            {
                string matPath = basePath + ".mat";
                CreateAtlasMaterial(matPath, atlasPath);
            }

            if (exportMetadata)
            {
                string jsonPath = basePath + ".json";
                ExportMetadata(jsonPath);

                string scriptPath = basePath + "_UVMapping.cs";
                GenerateUVMappingScript(scriptPath);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Atlas package exported: {basePath}");
        }

        // Replace this whole method
        private static void SaveAtlasTexture(string path, Texture2D tex)
        {
            byte[] data = tex.EncodeToPNG();
            File.WriteAllBytes(path, data);
            AssetDatabase.ImportAsset(path);

            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.textureType   = TextureImporterType.Default;
                imp.isReadable    = true;
                imp.mipmapEnabled = enableMipMaps;
                imp.sRGBTexture   = !linearColorSpace;

                // Default compression flag
                imp.textureCompression = TextureImporterCompression.Uncompressed;

                void SetPlatform(string platform, TextureImporterFormat fmt)
                {
                    var ps = imp.GetPlatformTextureSettings(platform);
                    ps.overridden = true;
                    ps.maxTextureSize = Mathf.Max(atlasSize, 256);
                    ps.format = fmt;
                    imp.SetPlatformTextureSettings(ps);
                }

                switch (atlasFormat)
                {
                    case AtlasFormat.DXT1:
                        SetPlatform("Standalone", TextureImporterFormat.DXT1);
                        break;
                    case AtlasFormat.DXT5:
                        SetPlatform("Standalone", TextureImporterFormat.DXT5);
                        break;
                    case AtlasFormat.ETC2_RGBA8:
                        SetPlatform("Android", TextureImporterFormat.ETC2_RGBA8);
                        SetPlatform("iPhone",  TextureImporterFormat.ASTC_4x4);
                        break;
                    case AtlasFormat.ASTC_4x4:
                        SetPlatform("Android", TextureImporterFormat.ASTC_4x4);
                        SetPlatform("iPhone",  TextureImporterFormat.ASTC_4x4);
                        break;
                    case AtlasFormat.RGB24:
                    case AtlasFormat.RGBA32:
                    case AtlasFormat.Auto:
                    default:
                        // keep as imported
                        break;
                }

                imp.SaveAndReimport();
            }
        }

        private static void CreateAtlasMaterial(string materialPath, string atlasPath)
        {
            var mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        private static void ExportMetadata(string path)
        {
            var md = new AtlasMetadata
            {
                atlasSize = currentResult.size,
                entries   = currentResult.entries.Values.ToArray(),
                settings  = new PackingSettings
                {
                    padding     = padding,
                    trimBorders = trimBorders,
                    atlasFormat = GetTextureFormat().ToString(),
                    created     = currentResult.created
                }
            };
            File.WriteAllText(path, JsonUtility.ToJson(md, true));
            AssetDatabase.ImportAsset(path);
        }

        private static void GenerateUVMappingScript(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("// Auto-generated UV mapping for texture atlas (GUID-keyed)");
            sb.AppendLine("public static class AtlasUVMapping");
            sb.AppendLine("{");
            sb.AppendLine("    public static readonly Dictionary<string, Rect> UVMappings = new Dictionary<string, Rect>");
            sb.AppendLine("    {");
            foreach (var e in currentResult.entries.Values)
            {
                sb.AppendLine($"        {{ \"{e.guid}\", new Rect({e.uvRect.x:F4}f, {e.uvRect.y:F4}f, {e.uvRect.width:F4}f, {e.uvRect.height:F4}f) }}, // {e.name}");
            }
            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    public static Rect GetUVByGuid(string guid)");
            sb.AppendLine("    {");
            sb.AppendLine("        return UVMappings.TryGetValue(guid, out var uv) ? uv : new Rect(0,0,1,1);");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
        }

        private static void OptimizeAtlas()
        {
            if (currentResult?.atlas == null) return;
            int choice = EditorUtility.DisplayDialogComplex("Optimize Atlas", "Choose optimization:", "Reduce Size", "Compress", "Cancel");
            switch (choice)
            {
                case 0: OptimizeAtlasSize(); break;
                case 1: CompressAtlas();     break;
            }
        }

        private static void OptimizeAtlasSize()
        {
            int original = atlasSize;
            int[] sizes = new[] { 256, 512, 1024, 2048, 4096, 8192 };
            var sel = textureInfos.Where(t => t.isSelected && t.originalTexture != null).ToList();

            List<Texture2D> toPack = new List<Texture2D>();
            List<Texture2D> temps  = new List<Texture2D>();

            try
            {
                foreach (var s in sizes)
                {
                    if (s >= original) continue;

                    toPack.Clear(); temps.Clear();

                    foreach (var info in sel)
                    {
                        Texture2D tex = info.originalTexture;
                        if (trimBorders && info.trimmedSize != info.originalSize)
                        {
                            tex = CreateTrimmedTexture(info, out bool created);
                            if (created && tex) temps.Add(tex);
                        }
                        toPack.Add(tex);
                    }

                    var test  = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    var rects = test.PackTextures(toPack.ToArray(), padding, s, false);
                    UnityEngine.Object.DestroyImmediate(test);

                    foreach (var t in temps) if (t) UnityEngine.Object.DestroyImmediate(t);

                    if (rects != null && rects.Length == toPack.Count)
                    {
                        atlasSize = s;
                        EditorUtility.DisplayDialog("Optimization", $"Atlas can be reduced to {s}x{s}. Repacking…", "OK");
                        PerformPacking();
                        return;
                    }
                }

                EditorUtility.DisplayDialog("Optimization", "No smaller size fits all textures.", "OK");
            }
            finally
            {
                foreach (var t in temps) if (t) UnityEngine.Object.DestroyImmediate(t);
                atlasSize = original;
            }
        }

        private static void CompressAtlas()
        {
            EditorUtility.DisplayDialog("Compression", "Texture compression optimization is a future update.", "OK");
        }
    }
}
#endif
