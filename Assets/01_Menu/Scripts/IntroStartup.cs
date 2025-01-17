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
    
    // Start is called before the first frame update
    void Start()
    {
        /*GameFile settings = GameFile.Load("settings.dat");

        // Setze oder aktualisiere die Werte
        settings.SetValue("Player.Name", "John");
        settings.SetValue("Player.Score", 100);
        settings.SetValue("Player.Location.X", 10);
        settings.SetValue("Player.Location.Y", 5);
        settings.SetValue("Player.Location.Z", 2);

        settings.Save();*/
        
        // Play background music
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = States[currentStateIndex].Music;
        //source.Play();
        
        // Sound -> FadeIn

        DOVirtual.DelayedCall(2, () =>
        {
            ShowPressButton();
        });
    }

    private void ShowPressButton()
    {
        checkForAnyButton = true;
        
        // Show UI
        UiPressAnyButton.DOFade(1, 1.5f).OnComplete(() =>
        {
            UiText.DOFade(0, 1f).SetLoops(-1, LoopType.Yoyo);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (checkForAnyButton)
        {
            if (Input.anyKeyDown)
            {
                checkForAnyButton = false;  
                
                Sequence sequence = DOTween.Sequence();

                // Fade out
                sequence.AppendCallback(() => {
                    TransitionManager.FadeOut();
                });

                // Wait for transition ends
                sequence.AppendInterval(1f);

                // Show file selection
                sequence.AppendCallback(() => {
                    // Hide ui
                    UiPressAnyButton.DOFade(0, 0);
                });

                // Wait?
                sequence.AppendInterval(1f);

                // Fade in
                sequence.AppendCallback(() => {
                    TransitionManager.FadeIn();
                });

                sequence.Play();
                
                
                
                 
                //SoundStart.Play();
                
                // Show next state

                //OpenFileSelection();
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

        // Wait for 1 second
        sequence.AppendInterval(1f);

        // Remove audio sources and switch to file selection
        sequence.AppendCallback(() => {
            // Remove audio sources
            Destroy(StartUp.GetComponent<AudioSource>());
            Destroy(GetComponent<AudioSource>());

            // Switch to file selection
            gameObject.SetActive(false);
            FileSelectionObj.SetActive(true);

            // Fade In animations and file selection music
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