#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Main tool window drawing and UI sections.
    /// </summary>
    public static partial class EnhancedInspectorTool
    {
        public static void Draw()
        {
            _settings.LoadSettings();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("🔧 Enhanced Inspector Editor",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.LabelField("Smart Inspector Experience with Search, Grouping & Quick Actions",
                EditorStyles.centeredGreyMiniLabel);

            DrawSeparator();
            GUILayout.Space(10);

            DrawQuickOverview();
            GUILayout.Space(10);

            _toolScrollPosition = EditorGUILayout.BeginScrollView(_toolScrollPosition);

            DrawQuickControls();
            GUILayout.Space(10);

            DrawCoreFeatures();
            GUILayout.Space(10);

            DrawAdvancedSettings();
            GUILayout.Space(10);

            DrawStatisticsAndActions();

            EditorGUILayout.EndScrollView();

            _settings.SaveSettings();
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private static void DrawQuickOverview()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Quick Overview", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Core Features: {GetEnabledFeatureCount()}/6", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Enhanced Classes: {_settings.GetEnhancedClassCount()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Favorites: {_settings.GetFavoriteCount()}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawStatusIndicator("🔍 Search", EnableSmartSearch);
            DrawStatusIndicator("📁 Grouping", EnableIntelligentGrouping);
            DrawStatusIndicator("⚡ Actions", EnableQuickActions);
            DrawStatusIndicator("🎨 Visual", EnableVisualEnhancements);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatusIndicator(string label, bool enabled)
        {
            Color oldColor = GUI.color;
            GUI.color = enabled ? Color.green : Color.gray;
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = oldColor;
        }

        private static void DrawQuickControls()
        {
            EditorGUILayout.LabelField("⚡ Quick Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🚀 Enable All Features", GUILayout.Height(25)))
                EnableAllFeatures();

            if (GUILayout.Button("⭐ Core Features Only", GUILayout.Height(25)))
                EnableCoreFeatures();

            if (GUILayout.Button("🔄 Reset to Defaults", GUILayout.Height(25)))
                ResetToDefaults();

            if (GUILayout.Button("❌ Disable All", GUILayout.Height(25)))
                DisableAllFeatures();

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawCoreFeatures()
        {
            EditorGUILayout.LabelField("⚙️ Core Features", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EnableSmartSearch = DrawFeatureToggle(
                "🔍 Smart Search & Filtering",
                EnableSmartSearch,
                "Global property search with type filtering, favorites, and smart suggestions");

            EnableIntelligentGrouping = DrawFeatureToggle(
                "📁 Intelligent Grouping",
                EnableIntelligentGrouping,
                "Auto-categorize properties into collapsible groups (Transform, Rendering, etc.)");

            EnableQuickActions = DrawFeatureToggle(
                "⚡ Quick Actions",
                EnableQuickActions,
                "Inline buttons for reset, copy/paste, and multi-object operations");

            EnableVisualEnhancements = DrawFeatureToggle(
                "🎨 Visual Enhancements",
                EnableVisualEnhancements,
                "Color coding, search highlights, and modern UI improvements");

            EnableFavoriteSystem = DrawFeatureToggle(
                "⭐ Favorite System",
                EnableFavoriteSystem,
                "Star important properties for quick access and filtering");

            EnableBatchOperations = DrawFeatureToggle(
                "🔄 Batch Operations",
                EnableBatchOperations,
                "Reset, copy, and modify multiple components at once");

            EditorGUILayout.EndVertical();
        }

        private static bool DrawFeatureToggle(string label, bool currentValue, string description)
        {
            EditorGUILayout.BeginHorizontal();
            bool newValue = EditorGUILayout.ToggleLeft(label, currentValue, GUILayout.Width(200));

            if (ShowTooltips && !string.IsNullOrEmpty(description))
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField($"({description})", EditorStyles.miniLabel);
                GUI.color = oldColor;
            }

            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private static void DrawAdvancedSettings()
        {
            EditorGUILayout.LabelField("🎮 Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("UI & Experience", EditorStyles.boldLabel);
            ButtonSize = EditorGUILayout.Slider("Button Size", ButtonSize, 14f, 24f);
            MaxVisibleButtons = EditorGUILayout.IntSlider("Max Visible Buttons", MaxVisibleButtons, 2, 8);
            CompactMode = EditorGUILayout.Toggle("Compact Mode", CompactMode);
            ShowTooltips = EditorGUILayout.Toggle("Show Tooltips", ShowTooltips);
            EnableAutoSave = EditorGUILayout.Toggle("Auto-save Settings", EnableAutoSave);

            EditorGUILayout.Space(5);

            if (EnableSmartSearch)
            {
                EditorGUILayout.LabelField("Search & Filtering", EditorStyles.boldLabel);
                _searchEngine.DrawSettings();
            }

            EditorGUILayout.Space(5);

            if (EnableIntelligentGrouping)
            {
                EditorGUILayout.LabelField("Intelligent Grouping", EditorStyles.boldLabel);
                _groupManager.DrawSettings();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatisticsAndActions()
        {
            EditorGUILayout.LabelField("📊 Statistics & Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Current Session:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"• Enhanced Classes: {_settings.GetEnhancedClassCount()}");
            EditorGUILayout.LabelField($"• Favorite Properties: {_settings.GetFavoriteCount()}");
            EditorGUILayout.LabelField($"• Clipboard Items: {_actionsManager.GetClipboardCount()}");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🧹 Clear All Favorites"))
            {
                if (EditorUtility.DisplayDialog("Clear Favorites",
                    "Are you sure you want to clear all favorite properties?", "Yes", "Cancel"))
                {
                    _settings.ClearFavorites();
                }
            }

            if (GUILayout.Button("📋 Clear Clipboard"))
            {
                _actionsManager.ClearClipboard();
                EditorUtility.DisplayDialog("Clipboard Cleared", "All clipboard items have been cleared.", "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔄 Refresh Inspectors"))
            {
                RepaintAllInspectors();
                EditorUtility.DisplayDialog("Inspectors Refreshed", "All inspector windows have been refreshed.", "OK");
            }

            if (GUILayout.Button("📖 Show Usage Guide"))
            {
                ShowUsageGuide();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private static void ShowUsageGuide()
        {
            string guide = @"🔧 Enhanced Inspector Editor - Usage Guide

SETUP:
1. Enable desired features in this tool
2. Add [EnhancedInspector] to your MonoBehaviour classes
3. Enjoy enhanced inspector experience!

EXAMPLE:
[EnhancedInspector]
public class PlayerController : MonoBehaviour
{
    public float health = 100f;
    public string playerName = ""Player"";
    public Transform target;
}

FEATURES:
🔍 Smart Search - Global search across all properties
📁 Intelligent Grouping - Auto-categorized property groups
⚡ Quick Actions - Reset, copy/paste, multi-object sync
🎨 Visual Enhancements - Color coding and highlights
⭐ Favorites - Star important properties
🔄 Batch Operations - Multi-component operations

TIPS:
• Use search to quickly find properties in complex components
• Favorite frequently used properties for quick access
• Use quick actions for faster workflow
• Grouping helps organize large components
• All features are opt-in and safe for production";

            EditorUtility.DisplayDialog("Enhanced Inspector Editor - Usage Guide", guide, "Got it!");
        }
    }
}
#endif
