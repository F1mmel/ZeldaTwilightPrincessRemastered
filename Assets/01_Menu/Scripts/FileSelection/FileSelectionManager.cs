using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FileSelectionManager : MonoBehaviour
{
    [Header("Strings")] public string ChooseQuestLog = "Choose a Quest Log";
    public string EnterName = "Enter name";
    public string EnterHorse = "Enter horse name";
    public string DefaultPlayerName = "Link";
    public string DefaultHorseName = "Epona";
    
    public SelectableFile[] Files;
    
    [FormerlySerializedAs("ButtonExit")] [Header("Buttons")]
    public Button ButtonLeft;
    [FormerlySerializedAs("ButtonSettings")] public Button ButtonRight;
    [FormerlySerializedAs("ButtonCancel")] public Button ButtonMiddle;

    [Header("States")] public Transform FileSelection;

    [Space]
    public Transform Settings;
    [Header("Equipment")] public Transform EquipmentScreen;
    public Button ButtonErase;
    public Button ButtonStart;
    public Button ButtonCopy;

    [Header("NewFile")] public TMP_Text Title;
    [Header("NewFile - Player")] public Transform ScreenPlayer;
    public TMP_InputField PlayerInput;
    public Button ButtonPlayerBack;
    public Button ButtonPlayerContinue;
    [Header("NewFile - Horse")] public Transform ScreenHorse;
    public TMP_InputField HorseInput;
    public Button ButtonHorseBack;
    public Button ButtonHorseContinue;

    private AudioSource _source;

    private SelectableFile SelectedFile;
    
    public const int DEFAULT = 0;
    public const int CREATE_PLAYER = 1;
    public const int CREATE_HORSE = 2;
    public const int LOAD = 3;
    public const int SETTINGS = 4;
    [Header("DEBUG")] public int currentState = DEFAULT;
    
    void Start()
    {
        // Set defaults to input
        PlayerInput.text = DefaultPlayerName;
        HorseInput.text = DefaultHorseName;
        
        _source = gameObject.AddComponent<AudioSource>();
        
        // Hide files on startup
        foreach (SelectableFile file in Files)
        {
            file.FileUi.DOLocalMoveX(-2000, 0f);
        }
        
        // Show files
        int i = 0;
        foreach (SelectableFile file in Files)
        {
            // Show files
            file.FileUi.DOLocalMoveX(0, .75f).SetEase(Ease.OutBack, .5f);
        
            // Animate all hearts
            foreach (Transform heart in file.HeartContainer)
            {
                heart.DOKill();
                heart.DOScale(.75f, .75f).SetLoops(-1, LoopType.Yoyo);
            }
            
            // Event for clicking on a file

            int saveIndex = i;
            file.FileUi.OnClick(() =>
            {
                SelectionData data = GetSelectedData(saveIndex);
                SelectedFile = data.Selected;
                
                // Hide not selected files
                foreach (SelectableFile file in data.NotSelected)
                {
                    file.FileUi.DOScale(0, .25f);
                    file.FileUi.GetComponent<CanvasGroup>().blocksRaycasts = false;
                }
                
                // Move selected file at top
                SelectedFile.FileUi.DOLocalMoveY(300, .5f);
                SelectedFile.FileUi.GetComponent<CanvasGroup>().blocksRaycasts = false;
                
                // Create new save
                if (SavegameLoader.Instance.SaveSlots[saveIndex].SaveDetails.PlayerName.Equals(""))
                {
                    // Show player screen
                    ScreenPlayer.DOScale(1, .5f);
                            
                    // Change title
                    FadeText(Title, EnterName);

                    currentState = CREATE_PLAYER;
                }
                else
                {
                    // Load save
                    EquipmentScreen.DOScale(1, .5f);
                
                    currentState = LOAD;
                }
                            
                // Hide buttons
                ButtonLeft.transform.DOLocalMoveY(-500, 0.5f);
                ButtonRight.transform.DOLocalMoveY(-500, 0.5f);
                            
                // Show button
                ButtonMiddle.transform.DOLocalMoveY(0, 0.5f);
            });

            i++;
        }
        
        // Click event for left button
        ButtonLeft.onClick.AddListener(() =>
        {
            if (currentState == SETTINGS)
            {
                Settings.DOLocalMoveX(4000, 1.5f).SetEase(Ease.OutBack, .5f);
                FileSelection.DOLocalMoveX(0, 1.5f).SetEase(Ease.OutBack, .5f);
                
                FadeText(ButtonLeft, "Exit");
                FadeText(ButtonRight, "Settings");

                currentState = DEFAULT;
            } else if (currentState == DEFAULT)
            {
                GetComponent<CanvasGroup>().blocksRaycasts = false;
                //_source.PlayOneShot(ButtonClickSound);
                Sequence sequence = DOTween.Sequence();
            
                // Hide files
                sequence.AppendCallback(() =>
                {
                    foreach (SelectableFile file in Files)
                    {
                        file.FileUi.DOLocalMoveX(-2000, .75f);
                    }
                
                    TransitionManager.FadeOut();
                });

                sequence.AppendInterval(1f);
            
                Application.Quit();

                sequence.Play();
            }
        });
        
        // Click event for right button
        ButtonRight.onClick.AddListener(() =>
        {
            if (currentState == DEFAULT)
            {
                FileSelection.DOLocalMoveX(-4000, 1.5f).SetEase(Ease.OutBack, .5f);
                Settings.DOLocalMoveX(0, 1.5f).SetEase(Ease.OutBack, .5f);
            
                FadeText(ButtonLeft, "Back");
                FadeText(ButtonRight, "Default");

                currentState = SETTINGS; 
            } else if (currentState == SETTINGS)
            {
                Debug.LogError("Restore default settings...");
            }
        });
        
        // Click event for middle button
        ButtonMiddle.onClick.AddListener(() =>
        {
            if (currentState == CREATE_HORSE)
            {
                TransitionManager.Fade(() =>
                {
                    ScreenHorse.gameObject.SetActive(false);
                    ScreenPlayer.gameObject.SetActive(true);
                    ScreenPlayer.DOScale(1, .5f);
                
                    // Change title
                    Title.text = EnterName;
                    
                    ButtonMiddle.OnPointerExit(null);
                    ButtonLeft.OnPointerExit(null);
                    ButtonRight.OnPointerExit(null);

                    currentState = CREATE_PLAYER;
                });

                return;
            } else if (currentState == CREATE_PLAYER)
            {
                ScreenPlayer.DOScale(0, .5f);
                
                // Change title
                Title.DOFade(0, .5f).OnComplete(() =>
                {
                    Title.text = ChooseQuestLog;
                    Title.DOFade(1, .5f);
                    
                    ButtonMiddle.OnPointerExit(null);
                    ButtonLeft.OnPointerExit(null);
                    ButtonRight.OnPointerExit(null);
                });

                currentState = DEFAULT;
            } else 
            {
                currentState = DEFAULT;
            }
            
            PlayerInput.text = DefaultPlayerName;
            HorseInput.text = DefaultHorseName;
            
            // Hide the other saves
            for (int j = 0; j < Files.Length; j++)
            {
                if (Files[j] != SelectedFile)
                {
                    // Hide other files
                    Files[j].FileUi.DOScale(1, .5f);
                }
                else
                {
                    Files[j].FileUi.DOLocalMoveY(300-(j * 250), .5f);
                }
                
                Files[j].FileUi.GetComponent<CanvasGroup>().blocksRaycasts = true;
            }

            // Scale properly
            SelectedFile.FileUi.GetComponent<HoverIndicator>().OnPointerExit(null);
            SelectedFile = null;
                            
            // Hide all screens
            EquipmentScreen.DOScale(0, .5f);
            //ScreenPlayer.DOScale(0, .5f);

            // Hide file selection buttons
            ButtonLeft.transform.DOLocalMoveY(0, 0.5f);
            ButtonRight.transform.DOLocalMoveY(0, 0.5f);

            ButtonMiddle.transform.DOLocalMoveY(-500, 0.5f);
        });
        
        // PlayerScreen
        ButtonPlayerContinue.onClick.AddListener(() =>
        {
            // Check if name is not empty
            if (PlayerInput.text.Equals(""))
            {
                PlayerInput.transform.DOShakeScale(.5f, .2f);
                
                return;
            }
            
            TransitionManager.Fade(() =>
            {
                // Change screen to epona
                ScreenPlayer.gameObject.SetActive(false);
                ScreenHorse.gameObject.SetActive(true);
                
                // Change title
                Title.text = EnterHorse;
            });

            ButtonPlayerContinue.OnPointerExit(null);
            currentState = CREATE_HORSE;
        });
        
        // HorseScreen
        ButtonHorseContinue.onClick.AddListener(() =>
        {
            // Check if name is not empty
            if (HorseInput.text.Equals(""))
            {
                HorseInput.transform.DOShakeScale(.5f, .2f);
                
                return;
            }
            
            // Play audio
            
            // Create new saw

            Sequence sequence = DOTween.Sequence();

            sequence.AppendCallback(() =>
            {
                TransitionManager.FadeOut();
            });
            sequence.AppendInterval(1f);
            sequence.AppendCallback(() =>
            {
                SavegameLoader.Instance.CreateNewSave(SelectedFile.Index - 1, PlayerInput.text, HorseInput.text);
            });

            sequence.Play();

            /*TransitionManager.Fade(() =>
            {
                // Change screen to epona
                ScreenPlayer.gameObject.SetActive(false);
                ScreenHorse.gameObject.SetActive(true);
                
                // Change title
                Title.text = EnterHorse;
            });

            ButtonPlayerContinue.OnPointerExit(null);
            currentState = CREATE_HORSE;*/
        });
    }

    private void FadeText(TMP_Text text, string newValue)
    {
        // Change title
        text.DOFade(0, .5f).OnComplete(() =>
        {
            text.text = newValue;
            text.DOFade(1, .5f);
        });
    }

    private void FadeText(Button button, string newValue)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        // Change title
        text.DOFade(0, .5f).OnComplete(() =>
        {
            text.text = newValue;
            text.DOFade(1, .5f);
        });
    }

    private SelectionData GetSelectedData(int saveIndex)
    {
        SelectionData data = new SelectionData();
        
        for (int j = 0; j < Files.Length; j++)
        {
            if (saveIndex != j)
            {
                data.NotSelected.Add(Files[j]);
            }
            else
            {
                data.Selected = Files[j];
            }
        }

        return data;
    }
}

[Serializable]
public class SelectableFile
{
    public int Index;
    public Transform FileUi;
    public Transform HeartContainer;
}

class SelectionData
{
    public SelectableFile Selected;
    public List<SelectableFile> NotSelected = new List<SelectableFile>();
}