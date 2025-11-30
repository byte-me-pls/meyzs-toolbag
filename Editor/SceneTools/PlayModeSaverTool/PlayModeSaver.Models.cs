#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    // Snapshot data structures
    [Serializable]
    public class PlayModeSnapshot
    {
        public string id;
        public string name;
        public DateTime timestamp;
        public string scenePath;
        public List<ComponentSnapshot> componentSnapshots;
        public string checksum;
        public bool isCheckpoint;

        public PlayModeSnapshot()
        {
            id = Guid.NewGuid().ToString();
            timestamp = DateTime.Now;
            componentSnapshots = new List<ComponentSnapshot>();
        }
    }

    [Serializable]
    public class ComponentSnapshot
    {
        public string gameObjectPath;
        public string componentType;
        public int instanceID;
        public string serializedData;
        public bool isEnabled;
    }

    [Serializable]
    public class SerializableStringContainer
    {
        public Dictionary<string, string> properties = new Dictionary<string, string>();
    }

    // Watch List için GameObject tracking
    [Serializable]
    public class WatchedObject
    {
        public GameObject gameObject;
        public string gameObjectName;
        public string gameObjectPath;
        public List<string> watchedComponentTypes;
        public bool isEnabled = true;
        public bool watchAllComponents = true;

        public WatchedObject(GameObject go)
        {
            gameObject = go;
            gameObjectName = go.name;
            gameObjectPath = GetGameObjectPath(go);
            watchedComponentTypes = new List<string>();

            // Get all components by default
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType().Name != "Transform")
                {
                    watchedComponentTypes.Add(comp.GetType().Name);
                }
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            if (go.transform.parent == null)
                return go.name;
            return GetGameObjectPath(go.transform.parent.gameObject) + "/" + go.name;
        }
    }

    // Diff data
    [Serializable]
    public class FieldDiff
    {
        public string fieldPath;
        public string oldValue;
        public string newValue;
        public bool isApplied;

        public FieldDiff(string path, string oldVal, string newVal)
        {
            fieldPath = path;
            oldValue = oldVal;
            newValue = newVal;
            isApplied = false;
        }
    }

    [Serializable]
    public class ComponentDiff
    {
        public string gameObjectPath;
        public string componentType;
        public int instanceID;
        public List<FieldDiff> fieldDiffs;
        public bool hasChanges => fieldDiffs != null && fieldDiffs.Count > 0;

        public ComponentDiff()
        {
            fieldDiffs = new List<FieldDiff>();
        }
    }

    // Helper classes for serialization
    [Serializable]
    public class SnapshotContainer
    {
        public PlayModeSnapshot[] snapshots;
        public string timestamp;
    }
}
#endif
