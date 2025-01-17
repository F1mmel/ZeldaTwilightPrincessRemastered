using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WiiUCreator : MonoBehaviour
{    
    public string File;
    public string Model;
    public Material UsedMaterial;
    
    void Start()
    {

        List<Texture2D> textures = PackManager.GetTextures(File, Model);

        GMX gmx = PackManager.GetModel(File, Model);
        gmx.Create(transform, textures, UsedMaterial);
    }

    void Update()
    {
        
    }
}
