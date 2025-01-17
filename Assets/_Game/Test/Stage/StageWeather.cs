using System.Collections;
using System.Collections.Generic;
using Mewlist.MassiveClouds;
using UnityEngine;
using UnityEngine.Rendering;
using WiiExplorer;
using Material = UnityEngine.Material;

public class StageWeather
{
    public static void Initialize(Stage stage, GameObject stageObject, Archive stageArchive, MassiveCloudsPhysicsCloud cloudPhysics)
    {
        // Von aktuellen Raum? Muss bei Raumwechsel geändert werden
        RenderSettings.ambientMode = AmbientMode.Flat;
        //RenderSettings.ambientLight = StageLoader.Instance.StageData.Palets[0].Class.actorAmbCol;
        RenderSettings.ambientLight = StageLoader.Instance.StageData.Palets[0].Class.lightCol[3];
        RenderSettings.ambientLight = new Color(RenderSettings.ambientLight.r / 1.1f, RenderSettings.ambientLight.g / 1.1f,
            RenderSettings.ambientLight.b / 1.1f, RenderSettings.ambientLight.a / 1.1f);
        
        //cloudPhysics.AtmospherePass.atmosphere.AtmosphereColor =
            //StageLoader.Instance.StageData.Palets[0].Class.lightCol[3];
        
        // Loop through all childs in objects
        foreach (GameObject o in stageObject.GetAllChildren())
        {
            MaterialData materialData = o.GetComponent<MaterialData>();
            if(materialData == null) continue;

            if (materialData.MaterialName.Contains("MA"))
            {
                Material material = new Material(StageLoader.Instance.Fog);
                string sub = materialData.MaterialName.Substring(3, 4);

                //if (sub.Equals("MA14"))
                {
                    /*material.mainTexture = materialData.TextureDatas[0].Texture;
                    material.SetFloat ("_Smoothness", 0f);
                    material.mainTextureOffset = new Vector2(0, 1);
                    material.SetVector("_Offset", new Vector4(0, 1, 0, 0));
                    o.GetComponent<MeshRenderer>().materials = new[] { material };*/
                    
                    // Read fog info
                    //Fog info = materialData.Material3.FogInfo;
                    //Debug.Log(info.StartZ);
                }
            }
        }

        //BMD.CreateNewModel(stageArchive, "vrbox_sora.bmd", null);
    }
}
