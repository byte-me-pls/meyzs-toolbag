#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        public static void Draw()
        {
            try
            {
                InitializeStyles();
                DrawHeader();
                DrawSettings();
                DrawTextureManagement();
                DrawTextureList();
                DrawPackingControls();

                if (showStatistics && currentResult != null) DrawStatistics();
                if (showPreview && currentResult != null) DrawPreview();

                DrawAtlasHistory();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error in Texture Atlas Packer: {e.Message}", MessageType.Error);
                Debug.LogError($"TextureAtlasPackerTool Draw Error: {e}");
            }
        }

        private static void InitializeStyles()
        {
            if (redStyle != null) return;
            redStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
            greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
            yellowStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } };
            boldStyle = new GUIStyle(EditorStyles.boldLabel);
        }

        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🗺️ Advanced Texture Atlas Packer", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);
        }
    }
}
#endif