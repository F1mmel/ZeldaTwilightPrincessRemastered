using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MaterialData : MonoBehaviour
{
    public string MaterialName;
    [Space] public short[] TexturesIndexes;
    [HideInInspector] public Material3 Material3;
    [HideInInspector] public TEX1 TEX1Tag;
    [HideInInspector] public MAT3 MAT3Tag;

    [Space] public List<TextureData> TextureDatas = new List<TextureData>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (TextureData textureData in TextureDatas)
        {
            if(textureData.Texture != null) textureData.Texture.wrapMode = textureData.WrapMode;
        }

        if (MAT3Tag == null || TEX1Tag == null) return;

        Material3.TextureIndexes = TexturesIndexes;

        BinaryTextureImage texture = null;
        BinaryTextureImage vertexColorTexture = null;
        
        if (ZeldaManager.Instance.UseHDTextures.IsActive)
        {
            int baseTexture = Material3.TextureIndexes[0];
            if (baseTexture >= 0) texture = TEX1Tag.BinaryTextureImages[baseTexture];
            //if (baseTexture <= -1) texture = TEX1Tag.BTIs[baseTexture];
            else return;
            int vertexTexture = Material3.TextureIndexes[1];
            if (vertexTexture >= 0)
            {
                int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                if (remap == 1 && Material3.TextureIndexes[2] != -1) vertexTexture = Material3.TextureIndexes[2];  // Get second texture, first propable fake cloud
                vertexColorTexture = TEX1Tag.BinaryTextureImages[vertexTexture];
            }
        }
        else
        {
            int baseTexture = Material3.TextureIndexes[0];
            if (baseTexture >= 0) texture = TEX1Tag.BinaryTextureImages[MAT3Tag.TextureRemapTable[baseTexture]];
            //if (baseTexture <= -1) texture = TEX1Tag.BTIs[baseTexture];
            else return;
            int vertexTexture = Material3.TextureIndexes[1];
            if (vertexTexture >= 0)
            {
                int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                if (remap == 1 && Material3.TextureIndexes[2] != -1) vertexTexture = Material3.TextureIndexes[2];  // Get second texture, first propable fake cloud
                vertexColorTexture = TEX1Tag.BinaryTextureImages[MAT3Tag.TextureRemapTable[vertexTexture]];
            }
        }

        MeshRenderer renderer = transform.GetComponent<MeshRenderer>();
        renderer.material.mainTexture = texture.SkiaToTexture();
        renderer.material.SetTexture("_BlendTexture", vertexColorTexture?.SkiaToTexture());
    }
#endif
}

[Serializable]
public class TextureData
{
    public string Name;
    public bool External;
    public Texture2D Texture;
    public TextureWrapMode WrapMode;

    public TextureData(string name, bool external, Texture2D texture)
    {
        this.Name = name;
        this.External = external;
        this.Texture = texture;

        WrapMode = texture.wrapMode;
    }
}