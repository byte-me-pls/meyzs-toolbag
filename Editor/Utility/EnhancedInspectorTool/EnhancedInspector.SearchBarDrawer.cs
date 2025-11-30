#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Global header search bar injected into inspector headers for attributed components.
    /// </summary>
    [InitializeOnLoad]
    public static class SearchBarDrawer
    {
        private static readonly PropertySearchEngine _searchEngine = new PropertySearchEngine();

        static SearchBarDrawer()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void OnHeaderGUI(UnityEditor.Editor editor)
        {
            if (!ShouldShowSearchBar(editor)) return;

            GUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔍 Enhanced Inspector Search", EditorStyles.boldLabel);

            // Search row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));

            string currentFilter = _searchEngine.GetSearchFilter();
            string newFilter = EditorGUILayout.TextField(currentFilter);
            if (newFilter != currentFilter)
            {
                _searchEngine.SetSearchFilter(newFilter);
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _searchEngine.SetSearchFilter("");
                RepaintAllInspectors();
            }

            EditorGUILayout.EndHorizontal();

            // Info line
            if (!string.IsNullOrEmpty(_searchEngine.GetSearchFilter()))
            {
                int matches = CountMatchingProperties(editor);
                string info = matches > 0 ? $"🎯 {matches} matches" : "❌ No matches";
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private static bool ShouldShowSearchBar(UnityEditor.Editor editor)
        {
            if (!EnhancedInspectorTool.EnableSmartSearch) return false;
            if (!(editor.target is MonoBehaviour)) return false;

            return editor.target.GetType()
                .GetCustomAttribute<EnhancedInspectorTool.EnhancedInspectorAttribute>() != null;
        }

        private static int CountMatchingProperties(UnityEditor.Editor editor)
        {
            if (!(editor.target is MonoBehaviour)) return 0;

            int count = 0;
            var so = new SerializedObject(editor.target);
            var iterator = so.GetIterator();
            var settings = new EnhancedInspectorSettings();

            while (iterator.NextVisible(true))
            {
                if (iterator.name == "m_Script") continue;
                if (_searchEngine.ShouldShowProperty(iterator, settings))
                    count++;
            }

            return count;
        }

        private static void RepaintAllInspectors()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window.GetType().Name == "InspectorWindow")
                    window.Repaint();
        }
    }
}
#endif
