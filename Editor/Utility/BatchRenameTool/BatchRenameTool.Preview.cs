#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// BatchRenameTool - Selection gathering, preview generation and apply/cancel.
    /// </summary>
    public static partial class BatchRenameTool
    {
        // ---------------------------------------------------------------------
        // Selection data
        // ---------------------------------------------------------------------
        private static (List<GameObject> gameObjects, List<string> assetPaths) GetSelectionData()
        {
            // GameObjects
            var rawGos = Selection.gameObjects;
            var gos = new List<GameObject>(rawGos.Length * (includeChildren ? 4 : 1));

            foreach (var root in rawGos)
            {
                if (includeChildren)
                {
                    foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
                        gos.Add(t.gameObject);
                }
                else
                {
                    gos.Add(root);
                }
            }

            // De-duplicate
            gos = gos.Distinct().ToList();

            // Filter by component type if requested
            if (filterByType && !string.IsNullOrEmpty(typeFilter))
                gos = gos.Where(go => go.GetComponent(typeFilter) != null).ToList();

            // Sort by name if requested
            if (sortSelection)
                gos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            // Assets
            var rawAssets = renameAssets
                ? Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets)
                    .Where(a => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a)))
                    .ToArray()
                : Array.Empty<UnityEngine.Object>();

            var assetPaths = rawAssets
                .Select(a => AssetDatabase.GetAssetPath(a))
                .Distinct()
                .ToList();

            if (sortSelection)
                assetPaths.Sort(StringComparer.OrdinalIgnoreCase);

            return (gos, assetPaths);
        }

        // ---------------------------------------------------------------------
        // Preview generation
        // ---------------------------------------------------------------------
        private static void PreparePreview(List<GameObject> gos, List<string> assetPaths)
        {
            previewGos.Clear();
            previewAssets.Clear();

            previewTimestamp = addTimestamp ? DateTime.Now.ToString("yyyyMMdd_HHmmss") : null;

            int idx = startIndex;

            // GameObjects
            foreach (var go in gos)
            {
                var preview = new PreviewGameObject
                {
                    go = go,
                    oldName = go.name
                };

                try
                {
                    string parentName   = go.transform.parent ? go.transform.parent.name : "";
                    string componentType = "Transform";
                    var mainComponent    = go.GetComponents<Component>().FirstOrDefault(c => !(c is UnityEngine.Transform));
                    if (mainComponent != null) componentType = mainComponent.GetType().Name;

                    preview.newName = ComputeNewName(go.name, ref idx, componentType, parentName, previewTimestamp);

                    // Validation
                    if (string.IsNullOrEmpty(preview.newName))
                    {
                        preview.hasError = true;
                        preview.errorMessage = "Empty name is not allowed.";
                    }
                    else if (!IsValidGameObjectName(preview.newName))
                    {
                        preview.hasError = true;
                        preview.errorMessage = "Invalid GameObject name.";
                    }
                    else if (skipIfExists)
                    {
                        var existing = GameObject.Find(preview.newName);
                        if (existing != null && existing != go)
                        {
                            preview.hasError = true;
                            preview.errorMessage = "A GameObject with this name already exists in the scene.";
                        }
                    }
                }
                catch (Exception e)
                {
                    preview.hasError = true;
                    preview.errorMessage = e.Message;
                    preview.newName = go.name;
                }

                previewGos.Add(preview);
            }

            // Assets
            int assetIdx = startIndex;
            foreach (var path in assetPaths)
            {
                var preview = new PreviewAsset
                {
                    path = path,
                    oldName = Path.GetFileNameWithoutExtension(path)
                };

                try
                {
                    string extension         = Path.GetExtension(path);
                    string nameWithoutExt    = Path.GetFileNameWithoutExtension(path);
                    string newNameWithoutExt = ComputeNewName(nameWithoutExt, ref assetIdx, "", "", previewTimestamp);

                    // For AssetDatabase.RenameAsset() we must pass the name WITHOUT extension.
                    preview.newName = newNameWithoutExt;

                    if (string.IsNullOrEmpty(preview.newName))
                    {
                        preview.hasError = true;
                        preview.errorMessage = "Empty name is not allowed.";
                    }
                    else if (!IsValidFileName(preview.newName))
                    {
                        preview.hasError = true;
                        preview.errorMessage = "Invalid file name.";
                    }
                    else if (skipIfExists)
                    {
                        // Cheap existence check in the same folder
                        var dir       = Path.GetDirectoryName(path).Replace("\\", "/");
                        var candidate = $"{dir}/{preview.newName}{extension}";
                        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidate) != null)
                        {
                            preview.hasError = true;
                            preview.errorMessage = "A file with this name already exists in the folder.";
                        }
                    }
                }
                catch (Exception e)
                {
                    preview.hasError = true;
                    preview.errorMessage = e.Message;
                    preview.newName = Path.GetFileNameWithoutExtension(path);
                }

                previewAssets.Add(preview);
            }

            renamePreviewing = true;
        }

        // ---------------------------------------------------------------------
        // Preview UI
        // ---------------------------------------------------------------------
        private static void DrawPreviewUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🔍 Rename Preview:", EditorStyles.boldLabel);

            int successCount = previewGos.Count(p => !p.hasError) + previewAssets.Count(p => !p.hasError);
            int errorCount   = previewGos.Count(p => p.hasError)  + previewAssets.Count(p => p.hasError);

            EditorGUILayout.LabelField($"✅ Success: {successCount}, ❌ Errors: {errorCount}");
            EditorGUILayout.EndVertical();

            // Scroll list
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(220));

            foreach (var p in previewGos)
            {
                EditorGUILayout.BeginHorizontal();
                var style = new GUIStyle(EditorStyles.label);
                if (p.hasError)
                {
                    style.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"❌ {p.oldName} → {p.newName}", style);
                    if (!string.IsNullOrEmpty(p.errorMessage))
                        EditorGUILayout.LabelField($"({p.errorMessage})", EditorStyles.miniLabel);
                }
                else
                {
                    style.normal.textColor = Color.green;
                    EditorGUILayout.LabelField($"✅ {p.oldName} → {p.newName}", style);
                }
                EditorGUILayout.EndHorizontal();
            }

            foreach (var p in previewAssets)
            {
                EditorGUILayout.BeginHorizontal();
                var style = new GUIStyle(EditorStyles.label);
                if (p.hasError)
                {
                    style.normal.textColor = Color.red;
                    EditorGUILayout.LabelField($"❌ {p.oldName} → {p.newName}", style);
                    if (!string.IsNullOrEmpty(p.errorMessage))
                        EditorGUILayout.LabelField($"({p.errorMessage})", EditorStyles.miniLabel);
                }
                else
                {
                    style.normal.textColor = Color.green;
                    EditorGUILayout.LabelField($"✅ {p.oldName} → {p.newName}", style);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);

            // Apply / Cancel buttons
            DrawTwoColumnButtons(new (string, Action)[]
            {
                ($"✅ Apply Rename ({successCount})", ApplyPreview),
                ("❌ Cancel", CancelPreview)
            }, height: 30f, emphasizeFirst: true);
        }

        // ---------------------------------------------------------------------
        // Apply / Cancel
        // ---------------------------------------------------------------------
        private static void ApplyPreview()
        {
            int successCount = 0;
            int errorCount   = 0;

            // GameObjects
            var validGos = previewGos.Where(p => !p.hasError).ToList();
            if (validGos.Count > 0)
            {
                var gos = validGos.Select(p => p.go).ToArray();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Advanced Batch Rename");
                Undo.RecordObjects(gos, "Advanced Batch Rename");

                foreach (var p in validGos)
                {
                    p.go.name = p.newName;
                    successCount++;
                }

                Undo.CollapseUndoOperations(group);
            }

            // Assets
            var validAssets = previewAssets.Where(p => !p.hasError).ToList();
            if (renameAssets && validAssets.Count > 0)
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (var p in validAssets)
                    {
                        // Note: pass name without extension
                        var error = AssetDatabase.RenameAsset(p.path, p.newName);
                        if (string.IsNullOrEmpty(error)) successCount++;
                        else                               errorCount++;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            // Count errors from invalid previews too
            errorCount += previewGos.Count(p => p.hasError) + previewAssets.Count(p => p.hasError);

            CancelPreview();

            if (errorCount > 0)
                Debug.LogWarning($"Advanced Batch Rename completed: {successCount} successful, {errorCount} skipped/failed.");
            else
                Debug.Log($"Advanced Batch Rename completed successfully: {successCount} items renamed.");
        }

        private static void CancelPreview()
        {
            previewGos.Clear();
            previewAssets.Clear();
            renamePreviewing = false;
            previewTimestamp = null;
        }
    }
}
#endif
