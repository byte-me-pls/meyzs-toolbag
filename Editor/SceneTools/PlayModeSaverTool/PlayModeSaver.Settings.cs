#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    // Settings data
    [CreateAssetMenu(fileName = "PlayModeSaverSettings", menuName = "MeyzToolbag/Play Mode Saver Settings")]
    public class PlayModeSaverSettings : ScriptableObject
    {
        [Header("Auto Save")]
        public bool autoSaveEnabled = true;
        public float autoSaveInterval = 60f;
        public int maxSnapshots = 10;

        [Header("Watch List")]
        public List<WatchedObject> watchedObjects = new List<WatchedObject>();

        [Header("UI")]
        public bool showToastNotifications = true;
        public bool autoMarkSceneDirty = true;
    }
}
#endif