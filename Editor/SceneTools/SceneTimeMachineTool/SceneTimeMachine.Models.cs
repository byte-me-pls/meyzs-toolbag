#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [Serializable]
    public class SceneSnapshot
    {
        public string path;
        public string sceneName;
        public string timestampString;
        public long fileSize;
        public string description;
        public bool isAutoSnapshot;
        public bool isPinned;
        public string tags;
        public int sceneObjectCount;
        public string unityVersion;
        public string author;
        public SnapshotType type;
        public List<string> dependencies = new List<string>();

        public DateTime timestamp
        {
            get => DateTime.TryParse(timestampString, out var r) ? r : DateTime.Now;
            set => timestampString = value.ToString("O");
        }
    }

    public enum SnapshotType
    {
        Manual, AutoSave, AutoInterval, PlayMode, Milestone, Backup
    }

    public enum ViewMode { List, Timeline, Compact }
    public enum FilterMode { All, Manual, Auto, Pinned, Recent, Large }
    public enum SortMode { Newest, Oldest, Largest, Smallest, Alphabetical }
}
#endif