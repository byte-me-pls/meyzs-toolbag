// QuickMaterialSwapperTool.Storage.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        private static void LoadData()
        {
            try
            {
                string presetsJson = EditorPrefs.GetString(PRESETS_KEY, "");
                if (!string.IsNullOrEmpty(presetsJson))
                {
                    var data = JsonUtility.FromJson<PresetsData>(presetsJson);
                    presets = data?.presets ?? new System.Collections.Generic.List<MaterialPreset>();
                }

                // HISTORY_KEY ayrıldı; karmaşık referanslar (Renderer vs.) için kalıcı serileştirme yapılmıyor.
                presetsLoaded = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load presets: {e.Message}");
                presets = new System.Collections.Generic.List<MaterialPreset>();
                operationHistory = new System.Collections.Generic.List<MaterialOperation>();
                presetsLoaded = true;
            }
        }

        private static void SaveData()
        {
            try
            {
                var data = new PresetsData { presets = presets };
                string json = JsonUtility.ToJson(data);
                EditorPrefs.SetString(PRESETS_KEY, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save presets: {e.Message}");
            }
        }
    }
}
#endif