#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Core flags, attribute, initialization and feature management.
    /// Split as partial for maintainability.
    /// </summary>
    public static partial class EnhancedInspectorTool
    {
        // ===== Core Feature Settings =====
        public static bool EnableSmartSearch = true;
        public static bool EnableIntelligentGrouping = true;
        public static bool EnableQuickActions = true;
        public static bool EnableVisualEnhancements = true;
        public static bool EnableFavoriteSystem = true;
        public static bool EnableBatchOperations = true;

        // ===== Advanced Settings =====
        public static float ButtonSize = 18f;
        public static bool CompactMode = false;
        public static bool ShowTooltips = true;
        public static bool EnableAutoSave = true;
        public static int MaxVisibleButtons = 4;

        // ===== Internal State =====
        private static Vector2 _toolScrollPosition;
        private static readonly EnhancedInspectorSettings _settings = new EnhancedInspectorSettings();
        private static readonly PropertySearchEngine _searchEngine = new PropertySearchEngine();
        private static readonly PropertyGroupManager _groupManager = new PropertyGroupManager();
        private static readonly QuickActionsManager _actionsManager = new QuickActionsManager();

        // ===== Constants =====
        private const string PREFS_PREFIX = "MeyzToolbag.EnhancedInspector.";

        // ===== Attribute =====
        [AttributeUsage(AttributeTargets.Class)]
        public class EnhancedInspectorAttribute : Attribute
        {
            public bool enableSearch = true;
            public bool enableGrouping = true;
            public bool enableQuickActions = true;
            public string defaultGroup = "";

            public EnhancedInspectorAttribute(bool search = true, bool grouping = true, bool quickActions = true)
            {
                enableSearch = search;
                enableGrouping = grouping;
                enableQuickActions = quickActions;
            }
        }

        // ===== Feature Management =====
        private static void EnableAllFeatures()
        {
            EnableSmartSearch = true;
            EnableIntelligentGrouping = true;
            EnableQuickActions = true;
            EnableVisualEnhancements = true;
            EnableFavoriteSystem = true;
            EnableBatchOperations = true;
            RepaintAllInspectors();
        }

        private static void EnableCoreFeatures()
        {
            EnableSmartSearch = true;
            EnableIntelligentGrouping = true;
            EnableQuickActions = true;
            EnableVisualEnhancements = true;
            EnableFavoriteSystem = false;
            EnableBatchOperations = false;
            RepaintAllInspectors();
        }

        private static void DisableAllFeatures()
        {
            EnableSmartSearch = false;
            EnableIntelligentGrouping = false;
            EnableQuickActions = false;
            EnableVisualEnhancements = false;
            EnableFavoriteSystem = false;
            EnableBatchOperations = false;
            RepaintAllInspectors();
        }

        private static void ResetToDefaults()
        {
            EnableSmartSearch = true;
            EnableIntelligentGrouping = true;
            EnableQuickActions = true;
            EnableVisualEnhancements = true;
            EnableFavoriteSystem = true;
            EnableBatchOperations = true;
            ButtonSize = 18f;
            CompactMode = false;
            ShowTooltips = true;
            EnableAutoSave = true;
            MaxVisibleButtons = 4;
            RepaintAllInspectors();
        }

        private static int GetEnabledFeatureCount()
        {
            int count = 0;
            if (EnableSmartSearch) count++;
            if (EnableIntelligentGrouping) count++;
            if (EnableQuickActions) count++;
            if (EnableVisualEnhancements) count++;
            if (EnableFavoriteSystem) count++;
            if (EnableBatchOperations) count++;
            return count;
        }

        private static void RepaintAllInspectors()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "InspectorWindow")
                    window.Repaint();
        }

        // ===== Initialization =====
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _settings.LoadSettings();
            EditorApplication.projectChanged += () => _settings.SaveSettings();
        }
    }
}
#endif
