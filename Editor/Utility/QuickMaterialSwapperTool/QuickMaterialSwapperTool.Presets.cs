// QuickMaterialSwapperTool.Presets.cs
#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        private static List<MaterialPreset> GetFilteredPresets()
        {
            IEnumerable<MaterialPreset> filtered = presets;

            if (!string.IsNullOrEmpty(searchFilter))
                filtered = filtered.Where(p => p.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);

            switch (filterMode)
            {
                case FilterMode.Favorites:     filtered = filtered.Where(p => p.isFavorite); break;
                case FilterMode.RecentlyUsed:  filtered = filtered.Where(p => p.usageCount > 0).OrderByDescending(p => p.usageCount); break;
                case FilterMode.ByTag:         if (!string.IsNullOrEmpty(tagFilter)) filtered = filtered.Where(p => p.tags != null && p.tags.Contains(tagFilter)); break;
                case FilterMode.ByShader:
                    var chosenShader = chosenMaterial?.shader;
                    if (chosenShader != null)
                        filtered = filtered.Where(p =>
                        {
                            var mat = MaterialUtils.GetMaterialFromGUID(p.guid);
                            return mat != null && mat.shader == chosenShader;
                        });
                    break;
                case FilterMode.Unused:        filtered = filtered.Where(p => p.usageCount == 0); break;
            }

            return filtered
                .OrderByDescending(p => p.isFavorite)
                .ThenByDescending(p => p.usageCount)
                .ThenBy(p => p.name)
                .ToList();
        }

        private static void QuickSavePreset(Material material)
        {
            if (material == null) return;

            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
            if (presets.Any(p => p.guid == guid)) return;

            var preset = new MaterialPreset
            {
                name = material.name,
                guid = guid,
                tags = "",
                created = System.DateTime.Now,
                usageCount = 0,
                isFavorite = false,
                thumbnailColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white
            };

            presets.Add(preset);
            SaveData();
            Debug.Log($"Saved preset: {material.name}");
        }

        private static void ShowSavePresetDialog()
        {
            if (chosenMaterial == null) return;

            int choice = EditorUtility.DisplayDialogComplex(
                "Save Material Preset",
                $"Save '{chosenMaterial.name}' as preset?",
                "Save",
                "Save with Tags",
                "Cancel"
            );

            if (choice == 0) QuickSavePreset(chosenMaterial);
            else if (choice == 1) ShowAdvancedSaveDialog();
        }

        private static void ShowAdvancedSaveDialog()
        {
            // Basit etkileşim: Tag girişini input window ile yapalım
            string tags = EditorUtility.DisplayDialog("Add Tags", "Preset will be saved. You can edit tags later from Edit.", "OK")
                ? "" : "";

            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(chosenMaterial));
            var existing = presets.FirstOrDefault(p => p.guid == guid);

            if (existing != null) existing.tags = tags;
            else
            {
                var preset = new MaterialPreset
                {
                    name = chosenMaterial.name,
                    guid = guid,
                    tags = tags,
                    created = System.DateTime.Now,
                    usageCount = 0,
                    isFavorite = false,
                    thumbnailColor = chosenMaterial.HasProperty("_Color") ? chosenMaterial.GetColor("_Color") : Color.white
                };
                presets.Add(preset);
            }

            SaveData();
        }

        private static void ShowEditPresetDialog(MaterialPreset preset)
        {
            if (preset == null) return;

            bool delete = EditorUtility.DisplayDialog(
                "Edit Preset",
                $"Preset: {preset.name}\nUsage: {preset.usageCount}\nTags: {preset.tags}",
                "Delete",
                "Cancel"
            );

            if (delete) DeletePreset(preset);
        }

        private static void DeletePreset(MaterialPreset preset)
        {
            if (preset == null) return;
            if (!EditorUtility.DisplayDialog("Delete Preset", $"Delete preset '{preset.name}'?", "Delete", "Cancel"))
                return;

            presets.Remove(preset);
            if (selectedPreset == preset) selectedPreset = null;
            SaveData();
        }

        private static void ToggleFavorite(MaterialPreset preset)
        {
            if (preset == null) return;
            preset.isFavorite = !preset.isFavorite;
            SaveData();
        }

        private static void ExportPresets()
        {
            string exportPath = EditorUtility.SaveFilePanel("Export Material Presets", "", "MaterialPresets", "json");
            if (string.IsNullOrEmpty(exportPath)) return;

            try
            {
                var data = new PresetsData { presets = presets };
                string json = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(exportPath, json);
                Debug.Log($"Presets exported to: {exportPath}");
                EditorUtility.DisplayDialog("Export Complete", $"Presets exported to:\n{exportPath}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export presets: {e.Message}");
            }
        }

        private static void ImportPresets()
        {
            string importPath = EditorUtility.OpenFilePanel("Import Material Presets", "", "json");
            if (string.IsNullOrEmpty(importPath)) return;

            try
            {
                string json = System.IO.File.ReadAllText(importPath);
                var data = JsonUtility.FromJson<PresetsData>(json);

                int imported = 0;
                foreach (var p in data.presets)
                    if (!presets.Any(x => x.guid == p.guid)) { presets.Add(p); imported++; }

                SaveData();
                Debug.Log($"Imported {imported} new presets");
                EditorUtility.DisplayDialog("Import Complete", $"Imported {imported} new presets", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import presets: {e.Message}");
                EditorUtility.DisplayDialog("Import Failed", $"Failed to import presets:\n{e.Message}", "OK");
            }
        }

        // Preset list/grid/compact UI
        private static void DrawPresetList(List<MaterialPreset> list)
        {
            foreach (var preset in list)
            {
                EditorGUILayout.BeginHorizontal(preset == selectedPreset ? EditorStyles.helpBox : GUI.skin.box);

                if (GUILayout.Button(preset.isFavorite ? "⭐" : "☆", GUILayout.Width(25)))
                    ToggleFavorite(preset);

                var mat = MaterialUtils.GetMaterialFromGUID(preset.guid);
                var nameStyle = mat == null ? redStyle : EditorStyles.label;
                if (GUILayout.Button(preset.name, nameStyle, GUILayout.Width(180)))
                {
                    selectedPreset = preset;
                    if (mat != null) { chosenMaterial = mat; preset.usageCount++; }
                }

                if (!string.IsNullOrEmpty(preset.tags))
                    EditorGUILayout.LabelField($"Tags: {preset.tags}", EditorStyles.miniLabel, GUILayout.Width(140));

                EditorGUILayout.LabelField($"Used: {preset.usageCount}", EditorStyles.miniLabel, GUILayout.Width(70));

                if (mat != null && GUILayout.Button("📌", GUILayout.Width(25)))
                    EditorGUIUtility.PingObject(mat);
                if (GUILayout.Button("✏️", GUILayout.Width(25)))
                    ShowEditPresetDialog(preset);

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawPresetGrid(List<MaterialPreset> list)
        {
            const int perRow = 3;
            for (int i = 0; i < list.Count; i += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < perRow && i + j < list.Count; j++)
                    DrawPresetGridItem(list[i + j]);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawPresetGridItem(MaterialPreset preset)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(140), GUILayout.Height(84));
            var mat = MaterialUtils.GetMaterialFromGUID(preset.guid);
            var nameStyle = mat == null ? redStyle : EditorStyles.label;

            if (GUILayout.Button(preset.name, nameStyle))
            {
                selectedPreset = preset;
                if (mat != null) { chosenMaterial = mat; preset.usageCount++; }
            }

            EditorGUILayout.LabelField($"{(preset.isFavorite ? "⭐" : "")} Used: {preset.usageCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void DrawPresetCompact(List<MaterialPreset> list)
        {
            foreach (var preset in list)
            {
                if (GUILayout.Button($"{(preset.isFavorite ? "⭐" : "☆")} {preset.name} ({preset.usageCount})", GUILayout.Height(22)))
                {
                    selectedPreset = preset;
                    var mat = MaterialUtils.GetMaterialFromGUID(preset.guid);
                    if (mat != null) { chosenMaterial = mat; preset.usageCount++; }
                }
            }
        }
    }
}
#endif
