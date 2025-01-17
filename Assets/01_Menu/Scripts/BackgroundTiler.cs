using UnityEngine;
using UnityEngine.UI;

public class BackgroundTiler : MonoBehaviour
{
    public Sprite backgroundSprite;
    public RectTransform parentCanvas;
    public Color backgroundColor = Color.white; // FFFCE6 als RGB

    void Start()
    {
        if (backgroundSprite == null || parentCanvas == null)
        {
            Debug.LogError("Sprite oder Canvas nicht gesetzt.");
            return;
        }

        Vector2 spriteSize = new Vector2(backgroundSprite.bounds.size.x * backgroundSprite.pixelsPerUnit, 
            backgroundSprite.bounds.size.y * backgroundSprite.pixelsPerUnit);

        Vector2 canvasSize = parentCanvas.rect.size;

        int tilesX = Mathf.CeilToInt(canvasSize.x / spriteSize.x);
        int tilesY = Mathf.CeilToInt(canvasSize.y / spriteSize.y);

        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                GameObject tile = new GameObject("BackgroundTile");
                tile.transform.SetParent(parentCanvas, false);

                Image image = tile.AddComponent<Image>();
                image.sprite = backgroundSprite;
                image.color = backgroundColor;

                RectTransform rectTransform = tile.GetComponent<RectTransform>();
                rectTransform.sizeDelta = spriteSize;
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(x * spriteSize.x, -y * spriteSize.y);
            }
        }
    }
}