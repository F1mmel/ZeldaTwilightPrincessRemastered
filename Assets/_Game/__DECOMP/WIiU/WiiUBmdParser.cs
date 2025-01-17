using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using JStudio.J3D.Animation;
using UltEvents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using WiiExplorer;

public class WiiUBmdParser : MonoBehaviour
{
    public const string OBJ_PATH = "Assets/GameFiles_HD/res/Stage/F_SP103";
    
    public Material Material1;
    public Material Material2;

    [Header("Model")]
    public string Archive;
    public string ModelName;

    [Header("Animation")] public string ExternalArchive;
    public string AnimationName;

    public bool CreateOnStart = true;
    public bool UseRigidbody;

    public BMD Bmd;

    [Header("Properties")] public Vector3 Rotation = Vector3.zero;
    public Vector3 Scale = Vector3.zero;

    [Space] public bool CalculateBoneWeights;

    void Start()
    {







        
        
        
        
        
        



        if(CreateOnStart) Create();
    }

    public UltEvent ExposedEvent;
    public void Create()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles_HD\res\Stage\F_SP103\" + Archive + ".arc");
        Bmd = BMD.CreateModelFromPathInPlace(archive, ModelName, null, transform, UseRigidbody);
        Bmd.transform.eulerAngles = Rotation;
        Bmd.transform.localScale = Scale;

        if (!ExternalArchive.Equals(""))
        {
            string arcPath = "";
#if UNITY_EDITOR
            arcPath = OBJ_PATH + "/" + ExternalArchive + ".arc";
#else
        arcPath = Application.dataPath + "/" + OBJ_PATH + "/" + ExternalArchive + ".arc";
#endif
        
            Bmd.PlayAnimationFromDifferentArchive(arcPath, AnimationName);
        }
        else
        {
            if(!AnimationName.Equals(""))
                Bmd.LoadAnimation(AnimationName);
        }

        if (CalculateBoneWeights)
        {
            Bmd.transform.AddComponent<WeightDataGenerator>();

            foreach (MeshFilter filter in transform.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.SetActive(false);
            }
        }
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.LogError("ELAPSED: " + elapsedMs);
    }
    
    public void MoveToTarget(Transform target, float speed, float acceleration, float deceleration)
    {
        Bmd.MoveToTarget(target, speed, acceleration, deceleration);
    }

    public void InterruptMove()
    {
        Bmd.InterruptMove();
    }

    public void Teleport(Transform target)
    {
        Bmd.transform.position = target.position;
    }
    
    void Update()
    {
        
    }
}