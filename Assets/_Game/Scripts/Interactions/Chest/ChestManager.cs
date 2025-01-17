using System;
using UnityEngine;

public class ChestManager : MonoBehaviour
{
    public AudioClip OpenSound;
    public AudioClip FanfareSound;
    public AudioClip EpicFanfareSound;

    public static ChestManager Instance;

    private void Awake()
    {
        Instance = this;
    }
}
