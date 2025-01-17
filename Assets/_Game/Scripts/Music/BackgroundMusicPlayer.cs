using System;
using UnityEngine;

public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip audioClip;

    [Header("FFT Settings")]
    public int fftWindowSize = 1024; // Größe des FFT-Fensters
    public float threshold = 0.9f; // Ähnlichkeitsschwelle (zwischen 0 und 1)

    private AudioSource audioSource;
    public float loopStart;
    public float loopEnd;

    void Awake()
    {
        // AudioSource automatisch erstellen und konfigurieren
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.clip = audioClip;
    }

    void Start()
    {
        if (audioClip == null)
        {
            Debug.LogWarning("Kein AudioClip zugewiesen.");
            return;
        }

        // Loop Points mit FFT berechnen
        GenerateLoopPointsWithFFT();

        // Audio abspielen und Loop prüfen
        audioSource.Play();
        InvokeRepeating(nameof(CheckLoopPoint), 0f, 0.1f);
    }

    void GenerateLoopPointsWithFFT()
    {
        Debug.Log("Analysiere Audioclip mit FFT für dynamische Loop-Points...");
        int totalSamples = audioClip.samples;
        int channels = audioClip.channels;
        int frequency = audioClip.frequency;

        // Lade die Sample-Daten
        float[] samples = new float[totalSamples * channels];
        audioClip.GetData(samples, 0);

        // FFT-Daten berechnen
        int numWindows = totalSamples / fftWindowSize;
        float[][] fftData = new float[numWindows][];

        for (int i = 0; i < numWindows; i++)
        {
            float[] windowSamples = new float[fftWindowSize];
            System.Array.Copy(samples, i * fftWindowSize, windowSamples, 0, fftWindowSize);

            fftData[i] = CalculateFFT(windowSamples);
        }

        // Loop Points finden
        for (int startWindow = 0; startWindow < numWindows - 1; startWindow++)
        {
            for (int endWindow = startWindow + 1; endWindow < numWindows; endWindow++)
            {
                float similarity = CompareFFT(fftData[startWindow], fftData[endWindow]);

                if (similarity >= threshold)
                {
                    loopStart = (float)startWindow * fftWindowSize / frequency;
                    loopEnd = (float)endWindow * fftWindowSize / frequency;
                    Debug.Log($"Loop Points gefunden: Start = {loopStart}s, Ende = {loopEnd}s");
                    return;
                }
            }
        }

        // Fallback: Standard-Loop-Punkte
        loopStart = 0f;
        loopEnd = audioClip.length;
        Debug.LogWarning("Keine ähnlichen Abschnitte gefunden. Standard-Loop verwendet.");
    }

    float[] CalculateFFT(float[] samples)
    {
        // FFT berechnen (Unity verwendet keine integrierte FFT-Bibliothek)
        var fft = new System.Numerics.Complex[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            fft[i] = new System.Numerics.Complex(samples[i], 0);
        }

        // Cooley-Tukey-FFT-Algorithmus
        FFT(fft);

        // Magnituden berechnen
        float[] magnitudes = new float[samples.Length / 2];
        for (int i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = (float)System.Numerics.Complex.Abs(fft[i]);
        }

        return magnitudes;
    }

    void FFT(System.Numerics.Complex[] buffer)
    {
        int n = buffer.Length;
        int bits = (int)Mathf.Log(n, 2);

        // Bit-Reversal-Reihenfolge
        for (int i = 0; i < n; i++)
        {
            int j = BitReverse(i, bits);
            if (j > i)
            {
                var temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }
        }

        // FFT-Transformation
        for (int length = 2; length <= n; length *= 2)
        {
            double angle = -2 * Mathf.PI / length;
            var wlen = new System.Numerics.Complex(Math.Cos(angle), Math.Sin(angle));

            for (int i = 0; i < n; i += length)
            {
                var w = System.Numerics.Complex.One;
                for (int j = 0; j < length / 2; j++)
                {
                    var u = buffer[i + j];
                    var v = buffer[i + j + length / 2] * w;

                    buffer[i + j] = u + v;
                    buffer[i + j + length / 2] = u - v;

                    w *= wlen;
                }
            }
        }
    }

    int BitReverse(int x, int bits)
    {
        int result = 0;
        for (int i = 0; i < bits; i++)
        {
            result = (result << 1) | (x & 1);
            x >>= 1;
        }
        return result;
    }

    float CompareFFT(float[] fft1, float[] fft2)
    {
        // Berechnet die Ähnlichkeit zwischen zwei FFT-Daten
        float similarity = 0f;
        for (int i = 0; i < fft1.Length; i++)
        {
            similarity += 1f - Mathf.Abs(fft1[i] - fft2[i]) / Mathf.Max(fft1[i], fft2[i]);
        }

        return similarity / fft1.Length;
    }

    void CheckLoopPoint()
    {
        if (audioSource.isPlaying && audioSource.time >= loopEnd)
        {
            audioSource.time = loopStart;
        }
    }
}
