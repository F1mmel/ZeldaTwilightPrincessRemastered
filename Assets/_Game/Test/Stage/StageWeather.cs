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
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = StageLoader.Instance.StageData.Palets[0].Class.lightCol[3];
        RenderSettings.ambientLight = new Color(RenderSettings.ambientLight.r / 1.1f, RenderSettings.ambientLight.g / 1.1f,
            RenderSettings.ambientLight.b / 1.1f, RenderSettings.ambientLight.a / 1.1f);
        
        
        foreach (GameObject o in stageObject.GetAllChildren())
        {
            MaterialData materialData = o.GetComponent<MaterialData>();
            if(materialData == null) continue;

            if (materialData.MaterialName.Contains("MA"))
            {
                Material material = new Material(StageLoader.Instance.Fog);
                string sub = materialData.MaterialName.Substring(3, 4);

                {
                    
                }
            }
        }

    }
}
