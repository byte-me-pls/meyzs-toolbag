#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void DrawTextureList()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📋 Texture List:", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(110));

                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(110));
                if (GUILayout.Button(sortDescending ? "🔽" : "🔼", GUILayout.Width(25))) sortDescending = !sortDescending;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                if (GUILayout.Button("❌", GUILayout.Width(25))) searchFilter = "";
                EditorGUILayout.EndHorizontal();

                var filtered = GetFilteredAndSortedTextures();
                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("No textures. Use 'Add Selected Textures' or 'Add from Folder'.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.LabelField($"Showing {filtered.Count} of {textureInfos.Count} textures");

                // Header
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("☑", GUILayout.Width(25));
                GUILayout.Label("Preview", GUILayout.Width(60));
                GUILayout.Label("Name", GUILayout.Width(160));
                GUILayout.Label("Size", GUILayout.Width(110));
                GUILayout.Label("Format", GUILayout.Width(80));
                GUILayout.Label("File Size", GUILayout.Width(80));
                GUILayout.Label("Alpha", GUILayout.Width(50));
                GUILayout.Label("Actions", GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(240));
                foreach (var t in filtered) DrawTextureItem(t);
                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawTextureList Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch {}
            }
        }

        private static void DrawTextureItem(TextureInfo t)
        {
            try
            {
                EditorGUILayout.BeginHorizontal(t.isSelected ? EditorStyles.helpBox : GUI.skin.box);

                t.isSelected = EditorGUILayout.Toggle(t.isSelected, GUILayout.Width(25));

                if (t.originalTexture != null)
                {
                    var r = GUILayoutUtility.GetRect(50, 50);
                    EditorGUI.DrawPreviewTexture(r, t.originalTexture);
                }
                else GUILayout.Space(60);

                var nameStyle = t.originalTexture == null ? redStyle : EditorStyles.label;
                GUILayout.Label(t.name, nameStyle, GUILayout.Width(160));

                string sizeText = (trimBorders && t.trimmedSize != t.originalSize)
                    ? $"{t.trimmedSize.x}x{t.trimmedSize.y}  ({t.originalSize.x}x{t.originalSize.y})"
                    : $"{t.originalSize.x}x{t.originalSize.y}";
                GUILayout.Label(sizeText, GUILayout.Width(110));

                GUILayout.Label(t.format.ToString(), GUILayout.Width(80));
                GUILayout.Label(FormatBytes(t.fileSize), GUILayout.Width(80));

                GUILayout.Label(t.hasAlpha ? "Yes" : "No", (t.hasAlpha ? greenStyle : redStyle), GUILayout.Width(50));

                EditorGUILayout.BeginHorizontal(GUILayout.Width(90));
                if (GUILayout.Button("📌", GUILayout.Width(28))) { if (t.originalTexture) EditorGUIUtility.PingObject(t.originalTexture); }
                if (GUILayout.Button("🔍", GUILayout.Width(28))) ShowTextureDetails(t);
                if (GUILayout.Button("❌", GUILayout.Width(28))) RemoveTexture(t);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawTextureItem Error: {e}");
                try { EditorGUILayout.EndHorizontal(); } catch {}
            }
        }
    }
}
#endif
