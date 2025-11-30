// QuickMaterialSwapperTool.Analytics.cs
#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        private static void AnalyzeMaterials()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0) return;

            var allRenderers = MaterialUtils.CollectRenderersFromSelection(selectedObjects, includeChildren, includeInactive);

            var materialCount = new Dictionary<Material, int>();
            var shaderCount   = new Dictionary<Shader, int>();

            foreach (var r in allRenderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    materialCount[m]        = materialCount.GetValueOrDefault(m, 0) + 1;
                    shaderCount[m.shader]   = shaderCount.GetValueOrDefault(m.shader, 0) + 1;
                }
            }

            string report = "🔍 MATERIAL ANALYSIS REPORT 🔍\n" +
                            "═══════════════════════════════════════\n" +
                            $"Objects Analyzed: {selectedObjects.Length}\n" +
                            $"Renderers Found: {allRenderers.Count}\n" +
                            $"Unique Materials: {materialCount.Count}\n" +
                            $"Unique Shaders: {shaderCount.Count}\n\n" +
                            "Most Used Materials:\n";

            foreach (var kv in materialCount.OrderByDescending(k => k.Value).Take(5))
                report += $"• {kv.Key.name}: {kv.Value} times\n";

            report += "\nShader Distribution:\n";
            foreach (var kv in shaderCount.OrderByDescending(k => k.Value).Take(5))
                report += $"• {kv.Key.name}: {kv.Value} materials\n";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Material Analysis", "Analysis complete. Check Console for details.", "OK");
        }

        private static void ShowMaterialSwapDialog()
        {
            EditorUtility.DisplayDialog(
                "Material Swap",
                "This feature would open a dialog to:\n" +
                "• Replace specific materials with others\n" +
                "• Batch swap by shader type\n" +
                "• Smart material replacement\n\n" +
                "Feature coming in a future update.",
                "OK"
            );
        }

        private static void FindSimilarMaterials()
        {
            if (chosenMaterial == null) return;

            var allMaterials = AssetDatabase.FindAssets("t:Material")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(m => m != null && m != chosenMaterial)
                .ToList();

            var similar = allMaterials.Where(m => m.shader == chosenMaterial.shader).ToList();

            string report = $"🔍 SIMILAR MATERIALS TO '{chosenMaterial.name}' 🔍\n" +
                            "═══════════════════════════════════════\n" +
                            $"Shader: {chosenMaterial.shader.name}\n" +
                            $"Similar Materials Found: {similar.Count}\n\n";

            foreach (var m in similar.Take(10))
                report += $"• {m.name}\n";
            if (similar.Count > 10) report += $"... and {similar.Count - 10} more\n";

            Debug.Log(report);
            EditorUtility.DisplayDialog("Similar Materials", $"Found {similar.Count} similar materials. Check Console for details.", "OK");
        }

        private static void GenerateReport()
        {
            string path = EditorUtility.SaveFilePanel("Save Material Report", "", "MaterialSwapperReport", "txt");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var w = new System.IO.StreamWriter(path))
                {
                    w.WriteLine("🎨 MATERIAL SWAPPER REPORT 🎨");
                    w.WriteLine("═══════════════════════════════════════");
                    w.WriteLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    w.WriteLine($"Total Presets: {presets.Count}");
                    w.WriteLine($"Total Operations: {operationHistory.Count}");
                    w.WriteLine();

                    w.WriteLine("SAVED PRESETS:");
                    w.WriteLine("─────────────────────────────────────");
                    foreach (var p in presets.OrderByDescending(p => p.usageCount))
                    {
                        w.WriteLine($"• {p.name}");
                        w.WriteLine($"  Usage Count: {p.usageCount}");
                        w.WriteLine($"  Favorite: {(p.isFavorite ? "Yes" : "No")}");
                        w.WriteLine($"  Tags: {p.tags ?? "None"}");
                        w.WriteLine($"  Created: {p.created:yyyy-MM-dd}");
                        w.WriteLine();
                    }

                    w.WriteLine("RECENT OPERATIONS:");
                    w.WriteLine("─────────────────────────────────────");
                    foreach (var op in operationHistory.TakeLast(20))
                    {
                        w.WriteLine($"[{op.timestamp:HH:mm:ss}] {op.objectName}");
                        w.WriteLine($"  {op.originalMaterial?.name ?? "None"} → {op.newMaterial?.name ?? "None"}");
                        w.WriteLine();
                    }

                    w.WriteLine("═══════════════════════════════════════");
                    w.WriteLine("Report generated by Meyz's Toolbag");
                }

                Debug.Log($"Material report saved to: {path}");
                EditorUtility.DisplayDialog("Report Generated", $"Report saved to:\n{path}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate report: {e.Message}");
            }
        }

        private static void ExportHistory()
        {
            string path = EditorUtility.SaveFilePanel("Export Operation History", "", "MaterialHistory", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var w = new System.IO.StreamWriter(path))
                {
                    w.WriteLine("Timestamp,ObjectName,ObjectPath,OriginalMaterial,NewMaterial,MaterialIndex");
                    foreach (var op in operationHistory)
                        w.WriteLine($"{op.timestamp:yyyy-MM-dd HH:mm:ss},\"{op.objectName}\",\"{op.objectPath}\",\"{op.originalMaterial?.name ?? "None"}\",\"{op.newMaterial?.name ?? "None"}\",{op.materialIndex}");
                }

                Debug.Log($"History exported to: {path}");
                EditorUtility.DisplayDialog("Export Complete", $"History exported to:\n{path}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export history: {e.Message}");
            }
        }
    }
}
#endif
