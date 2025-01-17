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

    // Start is called before the first frame update
    void Start()
    {
        /*string path = @"C:\Users\finne\Desktop\_Test\cache\dr_pole_stayd.bck\4.dat";
        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            BCKCachedData d = (BCKCachedData)formatter.UnsafeDeserialize(fileStream, null);
            Debug.LogError(d.Shape);
        }*/

        //BinaryFormatter formatter = new BinaryFormatter();
        //BCKCachedData d = (BCKCachedData)formatter.UnsafeDeserialize(fileStream, null);
        //BCKCachedData d = BCK.DeserializeAndDecompressFile(@"C:\Users\finne\Desktop\_Test\cache\dr_pole_stayd.bck\4.dat.comp");
        //Debug.LogError(d.Shape);

        /*BMD bmd = transform.AddComponent<BMD>();
        //bmd.Parse(File.ReadAllBytes(@"E:\Unity\Unity Projekte\Extracted\res\Object\Dungeons\1\Candle.bmd"));
        //bmd.Parse("boxa", File.ReadAllBytes(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\Test\boxa.bmd"));
        bmd.Parse("boxa", File.ReadAllBytes(@"E:\Unity\Unity Projekte\Extracted\res\Stage\_1_OrdonRanch\Ranch\bmd\Ranch.bmd"));*/

        //Archive archive = ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Stage\R_SP108\R00_00.arc");
        //BMD.CreateNewModel(archive, "model.bmd");
        //BMD.CreateNewModel(archive, "model1.bmd");

        // Bone demo?
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_bq.arc");
        BMD.CreateNewModel(archive, "bq.bmd");*/

        //BMD.CreateNewModel(archive, "model3.bmd");

        //Archive archive = ArcReader.Read(@"E:\Unity\Unity Projekte\Extracted\res\Stage\7_CityInTheSky\2_MainRoom.arc");
        //BMD.CreateNewModel(archive, "model.bmd", ExternalTextures.GetExternalTexturesFromStage(ArcReader.Read(@"E:\Unity\Unity Projekte\Extracted\res\Stage\7_CityInTheSky\STG_00.arc")));

        //Archive archive =
            //ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\E_oc.arc");
        //BMD.CreateModelFromPath(archive, "oc", null, enemy);
        //Archive archive =
            //ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\Sera.arc");
        //BMD.CreateModelFromPath(archive, "sera", null, enemy);
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_dr.arc");
        BMD dragon = BMD.CreateModelFromPath(archive, "dr", null);
        dragon.PreloadAnimation();*/
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_dr.arc");
        BMDOpenGL dragon = BMDOpenGL.CreateModelFromPath(archive, "dr", null);
        dragon.PreloadAnimation();*/
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\E_dd.arc");
        BMDOpenGL dragon = BMDOpenGL.CreateModelFromPath(archive, "dd", null);
        dragon.LoadAnimation("dd_walk");*/
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\E_dd.arc");
        BMD dragon = BMD.CreateModelFromPath(archive, "dd", null);
        dragon.LoadAnimation("dd_die_bomb");*/
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_dr.arc");
        BMD dragon = BMD.CreateModelFromPath(archive, "dr", null);*/
        //dragon.LoadAnimation("dr_pole_stayd");
        
        /*Archive archive =
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\E_rd.arc");
        BMD dragon = BMD.CreateModelFromPath(archive, "rd", null);
        dragon.AddComponent<WeightDataGenerator>();*/

        /*BMD.CreateModelFromPath(
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_oh.arc"), "oh", null).PreloadAnimation();*/

        /*BMD.CreateModelFromPath(
            ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\E_dd.arc"), "dd", null).PreloadAnimationFromOtherArchive(ArcReader.Read(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\res\Object\B_dr.arc"));*/

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
            // Add script
            Bmd.transform.AddComponent<WeightDataGenerator>();

            foreach (MeshFilter filter in transform.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.SetActive(false);
            }
        }
// the code that you want to measure comes here
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
    
    // Update is called once per frame
    void Update()
    {
        
    }
}