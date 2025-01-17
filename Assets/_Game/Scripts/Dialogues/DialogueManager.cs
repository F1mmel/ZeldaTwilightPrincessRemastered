using System;
using DG.Tweening;
using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DialogueManager : MonoBehaviour
{
    [Header("References")] public RawImage Image;
    public TextMeshProUGUI Text;
    public CanvasGroup Container;
    public Transform CameraItem;
    
    [Header("Dialogues")] public AudioClip SoundOnCharacter;
    private AudioSource _source;

    public static DialogueManager Instance;
    
    private TextAnimator_TMP _textAnimator;
    private TypewriterByCharacter _typewriter;

    private void Awake()
    {
        Instance = this;
        
        _source = gameObject.AddComponent<AudioSource>();
        _source.volume = 0.2f;
        
        _textAnimator = Text.GetComponent<TextAnimator_TMP>();
        _typewriter = Text.GetComponent<TypewriterByCharacter>();
        _typewriter.onCharacterVisible.AddListener(c =>
        {
            PlayCharacterSound();
        });

        Container.DOFade(0, 0f);
        CameraItem.gameObject.SetActive(false);
    }

    public static void PlayCharacterSound()
    {
        Instance._source.PlayOneShot(Instance.SoundOnCharacter);
    }

    private int currentId = -2;
    private IInteractable currentInteractable;
    public static void ShowDialogue(IInteractable interactable, BMD bmd/*, Sprite texture*/)
    {
        Instance.CameraItem.gameObject.SetActive(true);
        Instance.currentId = bmd.DialogueId;
        Instance.currentInteractable = interactable;
        BmgReader.Message m = null;
        foreach (BmgReader.Message message in Language.Messages)
        {
            if (message.Id == bmd.DialogueId)
            {
                m = message;
                break;
            }
        }
        
        // Is rupee
        if (bmd.Name.Equals("f_gd_rupy.bmd"))
        {
            Instance.Image.transform.localScale = new Vector3(6, 6, 6);
        }
        else
        {
            Instance.Image.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
        }

        if (m != null)
        {
            
            Instance.Text.text = m.FullText;
            Instance._typewriter.ShowText(m.FullText);
        }
        else
        {
            Instance.Text.text = "{ID MISSING}";
            Instance._typewriter.ShowText("{ID MISSING}");
        }
        
        //Instance.Text.text = m.TextParts[1];
        //Instance._typewriter.ShowText(m.TextParts[1]);

        Instance.Container.DOFade(1, 0.5f);
        
        //Instance.Image.sprite = texture;
    }

    private static void CloseDialogue()
    {
        Instance.Text.text = "";
        Instance.Container.DOFade(0, 0.5f);
        Instance.currentId = -2;
        Instance.CameraItem.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && Instance.currentId != -2)
        {
            if (Instance._typewriter.isShowingText)
            {
                Instance._typewriter.SkipTypewriter();
                return;
            }
            else
            {
                CloseDialogue();
                Instance.currentInteractable.End();
            }
        }
    }
}