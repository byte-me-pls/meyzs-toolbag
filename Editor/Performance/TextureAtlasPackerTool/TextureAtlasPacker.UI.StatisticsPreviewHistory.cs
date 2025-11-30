#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void DrawStatistics()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📊 Statistics:", EditorStyles.boldLabel);
                showStatistics = EditorGUILayout.Toggle(showStatistics, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (!showStatistics) { EditorGUILayout.EndVertical(); return; }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Atlas Size: {currentResult.size.x}x{currentResult.size.y}");
                EditorGUILayout.LabelField($"Textures Packed: {currentResult.entries.Count}");
                EditorGUILayout.LabelField($"Packing Efficiency: {currentResult.efficiency01:P1}");
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Estimated File Size: {FormatBytes(currentResult.totalFileSizeEstimate)}");
                EditorGUILayout.LabelField($"Padding Used: {currentResult.paddingUsed}px");
                EditorGUILayout.LabelField($"Created: {currentResult.created:HH:mm:ss}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                long originalTotal = textureInfos.Where(t => t.isSelected).Sum(t => t.fileSize);
                long saved = Math.Max(0, originalTotal - currentResult.totalFileSizeEstimate);
                float savedPct = originalTotal > 0 ? (float)saved / originalTotal : 0f;
                var style = saved > 0 ? greenStyle : yellowStyle;
                EditorGUILayout.LabelField($"Estimated Savings: {FormatBytes(saved)} ({savedPct:P1})", style);

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawStatistics Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch {}
            }
        }

        private static void DrawPreview()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔍 Atlas Preview:", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Zoom:", GUILayout.Width(40));
                previewZoom = EditorGUILayout.Slider(previewZoom, 0.1f, 2f, GUILayout.Width(120));
                showOutlines = EditorGUILayout.ToggleLeft("Outlines", showOutlines, GUILayout.Width(90));
                showNames    = EditorGUILayout.ToggleLeft("Names",    showNames,    GUILayout.Width(80));
                showPreview  = EditorGUILayout.Toggle(showPreview, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (!showPreview || currentResult?.atlas == null) { EditorGUILayout.EndVertical(); return; }

                float previewSize = Mathf.Min(EditorGUIUtility.currentViewWidth - 40, 420) * previewZoom;
                previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.Height(320));

                Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
                EditorGUI.DrawPreviewTexture(previewRect, currentResult.atlas);
                if (showOutlines || showNames) DrawAtlasOverlay(previewRect);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawPreview Error: {e}");
                try { EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical(); } catch {}
            }
        }

        private static void DrawAtlasOverlay(Rect previewRect)
        {
            if (currentResult?.entries == null) return;

            Handles.BeginGUI();
            foreach (var entry in currentResult.entries.Values)
            {
                Rect uv = entry.uvRect;
                var r = new Rect(
                    previewRect.x + uv.x * previewRect.width,
                    previewRect.y + (1 - uv.y - uv.height) * previewRect.height,
                    uv.width * previewRect.width,
                    uv.height * previewRect.height
                );

                if (showOutlines) DrawRectOutline(r, 2, Color.cyan);

                if (showNames && r.width > 50 && r.height > 14)
                {
                    var style = new GUIStyle(EditorStyles.whiteMiniLabel)
                    {
                        fontSize = Mathf.RoundToInt(Mathf.Clamp(10 * previewZoom, 8, 18))
                    };
                    GUI.Label(new Rect(r.x + 2, r.y + 2, r.width - 4, 18), entry.name, style);
                }
            }
            Handles.EndGUI();
        }

        private static void DrawAtlasHistory()
        {
            if (atlasHistory.Count == 0) return;
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📜 Atlas History:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{atlasHistory.Count} previous atlases", EditorStyles.miniLabel);
                if (GUILayout.Button("🗑️ Clear History", GUILayout.Width(120))) atlasHistory.Clear();
                EditorGUILayout.EndHorizontal();

                foreach (var result in atlasHistory.TakeLast(5).Reverse())
                {
                    EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.LabelField($"{result.size.x}x{result.size.y}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{result.entries.Count} textures", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"{result.efficiency01:P1}", GUILayout.Width(60));
                    EditorGUILayout.LabelField(result.created.ToString("HH:mm:ss"), GUILayout.Width(70));
                    if (GUILayout.Button("🔄", GUILayout.Width(28))) RestoreFromHistory(result);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawAtlasHistory Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch {}
            }
        }

        private static void DrawRectOutline(Rect r, float th, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, r.width, th), c);
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMax - th, r.width, th), c);
            EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, th, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - th, r.yMin, th, r.height), c);
        }
    }
}
#endif
