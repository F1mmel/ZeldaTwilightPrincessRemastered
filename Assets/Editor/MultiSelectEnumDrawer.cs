using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StageLoaderSettings))]
public class StageLoaderSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.LabelField(position, label);

        Rect dropdownPosition = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

        StageLoaderSettings currentSettings = (StageLoaderSettings)property.intValue;

        StageLoaderSettings newSettings = (StageLoaderSettings)EditorGUI.EnumFlagsField(dropdownPosition, currentSettings);

        if (newSettings != currentSettings)
        {
            property.intValue = (int)newSettings;
        }
    }
}