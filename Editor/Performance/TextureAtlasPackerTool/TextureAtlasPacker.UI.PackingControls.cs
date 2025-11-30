#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void DrawPackingControls()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🎯 Packing Controls:", EditorStyles.boldLabel);

                var selected = textureInfos.Where(t => t.isSelected && t.originalTexture != null).ToList();

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = selected.Count > 0 && !isPacking;
                GUI.backgroundColor = selected.Count > 0 ? Color.cyan : Color.gray;
                if (GUILayout.Button($"🗺️ Pack Atlas ({selected.Count})", GUILayout.Height(30))) StartPacking();
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;

                if (GUILayout.Button("📊 Estimate", GUILayout.Height(30), GUILayout.Width(100)))
                    EstimatePackingResult();
                EditorGUILayout.EndHorizontal();

                if (isPacking)
                {
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.ProgressBar(rect, packingProgress, packingStatus);
                }

                if (currentResult != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("💾 Save Atlas", GUILayout.Height(25))) SaveAtlas();
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("📤 Export All", GUILayout.Height(25))) ExportAll();
                    if (GUILayout.Button("🔧 Optimize",   GUILayout.Height(25))) OptimizeAtlas();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawPackingControls Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch {}
            }
        }
    }
}
#endif
