using UnityEngine;
using UnityEngine.UI;

public class LineTiler : MonoBehaviour
{
    public Sprite lineSprite;
    public Color lineColor;
    public int repeatCount = 10;
    
    // Call this method to initialize the tiling
    public void Initialize(Sprite sprite, Color color, int count)
    {
        lineSprite = sprite;
        lineColor = color;
        repeatCount = count;
        CreateTiles();
    }

    private void CreateTiles()
    {
        // Clear existing children
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Get the size of the sprite
        Vector2 spriteSize = lineSprite.rect.size;

        for (int i = 0; i < repeatCount; i++)
        {
            GameObject tile = new GameObject("LineTile_" + i);
            tile.transform.SetParent(transform);

            Image image = tile.AddComponent<Image>();
            image.sprite = lineSprite;
            image.color = lineColor;

            RectTransform rectTransform = tile.GetComponent<RectTransform>();
            rectTransform.sizeDelta = spriteSize;
            rectTransform.anchoredPosition = new Vector2(i * spriteSize.x, 0);
        }
    }
}