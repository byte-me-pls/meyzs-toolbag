#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.Performance
{
    [CreateAssetMenu(fileName = "NullSafetyIgnoredComponents", menuName = "MeyzToolbag/Null Safety Ignored Components Data")]
    public class NullSafetyIgnoredComponentsData : ScriptableObject
    {
        [Serializable]
        public class IgnoredComponent
        {
            public string componentTypeName;
            public string gameObjectName;
            public string scenePath;
            public int instanceID;
            public DateTime ignoredDate;
            public string reason;

            public IgnoredComponent(MonoBehaviour mb, string ignoreReason = "")
            {
                componentTypeName = mb.GetType().Name;
                gameObjectName = mb.gameObject.name;
                scenePath = EditorSceneManager.GetActiveScene().path;
                instanceID = mb.GetInstanceID();
                ignoredDate = DateTime.Now;
                reason = ignoreReason;
            }
        }

        [SerializeField]
        private List<IgnoredComponent> ignoredComponents = new List<IgnoredComponent>();
        public List<IgnoredComponent> IgnoredComponents => ignoredComponents;

        public void AddIgnoredComponent(MonoBehaviour mb, string reason = "")
        {
            if (mb == null) return;

            int existingIndex = ignoredComponents.FindIndex(ic => ic.instanceID == mb.GetInstanceID());
            if (existingIndex >= 0) ignoredComponents[existingIndex] = new IgnoredComponent(mb, reason);
            else ignoredComponents.Add(new IgnoredComponent(mb, reason));

            EditorUtility.SetDirty(this);
        }

        public bool RemoveIgnoredComponent(int instanceID)
        {
            int index = ignoredComponents.FindIndex(ic => ic.instanceID == instanceID);
            if (index >= 0)
            {
                ignoredComponents.RemoveAt(index);
                EditorUtility.SetDirty(this);
                return true;
            }
            return false;
        }

        public bool IsIgnored(int instanceID) => ignoredComponents.Exists(ic => ic.instanceID == instanceID);

        public void CleanupMissingReferences()
        {
            int originalCount = ignoredComponents.Count;
            ignoredComponents.RemoveAll(ic => EditorUtility.InstanceIDToObject(ic.instanceID) == null);

            if (ignoredComponents.Count != originalCount)
            {
                EditorUtility.SetDirty(this);
                Debug.Log($"Cleaned up {originalCount - ignoredComponents.Count} missing ignored component references.");
            }
        }
    }
}
#endif
