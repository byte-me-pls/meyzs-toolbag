#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Derives group names from property heuristics, manages colors and foldout states.
    /// </summary>
    public class PropertyGroupManager
    {
        private bool _enableAutoGrouping = true;
        private bool _showGroupCounts = true;
        private bool _colorCodeGroups = true;
        private readonly Dictionary<string, bool> _groupFoldouts = new Dictionary<string, bool>();

        public void DrawSettings()
        {
            EditorGUILayout.LabelField("Grouping Options:", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;

            _enableAutoGrouping = EditorGUILayout.Toggle("Auto Grouping", _enableAutoGrouping);
            _showGroupCounts    = EditorGUILayout.Toggle("Show Group Counts", _showGroupCounts);
            _colorCodeGroups    = EditorGUILayout.Toggle("Color Code Groups", _colorCodeGroups);

            EditorGUI.indentLevel--;
        }

        public string GetPropertyGroup(SerializedProperty prop)
        {
            if (!_enableAutoGrouping) return "Properties";

            string name = prop.name.ToLowerInvariant();

            if (name.Contains("transform") || name.Contains("position") || name.Contains("rotation") || name.Contains("scale"))
                return "Transform";
            if (name.Contains("render") || name.Contains("material") || name.Contains("color") || name.Contains("sprite"))
                return "Rendering";
            if (name.Contains("audio") || name.Contains("sound") || name.Contains("volume"))
                return "Audio";
            if (name.Contains("physics") || name.Contains("rigidbody") || name.Contains("collider"))
                return "Physics";
            if (name.Contains("ui") || name.Contains("canvas") || name.Contains("button") || name.Contains("text"))
                return "UI";

            return "General";
        }

        public Color GetGroupColor(string groupName)
        {
            if (!_colorCodeGroups) return Color.white;

            switch (groupName.ToLowerInvariant())
            {
                case "transform": return new Color(0.7f, 0.85f, 1f, 0.3f);
                case "rendering": return new Color(1f, 0.7f, 0.7f, 0.3f);
                case "audio":     return new Color(0.7f, 1f, 0.7f, 0.3f);
                case "physics":   return new Color(1f, 0.85f, 0.7f, 0.3f);
                case "ui":        return new Color(1f, 0.7f, 1f, 0.3f);
                default:          return new Color(0.9f, 0.9f, 0.9f, 0.2f);
            }
        }

        public bool GetGroupFoldout(string groupName, bool defaultValue = true)
            => _groupFoldouts.TryGetValue(groupName, out var val) ? val : defaultValue;

        public void SetGroupFoldout(string groupName, bool value)
            => _groupFoldouts[groupName] = value;

        public bool ShowGroupCounts => _showGroupCounts;
    }
}
#endif
