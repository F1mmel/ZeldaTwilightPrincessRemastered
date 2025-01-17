using UnityEngine;
using UnityEngine.UI;

public class HeartManager : MonoBehaviour
{
    public int Count = 10;
    public Material HeartMaterial;
    public float Gap = 5f;

    private const int MaxHeartsPerRow = 10;
    private const int MinHearts = 3;
    private const int MaxHearts = 20;

    public Color Color = new Color(255, 136, 128);
    
    public static Sprite heartSprite = null;

    void Start()
    {
    }

    public void InitializeHearts()
    {
        if (heartSprite == null)
        {
            heartSprite = TextureFetcher.LoadSprite("saveres", "tt_heart_00");
        }
        
        Count = Mathf.Clamp(Count, MinHearts, MaxHearts);

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < Count; i++)
        {
            GameObject heart = new GameObject("Heart_" + i);
            heart.transform.SetParent(transform);

            Image image = heart.AddComponent<Image>();
            image.sprite = heartSprite;
            image.material = HeartMaterial;
            image.color = Color;

            RectTransform rectTransform = heart.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50);
            rectTransform.localScale = Vector3.one;
            rectTransform.localEulerAngles = Vector3.zero;
            rectTransform.localPosition = Vector3.zero;

            int row = i / MaxHeartsPerRow;
            int column = i % MaxHeartsPerRow;
            rectTransform.anchoredPosition = new Vector2(column * (50 + Gap), -row * (50 + Gap));
        }
    }
}