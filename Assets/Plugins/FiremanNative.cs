using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class FiremanNative : MonoBehaviour
{
    void Start()
    {
        Thread workerThread = new Thread(new ThreadStart(YourMethod));
        workerThread.Start();
    }

    void YourMethod()
    {
        Debug.LogError(Native.load_samples(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\Audiores"));
        
        Thread workerThread = new Thread(() =>
        {
            Debug.LogError(Native.fireman_add("Z2SE_AL_WARP_IN_TATE"));
        });
        workerThread.Start();
        
        Debug.LogError("LOADED");
        Thread.Sleep(4000);
        Debug.LogError("LOADED2");
        
        Thread workerThread1 = new Thread(() =>
        {
            Debug.LogError(Native.fireman_add("Z2SE_BLUE_LUPY_GET"));
        });
        workerThread1.Start();
    }

    private void OnDestroy()
    {
        Debug.LogError("UNLOADING...");
        Debug.LogError("UNLOADING finished");
    }


    void Update()
    {
        
    }
}

static class Native
{
    [DllImport("tpaudio_tools")]
    public static extern int fireman_add(string name);
    
    [DllImport("tpaudio_tools")]
    public static extern string load_samples(string path);
    
    [DllImport("tpaudio_tools")]
    public static extern string unload_samples();
}