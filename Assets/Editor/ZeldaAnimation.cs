using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ZeldaAnimation))]
public class ZeldaAnimationDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Feldhöhe berechnen
        float singleLineHeight = EditorGUIUtility.singleLineHeight + 2;

        // Erste Zeile: Label
        position.height = singleLineHeight;
        EditorGUI.LabelField(position, label);

        // Zugriff auf die Properties
        var durationProp = property.FindPropertyRelative("duration");
        var loopTypeProp = property.FindPropertyRelative("loopType");
        var behaviorProp = property.FindPropertyRelative("behavior");
        var eventsProp = property.FindPropertyRelative("Events");

        // Zweite Zeile: Duration
        position.y += singleLineHeight;
        EditorGUI.PropertyField(position, durationProp);

        // Dritte Zeile: LoopType
        position.y += singleLineHeight;
        EditorGUI.PropertyField(position, loopTypeProp);

        // Vierte Zeile: Behavior
        position.y += singleLineHeight;
        EditorGUI.PropertyField(position, behaviorProp);

        // Events anzeigen
        position.y += singleLineHeight;
        EditorGUI.LabelField(position, "Events:");

        if (eventsProp != null)
        {
            for (int i = 0; i < eventsProp.arraySize; i++)
            {
                position.y += singleLineHeight;
                var eventProp = eventsProp.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(position, eventProp, new GUIContent($"Event {i + 1}"));
            }
        }

        if (GUI.Button(new Rect(position.x, position.y + singleLineHeight, position.width, singleLineHeight), "Add Event"))
        {
            eventsProp.arraySize++;
        }

        EditorGUI.EndProperty();
    }
}