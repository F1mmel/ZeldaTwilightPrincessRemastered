using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mewlist.MassiveClouds;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WiiExplorer;

public class IntroLoader : MonoBehaviour
{
    public const string STAGE_PATH = "Assets/GameFiles/res/Stage";
    
    public Stage Stage = Stage.MirrorChamber;
    
    [Space]
    public MassiveCloudsPhysicsCloud CloudsPhysics;

    public static IntroLoader Instance;
    public StageData StageData;

    private GameObject stageObject;

    public StageLoader StageLoader;
    
    private void OnEnable()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        StageLoader.Instance = StageLoader;
        EnterStage(Stage);
    }

    public GameObject EnterStage(Stage stageEnum, bool destroyPreviousStage = true)
    {
        Stage = stageEnum;
        
        StageData = new StageData(stageEnum.GetStageFile());
        stageObject = new GameObject(stageEnum.ToString());

        string stageFolder = "";
        #if UNITY_EDITOR
        stageFolder = STAGE_PATH + "/" + stageEnum.GetStageFile();
        #else
        stageFolder = Application.dataPath + "/" + STAGE_PATH + "/" + stageEnum.GetStageFile();
        #endif

        Archive stage = ArcReader.Read(stageFolder + "/" + "STG_00.arc");
        List<BTI> externalBTIs = ExternalTextures.GetExternalTexturesFromStage(stage);
        
        // Load stage dzs
        DZSLoader.DZS dzs = DZSLoader.ParseDZSHeaders(ArcReader.GetBuffer(stage, "stage.dzs"));
        dzs.Decode(stage, stageObject);

        int[] rooms = stageEnum.GetRooms();
        //LoadRoom("Assets/GameFiles/res/Stage/D_MN04/R05_00.arc", stageObject, stage, externalBTIs);

        //return;
        
        if(rooms.Length == 0) LoadRoom(stageFolder + "/R00_00.arc", stageObject, stage, externalBTIs);
        else
        {
            // Get all rooms in stage folder
            foreach (string arc in Directory.GetFiles(stageFolder))
            {
                if (Path.GetFileName(arc).StartsWith("R", StringComparison.OrdinalIgnoreCase) && 
                    Path.GetExtension(arc).Equals(".arc", StringComparison.OrdinalIgnoreCase))
                {
                    int roomId = Int32.Parse(Path.GetFileName(arc).Split(".arc")[0].Replace("R", "").Split("_")[0]);
                    if (rooms.Contains(roomId))
                    {
                        GameObject room = LoadRoom(arc, stageObject, stage, externalBTIs);
                    }
                }
            }
        }
        
        LoadStageResources();
        
        TransitionManager.FadeIn();

        return stageObject;
    }

    private void LoadStageResources()
    {
        // Sky
        var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);
        var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
 
        List<ScriptableRendererFeature> features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
        
        foreach (var feature in features)
        {
            if (feature.GetType() == typeof(MassiveCloudsUniversalRPScriptableRendererFeature))
            {
                MassiveCloudsUniversalRPScriptableRendererFeature f = (MassiveCloudsUniversalRPScriptableRendererFeature)feature;

                f.MassiveCloudsRenderer = CloudsPhysics;
                break;
            }
        }
    }

    private GameObject LoadRoom(string arc, GameObject stageObject, Archive stage, List<BTI> externalBTIs)
    {
        Archive room = ArcReader.Read(arc);
        GameObject roomObj = new GameObject(room.Name);
        roomObj.transform.parent = stageObject.transform;

        GameObject actorObj = new GameObject("---------- ACTORS ----------");
        actorObj.transform.parent = roomObj.transform;

        foreach (ArcFile file in room.Files)
        {
            if (Stage == Stage.DeathMountainTrail && file.Name.Contains("model5")) continue;
            if (Stage == Stage.SouthFaronWoods && file.Name.Contains("model1")) continue;
            
            ResourceLoader.LoadResource(room, file, stage, externalBTIs, stageObject, roomObj, actorObj);
                        
            //return;
        }

        return roomObj;
    }
}