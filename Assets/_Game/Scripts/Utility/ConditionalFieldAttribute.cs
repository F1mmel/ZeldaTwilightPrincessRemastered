using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ConditionalField
{
    public bool IsActive;
    public string Value;
}

public class ConditionalFieldAttribute : PropertyAttribute
{
    public string label;

    public ConditionalFieldAttribute(string label)
    {
        this.label = label;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Berechnung der Positionen
        float labelWidth = EditorGUIUtility.labelWidth;
        float checkboxWidth = 20f;
        float spacing = 5f;
        
        // Label
        Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        EditorGUI.LabelField(labelRect, (attribute as ConditionalFieldAttribute).label);
        
        // Checkbox
        Rect checkboxRect = new Rect(position.x + labelWidth, position.y, checkboxWidth, position.height);
        SerializedProperty isActiveProp = property.FindPropertyRelative("IsActive");
        isActiveProp.boolValue = EditorGUI.Toggle(checkboxRect, isActiveProp.boolValue);

        // Textfeld
        if (isActiveProp.boolValue)
        {
            Rect textFieldRect = new Rect(position.x + labelWidth + checkboxWidth + spacing, position.y, position.width - labelWidth - checkboxWidth - spacing, position.height);
            SerializedProperty textProp = property.FindPropertyRelative("Value");
            EditorGUI.PropertyField(textFieldRect, textProp, GUIContent.none);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
}
#endif