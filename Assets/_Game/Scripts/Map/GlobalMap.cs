using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WiiExplorer;

public class GlobalMap : MonoBehaviour
{
    public Transform Background;
    public List<GameObject> MapObjects = new List<GameObject>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadMap();
        CreateMap();
    }

    public List<BTI> Textures = new List<BTI>();
    private void LoadMap()
    {
        Archive archive = ArcReader.Read("Assets/GameFiles/res/FieldMap/Field0.arc");
        foreach (ArcFile file in archive.Files)
        {
            if (file.Name.EndsWith(".bti"))
            {
                BTI bti = file.ToBTI();
                Textures.Add(bti);
            }
        }
    }

    private void CreateMap()
    {
        Image bg = Background.GetComponent<Image>();
        Archive archive = ArcReader.Read("Assets/GameFiles/res/FieldMap/res-f.arc");
        bg.sprite = ArcReader.GetFile(archive, "region8.bti").ToBTI().Texture.ToSprite();

        RectTransform rectTransform = Background.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(bg.sprite.rect.width, bg.sprite.rect.height);
        
        for (int i = 0; i < MapObjects.Count; i++)
        {
            Image image = MapObjects[i].GetComponent<Image>();
            image.sprite = Textures[i].Texture.ToSprite();

            rectTransform = MapObjects[i].GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(Textures[i].Texture.width, Textures[i].Texture.height);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
