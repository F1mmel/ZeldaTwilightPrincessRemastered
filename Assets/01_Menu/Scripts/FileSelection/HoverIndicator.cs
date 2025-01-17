using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static Sprite cornerSprite = null;
    public float cornerSize = 50f;
    public float hoverDistance = 10f;
    public float hoverDuration = 0.5f;

    public float subtractWidth = 0;
    public float subtractHeight = 0;

    public Vector2 centerOffset = Vector2.zero; // Offset zur Mitte (Pivot)
    public Vector2 imageOffset = Vector2.zero; // Startposition-Offset

    public bool ShowCorner = true;

    [Header("Hover")] public bool Scale = true;
    public float ScaleValue = .1f;
    [Space]
    public bool Sound = false;
    public AudioClip HoverSound;
    public AudioClip ClickSound;

    private GameObject cornerParent; // New parent for the corners
    private GameObject[] corners = new GameObject[4];

    private float defaultScale;

    private AudioSource _source;
    
    private void Start()
    {
        _source = transform.AddComponent<AudioSource>();
        _source.volume = .5f;
        
        defaultScale = transform.localScale.x;
        
        cornerParent = new GameObject("CornerParent");
        cornerParent.transform.SetParent(transform, false);
        
        InitializeCorner();
    }

    void InitializeCorner()
    {
        if (cornerSprite == null)
        {
            cornerSprite = TextureFetcher.LoadSprite("main2D", "im_select_cursor_4parts_pikapika_try05_00_40x40_gre");
        }

        Color desiredColor = new Color(1f, 0.956f, 0.494f, 1f); // FFF47E in RGB values
        for (int i = 0; i < 4; i++)
        {
            if (corners[i] == null)
            {
                corners[i] = new GameObject("Corner" + i);
                corners[i].transform.SetParent(cornerParent.transform, false);

                var image = corners[i].AddComponent<UnityEngine.UI.Image>();
                image.sprite = cornerSprite;
                image.color = desiredColor;
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = new Vector2(cornerSize, cornerSize);

                corners[i].SetActive(false);
            }
        }

        RectTransform rectTransform = GetComponent<RectTransform>();
        float newWidth = rectTransform.rect.width - subtractWidth;
        float newHeight = rectTransform.rect.height - subtractHeight;

        // Set the positions and rotations for the corners, with the center and image offsets
        corners[0].GetComponent<RectTransform>().anchoredPosition = new Vector2(-newWidth / 2, newHeight / 2) + centerOffset + imageOffset;
        corners[1].GetComponent<RectTransform>().anchoredPosition = new Vector2(newWidth / 2, newHeight / 2) + centerOffset + imageOffset;
        corners[2].GetComponent<RectTransform>().anchoredPosition = new Vector2(-newWidth / 2, -newHeight / 2) + centerOffset + imageOffset;
        corners[3].GetComponent<RectTransform>().anchoredPosition = new Vector2(newWidth / 2, -newHeight / 2) + centerOffset + imageOffset;

        corners[1].transform.Rotate(0, 0, -90);
        corners[2].transform.Rotate(0, 0, 90);
        corners[3].transform.Rotate(0, 0, 180);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        foreach (var corner in corners)
        {
            corner.SetActive(true);
            corner.GetComponent<Image>().DOKill();
            corner.GetComponent<RectTransform>().DOKill();

            InitializeCorner();
            Color desiredColor = new Color(1f, 0.956f, 0.494f, 1f); // FFF47E in RGB values
            corner.GetComponent<Image>().DOColor(desiredColor, 0);

            RectTransform rectTransform = corner.GetComponent<RectTransform>();
            Vector2 originalPosition = rectTransform.anchoredPosition;

            Vector2 offsetPosition = Vector2.zero;

            if (corner.name == "Corner0")
                offsetPosition = new Vector2(-hoverDistance, hoverDistance);
            else if (corner.name == "Corner1")
                offsetPosition = new Vector2(hoverDistance, hoverDistance);
            else if (corner.name == "Corner2")
                offsetPosition = new Vector2(-hoverDistance, -hoverDistance);
            else if (corner.name == "Corner3")
                offsetPosition = new Vector2(hoverDistance, -hoverDistance);

            rectTransform.DOAnchorPos(originalPosition + offsetPosition, hoverDuration).SetLoops(-1, LoopType.Yoyo);
            rectTransform.DOScale(1.2f, hoverDuration).SetLoops(-1, LoopType.Yoyo);

            Image image = corner.GetComponent<Image>();
            if(ShowCorner) image.DOFade(1, .25f);

            Color targetColor = new Color(1f, 0.75f, 0f, 1); // FFF47E in RGB values
            if(ShowCorner) image.DOColor(targetColor, 2f).SetLoops(-1, LoopType.Yoyo);
        }

        if (Scale)
        {
            transform.DOScale(defaultScale + ScaleValue, 0.4f).SetEase(Ease.OutBack);
        }

        if (Sound)
        {
            _source.PlayOneShot(HoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        foreach (var corner in corners)
        {
            RectTransform rectTransform = corner.GetComponent<RectTransform>();

            corner.GetComponent<Image>().DOKill();
            corner.GetComponent<Image>().DOFade(0, .25f).OnComplete(() =>
            {
                corner.SetActive(false);

                rectTransform.DOKill();
                corner.GetComponent<Image>().DOKill();
                
                Color desiredColor = new Color(1f, 0.956f, 0.494f, 1f); // FFF47E in RGB values
                corner.GetComponent<Image>().DOColor(desiredColor, 0);
            });
        }

        if (Scale)
        {
            transform.DOScale(defaultScale, 0.4f).SetEase(Ease.OutBack);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Sound)
        {
            _source.PlayOneShot(ClickSound);
        }
    }
}
