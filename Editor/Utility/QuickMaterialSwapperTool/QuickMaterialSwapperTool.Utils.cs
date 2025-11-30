// QuickMaterialSwapperTool.UtilsAndMenu.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    internal static class MaterialUtils
    {
        public static List<Renderer> CollectRenderersFromSelection(GameObject[] selected, bool includeChildren, bool includeInactive)
        {
            var list = new List<Renderer>(256);
            foreach (var go in selected)
            {
                if (go == null) continue;
                if (includeChildren) list.AddRange(go.GetComponentsInChildren<Renderer>(includeInactive));
                else                  list.AddRange(go.GetComponents<Renderer>());
            }
            return list;
        }

        public static Material GetMaterialFromGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        public static string GetGameObjectPath(UnityEngine.Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }
}
#endif
