#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Reset / copy / paste logic and a lightweight property clipboard.
    /// </summary>
    public class QuickActionsManager
    {
        private readonly Dictionary<string, object> _clipboard = new Dictionary<string, object>();

        public void CopyProperty(SerializedProperty prop)
        {
            string key = GetPropertyKey(prop);
            object value = GetPropertyValue(prop);
            _clipboard[key] = value;
        }

        public void PasteProperty(SerializedProperty prop)
        {
            string key = GetPropertyKey(prop);
            if (_clipboard.TryGetValue(key, out object value))
            {
                Undo.RecordObject(prop.serializedObject.targetObject, "Paste Property");
                SetPropertyValue(prop, value);
                prop.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(prop.serializedObject.targetObject);
            }
        }

        public void ResetProperty(SerializedProperty prop)
        {
            Undo.RecordObject(prop.serializedObject.targetObject, "Reset Property");
            ResetPropertyToDefault(prop);
            prop.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prop.serializedObject.targetObject);
        }

        public void ClearClipboard() => _clipboard.Clear();
        public int  GetClipboardCount() => _clipboard.Count;

        private string GetPropertyKey(SerializedProperty prop)
            => $"{prop.serializedObject.targetObject.GetType().Name}.{prop.propertyPath}";

        private object GetPropertyValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:           return prop.floatValue;
                case SerializedPropertyType.Integer:         return prop.intValue;
                case SerializedPropertyType.Boolean:         return prop.boolValue;
                case SerializedPropertyType.String:          return prop.stringValue;
                case SerializedPropertyType.Vector2:         return prop.vector2Value;
                case SerializedPropertyType.Vector3:         return prop.vector3Value;
                case SerializedPropertyType.Color:           return prop.colorValue;
                case SerializedPropertyType.ObjectReference: return prop.objectReferenceValue;
                case SerializedPropertyType.Enum:            return prop.enumValueIndex;
                default:                                     return null;
            }
        }

        private void SetPropertyValue(SerializedProperty prop, object value)
        {
            if (value == null) return;

            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Float:           prop.floatValue = Convert.ToSingle(value); break;
                    case SerializedPropertyType.Integer:         prop.intValue = Convert.ToInt32(value); break;
                    case SerializedPropertyType.Boolean:         prop.boolValue = Convert.ToBoolean(value); break;
                    case SerializedPropertyType.String:          prop.stringValue = value.ToString(); break;
                    case SerializedPropertyType.Vector2:         prop.vector2Value = (Vector2)value; break;
                    case SerializedPropertyType.Vector3:         prop.vector3Value = (Vector3)value; break;
                    case SerializedPropertyType.Color:           prop.colorValue = (Color)value; break;
                    case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = value as UnityEngine.Object; break;
                    case SerializedPropertyType.Enum:            prop.enumValueIndex = Convert.ToInt32(value); break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Enhanced Inspector] SetPropertyValue failed: {ex.Message}");
            }
        }

        private void ResetPropertyToDefault(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:           prop.floatValue = 0f; break;
                case SerializedPropertyType.Integer:         prop.intValue = 0; break;
                case SerializedPropertyType.Boolean:         prop.boolValue = false; break;
                case SerializedPropertyType.String:          prop.stringValue = ""; break;
                case SerializedPropertyType.Vector2:         prop.vector2Value = Vector2.zero; break;
                case SerializedPropertyType.Vector3:         prop.vector3Value = Vector3.zero; break;
                case SerializedPropertyType.Color:           prop.colorValue = Color.white; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
                case SerializedPropertyType.Enum:            prop.enumValueIndex = 0; break;
            }
        }
    }
}
#endif
