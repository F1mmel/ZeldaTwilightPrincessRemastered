using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using WiiExplorer;
using static StageLoaderSettings;

public class ResourceLoader
{
    public static void LoadResource(Archive archive, ArcFile file, Archive stage, List<BTI> externalBTIs, GameObject stageObject, GameObject roomObj, GameObject actorObj)
    {
        switch (file.Name.Split(".")[1])
        {
            case "bmd":
                if ((StageLoader.Instance.StageLoaderSettings & StageLoaderSettings.LOAD_STAGE) == 0) break;
                
                if (!ZeldaManager.Instance.UseJ3DModel)
                {
                    BMD.CreateStage(archive, roomObj, file, file.Name.Replace(".bmd", ""), externalBTIs);
                }
                else
                {
                    GameObject o = new GameObject(file.Name);
                    o.transform.parent = roomObj.transform;
                    o.transform.localPosition = Vector3.zero;
                    o.transform.localRotation = Quaternion.identity;
                
                    J3DModel model = o.AddComponent<J3DModel>();
                    model.ExternalTextures = externalBTIs;
                    model.Parse(file);
                }

                break;
            case "dzr":
                if ((StageLoader.Instance.StageLoaderSettings & StageLoaderSettings.LOAD_ACTORS) == 0) break;
                
                DZSLoader.DZS dzs = DZSLoader.ParseDZSHeaders(file.Buffer);
                dzs.Decode(stage, actorObj); 
                
                break;
            /*case "plc":
                PLC plc = roomObj.AddComponent<PLC>();
                plc.Buffer = file.Buffer;
                plc.LoadFromStream();
                
                break;*/
            case "kcl":
                if ((StageLoader.Instance.StageLoaderSettings & StageLoaderSettings.LOAD_COLLISION) == 0) break;
                
                KCL kcl = roomObj.AddComponent<KCL>();
                //kcl.Buffer = file.Buffer;
                
                /*PLC plc = roomObj.AddComponent<PLC>();
                plc.Buffer = file.Buffer;
                plc.LoadFromStream();*/
                
                kcl.LoadFromStream(roomObj, archive, file, file.Buffer);
                
                break;
        }
    }
}