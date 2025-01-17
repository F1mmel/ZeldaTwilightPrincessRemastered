using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.IO;
using DotLiquid.Util;
#if UNITY_EDITOR
using UnityEditor;
#endif
using WiiExplorer;
using UnityEngine;

public class BMDCreator : MonoBehaviour
{
    [Header("Inspector")] 
    public string SelectedArcFile;
    public string SelectedBMD;
    public string SelectedBCK;

    private static string path = "Assets/GameFiles/res/Object/";
    private static string pathHD = "Assets/GameFilesHD/res/Object/";

    public static BMD CreateModel(string arcFile, string bmdFile, Transform parent)
    {
        Archive archive =
            ArcReader.Read(path + arcFile + ".arc");
        BMD Bmd = BMD.CreateModelFromPathInPlace(archive, bmdFile, null, parent, false);
        
        WeightDataGenerator weightDataGenerator = Bmd.transform.AddComponent<WeightDataGenerator>();
        weightDataGenerator.bmd = Bmd;
        
        weightDataGenerator.PrepareWeights(Bmd);

        foreach (MeshFilter filter in parent.GetComponentsInChildren<MeshFilter>())
        {
            filter.gameObject.SetActive(false);
            DestroyImmediate(filter.gameObject);
        }

        return Bmd;
    }

    public BMD FutureBmd;

    private BMD _bmd;

    private Vector3 startPos;

    private Vector3 startRot;
    void Start()
    {
        Archive archive =
            ArcReader.Read(path + SelectedArcFile);
        _bmd = BMD.CreateModelFromPathInPlace(archive, SelectedBMD, null, transform, false);
        
        WeightDataGenerator weightDataGenerator = _bmd.transform.AddComponent<WeightDataGenerator>();
        weightDataGenerator.bmd = _bmd;

        FutureBmd = _bmd;
        
        weightDataGenerator.PrepareWeights(_bmd);

        foreach (MeshFilter filter in transform.GetComponentsInChildren<MeshFilter>())
        {
            DestroyImmediate(filter.gameObject);
        }

        _bmd.transform.position = transform.position;
        _bmd.transform.eulerAngles = transform.eulerAngles;

        startPos = transform.position;
        startRot = transform.eulerAngles;


            if (!SelectedBCK.Equals("") && !SelectedBCK.Equals("--"))
            {

                    AnimationJobManager job = _bmd.AddComponent<AnimationJobManager>();
                    job.PlayAnimation(SelectedBCK);

                    foreach (ArcFile file in archive.Files)
                    {
                        if (file.Name.EndsWith(".bck"))
                        {
                        }
                    }
                    


            }
            


        {
        }
    }

    public void SetParentJoint(BMDCreator target, string boneName)
    {
        _bmd.SetParentJoint(target.FutureBmd, boneName);

        _bmd.transform.position = startPos;
        _bmd.transform.eulerAngles = startRot;
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(BMDCreator))]
public class BMDCreator_Inspector : Editor
{
    private string[] arcFiles;
    private List<string> bmdFiles = new List<string>();
    private List<string> bckFiles = new List<string>();
    private int selectedIndex = 0;
    private int previousIndex = -1;
    private int selectedBMDIndex = 0;
    private int previousBMDIndex = -1;
    private int selectedBCKIndex = 0;
    private int previousBCKIndex = -1;

    private string path = "Assets/GameFiles/res/Object/";

    private void OnEnable()
    {
        if (Directory.Exists(path))
        {
            arcFiles = Directory.GetFiles(path, "*.arc")
                                .Select(Path.GetFileName)
                                .ToArray();
        }
        else
        {
            arcFiles = new string[0];
        }

        BMDCreator creator = (BMDCreator)target;
        selectedIndex = System.Array.IndexOf(arcFiles, creator.SelectedArcFile);
        if (selectedIndex == -1) selectedIndex = 0;
        LoadArcContents(path + arcFiles[selectedIndex]);

        selectedBMDIndex = System.Array.IndexOf(bmdFiles.ToArray(), creator.SelectedBMD);
        if (selectedBMDIndex == -1) selectedBMDIndex = 0;

        selectedBCKIndex = System.Array.IndexOf(bckFiles.ToArray(), creator.SelectedBCK);
        if (selectedBCKIndex == -1) selectedBCKIndex = 0;

        previousIndex = selectedIndex;
        previousBMDIndex = selectedBMDIndex;
        previousBCKIndex = selectedBCKIndex;
    }

    public override void OnInspectorGUI()
    {
        BMDCreator creator = (BMDCreator)target;

        if (arcFiles.Length > 0)
        {
            selectedIndex = EditorGUILayout.Popup("Select ARC File", selectedIndex, arcFiles);
                
            selectedBMDIndex = System.Array.IndexOf(bmdFiles.ToArray(), creator.SelectedBMD);
            if (selectedBMDIndex == -1) selectedBMDIndex = 0;

            selectedBCKIndex = System.Array.IndexOf(bckFiles.ToArray(), creator.SelectedBCK);
            if (selectedBCKIndex == -1) selectedBCKIndex = 0;

            previousBMDIndex = selectedBMDIndex;
            previousBCKIndex = selectedBCKIndex;

            if(bmdFiles.Count > previousBMDIndex) creator.SelectedBMD = bmdFiles[previousBMDIndex];
            if(bckFiles.Count > previousBCKIndex) creator.SelectedBCK = bckFiles[previousBCKIndex];

            if (selectedIndex != previousIndex)
            {
                previousIndex = selectedIndex;
                creator.SelectedArcFile = arcFiles[selectedIndex];
                Debug.Log("Selected ARC File: " + creator.SelectedArcFile);
                
                LoadArcContents(path + creator.SelectedArcFile);
            }



                
                

            if (bmdFiles.Count > 0)
            {
                selectedBMDIndex = EditorGUILayout.Popup("Select BMD File", selectedBMDIndex, bmdFiles.ToArray());
                if (selectedBMDIndex != previousBMDIndex)
                {
                    previousBMDIndex = selectedBMDIndex;
                    creator.SelectedBMD = bmdFiles[selectedBMDIndex];
                }
            }
            else
            {
                EditorGUILayout.LabelField("No .bmd files found in the selected .arc file.");
            }

            if (bckFiles.Count > 0)
            {
                selectedBCKIndex = EditorGUILayout.Popup("Select BCK File", selectedBCKIndex, bckFiles.ToArray());
                if (selectedBCKIndex != previousBCKIndex)
                {
                    previousBCKIndex = selectedBCKIndex;
                    creator.SelectedBCK = bckFiles[selectedBCKIndex];
                }
            }
            else
            {
                EditorGUILayout.LabelField("No .bck files found in the selected .arc file.");
                creator.SelectedBCK = "";
            }
        }
        else
        {
            EditorGUILayout.LabelField("No .arc files found in the specified directory.");
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void LoadArcContents(string arcFilePath)
    {
        bmdFiles = new List<string>();
        bckFiles = new List<string>();
        bckFiles.Add("--");
        
        Archive archive = ArcReader.Read(arcFilePath);
        foreach (ArcFile file in archive.Files)
        {
            if (file.Name.EndsWith(".bmd"))
            {
                bmdFiles.Add(file.Name);
            } else if (file.Name.EndsWith(".bck"))
            {
                bckFiles.Add(file.Name);
            }
        }


        selectedBMDIndex = 0;
        selectedBCKIndex = 0;
    }
}
#endif