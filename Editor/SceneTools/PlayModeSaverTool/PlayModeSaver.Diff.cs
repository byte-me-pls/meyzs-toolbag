#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        private static void GenerateDiff()
        {
            if (selectedSnapshot == null) return;

            currentDiff.Clear();

            Debug.Log($"=== DIFF DEBUG START ===");
            Debug.Log($"Snapshot has {selectedSnapshot.componentSnapshots.Count} saved components");

            foreach (var savedComponent in selectedSnapshot.componentSnapshots)
            {
                // Play moddan çıktıktan sonra Instance ID'ler değişebilir
                // O yüzden GameObject path ile bulmaya çalışalım
                var currentComponent = FindComponentByPath(savedComponent.gameObjectPath, savedComponent.componentType);

                if (currentComponent != null)
                {
                    Debug.Log($"Comparing {savedComponent.componentType} on {savedComponent.gameObjectPath}");
                    Debug.Log($"Saved InstanceID: {savedComponent.instanceID}, Current InstanceID: {currentComponent.GetInstanceID()}");
                    Debug.Log($"Saved data length: {savedComponent.serializedData.Length}");

                    var diff = CompareComponents(savedComponent, currentComponent);
                    Debug.Log($"Found {diff.fieldDiffs.Count} field differences");

                    foreach (var fieldDiff in diff.fieldDiffs)
                    {
                        Debug.Log($"  - {fieldDiff.fieldPath}: '{fieldDiff.oldValue}' → '{fieldDiff.newValue}'");
                    }

                    if (diff.hasChanges)
                    {
                        currentDiff.Add(diff);
                    }
                }
                else
                {
                    Debug.LogWarning($"Component {savedComponent.componentType} on path {savedComponent.gameObjectPath} not found!");

                    // Alternatif olarak InstanceID ile de deneyelim
                    var altComponent = EditorUtility.InstanceIDToObject(savedComponent.instanceID) as Component;
                    if (altComponent != null)
                    {
                        Debug.Log($"Found component via InstanceID: {altComponent.GetType().Name}");
                        var diff = CompareComponents(savedComponent, altComponent);
                        if (diff.hasChanges)
                        {
                            currentDiff.Add(diff);
                        }
                    }
                }
            }

            Debug.Log($"Total components with changes: {currentDiff.Count}");
            Debug.Log($"=== DIFF DEBUG END ===");
        }

        private static Component FindComponentByPath(string gameObjectPath, string componentType)
        {
            try
            {
                // Scene'deki tüm objeler arasında path ile ara
                var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

                foreach (var go in allGameObjects)
                {
                    // Sadece scene'deki objeler (prefab'lar değil)
                    if (go.scene.name == null) continue;

                    string currentPath = GetGameObjectPath(go);
                    if (currentPath == gameObjectPath)
                    {
                        // Doğru GameObject bulundu, şimdi component'i ara
                        var components = go.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp != null && comp.GetType().Name == componentType)
                            {
                                return comp;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error finding component by path: {e.Message}");
            }

            return null;
        }

        private static ComponentDiff CompareComponents(ComponentSnapshot saved, Component current)
        {
            var diff = new ComponentDiff
            {
                gameObjectPath = saved.gameObjectPath,
                componentType = saved.componentType,
                instanceID = current.GetInstanceID() // Current component'in ID'sini kullan
            };

            try
            {
                string currentSerialized = SerializeComponentWithSerializedObject(current);

                Debug.Log($"=== COMPONENT COMPARISON ===");
                Debug.Log($"Component: {saved.componentType}");
                Debug.Log($"Saved Data: {saved.serializedData}");
                Debug.Log($"Current Data: {currentSerialized}");

                var savedData = ParseSerializedStringData(saved.serializedData);
                var currentData = ParseSerializedStringData(currentSerialized);

                Debug.Log($"Saved properties count: {savedData.Count}");
                Debug.Log($"Current properties count: {currentData.Count}");

                // Her property'yi detaylı logla
                foreach (var kvp in savedData)
                {
                    Debug.Log($"Saved Property '{kvp.Key}': '{kvp.Value}'");
                }

                foreach (var kvp in currentData)
                {
                    Debug.Log($"Current Property '{kvp.Key}': '{kvp.Value}'");
                }

                CompareStringPropertyData(savedData, currentData, diff);

            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to compare component {current.GetType().Name}: {e.Message}");
            }

            return diff;
        }

        private static void CompareStringPropertyData(
            System.Collections.Generic.Dictionary<string, string> saved,
            System.Collections.Generic.Dictionary<string, string> current,
            ComponentDiff diff)
        {
            var allKeys = new System.Collections.Generic.HashSet<string>(saved.Keys);
            allKeys.UnionWith(current.Keys);

            Debug.Log($"=== PROPERTY COMPARISON ===");
            Debug.Log($"Total properties to compare: {allKeys.Count}");

            foreach (var key in allKeys)
            {
                saved.TryGetValue(key, out var savedValue);
                current.TryGetValue(key, out var currentValue);

                string savedStr = savedValue ?? "null";
                string currentStr = currentValue ?? "null";

                Debug.Log($"Comparing '{key}': Saved='{savedStr}' vs Current='{currentStr}'");

                if (savedStr != currentStr)
                {
                    Debug.Log($"*** DIFFERENCE FOUND in '{key}': '{savedStr}' → '{currentStr}'");
                    // Old = saved (snapshot), New = current (şu anki durum)
                    diff.fieldDiffs.Add(new FieldDiff(key, savedStr, currentStr));
                }
                else
                {
                    Debug.Log($"    No change in '{key}'");
                }
            }

            Debug.Log($"Total differences found: {diff.fieldDiffs.Count}");
        }
    }
}
#endif
