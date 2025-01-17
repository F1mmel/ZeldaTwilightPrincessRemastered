using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RuntimeFont : MonoBehaviour
{
    public RuntimeFontType FontType;
    [SerializeField] private string Text;

    private TMP_Text _textComponent;
    
    // Start is called before the first frame update
    void Start()
    {
        // Create text component
        _textComponent = gameObject.AddComponent<TextMeshProUGUI>();

        BFNData data = null;
        
        if (FontType == RuntimeFontType.Default) data = FontLoader.Instance.DefaultFont;
        else if (FontType == RuntimeFontType.Title) data = FontLoader.Instance.TitleFont;
            
        _textComponent.font = data.Asset;
        _textComponent.fontSize = data.Scale;
        _textComponent.characterSpacing = data.Spacing;
        
        _textComponent.text = Text;
    }

    public void SetText(string text)
    {
        Text = text;
        _textComponent.text = text;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public enum RuntimeFontType
{
    Default,
    Title
}