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

    // Start is called before the first frame update
    void Start()
    {
        /*DirectoryInfo dir = new DirectoryInfo("Assets/GameFiles/res/Object");
        FileInfo[] info = dir.GetFiles("*.arc");
        foreach (FileInfo f in info)
        {
            Archive archive = ArcReader.Read(f.FullName);
            Archives.Add(archive);
        }*/
    }

    // Update is called once per frame
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
        //this method for create default monoBehavior fields
        //that i make it in base class (Fluid3D)
        DrawDefaultInspector();

        foreach (Archive archive in ArcGlobalLoader.Archives)
        {
            
        }
        
        
        GUILayout.Box(ArcGlobalLoader.Archives.Count + "", GUILayout.Width(Screen.width - 20), GUILayout.Height(15));
    }
}
#endif