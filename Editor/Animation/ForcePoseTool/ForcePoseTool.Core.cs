#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Animation
{
    public static partial class ForcePoseTool
    {
        // --- State ---
        private static GameObject targetObject;
        private static AnimationClip selectedClip;
        private static float normalizedTime = 0f;
        private static bool previewing;

        // --- Reset support ---
        private static TransformSnapshot originalSnapshot;
        private static bool hasOriginalSnapshot = false;

        // --- Presets persistence ---
        private const string PRESET_KEY = "ForcePoseTool.Presets";

        [Serializable]
        private class TransformSnapshot
        {
            public string name;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public List<TransformSnapshot> children = new List<TransformSnapshot>();
        }

        [Serializable]
        private class PresetData
        {
            public string name;
            public TransformSnapshot snapshot;
            public float normalizedTime;
        }

        [Serializable]
        private class PresetCollection
        {
            public List<PresetData> presets = new List<PresetData>();
        }

        private static List<PresetData> presets = new List<PresetData>();
        private static int selectedPresetIndex = -1;
        private static string newPresetName = "";
        private static bool presetsLoaded = false;

        // --- Persistence helpers ---
        private static void LoadPresets()
        {
            presets.Clear();
            string json = EditorPrefs.GetString(PRESET_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var coll = JsonUtility.FromJson<PresetCollection>(json);
                    presets = coll.presets ?? new List<PresetData>();
                }
                catch
                {
                    presets = new List<PresetData>();
                }
            }

            selectedPresetIndex = presets.Count > 0 ? 0 : -1;
        }

        private static void SavePresets()
        {
            var coll = new PresetCollection { presets = presets };
            EditorPrefs.SetString(PRESET_KEY, JsonUtility.ToJson(coll, true));
        }
    }
}
#endif
