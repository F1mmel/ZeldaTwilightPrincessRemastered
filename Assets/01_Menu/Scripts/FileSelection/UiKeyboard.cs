using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UiKeyboard : MonoBehaviour
{
    public float buttonWidth = 50f;
    public float buttonHeight = 50f;
    public float gap = 10f;
    public float fontSize = 30;

    [Header("References")] public TMP_InputField InputField;

    [Header("Sounds")] [Range(0f, 1f)] public float Volume = .5f;
    public AudioClip HoverSound;
    public AudioClip ClickSound;

    private AudioSource _source;

    private void Start()
    {
        _source = transform.AddComponent<AudioSource>();
        _source.volume = Volume;
        
        CreateKeyboard();
    }

    void CreateKeyboard()
    {
        string keys = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.,";
        RectTransform rectTransform = GetComponent<RectTransform>();
        float currentX = 0;
        float currentY = 0;
        float parentWidth = rectTransform.rect.width;

        foreach (char key in keys)
        {
            GameObject buttonObject = new GameObject(key.ToString());
            buttonObject.transform.SetParent(transform, false);
            buttonObject.transform.localScale = new Vector3(1, 1, 1);

            HoverIndicator hover = buttonObject.AddComponent<HoverIndicator>();
            //hover.ShowCorner = false;
            hover.cornerSize = 25f;
            hover.hoverDistance = 5f;
            hover.subtractWidth = 10;
            hover.subtractHeight = 10;

            Button button = buttonObject.AddComponent<Button>();
            
            button.onClick.AddListener(() =>
            {
                if (InputField.text.Length < InputField.characterLimit)
                {
                    InputField.text += key.ToString();
                    _source.PlayOneShot(ClickSound);
                }
            });
            
            EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((eventData) =>
            {
                //_source.PlayOneShot(HoverSound);
            });
            trigger.triggers.Add(entry);
            
            TextMeshProUGUI text = buttonObject.AddComponent<TextMeshProUGUI>();
            text.text = key.ToString();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = true;
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;

            RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
            buttonRectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            RectTransform textRectTransform = text.GetComponent<RectTransform>();
            textRectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            textRectTransform.anchoredPosition = Vector2.zero;

            if (currentX + buttonWidth > parentWidth)
            {
                currentX = 0;
                currentY -= buttonHeight + gap;
            }

            buttonRectTransform.anchoredPosition = new Vector2(currentX, currentY);
            currentX += buttonWidth + gap;
        }

        rectTransform.sizeDelta = new Vector2(parentWidth, Mathf.Abs(currentY) + buttonHeight);
    }
}
