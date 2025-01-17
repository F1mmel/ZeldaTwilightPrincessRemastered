using UnityEngine;
using DG.Tweening;

public static class AudioSourceExtensions
{
    public static void FadeIn(this AudioSource audioSource, float duration = 1)
    {
        audioSource.volume = 0;
        audioSource.Play();
        
        audioSource.DOFade(1.0f, duration);
    }

    public static void FadeOut(this AudioSource audioSource, float duration = 1)
    {
        audioSource.DOFade(0.0f, duration).OnComplete(() => audioSource.Stop());
    }
}