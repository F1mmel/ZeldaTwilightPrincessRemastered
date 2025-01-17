using Animancer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Link))]
public class LinkAnimPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Link player = (Link)target;

        DrawDefaultInspector();

        EditorGUILayout.LabelField("Animation States", EditorStyles.boldLabel);

        if (player._allAnims != null && player._allAnims.Count > 0)
        {
            foreach (var kvp in player._allAnims)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));

                if (GUILayout.Button("Play"))
                {
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