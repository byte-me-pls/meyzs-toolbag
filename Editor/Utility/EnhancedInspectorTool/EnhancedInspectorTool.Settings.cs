#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Persistent editor prefs and runtime session registries.
    /// </summary>
    public class EnhancedInspectorSettings
    {
        private readonly HashSet<string> _favoriteProperties = new HashSet<string>();
        private readonly HashSet<string> _enhancedClasses = new HashSet<string>();
        private const string PREFS_PREFIX = "MeyzToolbag.EnhancedInspector.";

        public void SaveSettings()
        {
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableSmartSearch", EnhancedInspectorTool.EnableSmartSearch);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableIntelligentGrouping", EnhancedInspectorTool.EnableIntelligentGrouping);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableQuickActions", EnhancedInspectorTool.EnableQuickActions);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableVisualEnhancements", EnhancedInspectorTool.EnableVisualEnhancements);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableFavoriteSystem", EnhancedInspectorTool.EnableFavoriteSystem);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableBatchOperations", EnhancedInspectorTool.EnableBatchOperations);

            EditorPrefs.SetFloat(PREFS_PREFIX + "ButtonSize", EnhancedInspectorTool.ButtonSize);
            EditorPrefs.SetBool(PREFS_PREFIX + "CompactMode", EnhancedInspectorTool.CompactMode);
            EditorPrefs.SetBool(PREFS_PREFIX + "ShowTooltips", EnhancedInspectorTool.ShowTooltips);
            EditorPrefs.SetBool(PREFS_PREFIX + "EnableAutoSave", EnhancedInspectorTool.EnableAutoSave);
            EditorPrefs.SetInt(PREFS_PREFIX + "MaxVisibleButtons", EnhancedInspectorTool.MaxVisibleButtons);

            EditorPrefs.SetString(PREFS_PREFIX + "Favorites", string.Join("|", _favoriteProperties));
        }

        public void LoadSettings()
        {
            EnhancedInspectorTool.EnableSmartSearch        = EditorPrefs.GetBool (PREFS_PREFIX + "EnableSmartSearch", true);
            EnhancedInspectorTool.EnableIntelligentGrouping= EditorPrefs.GetBool (PREFS_PREFIX + "EnableIntelligentGrouping", true);
            EnhancedInspectorTool.EnableQuickActions       = EditorPrefs.GetBool (PREFS_PREFIX + "EnableQuickActions", true);
            EnhancedInspectorTool.EnableVisualEnhancements = EditorPrefs.GetBool (PREFS_PREFIX + "EnableVisualEnhancements", true);
            EnhancedInspectorTool.EnableFavoriteSystem     = EditorPrefs.GetBool (PREFS_PREFIX + "EnableFavoriteSystem", true);
            EnhancedInspectorTool.EnableBatchOperations    = EditorPrefs.GetBool (PREFS_PREFIX + "EnableBatchOperations", true);

            EnhancedInspectorTool.ButtonSize       = EditorPrefs.GetFloat(PREFS_PREFIX + "ButtonSize", 18f);
            EnhancedInspectorTool.CompactMode      = EditorPrefs.GetBool (PREFS_PREFIX + "CompactMode", false);
            EnhancedInspectorTool.ShowTooltips     = EditorPrefs.GetBool (PREFS_PREFIX + "ShowTooltips", true);
            EnhancedInspectorTool.EnableAutoSave   = EditorPrefs.GetBool (PREFS_PREFIX + "EnableAutoSave", true);
            EnhancedInspectorTool.MaxVisibleButtons= EditorPrefs.GetInt  (PREFS_PREFIX + "MaxVisibleButtons", 4);

            string favoritesData = EditorPrefs.GetString(PREFS_PREFIX + "Favorites", "");
            _favoriteProperties.Clear();
            if (!string.IsNullOrEmpty(favoritesData))
            {
                var parts = favoritesData.Split('|');
                foreach (var p in parts)
                    if (!string.IsNullOrWhiteSpace(p))
                        _favoriteProperties.Add(p);
            }
        }

        public int  GetEnhancedClassCount() => _enhancedClasses.Count;
        public int  GetFavoriteCount()      => _favoriteProperties.Count;

        public bool IsFavorite(string propertyKey) => _favoriteProperties.Contains(propertyKey);
        public void AddFavorite(string propertyKey) => _favoriteProperties.Add(propertyKey);
        public void RemoveFavorite(string propertyKey) => _favoriteProperties.Remove(propertyKey);
        public void ClearFavorites() => _favoriteProperties.Clear();

        public void RegisterEnhancedClass(string className) => _enhancedClasses.Add(className);
    }
}
#endif
