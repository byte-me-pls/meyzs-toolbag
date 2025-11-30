#if UNITY_EDITOR
using System;
using UnityEditor;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Centralized search & filter logic used by header search bar and editor UI.
    /// </summary>
    public class PropertySearchEngine
    {
        private string _searchFilter = "";
        private SerializedPropertyType _typeFilter = SerializedPropertyType.Generic;
        private bool _showFavoritesOnly = false;
        private bool _showModifiedOnly = false;

        public void DrawSettings()
        {
            EditorGUILayout.LabelField("Search Filter Options:", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            _searchFilter      = EditorGUILayout.TextField("Current Filter", _searchFilter);
            _typeFilter        = (SerializedPropertyType)EditorGUILayout.EnumPopup("Type Filter", _typeFilter);
            _showFavoritesOnly = EditorGUILayout.Toggle("Favorites Only", _showFavoritesOnly);
            _showModifiedOnly  = EditorGUILayout.Toggle("Modified Only", _showModifiedOnly);

            EditorGUI.indentLevel--;
        }

        public bool ShouldShowProperty(SerializedProperty prop, EnhancedInspectorSettings settings)
        {
            // Search string
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                bool match = prop.displayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                             || prop.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!match) return false;
            }

            // Type filter
            if (_typeFilter != SerializedPropertyType.Generic && prop.propertyType != _typeFilter)
                return false;

            // Favorites filter
            if (_showFavoritesOnly && !settings.IsFavorite(GetPropertyKey(prop)))
                return false;

            // Modified filter (prefab override highlight)
            if (_showModifiedOnly && !prop.prefabOverride)
                return false;

            return true;
        }

        public string GetSearchFilter() => _searchFilter;
        public void SetSearchFilter(string filter) => _searchFilter = filter ?? "";

        private string GetPropertyKey(SerializedProperty prop)
            => $"{prop.serializedObject.targetObject.GetType().Name}.{prop.propertyPath}";
    }
}
#endif
