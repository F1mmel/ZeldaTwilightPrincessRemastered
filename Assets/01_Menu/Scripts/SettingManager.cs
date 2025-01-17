using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.DOFade(1, 1).OnComplete(() =>
        {
            
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
