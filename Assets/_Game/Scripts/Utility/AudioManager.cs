using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource Source;

    private static AudioManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void Play(AudioClip clip)
    {
        Instance.StartCoroutine(Instance._ChangeClip(clip));
    }

    public static void FadeOut()
    {
        Instance.StartCoroutine(Instance._FadeOut());
    }

    public static void FadeIn(AudioClip clip)
    {
        Instance.StartCoroutine(Instance._FadeIn());
    }

    private IEnumerator _FadeOut()
    {
        while (Source.volume > 0f)
        {
            Source.volume -= 0.01f;

            yield return null;
        }

        Source.volume = 0f;
    }

    private IEnumerator _FadeIn()
    {
        while (Source.volume < 1f)
        {
            Source.volume += 0.01f;

            yield return null;
        }

        Source.volume = 1f;
    }

    private IEnumerator _ChangeClip(AudioClip clip)
    {
        // Fade out
        while (Source.volume > 0f)
        {
            Source.volume -= 0.001f;

            yield return null;
        }
        Source.volume = 0f;

        Source.clip = clip;
        //Source.PlayOneShot(clip);
        Source.Play();
        
        // Fade in
        while (Source.volume < 1f)
        {
            Source.volume += 0.001f;

            yield return null;
        }
        Source.volume = 1f;
    }
}
