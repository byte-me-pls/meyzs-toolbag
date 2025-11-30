#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Core lifecycle and shared helpers for the enhanced inspector editor.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public partial class EnhancedInspectorEditor : UnityEditor.Editor
    {
        private bool _useEnhancedInspector;
        private PropertySearchEngine _searchEngine;
        private PropertyGroupManager _groupManager;
        private QuickActionsManager _actionsManager;
        private EnhancedInspectorSettings _settings;

        private void OnEnable()
        {
            // Only activate for classes marked with attribute
            _useEnhancedInspector = target.GetType()
                .GetCustomAttribute<EnhancedInspectorTool.EnhancedInspectorAttribute>() != null;

            if (_useEnhancedInspector)
            {
                _searchEngine = new PropertySearchEngine();
                _groupManager = new PropertyGroupManager();
                _actionsManager = new QuickActionsManager();
                _settings = new EnhancedInspectorSettings();
                _settings.LoadSettings();
                _settings.RegisterEnhancedClass(target.GetType().Name);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!_useEnhancedInspector)
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();

            DrawEnhancedProperties();

            if (EnhancedInspectorTool.EnableBatchOperations)
                DrawBatchControls();

            serializedObject.ApplyModifiedProperties();
        }

        // ------- shared helpers used across Draw/Batch partials -------

        private int GetButtonCount()
        {
            int count = 1; // reset
            count++;       // copy
            count++;       // paste

            if (Selection.gameObjects.Length > 1)
                count++;   // multi-apply

            return count;
        }

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
                    case SerializedPropertyType.Float:           prop.floatValue = System.Convert.ToSingle(value); break;
                    case SerializedPropertyType.Integer:         prop.intValue = System.Convert.ToInt32(value);   break;
                    case SerializedPropertyType.Boolean:         prop.boolValue = System.Convert.ToBoolean(value); break;
                    case SerializedPropertyType.String:          prop.stringValue = value.ToString();              break;
                    case SerializedPropertyType.Vector2:         prop.vector2Value = (Vector2)value;              break;
                    case SerializedPropertyType.Vector3:         prop.vector3Value = (Vector3)value;              break;
                    case SerializedPropertyType.Color:           prop.colorValue = (Color)value;                  break;
                    case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = value as Object;      break;
                    case SerializedPropertyType.Enum:            prop.enumValueIndex = System.Convert.ToInt32(value); break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Enhanced Inspector] SetPropertyValue failed: {ex.Message}");
            }
        }

        private void ResetPropertyToDefault(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:           prop.floatValue = 0f;             break;
                case SerializedPropertyType.Integer:         prop.intValue = 0;                break;
                case SerializedPropertyType.Boolean:         prop.boolValue = false;           break;
                case SerializedPropertyType.String:          prop.stringValue = "";            break;
                case SerializedPropertyType.Vector2:         prop.vector2Value = Vector2.zero; break;
                case SerializedPropertyType.Vector3:         prop.vector3Value = Vector3.zero; break;
                case SerializedPropertyType.Color:           prop.colorValue = Color.white;    break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
                case SerializedPropertyType.Enum:            prop.enumValueIndex = 0;          break;
            }
        }
    }
}
#endif
