using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StageLoaderSettings))]
public class StageLoaderSettingsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Zeichne das Label
        EditorGUI.LabelField(position, label);

        // Berechne die Position des Dropdowns
        Rect dropdownPosition = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

        // Hole den aktuellen Wert
        StageLoaderSettings currentSettings = (StageLoaderSettings)property.intValue;

        // Zeichne das Dropdown-Menü mit der MaskField-Methode
        StageLoaderSettings newSettings = (StageLoaderSettings)EditorGUI.EnumFlagsField(dropdownPosition, currentSettings);

        // Aktualisiere den Wert, wenn sich etwas geändert hat
        if (newSettings != currentSettings)
        {
            property.intValue = (int)newSettings;
        }
    }
}