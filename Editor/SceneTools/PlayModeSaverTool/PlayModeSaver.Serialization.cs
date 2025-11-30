#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        private static string SerializeComponentWithSerializedObject(Component component)
        {
            var serializedObject = new SerializedObject(component);
            var propertyData = new Dictionary<string, string>();

            // Tüm visible property'leri al
            var prop = serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false; // İlk seviye sonrası children'a girme

                if (prop.name == "m_Script") continue;

                try
                {
                    string value = GetPropertyValueAsString(prop);
                    if (!string.IsNullOrEmpty(value))
                    {
                        propertyData[prop.propertyPath] = value;
                        Debug.Log($"Serialized property '{prop.propertyPath}' = '{value}' (type: {prop.propertyType})");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to serialize property {prop.propertyPath}: {e.Message}");
                }
            }

            // Eğer hiç property bulunamadıysa, tüm public field'ları manuel serialize et
            if (propertyData.Count == 0)
            {
                Debug.LogWarning($"No serialized properties found for {component.GetType().Name}, trying reflection...");
                SerializeComponentWithReflection(component, propertyData);
            }

            Debug.Log($"Total properties found for {component.GetType().Name}: {propertyData.Count}");

            // JsonUtility çalışmıyor, manuel JSON oluşturalım
            if (propertyData.Count == 0)
            {
                Debug.LogWarning($"No properties captured for {component.GetType().Name}!");
                return "{}";
            }

            var jsonPairs = new List<string>();
            foreach (var kvp in propertyData)
            {
                string escapedKey = kvp.Key.Replace("\"", "\\\"");
                string escapedValue = kvp.Value.Replace("\"", "\\\"");
                jsonPairs.Add($"\"{escapedKey}\":\"{escapedValue}\"");
            }

            string json = "{\"properties\":{" + string.Join(",", jsonPairs) + "}}";
            Debug.Log($"Final serialized data for {component.GetType().Name}: {json}");
            return json;
        }

        private static void SerializeComponentWithReflection(Component component, Dictionary<string, string> propertyData)
        {
            var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(component);
                    if (value != null)
                    {
                        string valueStr = value.ToString();

                        // Özel tip kontrolü
                        if (field.FieldType == typeof(Vector3))
                        {
                            var v3 = (Vector3)value;
                            valueStr = $"{v3.x:F6},{v3.y:F6},{v3.z:F6}";
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            valueStr = ((float)value).ToString("F6");
                        }
                        else if (field.FieldType == typeof(bool))
                        {
                            valueStr = value.ToString();
                        }

                        propertyData[field.Name] = valueStr;
                        Debug.Log($"Reflected field '{field.Name}' = '{valueStr}' (type: {field.FieldType.Name})");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to get field {field.Name}: {e.Message}");
                }
            }
        }

        private static string GetPropertyValueAsString(SerializedProperty prop)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        return prop.intValue.ToString();
                    case SerializedPropertyType.Boolean:
                        return prop.boolValue.ToString();
                    case SerializedPropertyType.Float:
                        return prop.floatValue.ToString("F6");
                    case SerializedPropertyType.String:
                        return prop.stringValue ?? "";
                    case SerializedPropertyType.Vector2:
                        var v2 = prop.vector2Value;
                        return $"{v2.x:F6},{v2.y:F6}";
                    case SerializedPropertyType.Vector3:
                        var v3 = prop.vector3Value;
                        return $"{v3.x:F6},{v3.y:F6},{v3.z:F6}";
                    case SerializedPropertyType.Vector4:
                        var v4 = prop.vector4Value;
                        return $"{v4.x:F6},{v4.y:F6},{v4.z:F6},{v4.w:F6}";
                    case SerializedPropertyType.Color:
                        var c = prop.colorValue;
                        return $"{c.r:F6},{c.g:F6},{c.b:F6},{c.a:F6}";
                    case SerializedPropertyType.ObjectReference:
                        if (prop.objectReferenceValue != null)
                        {
                            var obj = prop.objectReferenceValue;
                            string assetPath = AssetDatabase.GetAssetPath(obj);
                            return $"{obj.GetInstanceID()}|{obj.name}|{obj.GetType().Name}|{assetPath}";
                        }
                        return "null";
                    case SerializedPropertyType.Enum:
                        return $"{prop.enumValueIndex}|{prop.enumDisplayNames[prop.enumValueIndex]}";
                    case SerializedPropertyType.Rect:
                        var r = prop.rectValue;
                        return $"{r.x:F6},{r.y:F6},{r.width:F6},{r.height:F6}";
                    case SerializedPropertyType.AnimationCurve:
                        if (prop.animationCurveValue != null)
                        {
                            var curve = prop.animationCurveValue;
                            var keys = new System.Collections.Generic.List<string>();
                            foreach (var key in curve.keys)
                            {
                                keys.Add($"{key.time:F6}:{key.value:F6}:{key.inTangent:F6}:{key.outTangent:F6}");
                            }
                            return $"CURVE[{curve.preWrapMode},{curve.postWrapMode}]({string.Join(";", keys)})";
                        }
                        return "null";
                    case SerializedPropertyType.Bounds:
                        var b = prop.boundsValue;
                        return $"{b.center.x:F6},{b.center.y:F6},{b.center.z:F6}|{b.size.x:F6},{b.size.y:F6},{b.size.z:F6}";
                    case SerializedPropertyType.Quaternion:
                        var q = prop.quaternionValue;
                        return $"{q.x:F6},{q.y:F6},{q.z:F6},{q.w:F6}";
                    case SerializedPropertyType.Vector2Int:
                        var v2i = prop.vector2IntValue;
                        return $"{v2i.x},{v2i.y}";
                    case SerializedPropertyType.Vector3Int:
                        var v3i = prop.vector3IntValue;
                        return $"{v3i.x},{v3i.y},{v3i.z}";
                    case SerializedPropertyType.RectInt:
                        var ri = prop.rectIntValue;
                        return $"{ri.x},{ri.y},{ri.width},{ri.height}";
                    case SerializedPropertyType.BoundsInt:
                        var bi = prop.boundsIntValue;
                        return $"{bi.center.x},{bi.center.y},{bi.center.z}|{bi.size.x},{bi.size.y},{bi.size.z}";
                    case SerializedPropertyType.LayerMask:
                        return prop.intValue.ToString();
                    case SerializedPropertyType.Character:
                        return ((int)prop.intValue).ToString();
                    case SerializedPropertyType.Gradient:
                        return "GRADIENT"; // Gradient serialization is complex
                    case SerializedPropertyType.ExposedReference:
                        if (prop.exposedReferenceValue != null)
                            return $"EXPOSED|{prop.exposedReferenceValue.GetInstanceID()}|{prop.exposedReferenceValue.name}";
                        return "EXPOSED|null";
                    case SerializedPropertyType.FixedBufferSize:
                        return $"FIXEDBUFFER|{prop.fixedBufferSize}";
                    case SerializedPropertyType.ManagedReference:
                        if (prop.managedReferenceValue != null)
                            return $"MANAGED|{prop.managedReferenceValue.GetType().Name}";
                        return "MANAGED|null";
                    default:
                        // Array ve Generic tipler için
                        if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                        {
                            var arrayValues = new System.Collections.Generic.List<string>();
                            for (int i = 0; i < prop.arraySize; i++)
                            {
                                var element = prop.GetArrayElementAtIndex(i);
                                arrayValues.Add(GetPropertyValueAsString(element));
                            }
                            return $"ARRAY[{prop.arraySize}]({string.Join(";", arrayValues)})";
                        }
                        else if (prop.hasChildren)
                        {
                            // Nested object serialization
                            var children = new System.Collections.Generic.List<string>();
                            var iterator = prop.Copy();
                            if (iterator.NextVisible(true))
                            {
                                var depth = iterator.depth;
                                do
                                {
                                    if (iterator.depth <= depth) break;
                                    children.Add($"{iterator.name}={GetPropertyValueAsString(iterator)}");
                                } while (iterator.NextVisible(false));
                            }
                            return $"NESTED({string.Join(";", children)})";
                        }

                        return $"[{prop.propertyType}]UNKNOWN";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error reading property {prop.propertyPath}: {e.Message}");
                return $"[ERROR:{prop.propertyType}]";
            }
        }

        private static System.Collections.Generic.Dictionary<string, string> ParseSerializedStringData(string serializedData)
        {
            try
            {
                Debug.Log($"Parsing serialized data: {serializedData}");

                if (string.IsNullOrEmpty(serializedData) || serializedData == "{}")
                {
                    Debug.Log("Empty or null serialized data");
                    return new System.Collections.Generic.Dictionary<string, string>();
                }

                // JsonUtility yerine manuel parsing
                if (serializedData.Contains("\"properties\":"))
                {
                    int propertiesStart = serializedData.IndexOf("\"properties\":{") + "\"properties\":{".Length;
                    int propertiesEnd = serializedData.LastIndexOf("}}");

                    if (propertiesEnd == -1) propertiesEnd = serializedData.LastIndexOf("}");

                    if (propertiesStart > 0 && propertiesEnd > propertiesStart)
                    {
                        string propertiesJson = serializedData.Substring(propertiesStart, propertiesEnd - propertiesStart);
                        Debug.Log($"Extracted properties JSON: {propertiesJson}");

                        var result = new System.Collections.Generic.Dictionary<string, string>();

                        // Basit JSON parsing - regex kullan
                        var regex = new Regex("\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
                        var matches = regex.Matches(propertiesJson);

                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count >= 3)
                            {
                                string key = match.Groups[1].Value;
                                string value = match.Groups[2].Value;
                                result[key] = value;
                                Debug.Log($"Parsed property: '{key}' = '{value}'");
                            }
                        }

                        Debug.Log($"Total parsed properties: {result.Count}");
                        return result;
                    }
                }

                // Fallback: JsonUtility deneyelim
                var container = JsonUtility.FromJson<SerializableStringContainer>(serializedData);
                return container?.properties ?? new System.Collections.Generic.Dictionary<string, string>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse serialized data: {e.Message}");
                Debug.LogError($"Data was: {serializedData}");
                return new System.Collections.Generic.Dictionary<string, string>();
            }
        }

        private static void ApplySerializedDataToComponent(Component component, string serializedData)
        {
            var propertyData = ParseSerializedStringData(serializedData);
            var serializedObject = new SerializedObject(component);

            foreach (var kvp in propertyData)
            {
                var prop = serializedObject.FindProperty(kvp.Key);
                if (prop != null)
                {
                    SetPropertyValueFromString(prop, kvp.Value);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetPropertyValueFromString(SerializedProperty prop, string value)
        {
            if (string.IsNullOrEmpty(value) || value == "null")
            {
                // Handle null values appropriately
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    prop.objectReferenceValue = null;
                return;
            }

            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (int.TryParse(value, out int intVal)) prop.intValue = intVal;
                        break;
                    case SerializedPropertyType.Boolean:
                        if (bool.TryParse(value, out bool boolVal)) prop.boolValue = boolVal;
                        break;
                    case SerializedPropertyType.Float:
                        if (float.TryParse(value, out float floatVal)) prop.floatValue = floatVal;
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = value;
                        break;
                    case SerializedPropertyType.Vector2:
                        var v2Parts = value.Split(',');
                        if (v2Parts.Length == 2 && float.TryParse(v2Parts[0], out float x2) && float.TryParse(v2Parts[1], out float y2))
                            prop.vector2Value = new Vector2(x2, y2);
                        break;
                    case SerializedPropertyType.Vector3:
                        var v3Parts = value.Split(',');
                        if (v3Parts.Length == 3 && float.TryParse(v3Parts[0], out float x3) && float.TryParse(v3Parts[1], out float y3) && float.TryParse(v3Parts[2], out float z3))
                            prop.vector3Value = new Vector3(x3, y3, z3);
                        break;
                    case SerializedPropertyType.Vector4:
                        var v4Parts = value.Split(',');
                        if (v4Parts.Length == 4 && float.TryParse(v4Parts[0], out float x4) && float.TryParse(v4Parts[1], out float y4) && float.TryParse(v4Parts[2], out float z4) && float.TryParse(v4Parts[3], out float w4))
                            prop.vector4Value = new Vector4(x4, y4, z4, w4);
                        break;
                    case SerializedPropertyType.Color:
                        var cParts = value.Split(',');
                        if (cParts.Length == 4 && float.TryParse(cParts[0], out float r) && float.TryParse(cParts[1], out float g) && float.TryParse(cParts[2], out float b) && float.TryParse(cParts[3], out float a))
                            prop.colorValue = new Color(r, g, b, a);
                        break;
                    case SerializedPropertyType.Enum:
                        var enumParts = value.Split('|');
                        if (enumParts.Length >= 1 && int.TryParse(enumParts[0], out int enumVal))
                            prop.enumValueIndex = enumVal;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        if (value != "null" && value.Contains('|'))
                        {
                            var objParts = value.Split('|');
                            if (objParts.Length >= 3 && int.TryParse(objParts[0], out int instanceID))
                            {
                                var obj = EditorUtility.InstanceIDToObject(instanceID);
                                if (obj == null && objParts.Length >= 4 && !string.IsNullOrEmpty(objParts[3]))
                                {
                                    // Try to load by asset path
                                    obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objParts[3]);
                                }
                                prop.objectReferenceValue = obj;
                            }
                        }
                        else
                        {
                            prop.objectReferenceValue = null;
                        }
                        break;
                    case SerializedPropertyType.Rect:
                        var rParts = value.Split(',');
                        if (rParts.Length == 4 &&
                            float.TryParse(rParts[0], out float rx) &&
                            float.TryParse(rParts[1], out float ry) &&
                            float.TryParse(rParts[2], out float rw) &&
                            float.TryParse(rParts[3], out float rh))
                            prop.rectValue = new Rect(rx, ry, rw, rh);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        if (value.StartsWith("CURVE["))
                        {
                            try
                            {
                                // Parse curve data: CURVE[preWrap,postWrap](time:value:inTangent:outTangent;...)
                                var wrapStart = value.IndexOf('[') + 1;
                                var wrapEnd = value.IndexOf(']');
                                var keysStart = value.IndexOf('(') + 1;
                                var keysEnd = value.LastIndexOf(')');

                                if (wrapStart > 0 && wrapEnd > wrapStart && keysStart > 0 && keysEnd > keysStart)
                                {
                                    var wrapModes = value.Substring(wrapStart, wrapEnd - wrapStart).Split(',');
                                    var keysStr = value.Substring(keysStart, keysEnd - keysStart);

                                    var curve = new AnimationCurve();

                                    if (!string.IsNullOrEmpty(keysStr))
                                    {
                                        var keyStrings = keysStr.Split(';');
                                        foreach (var keyStr in keyStrings)
                                        {
                                            var keyParts = keyStr.Split(':');
                                            if (keyParts.Length >= 4 &&
                                                float.TryParse(keyParts[0], out float time) &&
                                                float.TryParse(keyParts[1], out float keyValue) &&
                                                float.TryParse(keyParts[2], out float inTangent) &&
                                                float.TryParse(keyParts[3], out float outTangent))
                                            {
                                                curve.AddKey(new Keyframe(time, keyValue, inTangent, outTangent));
                                            }
                                        }
                                    }

                                    if (wrapModes.Length >= 2)
                                    {
                                        if (System.Enum.TryParse<WrapMode>(wrapModes[0], out WrapMode preWrap))
                                            curve.preWrapMode = preWrap;
                                        if (System.Enum.TryParse<WrapMode>(wrapModes[1], out WrapMode postWrap))
                                            curve.postWrapMode = postWrap;
                                    }

                                    prop.animationCurveValue = curve;
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"Failed to parse AnimationCurve: {e.Message}");
                            }
                        }
                        break;
                    case SerializedPropertyType.Bounds:
                        var bParts = value.Split('|');
                        if (bParts.Length == 2)
                        {
                            var centerParts = bParts[0].Split(',');
                            var sizeParts = bParts[1].Split(',');
                            if (centerParts.Length == 3 && sizeParts.Length == 3 &&
                                float.TryParse(centerParts[0], out float cx) && float.TryParse(centerParts[1], out float cy) && float.TryParse(centerParts[2], out float cz) &&
                                float.TryParse(sizeParts[0], out float sx) && float.TryParse(sizeParts[1], out float sy) && float.TryParse(sizeParts[2], out float sz))
                            {
                                prop.boundsValue = new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
                            }
                        }
                        break;
                    case SerializedPropertyType.Quaternion:
                        var qParts = value.Split(',');
                        if (qParts.Length == 4 &&
                            float.TryParse(qParts[0], out float qx) &&
                            float.TryParse(qParts[1], out float qy) &&
                            float.TryParse(qParts[2], out float qz) &&
                            float.TryParse(qParts[3], out float qw))
                            prop.quaternionValue = new Quaternion(qx, qy, qz, qw);
                        break;
                    case SerializedPropertyType.Vector2Int:
                        var v2iParts = value.Split(',');
                        if (v2iParts.Length == 2 && int.TryParse(v2iParts[0], out int x2i) && int.TryParse(v2iParts[1], out int y2i))
                            prop.vector2IntValue = new Vector2Int(x2i, y2i);
                        break;
                    case SerializedPropertyType.Vector3Int:
                        var v3iParts = value.Split(',');
                        if (v3iParts.Length == 3 && int.TryParse(v3iParts[0], out int x3i) && int.TryParse(v3iParts[1], out int y3i) && int.TryParse(v3iParts[2], out int z3i))
                            prop.vector3IntValue = new Vector3Int(x3i, y3i, z3i);
                        break;
                    case SerializedPropertyType.RectInt:
                        var riParts = value.Split(',');
                        if (riParts.Length == 4 &&
                            int.TryParse(riParts[0], out int rix) &&
                            int.TryParse(riParts[1], out int riy) &&
                            int.TryParse(riParts[2], out int riw) &&
                            int.TryParse(riParts[3], out int rih))
                            prop.rectIntValue = new RectInt(rix, riy, riw, rih);
                        break;
                    case SerializedPropertyType.BoundsInt:
                        var biParts = value.Split('|');
                        if (biParts.Length == 2)
                        {
                            var centerIntParts = biParts[0].Split(',');
                            var sizeIntParts = biParts[1].Split(',');
                            if (centerIntParts.Length == 3 && sizeIntParts.Length == 3 &&
                                int.TryParse(centerIntParts[0], out int cix) && int.TryParse(centerIntParts[1], out int ciy) && int.TryParse(centerIntParts[2], out int ciz) &&
                                int.TryParse(sizeIntParts[0], out int six) && int.TryParse(sizeIntParts[1], out int siy) && int.TryParse(sizeIntParts[2], out int siz))
                            {
                                prop.boundsIntValue = new BoundsInt(new Vector3Int(cix, ciy, ciz), new Vector3Int(six, siy, siz));
                            }
                        }
                        break;
                    case SerializedPropertyType.LayerMask:
                        if (int.TryParse(value, out int layerVal)) prop.intValue = layerVal;
                        break;
                    case SerializedPropertyType.Character:
                        if (int.TryParse(value, out int charVal)) prop.intValue = charVal;
                        break;
                    default:
                        if (value.StartsWith("ARRAY[") && prop.isArray)
                        {
                            // Parse array data: ARRAY[size](element1;element2;...)
                            var sizeStart = value.IndexOf('[') + 1;
                            var sizeEnd = value.IndexOf(']');
                            var elementsStart = value.IndexOf('(') + 1;
                            var elementsEnd = value.LastIndexOf(')');

                            if (sizeStart > 0 && sizeEnd > sizeStart && elementsStart > 0 && elementsEnd > elementsStart)
                            {
                                if (int.TryParse(value.Substring(sizeStart, sizeEnd - sizeStart), out int arraySize))
                                {
                                    prop.arraySize = arraySize;
                                    var elementsStr = value.Substring(elementsStart, elementsEnd - elementsStart);

                                    if (!string.IsNullOrEmpty(elementsStr) && arraySize > 0)
                                    {
                                        var elements = elementsStr.Split(';');
                                        for (int i = 0; i < Mathf.Min(elements.Length, arraySize); i++)
                                        {
                                            var element = prop.GetArrayElementAtIndex(i);
                                            SetPropertyValueFromString(element, elements[i]);
                                        }
                                    }
                                }
                            }
                        }
                        else if (value.StartsWith("NESTED(") && prop.hasChildren)
                        {
                            // Parse nested object: NESTED(child1=value1;child2=value2;...)
                            var childrenStart = value.IndexOf('(') + 1;
                            var childrenEnd = value.LastIndexOf(')');

                            if (childrenStart > 0 && childrenEnd > childrenStart)
                            {
                                var childrenStr = value.Substring(childrenStart, childrenEnd - childrenStart);
                                var children = childrenStr.Split(';');

                                foreach (var child in children)
                                {
                                    var equalIndex = child.IndexOf('=');
                                    if (equalIndex > 0)
                                    {
                                        var childName = child.Substring(0, equalIndex);
                                        var childValue = child.Substring(equalIndex + 1);
                                        var childProp = prop.FindPropertyRelative(childName);
                                        if (childProp != null)
                                        {
                                            SetPropertyValueFromString(childProp, childValue);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set property {prop.name}: {e.Message}");
            }
        }
    }
}
#endif
