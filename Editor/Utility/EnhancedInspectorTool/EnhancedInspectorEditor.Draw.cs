#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Drawing logic: property collection, grouping, row rendering, action buttons.
    /// </summary>
    public partial class EnhancedInspectorEditor
    {
        private void DrawEnhancedProperties()
        {
            var allProperties = GetVisibleProperties();
            string componentTitle = target.GetType().Name;

            if (allProperties.Count == 0)
            {
                if (!string.IsNullOrEmpty(_searchEngine.GetSearchFilter()))
                {
                    Color prev = GUI.color;
                    GUI.color = Color.gray;
                    EditorGUILayout.LabelField($"{componentTitle} (filtered out)", EditorStyles.miniLabel);
                    GUI.color = prev;
                }
                else
                {
                    DrawDefaultInspector();
                }
                return;
            }

            if (EnhancedInspectorTool.EnableIntelligentGrouping)
            {
                DrawGroupedProperties(allProperties, componentTitle);
            }
            else
            {
                EditorGUILayout.LabelField(componentTitle, EditorStyles.boldLabel);
                foreach (var property in allProperties)
                    DrawEnhancedPropertyLine(property);
            }
        }

        private List<SerializedProperty> GetVisibleProperties()
        {
            var list = new List<SerializedProperty>();
            var it = serializedObject.GetIterator();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (it.name == "m_Script") continue;
                if (it.depth != 0) continue;

                if (_searchEngine.ShouldShowProperty(it, _settings))
                    list.Add(it.Copy());
            }

            return list;
        }

        private void DrawGroupedProperties(List<SerializedProperty> properties, string componentTitle)
        {
            var groups = new Dictionary<string, List<SerializedProperty>>();

            foreach (var prop in properties)
            {
                string group = _groupManager.GetPropertyGroup(prop);
                if (!groups.TryGetValue(group, out var l))
                    groups[group] = l = new List<SerializedProperty>();
                l.Add(prop);
            }

            EditorGUILayout.LabelField($"🔧 {componentTitle}", EditorStyles.boldLabel);

            foreach (var group in groups.OrderBy(g => g.Key))
                DrawPropertyGroup(group.Key, group.Value);
        }

        private void DrawPropertyGroup(string groupName, List<SerializedProperty> properties)
        {
            if (properties.Count == 0) return;

            Color groupColor = _groupManager.GetGroupColor(groupName);
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = groupColor;

            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = prev;

            string header = _groupManager.ShowGroupCounts
                ? $"📁 {groupName} ({properties.Count})"
                : $"📁 {groupName}";

            bool expanded = _groupManager.GetGroupFoldout(groupName, true);
            expanded = EditorGUILayout.Foldout(expanded, header, true, EditorStyles.foldoutHeader);
            _groupManager.SetGroupFoldout(groupName, expanded);

            if (expanded)
            {
                EditorGUI.indentLevel++;
                foreach (var prop in properties)
                    DrawEnhancedPropertyLine(prop);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void DrawEnhancedPropertyLine(SerializedProperty prop)
        {
            float height = EditorGUI.GetPropertyHeight(prop, true);
            Rect full = EditorGUILayout.GetControlRect(true, height);
            full = EditorGUI.IndentedRect(full);

            Color prevBG = GUI.backgroundColor;
            if (EnhancedInspectorTool.EnableVisualEnhancements)
            {
                string filter = _searchEngine.GetSearchFilter();
                bool isMatch = !string.IsNullOrEmpty(filter) &&
                               (prop.displayName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                prop.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0);

                if (isMatch)
                    GUI.backgroundColor = new Color(1f, 1f, 0.6f, 0.5f);
                else if (prop.prefabOverride)
                    GUI.backgroundColor = new Color(0.7f, 0.85f, 1f, 0.5f);
            }

            int buttonCount = GetButtonCount();
            float spacing = 2f;
            float totalButtons = buttonCount > 0
                ? (buttonCount * EnhancedInspectorTool.ButtonSize) + ((buttonCount - 1) * spacing) + 4f
                : 0f;

            Rect fieldRect = new Rect(full.x, full.y, full.width - totalButtons, full.height);
            EditorGUI.PropertyField(fieldRect, prop, true);

            if (EnhancedInspectorTool.EnableFavoriteSystem)
                DrawFavoriteStar(prop, fieldRect, full);

            if (EnhancedInspectorTool.EnableQuickActions && buttonCount > 0)
                DrawActionButtons(prop, fieldRect, full, buttonCount);

            GUI.backgroundColor = prevBG;
        }

        private void DrawFavoriteStar(SerializedProperty prop, Rect fieldRect, Rect fullRect)
        {
            Rect star = new Rect(fieldRect.x - 20f, fullRect.y + 1f, 18f, EditorGUIUtility.singleLineHeight - 2f);
            string key = GetPropertyKey(prop);
            bool fav = _settings.IsFavorite(key);

            Color prev = GUI.color;
            GUI.color = fav ? Color.yellow : new Color(0.7f, 0.7f, 0.7f);

            if (GUI.Button(star, fav ? "★" : "☆", EditorStyles.miniButton))
            {
                if (fav) _settings.RemoveFavorite(key);
                else     _settings.AddFavorite(key);
            }

            GUI.color = prev;
        }

        private void DrawActionButtons(SerializedProperty prop, Rect fieldRect, Rect fullRect, int buttonCount)
        {
            Rect btn = new Rect(
                fieldRect.xMax + 4f,
                fullRect.y + 1f,
                EnhancedInspectorTool.ButtonSize,
                EditorGUIUtility.singleLineHeight - 2f
            );

            int drawn = 0;
            int maxButtons = Mathf.Min(buttonCount, EnhancedInspectorTool.MaxVisibleButtons);

            // reset
            if (drawn < maxButtons)
            {
                if (GUI.Button(btn, "↻", EditorStyles.miniButton))
                    _actionsManager.ResetProperty(prop);
                btn.x += EnhancedInspectorTool.ButtonSize + 2f;
                drawn++;
            }

            // copy
            if (drawn < maxButtons)
            {
                if (GUI.Button(btn, "📋", EditorStyles.miniButton))
                    _actionsManager.CopyProperty(prop);
                btn.x += EnhancedInspectorTool.ButtonSize + 2f;
                drawn++;
            }

            // paste
            if (drawn < maxButtons)
            {
                if (GUI.Button(btn, "📝", EditorStyles.miniButton))
                    _actionsManager.PasteProperty(prop);
                btn.x += EnhancedInspectorTool.ButtonSize + 2f;
                drawn++;
            }

            // multi-apply
            if (drawn < maxButtons && Selection.gameObjects.Length > 1)
            {
                if (GUI.Button(btn, ">>", EditorStyles.miniButton))
                    ApplyToMultipleObjects(prop);
            }
        }

        private void ApplyToMultipleObjects(SerializedProperty sourceProp)
        {
            var selected = Selection.gameObjects;
            if (selected.Length <= 1) return;

            var value = GetPropertyValue(sourceProp);
            string path = sourceProp.propertyPath;
            var compType = sourceProp.serializedObject.targetObject.GetType();

            var targets = selected
                .Select(go => go.GetComponent(compType))
                .Where(c => c != null && c != sourceProp.serializedObject.targetObject)
                .ToArray();

            if (targets.Length == 0) return;

            Undo.RecordObjects(targets, "Apply to Multiple Objects");

            foreach (var component in targets)
            {
                var so = new SerializedObject(component);
                var prop = so.FindProperty(path);
                if (prop != null)
                {
                    SetPropertyValue(prop, value);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(component);
                }
            }
        }
    }
}
#endif
