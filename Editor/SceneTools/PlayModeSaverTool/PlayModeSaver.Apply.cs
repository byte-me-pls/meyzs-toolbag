#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        // Apply/Revert methods
        private static void ApplySnapshot(PlayModeSnapshot snapshot)
        {
            Undo.RecordObjects(Object.FindObjectsOfType<GameObject>(), "Apply PlayMode Snapshot");

            foreach (var componentSnapshot in snapshot.componentSnapshots)
            {
                ApplyComponentSnapshot(componentSnapshot);
            }

            if (settings?.autoMarkSceneDirty == true)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            ShowToast($"Applied snapshot '{snapshot.name}' with {snapshot.componentSnapshots.Count} components");
        }

        private static void ApplyComponentSnapshot(ComponentSnapshot componentSnapshot)
        {
            var component = EditorUtility.InstanceIDToObject(componentSnapshot.instanceID) as Component;
            if (component != null)
            {
                Undo.RecordObject(component, "Apply Component Snapshot");

                try
                {
                    ApplySerializedDataToComponent(component, componentSnapshot.serializedData);

                    if (component is Behaviour behaviour)
                    {
                        behaviour.enabled = componentSnapshot.isEnabled;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to apply component snapshot for {component.GetType().Name}: {e.Message}");
                }
            }
        }

        private static void ApplyAllDiffs()
        {
            foreach (var componentDiff in currentDiff)
            {
                ApplyComponentDiff(componentDiff);
            }
            ShowToast("Applied all differences");
        }

        private static void RevertAllDiffs()
        {
            foreach (var componentDiff in currentDiff)
            {
                RevertComponentDiff(componentDiff);
            }
            ShowToast("Reverted all differences");
        }

        private static void ApplyComponentDiff(ComponentDiff componentDiff)
        {
            foreach (var fieldDiff in componentDiff.fieldDiffs)
            {
                ApplyFieldDiff(fieldDiff);
            }
        }

        private static void RevertComponentDiff(ComponentDiff componentDiff)
        {
            foreach (var fieldDiff in componentDiff.fieldDiffs)
            {
                RevertFieldDiff(fieldDiff);
            }
        }

        private static void ApplyFieldDiff(FieldDiff fieldDiff)
        {
            // Snapshot'taki değeri şu anki component'e uygula
            try
            {
                if (selectedSnapshot == null) return;

                // Bu field hangi component'e ait?
                var componentDiff = currentDiff.FirstOrDefault(cd => cd.fieldDiffs.Contains(fieldDiff));
                if (componentDiff == null) return;

                // Component'i bul
                var component = FindComponentByPath(componentDiff.gameObjectPath, componentDiff.componentType);
                if (component == null)
                {
                    component = EditorUtility.InstanceIDToObject(componentDiff.instanceID) as Component;
                }

                if (component != null)
                {
                    Undo.RecordObject(component, "Apply Field Diff");

                    var serializedObject = new SerializedObject(component);
                    var prop = serializedObject.FindProperty(fieldDiff.fieldPath);

                    if (prop != null)
                    {
                        Debug.Log($"Applying field '{fieldDiff.fieldPath}': '{fieldDiff.newValue}' → '{fieldDiff.oldValue}'");
                        SetPropertyValueFromString(prop, fieldDiff.oldValue);
                        serializedObject.ApplyModifiedProperties();

                        fieldDiff.isApplied = true;
                        ShowToast($"Applied {fieldDiff.fieldPath} = {fieldDiff.oldValue}");

                        // Diff'i yenile
                        GenerateDiff();
                    }
                    else
                    {
                        Debug.LogError($"Property {fieldDiff.fieldPath} not found on {component.GetType().Name}");
                    }
                }
                else
                {
                    Debug.LogError($"Component {componentDiff.componentType} not found");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to apply field diff: {e.Message}");
            }
        }

        private static void RevertFieldDiff(FieldDiff fieldDiff)
        {
            // Şu anki değeri koru (hiçbir şey yapma, sadece işaretle)
            fieldDiff.isApplied = false;
            ShowToast($"Kept current value for {fieldDiff.fieldPath}");
        }
    }
}
#endif
