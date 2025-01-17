using UnityEngine;
using UnityEngine.UI;

public class TextureTiler : MonoBehaviour
{
    private Sprite middleSprite;
    public int count = 3;
    public int PosXOffset = 0;

    public string Arc;
    public string Bti;

    private void Start()
    {
        middleSprite = TextureFetcher.LoadSprite(Arc, Bti);

        currentX = 0;
        CreateTiledButtonAutomatically();
    }

    private float currentX = 0;
    private GameObject CreateTile(string name, Sprite sprite)
    {
        // Create left sprite
        GameObject leftObject = new GameObject(name);
        leftObject.transform.SetParent(transform, false);
        leftObject.transform.localScale = new Vector3(1, 1, 1);
        
        Image leftImage = leftObject.AddComponent<Image>();
        leftImage.raycastTarget = false;
        leftImage.sprite = sprite;
        leftImage.SetNativeSize();
        leftImage.color = new Color32(0x77, 0x77, 0x77, 0xFF);
        
        RectTransform leftRect = leftObject.GetComponent<RectTransform>();
        leftRect.localPosition = new Vector3(currentX, 0, 0);

        currentX += sprite.bounds.size.x * sprite.pixelsPerUnit;

        return leftObject;
    }

    public void CreateTiledButtonAutomatically()
    {
        // Set pivot for parent
        RectTransform p = GetComponent<RectTransform>();
        
        // Clear existing children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        for(int i = 0; i < count; i++)
            CreateTile("Middle" + i, middleSprite);
    }

    private Sprite RotateSprite(Sprite originalSprite, float angle)
    {
        Texture2D originalTexture = originalSprite.texture;
        Texture2D rotatedTexture = new Texture2D(originalTexture.width, originalTexture.height);

        Color[] originalPixels = originalTexture.GetPixels();
        Color[] rotatedPixels = new Color[originalPixels.Length];

        int width = originalTexture.width;
        int height = originalTexture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = width - x - 1;
                int newY = height - y - 1;
                rotatedPixels[newY * width + newX] = originalPixels[y * width + x];
            }
        }

        rotatedTexture.SetPixels(rotatedPixels);
        rotatedTexture.Apply();

        return Sprite.Create(rotatedTexture, originalSprite.rect, new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateClippedSprite(Sprite originalSprite, float width)
    {
        Rect originalRect = originalSprite.rect;
        Rect newRect = new Rect(originalRect.x, originalRect.y, width * originalSprite.pixelsPerUnit, originalRect.height);
        return Sprite.Create(originalSprite.texture, newRect, new Vector2(0.5f, 0.5f));
    }
}
