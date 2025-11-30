#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Cleanup & analysis actions (remove, export, report, analysis, focus, backup).
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        private static void DrawActions()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🛠️ Cleanup Actions:", EditorStyles.boldLabel);

                int selectedCount = 0;
                for (int i = 0; i < foundObjects.Count; i++)
                    if (foundObjects[i].isSelected)
                        selectedCount++;

                if (selectedCount == 0)
                {
                    EditorGUILayout.HelpBox("Select objects to enable cleanup actions.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                Color prev = GUI.backgroundColor;

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button($"🗑️ Remove from Selected ({selectedCount})", GUILayout.Height(26)))
                    RemoveSelected();

                GUI.backgroundColor = new Color(1f, 1f, 0.5f);
                if (GUILayout.Button("📋 Generate Report", GUILayout.Height(26)))
                    GenerateReport();

                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("💾 Export List", GUILayout.Height(26)))
                    ExportList();

                GUI.backgroundColor = prev;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔍 Find Script Origins", GUILayout.Height(24)))
                    FindScriptOrigins();

                if (GUILayout.Button("📊 Analyze Components", GUILayout.Height(24)))
                    AnalyzeComponents();

                if (GUILayout.Button("🎯 Focus in Hierarchy", GUILayout.Height(24)))
                    FocusInHierarchy();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawActions Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void RemoveSelected()
        {
            var selected = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
                if (foundObjects[i].isSelected)
                    selected.Add(foundObjects[i]);

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No objects selected for cleanup.", "OK");
                return;
            }

            int totalMissing = 0;
            for (int i = 0; i < selected.Count; i++) totalMissing += selected[i].missingCount;

            if (!EditorUtility.DisplayDialog(
                    "Remove Missing Scripts",
                    $"Remove missing scripts from {selected.Count} objects?\n\nTotal missing scripts to remove: {totalMissing}\n\nThis action cannot be undone!",
                    "Remove", "Cancel")) return;

            if (createBackup) CreateBackup();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Missing Scripts");

            for (int i = 0; i < selected.Count; i++)
            {
                var info = selected[i];
                if (!info.isPrefab && info.gameObject != null)
                {
                    Undo.RegisterCompleteObjectUndo(info.gameObject, "Remove Missing Scripts");
                    RemoveMissingScriptsRecursive(info.gameObject);
                }
            }

            EditorSceneManager.MarkAllScenesDirty();
            Undo.CollapseUndoOperations(group);

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    var info = selected[i];
                    if (!info.isPrefab) continue;
                    if (string.IsNullOrEmpty(info.path)) continue;

                    var root = PrefabUtility.LoadPrefabContents(info.path);
                    try
                    {
                        if (root != null)
                        {
                            RemoveMissingScriptsRecursive(root);
                            PrefabUtility.SaveAsPrefabAsset(root, info.path);
                        }
                    }
                    finally
                    {
                        if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            for (int i = 0; i < selected.Count; i++)
                foundObjects.Remove(selected[i]);

            Debug.Log($"Removed missing scripts from {selected.Count} objects");
        }

        private static void RemoveSingleObject(MissingScriptInfo info)
        {
            if (info == null) return;

            if (info.isPrefab)
            {
                if (string.IsNullOrEmpty(info.path)) return;

                AssetDatabase.StartAssetEditing();
                try
                {
                    var root = PrefabUtility.LoadPrefabContents(info.path);
                    try
                    {
                        if (root != null)
                        {
                            RemoveMissingScriptsRecursive(root);
                            PrefabUtility.SaveAsPrefabAsset(root, info.path);
                        }
                    }
                    finally
                    {
                        if (root != null) PrefabUtility.UnloadPrefabContents(root);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                }
            }
            else if (info.gameObject != null)
            {
                Undo.RegisterCompleteObjectUndo(info.gameObject, "Remove Missing Scripts");
                RemoveMissingScriptsRecursive(info.gameObject);
                EditorSceneManager.MarkAllScenesDirty();
            }

            foundObjects.Remove(info);
            Debug.Log($"Removed missing scripts from {(info.gameObject != null ? info.gameObject.name : "<deleted>")}");
        }

        private static void ShowObjectDetails(MissingScriptInfo info)
        {
            string details =
                $"🗑️ MISSING SCRIPTS DETAILS 🗑️\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"Object: {(info.gameObject != null ? info.gameObject.name : "<deleted>")}\n" +
                $"Type: {(info.isPrefab ? "Prefab" : "Scene Object")}\n" +
                $"Location: {(info.isPrefab ? info.path : info.sceneName)}\n" +
                $"Missing Scripts: {info.missingCount}\n" +
                $"Last Modified: {info.lastModified:yyyy-MM-dd HH:mm}\n" +
                $"Path: {info.path}\n\n";

            if (info.componentNames.Count > 0)
            {
                details += "Component Analysis:\n";
                for (int i = 0; i < info.componentNames.Count; i++)
                    details += $"• {info.componentNames[i]}\n";
            }

            if (info.gameObject != null)
            {
                var comps = ListPool<Component>.Get();
                try
                {
                    info.gameObject.GetComponents(comps);
                    if (comps.Count > 0)
                    {
                        details += "\nValid Components:\n";
                        for (int i = 0; i < comps.Count; i++)
                            if (comps[i] != null)
                                details += $"• {comps[i].GetType().Name}\n";
                    }
                }
                finally
                {
                    ListPool<Component>.Release(comps);
                }
            }

            EditorUtility.DisplayDialog("Object Details", details, "OK");
        }

        private static void GenerateReport()
        {
            var selected = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
                if (foundObjects[i].isSelected)
                    selected.Add(foundObjects[i]);

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No objects selected for report.", "OK");
                return;
            }

            string reportPath =
                EditorUtility.SaveFilePanel("Save Missing Scripts Report", "", "MissingScriptsReport", "txt");
            if (string.IsNullOrEmpty(reportPath)) return;

            try
            {
                using (var writer = new StreamWriter(reportPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("🗑️ MISSING SCRIPTS REPORT 🗑️");
                    writer.WriteLine("═══════════════════════════════════════");
                    writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Scan Mode: {scanMode}");
                    writer.WriteLine($"Total Objects Found: {foundObjects.Count}");
                    writer.WriteLine($"Objects in Report: {selected.Count}");
                    int sum = 0;
                    for (int i = 0; i < selected.Count; i++) sum += selected[i].missingCount;
                    writer.WriteLine($"Total Missing Scripts: {sum}");
                    writer.WriteLine();

                    bool anyScene = selected.Any(o => !o.isPrefab);
                    if (anyScene)
                    {
                        writer.WriteLine("🎬 SCENE OBJECTS:");
                        writer.WriteLine("─────────────────");
                        for (int i = 0; i < selected.Count; i++)
                        {
                            var obj = selected[i];
                            if (obj.isPrefab) continue;
                            writer.WriteLine($"Scene: {obj.sceneName}");
                            writer.WriteLine($"Object: {obj.path}");
                            writer.WriteLine($"Missing: {obj.missingCount} scripts");
                            writer.WriteLine($"Modified: {obj.lastModified:yyyy-MM-dd HH:mm}");
                            writer.WriteLine();
                        }
                    }

                    bool anyPrefab = selected.Any(o => o.isPrefab);
                    if (anyPrefab)
                    {
                        writer.WriteLine("📦 PREFAB OBJECTS:");
                        writer.WriteLine("──────────────────");
                        for (int i = 0; i < selected.Count; i++)
                        {
                            var obj = selected[i];
                            if (!obj.isPrefab) continue;
                            writer.WriteLine($"Prefab: {obj.path}");
                            writer.WriteLine($"Object: {(obj.gameObject != null ? obj.gameObject.name : "<deleted>")}");
                            writer.WriteLine($"Missing: {obj.missingCount} scripts");
                            writer.WriteLine($"Modified: {obj.lastModified:yyyy-MM-dd HH:mm}");
                            writer.WriteLine();
                        }
                    }

                    writer.WriteLine("═══════════════════════════════════════");
                    writer.WriteLine("Report generated by Meyz's Toolbag");
                }

                Debug.Log($"Missing scripts report saved to: {reportPath}");
                EditorUtility.DisplayDialog("Report Generated", $"Report saved to:\n{reportPath}", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate report: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate report:\n{e.Message}", "OK");
            }
        }

        private static void ExportList()
        {
            var selected = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
                if (foundObjects[i].isSelected)
                    selected.Add(foundObjects[i]);

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No objects selected for export.", "OK");
                return;
            }

            string csvPath =
                EditorUtility.SaveFilePanel("Export Missing Scripts List", "", "MissingScriptsList", "csv");
            if (string.IsNullOrEmpty(csvPath)) return;

            try
            {
                using (var w = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    w.WriteLine("ObjectName,Type,SceneOrPath,MissingCount,LastModified,FullPath");
                    string Q(string s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";

                    for (int i = 0; i < selected.Count; i++)
                    {
                        var obj = selected[i];
                        string type = obj.isPrefab ? "Prefab" : "Scene";
                        string last = obj.lastModified.ToString("yyyy-MM-dd HH:mm");
                        w.WriteLine(
                            $"{Q(obj.gameObject != null ? obj.gameObject.name : "<deleted>")},{Q(type)},{Q(obj.sceneName)},{obj.missingCount},{Q(last)},{Q(obj.path)}");
                    }
                }

                Debug.Log($"Missing scripts list exported to: {csvPath}");
                EditorUtility.DisplayDialog("Export Complete", $"List exported to:\n{csvPath}", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export list: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to export list:\n{e.Message}", "OK");
            }
        }

        private static void FindScriptOrigins()
        {
            var selected = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
                if (foundObjects[i].isSelected)
                    selected.Add(foundObjects[i]);

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No objects selected for analysis.", "OK");
                return;
            }

            var sb = new StringBuilder(1024);
            sb.AppendLine("🔍 SCRIPT ORIGIN ANALYSIS 🔍");
            sb.AppendLine("═══════════════════════════════════════");

            var sceneCount = new Dictionary<string, (int objCount, int missSum)>(StringComparer.OrdinalIgnoreCase);
            var folderCount = new Dictionary<string, (int objCount, int missSum)>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < selected.Count; i++)
            {
                var it = selected[i];
                if (it.isPrefab)
                {
                    string dir = Path.GetDirectoryName(it.path)?.Replace('\\', '/') ?? "";
                    if (!folderCount.TryGetValue(dir, out var v)) v = (0, 0);
                    v.objCount++;
                    v.missSum += it.missingCount;
                    folderCount[dir] = v;
                }
                else
                {
                    string sc = it.sceneName ?? "";
                    if (!sceneCount.TryGetValue(sc, out var v)) v = (0, 0);
                    v.objCount++;
                    v.missSum += it.missingCount;
                    sceneCount[sc] = v;
                }
            }

            sb.AppendLine("Most affected scenes:");
            foreach (var kv in sceneCount.OrderByDescending(k => k.Value.objCount).Take(5))
                sb.AppendLine($"• {kv.Key}: {kv.Value.objCount} objects, {kv.Value.missSum} scripts");

            sb.AppendLine("\nMost affected prefab folders:");
            foreach (var kv in folderCount.OrderByDescending(k => k.Value.objCount).Take(5))
                sb.AppendLine($"• {kv.Key}: {kv.Value.objCount} objects, {kv.Value.missSum} scripts");

            var dist = new SortedDictionary<int, int>();
            for (int i = 0; i < selected.Count; i++)
            {
                int k = selected[i].missingCount;
                if (!dist.TryGetValue(k, out int c)) c = 0;
                dist[k] = c + 1;
            }

            sb.AppendLine("\nMissing scripts distribution:");
            foreach (var kv in dist)
                sb.AppendLine($"• {kv.Key} scripts: {kv.Value} objects");

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Script Origin Analysis", "Analysis completed. Check Console for details.", "OK");
        }

        private static void AnalyzeComponents()
        {
            var selected = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
                if (foundObjects[i].isSelected)
                    selected.Add(foundObjects[i]);

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "No objects selected for analysis.", "OK");
                return;
            }

            var componentTypes = new Dictionary<string, int>(256, StringComparer.Ordinal);
            for (int i = 0; i < selected.Count; i++)
            {
                var go = selected[i].gameObject;
                if (go == null) continue;

                var comps = ListPool<Component>.Get();
                try
                {
                    go.GetComponents(comps);
                    for (int c = 0; c < comps.Count; c++)
                    {
                        var comp = comps[c];
                        if (comp == null) continue;
                        string name = comp.GetType().Name;
                        componentTypes[name] = componentTypes.TryGetValue(name, out var v) ? v + 1 : 1;
                    }
                }
                finally
                {
                    ListPool<Component>.Release(comps);
                }
            }

            var sb = new StringBuilder(512);
            sb.AppendLine("📊 COMPONENT ANALYSIS 📊");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("Most common components on affected objects:");
            foreach (var kv in componentTypes.OrderByDescending(k => k.Value).Take(12))
                sb.AppendLine($"• {kv.Key}: {kv.Value} objects");

            var worst = selected.OrderByDescending(o => o.missingCount).Take(5).ToList();
            sb.AppendLine("\nObjects with most missing scripts:");
            for (int i = 0; i < worst.Count; i++)
                sb.AppendLine($"• {(worst[i].gameObject != null ? worst[i].gameObject.name : "<deleted>")}: {worst[i].missingCount} missing ({(worst[i].isPrefab ? "Prefab" : worst[i].sceneName)})");

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Component Analysis", "Analysis completed. Check Console for details.", "OK");
        }

        private static void FocusInHierarchy()
        {
            var selected = new List<UnityEngine.Object>();
            for (int i = 0; i < foundObjects.Count; i++)
            {
                var it = foundObjects[i];
                if (it.isSelected && !it.isPrefab && it.gameObject != null)
                    selected.Add(it.gameObject);
            }

            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog("No Scene Objects",
                    "No scene objects selected. Cannot focus prefabs in hierarchy.", "OK");
                return;
            }

            Selection.objects = selected.ToArray();
            EditorGUIUtility.PingObject(selected[0]);
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log($"Focused on {selected.Count} objects with missing scripts in hierarchy");
        }

        private static void CreateBackup()
        {
            string backupPath = EditorUtility.SaveFolderPanel("Backup Location", "", "MissingScriptsBackup");
            if (string.IsNullOrEmpty(backupPath)) return;

            try
            {
                string timestampedPath = Path.Combine(backupPath, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(timestampedPath);

                EditorSceneManager.SaveOpenScenes();

                var prefabObjects = new List<MissingScriptInfo>();
                for (int i = 0; i < foundObjects.Count; i++)
                    if (foundObjects[i].isSelected && foundObjects[i].isPrefab)
                        prefabObjects.Add(foundObjects[i]);

                if (prefabObjects.Count > 0)
                {
                    string prefabBackupPath = Path.Combine(timestampedPath, "Prefabs");
                    Directory.CreateDirectory(prefabBackupPath);

                    for (int i = 0; i < prefabObjects.Count; i++)
                    {
                        string src = ToFullPath(prefabObjects[i].path);
                        if (File.Exists(src))
                        {
                            string dest = Path.Combine(prefabBackupPath, Path.GetFileName(src));
                            File.Copy(src, dest, true);
                        }
                    }
                }

                var sceneObjects = new List<MissingScriptInfo>();
                for (int i = 0; i < foundObjects.Count; i++)
                    if (foundObjects[i].isSelected && !foundObjects[i].isPrefab)
                        sceneObjects.Add(foundObjects[i]);

                if (sceneObjects.Count > 0)
                {
                    string sceneInfoPath = Path.Combine(timestampedPath, "SceneInfo.txt");
                    using (var writer = new StreamWriter(sceneInfoPath, false, Encoding.UTF8))
                    {
                        writer.WriteLine("Scene Objects Backup Info");
                        writer.WriteLine("========================");
                        writer.WriteLine($"Backup Created: {DateTime.Now}");
                        writer.WriteLine();

                        for (int i = 0; i < sceneObjects.Count; i++)
                        {
                            var obj = sceneObjects[i];
                            writer.WriteLine($"Scene: {obj.sceneName}");
                            writer.WriteLine($"Object: {obj.path}");
                            writer.WriteLine($"Missing Scripts: {obj.missingCount}");
                            writer.WriteLine();
                        }
                    }
                }

                Debug.Log($"Backup created at: {timestampedPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create backup: {e.Message}");
            }
        }
    }
}
#endif
