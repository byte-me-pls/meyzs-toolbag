// QuickMaterialSwapperTool.Core.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    public static partial class QuickMaterialSwapperTool
    {
        private static void ApplyMaterialToSelection()
        {
            if (chosenMaterial == null || Selection.gameObjects.Length == 0) return;

            lastOperations.Clear();
            var selectedObjects = Selection.gameObjects;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Apply Material: {chosenMaterial.name}");

            foreach (var go in selectedObjects)
            {
                var renderers = includeChildren
                    ? go.GetComponentsInChildren<Renderer>(includeInactive)
                    : go.GetComponents<Renderer>();

                foreach (var renderer in renderers)
                    ApplyMaterialToRenderer(renderer);
            }

            Undo.CollapseUndoOperations(group);
            canPerformUndo = true;

            Debug.Log($"Applied '{chosenMaterial.name}' to {lastOperations.Count} changes");

            var preset = presets.FirstOrDefault(p => MaterialUtils.GetMaterialFromGUID(p.guid) == chosenMaterial);
            if (preset != null) preset.usageCount++;

            SaveData();
        }

        private static void ApplyMaterialToRenderer(Renderer renderer)
        {
            Undo.RegisterCompleteObjectUndo(renderer, "Apply Material");

            var materials         = renderer.sharedMaterials;
            var originalMaterials = materials.ToArray();

            switch (applyMode)
            {
                case ApplyMode.ReplaceAll:
                    for (int i = 0; i < materials.Length; i++)
                    {
                        RecordOperation(renderer, i, materials[i], chosenMaterial);
                        materials[i] = chosenMaterial;
                    }
                    break;

                case ApplyMode.ReplaceFirst:
                    if (materials.Length > 0)
                    {
                        RecordOperation(renderer, 0, materials[0], chosenMaterial);
                        materials[0] = chosenMaterial;
                    }
                    break;

                case ApplyMode.ReplaceSpecific:
                    if (specificMaterialIndex >= 0 && specificMaterialIndex < materials.Length)
                    {
                        RecordOperation(renderer, specificMaterialIndex, materials[specificMaterialIndex], chosenMaterial);
                        materials[specificMaterialIndex] = chosenMaterial;
                    }
                    break;

                case ApplyMode.AddToEnd:
                    var newMaterials = new Material[materials.Length + 1];
                    Array.Copy(materials, newMaterials, materials.Length);
                    newMaterials[materials.Length] = chosenMaterial;
                    materials = newMaterials;
                    RecordOperation(renderer, materials.Length - 1, null, chosenMaterial);
                    break;

                case ApplyMode.SmartReplace:
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].shader == chosenMaterial.shader)
                        {
                            RecordOperation(renderer, i, materials[i], chosenMaterial);
                            materials[i] = chosenMaterial;
                        }
                    }
                    break;
            }

            renderer.sharedMaterials = materials;
        }

        private static void RecordOperation(Renderer renderer, int materialIndex, Material originalMaterial, Material newMaterial)
        {
            var op = new MaterialOperation
            {
                objectName       = renderer.gameObject.name,
                objectPath       = MaterialUtils.GetGameObjectPath(renderer.transform),
                originalMaterial = originalMaterial,
                newMaterial      = newMaterial,
                renderer         = renderer,
                materialIndex    = materialIndex,
                timestamp        = DateTime.Now
            };

            lastOperations.Add(op);
            operationHistory.Add(op);

            if (operationHistory.Count > MAX_HISTORY_ITEMS)
                operationHistory.RemoveAt(0);
        }

        private static void UndoLastOperation()
        {
            if (lastOperations.Count == 0) return;
            Undo.PerformUndo();
            lastOperations.Clear();
            canPerformUndo = false;
        }
    }
}
#endif
