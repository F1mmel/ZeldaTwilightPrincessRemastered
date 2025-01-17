using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageEffects : MonoBehaviour
{
    public void ScaleSmooth(float scale)
    {
        StartCoroutine(ScaleSmoothCoroutine(transform, scale));
    }
    private static IEnumerator ScaleSmoothCoroutine(Transform transform, float target, float duration = 1.0f)
    {
        Vector3 initialScale = transform.localScale;
        float elapsedTime = 0.0f;
        Vector3 targetScale = new Vector3(target, target, target);

        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale; // Sicherstellen, dass das Ziel erreicht wird
    }

    public void AlphaSmooth(float alpha)
    {
        Image image = transform.GetComponent<Image>();
        StartCoroutine(ScaleAlphaCoroutine(image, transform, alpha / 255));
    }
    private static IEnumerator ScaleAlphaCoroutine(Image image, Transform transform, float target, float duration = 1.0f)
    {
        Color initialColor = image.color;
        float elapsedTime = 0.0f;

        Color targetColor = initialColor;
        targetColor.a = target;

        while (elapsedTime < duration)
        {
            image.color = Color.Lerp(initialColor, targetColor, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stelle sicher, dass das Ziel erreicht wird
        image.color = targetColor;
    }
    
    public void ShineSmooth(float speed = 5)
    {
        Material material = transform.GetComponent<Image>().material;
        StartCoroutine(_ShineCoroutine(material, speed / 1000));
    }
    private IEnumerator _ShineCoroutine(Material material, float speed)
    {
        float location = 0f;

        while (location < 1f)
        {
            location += speed;
            material.SetFloat("_ShineLocation", location);

            yield return null;
        }
        
        material.SetFloat("_ShineLocation", 0f);
    }
}