using UnityEngine;
using UnityEngine.UI;

public class HeartManager : MonoBehaviour
{
    public int Count = 10; // The number of hearts to display, default is 10
    public Material HeartMaterial; // The material for the hearts
    public float Gap = 5f; // The gap between each heart

    private const int MaxHeartsPerRow = 10;
    private const int MinHearts = 3;
    private const int MaxHearts = 20;

    public Color Color = new Color(255, 136, 128);
    
    public static Sprite heartSprite = null;

    void Start()
    {
        //InitializeHearts();
    }

    public void InitializeHearts()
    {
        if (heartSprite == null)
        {
            heartSprite = TextureFetcher.LoadSprite("saveres", "tt_heart_00");
        }
        
        // Ensure the count is within the allowed range
        Count = Mathf.Clamp(Count, MinHearts, MaxHearts);

        // Clear existing hearts
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < Count; i++)
        {
            // Create a new GameObject for each heart
            GameObject heart = new GameObject("Heart_" + i);
            heart.transform.SetParent(transform);

            // Add Image component and set the sprite and material
            Image image = heart.AddComponent<Image>();
            image.sprite = heartSprite;
            image.material = HeartMaterial;
            image.color = Color;

            // Set RectTransform properties
            RectTransform rectTransform = heart.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 50); // Assuming each heart is 50x50 units
            rectTransform.localScale = Vector3.one;
            rectTransform.localEulerAngles = Vector3.zero;
            rectTransform.localPosition = Vector3.zero;

            // Position the heart
            int row = i / MaxHeartsPerRow;
            int column = i % MaxHeartsPerRow;
            rectTransform.anchoredPosition = new Vector2(column * (50 + Gap), -row * (50 + Gap)); // Adjust position based on gap
        }
    }
}