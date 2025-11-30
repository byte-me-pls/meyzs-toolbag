#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Results list, filtering, table rows.
    /// </summary>
    public static partial class MissingScriptsCleanerTool
    {
        private static List<MissingScriptInfo> GetFilteredObjects()
        {
            var list = new List<MissingScriptInfo>(foundObjects.Count);
            for (int i = 0; i < foundObjects.Count; i++)
            {
                var it = foundObjects[i];

                if (!string.IsNullOrEmpty(searchFilter))
                {
                    bool match =
                        (it.gameObject != null &&
                         it.gameObject.name.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(it.sceneName) &&
                         it.sceneName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(it.path) &&
                         it.path.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!match) continue;
                }

                switch (filterMode)
                {
                    case FilterMode.SceneOnly:
                        if (it.isPrefab) continue; break;
                    case FilterMode.PrefabsOnly:
                        if (!it.isPrefab) continue; break;
                    case FilterMode.RecentlyModified:
                        if ((DateTime.Now - it.lastModified).TotalDays >= 7) continue; break;
                    case FilterMode.HighMissingCount:
                        if (it.missingCount <= 2) continue; break;
                }

                list.Add(it);
            }

            list.Sort((a, b) =>
            {
                int pa = a.isPrefab ? 0 : 1;
                int pb = b.isPrefab ? 0 : 1;
                int p = pa.CompareTo(pb);
                if (p != 0) return p;

                int n = string.Compare(a.sceneName, b.sceneName, StringComparison.OrdinalIgnoreCase);
                if (n != 0) return n;

                string an = a.gameObject != null ? a.gameObject.name : "";
                string bn = b.gameObject != null ? b.gameObject.name : "";
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private static void DrawResults()
        {
            try
            {
                var filteredObjects = GetFilteredObjects();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"🎯 Found Objects ({filteredObjects.Count}):", EditorStyles.boldLabel);

                if (filteredObjects.Count == 0)
                {
                    EditorGUILayout.HelpBox("No objects match the current filter.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("☑", GUILayout.Width(28));
                GUILayout.Label("Object", GUILayout.MinWidth(140), GUILayout.ExpandWidth(true));
                GUILayout.Label("Type", GUILayout.Width(60));
                GUILayout.Label("Missing", GUILayout.Width(70));
                GUILayout.Label("Scene/Path", GUILayout.MinWidth(120), GUILayout.ExpandWidth(true));
                GUILayout.Label("Actions", GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(220));

                for (int i = 0; i < filteredObjects.Count; i++)
                    DrawObjectItem(filteredObjects[i]);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawResults Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch { }
            }
        }

        private static void DrawObjectItem(MissingScriptInfo info)
        {
            try
            {
                EditorGUILayout.BeginHorizontal(info.isSelected ? EditorStyles.helpBox : GUI.skin.box);

                info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(28));

                string icon = info.isPrefab ? "📦" : "🎬";
                if (GUILayout.Button($"{icon} {(info.gameObject != null ? info.gameObject.name : "<deleted>")}",
                        info.missingCount > 3 ? redStyle : EditorStyles.label,
                        GUILayout.MinWidth(140), GUILayout.ExpandWidth(true)))
                {
                    if (info.isPrefab)
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.path);
                        if (obj != null) EditorGUIUtility.PingObject(obj);
                    }
                    else if (info.gameObject != null)
                    {
                        EditorGUIUtility.PingObject(info.gameObject);
                    }
                }

                GUILayout.Label(info.isPrefab ? "Prefab" : "Scene", GUILayout.Width(60));

                var countStyle = info.missingCount > 5 ? redStyle : (info.missingCount > 2 ? yellowStyle : EditorStyles.label);
                GUILayout.Label(info.missingCount.ToString(), countStyle, GUILayout.Width(70));

                string location = info.isPrefab
                    ? (info.displayLocationCached ?? Path.GetFileName(info.path))
                    : info.sceneName;
                GUILayout.Label(location, GUILayout.MinWidth(120), GUILayout.ExpandWidth(true));

                EditorGUILayout.BeginHorizontal(GUILayout.Width(120));
                if (GUILayout.Button("🔍", GUILayout.Width(30))) ShowObjectDetails(info);

                if (GUILayout.Button("🗑️", GUILayout.Width(30)))
                {
                    if (EditorUtility.DisplayDialog("Remove Missing Scripts?",
                            $"Remove {info.missingCount} missing scripts from '{(info.gameObject != null ? info.gameObject.name : "<deleted>")}'?",
                            "Yes", "No"))
                    {
                        RemoveSingleObject(info);
                    }
                }

                if (!info.isPrefab && info.gameObject != null && GUILayout.Button("📌", GUILayout.Width(30)))
                {
                    Selection.activeGameObject = info.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawObjectItem Error: {e}");
                try { EditorGUILayout.EndHorizontal(); } catch { }
            }
        }
    }
}
#endif
