using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class IntroStartup : MonoBehaviour
{
    [Header("References")] public Sound SoundStart;
    public Animator FadeAnimator;
    public Animator FileInAnimator;
    public GameObject FileSelectionObj;
    public Sound BackgroundMusic;
    public Sound FileSelectionMusic;

    [Space] public GameObject StartUp;

    [Space] public IntroState[] States;

    [Header("Default")] public CanvasGroup UiPressAnyButton; 
    public CanvasGroup UiText;

    private bool checkForAnyButton;

    private int currentStateIndex = 0;
    
    void Start()
    {
        
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = States[currentStateIndex].Music;
        

        DOVirtual.DelayedCall(2, () =>
        {
            ShowPressButton();
        });
    }

    private void ShowPressButton()
    {
        checkForAnyButton = true;
        
        UiPressAnyButton.DOFade(1, 1.5f).OnComplete(() =>
        {
            UiText.DOFade(0, 1f).SetLoops(-1, LoopType.Yoyo);
        });
    }

    void Update()
    {
        if (checkForAnyButton)
        {
            if (Input.anyKeyDown)
            {
                checkForAnyButton = false;  
                
                Sequence sequence = DOTween.Sequence();

                sequence.AppendCallback(() => {
                    TransitionManager.FadeOut();
                });

                sequence.AppendInterval(1f);

                sequence.AppendCallback(() => {
                    UiPressAnyButton.DOFade(0, 0);
                });

                sequence.AppendInterval(1f);

                sequence.AppendCallback(() => {
                    TransitionManager.FadeIn();
                });

                sequence.Play();
                
                
                
                 
                

            }
        }
    }

    private void OpenFileSelection()
    {
        return;
        Sequence sequence = DOTween.Sequence();

        sequence.AppendCallback(() => {
            FadeAnimator.Play("FadeOut");
            TransitionManager.FadeOut();
            BackgroundMusic.PlayFadeOut();
        });

        sequence.AppendInterval(1f);

        sequence.AppendCallback(() => {
            Destroy(StartUp.GetComponent<AudioSource>());
            Destroy(GetComponent<AudioSource>());

            gameObject.SetActive(false);
            FileSelectionObj.SetActive(true);

            FadeAnimator.Play("FadeIn");
            FileSelectionMusic.PlayFadeIn(true);
        });

        sequence.Play();
    }
}

[Serializable]
public class IntroState
{
    public string Name;
    public GameObject Object;
    public AudioClip Music;
}