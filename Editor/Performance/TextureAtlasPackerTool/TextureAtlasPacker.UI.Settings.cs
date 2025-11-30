#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    public static partial class TextureAtlasPackerTool
    {
        private static void DrawSettings()
        {
            try
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("⚙️ Packing Settings:", EditorStyles.boldLabel);

                // Basic settings
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Atlas Size:", GUILayout.Width(80));
                atlasSize = EditorGUILayout.IntPopup(atlasSize,
                    new[] { "256", "512", "1024", "2048", "4096", "8192" },
                    new[] { 256, 512, 1024, 2048, 4096, 8192 }, GUILayout.Width(80));

                EditorGUILayout.LabelField("Padding:", GUILayout.Width(60));
                padding = EditorGUILayout.IntSlider(padding, 0, 32, GUILayout.Width(160));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Format:", GUILayout.Width(80));
                atlasFormat = (AtlasFormat)EditorGUILayout.EnumPopup(atlasFormat, GUILayout.Width(120));

                EditorGUILayout.LabelField("Packing:", GUILayout.Width(60));
                packingMode = (PackingMode)EditorGUILayout.EnumPopup(packingMode, GUILayout.Width(140));
                EditorGUILayout.EndHorizontal();

                // Toggles
                EditorGUILayout.BeginHorizontal();
                trimBorders = EditorGUILayout.ToggleLeft("Trim Alpha Borders", trimBorders, GUILayout.Width(150));
                powerOfTwo = EditorGUILayout.ToggleLeft("Power of Two Atlas", powerOfTwo, GUILayout.Width(160));
                allowRotation = EditorGUILayout.ToggleLeft("Allow Rotation (non-Unity built-in)", allowRotation, GUILayout.Width(230));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                generateMaterial = EditorGUILayout.ToggleLeft("Generate Material", generateMaterial, GUILayout.Width(150));
                exportMetadata  = EditorGUILayout.ToggleLeft("Export Metadata (.json + UV helper)", exportMetadata, GUILayout.Width(260));
                enableMipMaps   = EditorGUILayout.ToggleLeft("Enable MipMaps", enableMipMaps, GUILayout.Width(140));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button(showAdvancedSettings ? "🔽 Advanced Settings" : "🔼 Advanced Settings"))
                    showAdvancedSettings = !showAdvancedSettings;

                if (showAdvancedSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Max Texture Size:", GUILayout.Width(140));
                    maxTextureSize = EditorGUILayout.IntPopup(maxTextureSize,
                        new[] { "64", "128", "256", "512", "1024", "2048" },
                        new[] { 64, 128, 256, 512, 1024, 2048 }, GUILayout.Width(100));

                    EditorGUILayout.LabelField("Trim Threshold:", GUILayout.Width(110));
                    trimThreshold = EditorGUILayout.Slider(trimThreshold, 0f, 1f, GUILayout.Width(160));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    onlyPowerOfTwo = EditorGUILayout.ToggleLeft("Only Power of Two Sources", onlyPowerOfTwo, GUILayout.Width(200));
                    forceSquare    = EditorGUILayout.ToggleLeft("Force Square Atlas", forceSquare, GUILayout.Width(180));
                    linearColorSpace = EditorGUILayout.ToggleLeft("Linear Color Readback", linearColorSpace, GUILayout.Width(200));
                    EditorGUILayout.EndHorizontal();

                    preserveAspectRatio = EditorGUILayout.ToggleLeft("Preserve Aspect Ratio (preview)", preserveAspectRatio);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
            catch (Exception e)
            {
                Debug.LogError($"DrawSettings Error: {e}");
                try { EditorGUILayout.EndVertical(); } catch {}
            }
        }
    }
}
#endif
