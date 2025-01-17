using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WiiUCreator : MonoBehaviour
{    
    public string File;
    public string Model;
    public Material UsedMaterial;
    
    // Start is called before the first frame update
    void Start()
    {
        /*GMX gmx = transform.AddComponent<GMX>();
        gmx.File = File;
        gmx.UsedMaterial = UsedMaterial;

        gmx.Create();*/

        List<Texture2D> textures = PackManager.GetTextures(File, Model);

        GMX gmx = PackManager.GetModel(File, Model);
        gmx.Create(transform, textures, UsedMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
