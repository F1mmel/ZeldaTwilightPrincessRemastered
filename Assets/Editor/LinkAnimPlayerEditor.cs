using Animancer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Link))]
public class LinkAnimPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Hol das Zielobjekt
        Link player = (Link)target;

        // Zeichne die Standard-Inspector-Elemente
        DrawDefaultInspector();

        // Zeichne die Animation States
        EditorGUILayout.LabelField("Animation States", EditorStyles.boldLabel);

        if (player._allAnims != null && player._allAnims.Count > 0)
        {
            foreach (var kvp in player._allAnims)
            {
                EditorGUILayout.BeginHorizontal();

                // Zeige den Schlüssel (Name der Animation)
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));

                // "Play"-Button
                if (GUILayout.Button("Play"))
                {
                    //Link.PlayAnimation(kvp.Value);
                }

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No Animations available.", EditorStyles.helpBox);
        }
    }
}