using UnityEngine;
using DG.Tweening;

public static class AudioSourceExtensions
{
    /// <summary>
    /// Fades in the volume of the AudioSource over a specified duration.
    /// </summary>
    /// <param name="audioSource">The AudioSource to fade in.</param>
    /// <param name="duration">The duration of the fade-in effect.</param>
    public static void FadeIn(this AudioSource audioSource, float duration = 1)
    {
        // Ensure the AudioSource is playing and its volume is initially set to 0
        audioSource.volume = 0;
        audioSource.Play();
        
        // Use DOTween to fade in the volume
        audioSource.DOFade(1.0f, duration);
    }

    /// <summary>
    /// Fades out the volume of the AudioSource over a specified duration.
    /// </summary>
    /// <param name="audioSource">The AudioSource to fade out.</param>
    /// <param name="duration">The duration of the fade-out effect.</param>
    public static void FadeOut(this AudioSource audioSource, float duration = 1)
    {
        // Use DOTween to fade out the volume
        audioSource.DOFade(0.0f, duration).OnComplete(() => audioSource.Stop());
    }
}