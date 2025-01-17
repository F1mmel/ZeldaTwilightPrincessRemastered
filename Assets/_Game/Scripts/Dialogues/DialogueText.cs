using System;
using Febucci.UI;
using UnityEngine;
using Febucci.UI.Core;
using Febucci.UI.Core.Parsing;
using TMPro;

public class DialogueText : MonoBehaviour
{
    public string Text;

    public bool StartAutomatically = false;

    private void Start()
    {
        TextMeshProUGUI t = gameObject.GetComponent<TextMeshProUGUI>();
        if(StartAutomatically) t.text = Text;
        
        TextAnimator_TMP animator = gameObject.AddComponent<TextAnimator_TMP>();
        animator.typewriterStartsAutomatically = StartAutomatically;
        
        TypewriterByCharacter typewriter = gameObject.AddComponent<TypewriterByCharacter>();
        typewriter.useTypeWriter = true;
        
        typewriter.onCharacterVisible.AddListener(c =>
        {
            DialogueManager.PlayCharacterSound();
        });
    }

    public void StartTypewriter()
    {
        gameObject.GetComponent<TextMeshProUGUI>().text = Text;
        gameObject.GetComponent<TypewriterByCharacter>().ShowText(Text);
    }
}
