using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WiiExplorer;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArcGlobalLoader : MonoBehaviour
{
    public List<Archive> Archives = new List<Archive>();

    void Start()
    {
    }

    void Update()
    {
        
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArcGlobalLoader))]
public class Car_Inspector : Editor
{
    public ArcGlobalLoader ArcGlobalLoader;
    
    void OnEnable()
    {
        ArcGlobalLoader = (ArcGlobalLoader) target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        foreach (Archive archive in ArcGlobalLoader.Archives)
        {
            
        }
        
        
        GUILayout.Box(ArcGlobalLoader.Archives.Count + "", GUILayout.Width(Screen.width - 20), GUILayout.Height(15));
    }
}
#endif