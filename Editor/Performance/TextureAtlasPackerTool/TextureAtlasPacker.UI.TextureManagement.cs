#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void DrawTextureManagement()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📦 Texture Management:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📥 Add Selected Textures")) AddSelectedTextures();
                if (GUILayout.Button("📁 Add from Folder"))      AddFromFolder();
                if (GUILayout.Button("🗑️ Clear All"))            ClearAllTextures();
                if (GUILayout.Button("❌ Remove Selected"))       RemoveSelectedTextures();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("✅ Select All"))  SelectAllTextures(true);
                if (GUILayout.Button("❌ Deselect All")) SelectAllTextures(false);
                if (GUILayout.Button("🔄 Refresh Info")) RefreshTextureInfo();
                if (GUILayout.Button("📊 Analyze"))      AnalyzeTextures();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawTextureManagement Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch {}
            }
        }
    }
}
#endif