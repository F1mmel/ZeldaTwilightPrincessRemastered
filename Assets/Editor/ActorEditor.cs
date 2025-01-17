using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Actor))]
public class ActorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Actor actor = (Actor)target;

        SerializedProperty property = serializedObject.GetIterator();
        property.NextVisible(true);

        string[] currentGroup = new[] { "Always" };

        while (property.NextVisible(false))
        {
            var field = target.GetType().GetField(property.name);
            if (field != null)
            {
                var groupAttribute = field.GetCustomAttributes(typeof(ActorGroupAttribute), false).FirstOrDefault() as ActorGroupAttribute;
                if (groupAttribute != null)
                {
                    currentGroup = groupAttribute.Groups;
                }

                if (currentGroup.Contains("Always") || currentGroup.Contains(actor.ActorType.ToString()))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}