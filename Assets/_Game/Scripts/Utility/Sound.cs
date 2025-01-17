using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Sound : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 1f;
    public AudioClip Clip;

    private AudioSource _source;

    public AudioClip GetSound()
    {
        return Clip;
    }

    public void Play()
    {
        // Create new audio source
        if (transform.GetComponent<AudioSource>() == null)
        {
            _source = transform.AddComponent<AudioSource>();
        }
        else
        {
            _source = transform.GetComponent<AudioSource>();
        }

        _source.loop = false;

        // Get random clip
        AudioClip clip = GetSound();

        // Set clip and volume
        _source.clip = clip;
        _source.volume = volume;

        // Play clip
        _source.Play();
    }

    public void PlayFadeIn(bool loop)
    {
        // Create new audio source
        _source = transform.AddComponent<AudioSource>();
        _source.loop = loop;
        _source.volume = 0f;

        // Get random clip
        AudioClip clip = GetSound();

        // Set clip and volume
        _source.clip = clip;
        StartCoroutine(FadeIn(_source));

        // Play clip
        _source.Play();
    }

    private IEnumerator FadeIn(AudioSource _source)
    {
        while (_source.volume < 0.95f)
        {
            _source.volume += 0.01f;

            yield return null;
            yield return null;
        }

        _source.volume = 1f;
    }

    public void PlayFadeOut()
    {
        // Create new audio source
        _source = transform.GetComponent<AudioSource>();

        StartCoroutine(FadeOut(_source));
    }

    public void PlayFadeOutAndDelete()
    {
        // Create new audio source
        _source = transform.GetComponent<AudioSource>();

        StartCoroutine(FadeOutDelete(_source));
    }

    private IEnumerator FadeOut(AudioSource _source)
    {
        while (_source.volume > 0.05f)
        {
            _source.volume -= 0.01f;

            yield return null;
        }

        _source.volume = 0f;
    }

    private IEnumerator FadeOutDelete(AudioSource _source)
    {
        if (_source != null)
        {
            while (_source.volume > 0.05f)
            {
                _source.volume -= 0.01f;

                yield return null;
            }

            Destroy(_source);
        }
    }
}