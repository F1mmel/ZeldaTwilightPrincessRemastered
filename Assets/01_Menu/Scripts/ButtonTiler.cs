using UnityEngine;
using UnityEngine.UI;

public class ButtonTiler : MonoBehaviour
{
    public static Sprite leftSprite;
    public static Sprite middleSprite;
    public static Sprite rightSprite;
    public int count = 3;
    public bool Automatically = false;
    public int PosXOffset = 0;

    private void Start()
    {
        if (leftSprite == null)
        {
            leftSprite = TextureFetcher.LoadSprite("clctresR", "tt_button_base0_side");
            rightSprite = RotateSprite(leftSprite, 180f);
            middleSprite = TextureFetcher.LoadSprite("clctresR", "tt_button_base0_center_tate");
        }

        currentX = 0;
        if (Automatically)
        {
            CreateTiledButtonAutomatically();
        }
        else
        {
            
        }
    }

    private float currentX = 0;
    private GameObject CreateTile(string name, Sprite sprite)
    {
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
        RectTransform p = GetComponent<RectTransform>();
        p.anchorMin = new Vector2(0, .5f);
        p.anchorMax = new Vector2(0, .5f);
        p.pivot = new Vector2(.5f, .5f);
        p.anchoredPosition = new Vector3(PosXOffset, 0);
        
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        CreateTile("Left", leftSprite);
        
        float remainingWidth = transform.GetComponent<RectTransform>().rect.width - (leftSprite.bounds.size.x + rightSprite.bounds.size.x) * leftSprite.pixelsPerUnit;
        float middleWidth = middleSprite.bounds.size.x * middleSprite.pixelsPerUnit;

        int middleCount = Mathf.FloorToInt(remainingWidth / middleWidth) + 1;
        float lastMiddleWidth = remainingWidth - (middleCount * middleWidth);
        
        for(int i = 0; i < middleCount; i++)
            CreateTile("Middle" + i, middleSprite);
        
        if (lastMiddleWidth > 0)
        {
            CreateTile("MiddleLast", CreateClippedSprite(middleSprite, lastMiddleWidth / middleSprite.pixelsPerUnit));
        }

        CreateTile("Last", rightSprite);


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
