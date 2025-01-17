using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.DOFade(1, 1).OnComplete(() =>
        {
            
        });
    }

    void Update()
    {
        
    }
}
