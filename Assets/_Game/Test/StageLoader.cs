using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DG.Tweening;
using GameFormatReader.Common;
using Mewlist.MassiveClouds;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WiiExplorer;

public class StageLoader : MonoBehaviour
{
    public const string STAGE_PATH = "Assets/GameFiles/res/Stage";
    
    [MultiSelectEnum]
    public StageLoaderSettings StageLoaderSettings;
    
    public Stage Stage;
    public string StageName;
    public int RoomId;
    
    public bool LoadStageOnStart = true;
    public bool UseExternalTextures;
    
    [Space]
    public MassiveCloudsPhysicsCloud CloudsPhysics;
    
    [Header("Shared Materials")] public Material Sunbeam;
    public Material Fog;
    public Material HotWater;
    public Material IceWater;
    public Material LanternOil;
    public Material Water;
    public Material ToxicPurple;
    public Material Lava;
    [Space]
    public Material GrayscaleWater;
    public Material Grayscale;
    [Space]
    public Material DefaultMaterial;
    [Space] public Material TransparentSurface;
    public Material KageInterior;

    [Space] public Texture2D FallbackTexture;
    public Texture2D TextureBehindDoor;

    [Header("VFX")] public GameObject Fire;
    public GameObject FireDistortion;
    public GameObject FireSmoke;

    [Header("References")] public GameObject StageObjects;

    [Header("Fallback")] public VolumeProfile FallbackVolume;

    [Header("Environment")] public GameObject Grass;
    public GameObject Flower7;
    public GameObject Flower17;
    public GameObject FlowerLong;
    public GameObject FlowerLongSmall;

    [Header("Material Overrides")] public Material OverrideRupy;

    [Space(5)] public DisabledShapes DisabledShapes;
    
    [Space(15)]

    public static StageLoader Instance;
    public StageData StageData;

    private GameObject stageObject;

    private static Dictionary<GameObject, BMD> _storedModels = new Dictionary<GameObject, BMD>();

    public static List<JPAC> JPACs = new List<JPAC>();
    public List<Texture2D> Texture2Ds = new List<Texture2D>();

    public List<Actor> SCLC = new List<Actor>();
    
    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        ArcReader.CachedArchives.Clear();
        
        // Load particles
        //LoadParticles();
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.image.DOFade(1f, 0.0f);
            TransitionManager.Instance.image.DOFade(0f, 0.5f);
        }

        if(LoadStageOnStart) EnterStage(Stage, StageEnterType.UNLOAD);
    }

    public GameObject EnterStage(Stage stageEnum, StageEnterType enterType)
    {
        // Update discord rpc
        
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Stage = stageEnum;
        if (stageObject != null)
        {
            if(enterType == StageEnterType.UNLOAD) stageObject.SetActive(false);
            else if(enterType == StageEnterType.DESTROY) Destroy(stageObject);
        }
        
        StageName = stageEnum.GetStageFile();
        StageData = new StageData(StageName);
        stageObject = new GameObject(stageEnum.ToString());

        string stageFolder = STAGE_PATH + "/" + stageEnum.GetStageFile();

        Archive stage = ArcReader.Read(stageFolder + "/" + "STG_00.arc");
        List<BTI> externalBTIs = new List<BTI>();
        if(UseExternalTextures) externalBTIs = ExternalTextures.GetExternalTexturesFromStage(stage);
        
        // Load stage dzs
        DZSLoader.DZS dzs = DZSLoader.ParseDZSHeaders(ArcReader.GetBuffer(stage, "stage.dzs"));
        dzs.Decode(stage, stageObject);

        int[] rooms = stageEnum.GetRooms();
        if(rooms.Length == 0) LoadRoom(stageFolder + "/R00_00.arc", stageObject, stage, externalBTIs);
        else
        {
            RoomId = rooms[0];
            string[] roomFiles = Directory.GetFiles(stageFolder)
                .Where(arc =>
                    Path.GetFileName(arc).StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetExtension(arc).Equals(".arc", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string arc in roomFiles)
            {
                int roomId = Int32.Parse(Path.GetFileName(arc)
                    .Split(".arc")[0]
                    .Replace("R", "")
                    .Split("_")[0]);

                if (rooms.Contains(roomId))
                {                        
                    LoadRoom(arc, stageObject, stage, externalBTIs);
                }
            } 
        }

        HandleStageObjects();
        LoadStageResources();

        //StageWeather.Initialize(stageEnum, stageObject, stage, CloudsPhysics);
        
        //if (!ParticleLoader.ParticlesLoaded) ParticleLoader.LoadParticles();
        
        /*_storedModels.Clear(); // Dann wird immer gespeichert?
        foreach (BMD bmd in stageObject.GetComponentsInChildren<BMD>())
        {
            _storedModels.Add(bmd.gameObject, bmd);
        }*/
        
        //TransitionManager.FadeIn();
        
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.LogError("ELAPSED: " + elapsedMs);
        
        // Load background music
        /*string bgm = stageEnum.GetBackgroundMusic();
        if (bgm != "")
        {
            AudioSource source = stageObject.AddComponent<AudioSource>();
            
            AudioClip selectedClip = Resources.Load ("_______DX_MUSIC/" + bgm + "" + " - Zelda Twilight Princess DX") as AudioClip;
            source.clip = selectedClip;
            
            //source.Play();
            source.FadeIn(3f);
        }*/

        return stageObject;
    }

    private void LoadParticles()
    {
        string[] particleArchives = {
            "res/Particle/common.jpc",
            "res/Particle/Pscene001.jpc",
            "res/Particle/Pscene010.jpc",
            "res/Particle/Pscene011.jpc",
            "res/Particle/Pscene012.jpc",
            "res/Particle/Pscene013.jpc",
            "res/Particle/Pscene014.jpc",
            "res/Particle/Pscene015.jpc",
            "res/Particle/Pscene020.jpc",
            "res/Particle/Pscene021.jpc",
            "res/Particle/Pscene022.jpc",
            "res/Particle/Pscene032.jpc",
            "res/Particle/Pscene034.jpc",
            "res/Particle/Pscene037.jpc",
            "res/Particle/Pscene040.jpc",
            "res/Particle/Pscene041.jpc",
            "res/Particle/Pscene050.jpc",
            "res/Particle/Pscene052.jpc",
            "res/Particle/Pscene100.jpc",
            "res/Particle/Pscene101.jpc",
            "res/Particle/Pscene102.jpc",
            "res/Particle/Pscene110.jpc",
            "res/Particle/Pscene111.jpc",
            "res/Particle/Pscene112.jpc",
            "res/Particle/Pscene120.jpc",
            "res/Particle/Pscene121.jpc",
            "res/Particle/Pscene122.jpc",
            "res/Particle/Pscene130.jpc",
            "res/Particle/Pscene131.jpc",
            "res/Particle/Pscene140.jpc",
            "res/Particle/Pscene141.jpc",
            "res/Particle/Pscene150.jpc",
            "res/Particle/Pscene151.jpc",
            "res/Particle/Pscene160.jpc",
            "res/Particle/Pscene161.jpc",
            "res/Particle/Pscene170.jpc",
            "res/Particle/Pscene171.jpc",
            "res/Particle/Pscene180.jpc",
            "res/Particle/Pscene181.jpc",
            "res/Particle/Pscene200.jpc",
            "res/Particle/Pscene201.jpc",
            "res/Particle/Pscene202.jpc",
            "res/Particle/Pscene203.jpc",
            "res/Particle/Pscene204.jpc",
            "res/Particle/Pscene205.jpc",
        };
        
        string path = "Assets/GameFiles/";

        foreach (string archive in particleArchives)
        {
            string fullPath = path + archive;
            byte[] buffer = File.ReadAllBytes(fullPath);

            using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
            {
                JPACs.Add(JPA.Parse(reader));
            }
        }

        for (int i = 0; i < JPACs.Count; i++)
        {
            var jpacData = new JPACData(JPACs[i]);

            foreach (BTI bti in JPACs[i].Textures)
            {
                Texture2Ds.Add(bti.Texture);
            }
            
        }
    }

    public GameObject CreateSimpleStage(Stage stageEnum, bool destroyPreviousStage = true)
    {
        StageName = stageEnum.GetStageFile();
        StageData = new StageData(StageName);
        stageObject = new GameObject(stageEnum.ToString());

        string stageFolder = STAGE_PATH + "/" + stageEnum.GetStageFile();

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
        
        //LoadStageResources();
        
        _storedModels.Clear(); // Dann wird immer gespeichert?
        foreach (BMD bmd in stageObject.GetComponentsInChildren<BMD>())
        {
            _storedModels.Add(bmd.gameObject, bmd);
        }

        return stageObject;
    }

    public static Dictionary<GameObject, BMD> GetModelByName(string name)
    {
        Dictionary<GameObject, BMD> bmds = new Dictionary<GameObject, BMD>();
        foreach(var model in _storedModels)
        {
            if (model.Key.name.Equals(name)) bmds.Add(model.Key, model.Value);
        }

        return bmds;
    }

    private void LoadStageResources()
    {
        string stagePath = "StageData/" + Stage.GetStageFile() + "_" + Stage + "/";
        
        // Volume
        VolumeProfile volume = Resources.Load<VolumeProfile>(stagePath + "StageVolume");
        if (volume == null) Camera.main.GetComponent<Volume>().profile = FallbackVolume;
        else Camera.main.GetComponent<Volume>().profile = volume;
        
// Sky
        var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);
        var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
 
        List<ScriptableRendererFeature> features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
        
        foreach (var feature in features)
        {
            if (feature.GetType() == typeof(MassiveCloudsUniversalRPScriptableRendererFeature))
            {
                MassiveCloudsUniversalRPScriptableRendererFeature f = (MassiveCloudsUniversalRPScriptableRendererFeature)feature;
                        
                MassiveCloudsPhysicsCloud clouds = Resources.Load<MassiveCloudsPhysicsCloud>(stagePath + "StageSky");
                //if(clouds == null) Debug.LogError("Sky for " + StageName + " not found!");

                f.MassiveCloudsRenderer = clouds;
                WheaterSystem.Instance.CloudsPhysicsCloud = clouds;
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
            if (Stage == Stage.LinksHouse && file.Name.Contains("model3")) continue;
            if (Stage == Stage.ArbitersGrounds && file.Name.Contains("model1")) continue;
            if (Stage == Stage.OrdonVillage && file.Name.Contains("model1")) continue;
            
            ResourceLoader.LoadResource(room, file, stage, externalBTIs, stageObject, roomObj, actorObj);
                        
            //return;
        }

        return roomObj;
    }
    
    private List<BinaryTextureImage> GetExternalTexturesFromStage(Archive archive)
    {
        List<BinaryTextureImage> BTIs = new List<BinaryTextureImage>();

        foreach(ArcFile file in archive.Files)
        {
            if (file.ParentDir.Equals("texc"))
            {
                BinaryTextureImage compressedTex = new BinaryTextureImage();
                
                EndianBinaryReader reader = new EndianBinaryReader(file.Buffer, Endian.Big);
                compressedTex.Load(reader, 0);
                reader.Close();
                BTIs.Add(compressedTex);
            }
        }

        return BTIs;
    }

    private void HandleStageObjects()
    {
        if (StageObjects != null)
        {
            bool found = false;
            for(int i = 0; i < StageObjects.transform.childCount; i++)
            {
                GameObject child = StageObjects.transform.GetChild(i).gameObject;
                if (child.name.Equals(Stage.GetStageFile() + "_" + Stage))
                {
                    child.SetActive(true);
                    found = true;
                }
                else child.SetActive(false);
            }
        }
    }

    //public bool CreateStageObjects;
    #if UNITY_EDITOR
    private void OnValidate()
    {
        //if(!gameObject) return;
        
        // Update selection
        if (StageObjects != null)
        {
            bool found = false;
            for(int i = 0; i < StageObjects.transform.childCount; i++)
            {
                GameObject child = StageObjects.transform.GetChild(i).gameObject;
                if (child.name.Equals(Stage.GetStageFile() + "_" + Stage))
                {
                    child.SetActive(true); 
                    found = true;
                }
                else child.SetActive(false);
            }

            if (!found)
            {
                // Create stage object
                /*int[] rooms = Stage.GetRooms();
                
                if (rooms.Length == 0)
                {
                    GameObject stageObj = new GameObject(Stage.GetStageFile() + "_" + Stage);

                    stageObj.transform.parent = StageObjects.transform;
                    stageObj.transform.localPosition = Vector3.zero;
                }
                else
                {
                    GameObject stageObj = new GameObject(Stage.GetStageFile() + "_" + Stage);
                    stageObj.transform.parent = StageObjects.transform;
                    stageObj.transform.localPosition = Vector3.zero;

                    foreach (int room in rooms)
                    {
                        GameObject roomObj = new GameObject("Room_" + room);
                        roomObj.transform.parent = stageObj.transform;
                        roomObj.transform.localPosition = Vector3.zero;
                    }
                }*/

                /*if (CreateStageObjects)
                {
                    foreach(Stage stage in Enum.GetValues(typeof(Stage)))
                    {
                        GameObject stageObj = null;
                        if (stage.ToString().Contains("___")) stageObj = new GameObject(stage.ToString());
                        else stageObj = new GameObject(stage.GetStageFile() + "_" + stage);
                        
                        stageObj.transform.parent = StageObjects.transform;
                        stageObj.transform.localPosition = Vector3.zero;
                    }
                }*/
                
                /*GameObject stageObj = new GameObject(Stage.GetStageFile() + "_" + Stage);
                stageObj.transform.parent = StageObjects.transform;
                stageObj.transform.localPosition = Vector3.zero;

                foreach (int room in Stage.GetRooms())
                {
                    GameObject roomObj = new GameObject("Room_" + room);
                    roomObj.transform.parent = stageObj.transform;
                    roomObj.transform.localPosition = Vector3.zero;
                }*/
            }
        }
    }
#endif
}

[Serializable]
public class StageData
{
    public string Name;

    public List<StageValue<stage_pure_lightvec_info_class>> Lights =
        new List<StageValue<stage_pure_lightvec_info_class>>();

    public List<StageValue<stage_envr_info_class>> Envr =
        new List<StageValue<stage_envr_info_class>>();

    public List<StageValue<stage_pselect_info_class>> Selects =
        new List<StageValue<stage_pselect_info_class>>();

    public List<StageValue<stage_palet_info_class>> Palets =
        new List<StageValue<stage_palet_info_class>>();

    public List<StageValue<stage_vrbox_info_class>> Vrbox =
        new List<StageValue<stage_vrbox_info_class>>();

    public StageData(string name)
    {
        Name = name;
    }
}

[Serializable]
public class StageValue<T>
{
    public int Layer;
    public T Class;
}

[Serializable]
public class DisabledShapes
{
    public string[] ShapeNames;
}

public enum StageEnterType
{
    UNLOAD,
    DESTROY
}

[Flags]
public enum StageLoaderSettings
{
    NONE = 0,         // Kein Wert
    LOAD_STAGE = 1 << 0, // 1
    LOAD_ACTORS = 1 << 1, // 2
    LOAD_COLLISION = 1 << 2 // 4
}
public class MultiSelectEnumAttribute : PropertyAttribute
{
    // Dieser Attribute-Typ dient nur als Marker für den Editor.
}
