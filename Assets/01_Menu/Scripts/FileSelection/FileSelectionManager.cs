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
        PlayerInput.text = DefaultPlayerName;
        HorseInput.text = DefaultHorseName;
        
        _source = gameObject.AddComponent<AudioSource>();
        
        foreach (SelectableFile file in Files)
        {
            file.FileUi.DOLocalMoveX(-2000, 0f);
        }
        
        int i = 0;
        foreach (SelectableFile file in Files)
        {
            file.FileUi.DOLocalMoveX(0, .75f).SetEase(Ease.OutBack, .5f);
        
            foreach (Transform heart in file.HeartContainer)
            {
                heart.DOKill();
                heart.DOScale(.75f, .75f).SetLoops(-1, LoopType.Yoyo);
            }
            

            int saveIndex = i;
            file.FileUi.OnClick(() =>
            {
                SelectionData data = GetSelectedData(saveIndex);
                SelectedFile = data.Selected;
                
                foreach (SelectableFile file in data.NotSelected)
                {
                    file.FileUi.DOScale(0, .25f);
                    file.FileUi.GetComponent<CanvasGroup>().blocksRaycasts = false;
                }
                
                SelectedFile.FileUi.DOLocalMoveY(300, .5f);
                SelectedFile.FileUi.GetComponent<CanvasGroup>().blocksRaycasts = false;
                
                if (SavegameLoader.Instance.SaveSlots[saveIndex].SaveDetails.PlayerName.Equals(""))
                {
                    ScreenPlayer.DOScale(1, .5f);
                            
                    FadeText(Title, EnterName);

                    currentState = CREATE_PLAYER;
                }
                else
                {
                    EquipmentScreen.DOScale(1, .5f);
                
                    currentState = LOAD;
                }
                            
                ButtonLeft.transform.DOLocalMoveY(-500, 0.5f);
                ButtonRight.transform.DOLocalMoveY(-500, 0.5f);
                            
                ButtonMiddle.transform.DOLocalMoveY(0, 0.5f);
            });

            i++;
        }
        
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
                Sequence sequence = DOTween.Sequence();
            
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
        
        ButtonMiddle.onClick.AddListener(() =>
        {
            if (currentState == CREATE_HORSE)
            {
                TransitionManager.Fade(() =>
                {
                    ScreenHorse.gameObject.SetActive(false);
                    ScreenPlayer.gameObject.SetActive(true);
                    ScreenPlayer.DOScale(1, .5f);
                
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
            
            for (int j = 0; j < Files.Length; j++)
            {
                if (Files[j] != SelectedFile)
                {
                    Files[j].FileUi.DOScale(1, .5f);
                }
                else
                {
                    Files[j].FileUi.DOLocalMoveY(300-(j * 250), .5f);
                }
                
                Files[j].FileUi.GetComponent<CanvasGroup>().blocksRaycasts = true;
            }

            SelectedFile.FileUi.GetComponent<HoverIndicator>().OnPointerExit(null);
            SelectedFile = null;
                            
            EquipmentScreen.DOScale(0, .5f);

            ButtonLeft.transform.DOLocalMoveY(0, 0.5f);
            ButtonRight.transform.DOLocalMoveY(0, 0.5f);

            ButtonMiddle.transform.DOLocalMoveY(-500, 0.5f);
        });
        
        ButtonPlayerContinue.onClick.AddListener(() =>
        {
            if (PlayerInput.text.Equals(""))
            {
                PlayerInput.transform.DOShakeScale(.5f, .2f);
                
                return;
            }
            
            TransitionManager.Fade(() =>
            {
                ScreenPlayer.gameObject.SetActive(false);
                ScreenHorse.gameObject.SetActive(true);
                
                Title.text = EnterHorse;
            });

            ButtonPlayerContinue.OnPointerExit(null);
            currentState = CREATE_HORSE;
        });
        
        ButtonHorseContinue.onClick.AddListener(() =>
        {
            if (HorseInput.text.Equals(""))
            {
                HorseInput.transform.DOShakeScale(.5f, .2f);
                
                return;
            }
            
            

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

        });
    }

    private void FadeText(TMP_Text text, string newValue)
    {
        text.DOFade(0, .5f).OnComplete(() =>
        {
            text.text = newValue;
            text.DOFade(1, .5f);
        });
    }

    private void FadeText(Button button, string newValue)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
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