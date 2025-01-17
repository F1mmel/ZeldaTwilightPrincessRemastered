using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;
    public Image image;
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        Instance = this;
    }

    // Verblassen des Bildes zu vollständigem Schwarz
    public static void FadeOut()
    {
        Instance.StartCoroutine(Instance.FadeImage(0.0f, 1.0f));
    }

    // Verblassen des Bildes zu vollständiger Transparenz
    public static void FadeIn()
    {
        Instance.StartCoroutine(Instance.FadeImage(1.0f, 0.0f));
    }

    public static void Fade(Action callback, float duration = .5f)
    {
        Instance.image.DOFade(1, duration).OnComplete(() =>
        {
            callback.Invoke();

            Instance.image.DOFade(0, duration);
        });
    }

    // Coroutine zum Verarbeiten der Fade-Transition
    private IEnumerator FadeImage(float startAlpha, float targetAlpha)
    {
        float elapsedTime = 0.0f;
        Color color = image.color;
        color.a = startAlpha;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            color.a = alpha;
            image.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stellen Sie sicher, dass das Ziel-Alpha erreicht wird
        color.a = targetAlpha;
        image.color = color;
    }
}