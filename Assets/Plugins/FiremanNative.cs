using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class FiremanNative : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Thread workerThread = new Thread(new ThreadStart(YourMethod));
        workerThread.Start();
    }

    void YourMethod()
    {
        // SCRIPT RELOAD TAKES FOREVER?, onDestroy clearSamples, playSoundEffect geht unendlich lang auch wenn schon fertig, deswegen wird nicht ausgeführt
        // Load samples
        Debug.LogError(Native.load_samples(@"E:\Unity\Unity Projekte\ZeldaTPBuilder\Assets\GameFiles\Audiores"));
        
        Thread workerThread = new Thread(() =>
        {
            Debug.LogError(Native.fireman_add("Z2SE_AL_WARP_IN_TATE"));
        });
        workerThread.Start();
        
        //Debug.LogError(Native.fireman_add("Z2SE_AL_WARP_IN_TATE"));
        Debug.LogError("LOADED");
        Thread.Sleep(4000);
        Debug.LogError("LOADED2");
        
        Thread workerThread1 = new Thread(() =>
        {
            Debug.LogError(Native.fireman_add("Z2SE_BLUE_LUPY_GET"));
        });
        workerThread1.Start();
        //Debug.LogError(Native.fireman_add("Z2SE_RED_LUPY_GET"));
        /*Debug.LogError(Native.fireman_add("Z2SE_AL_WARP_IN_TATE"));
        
        Thread.Sleep(5000);
        Debug.LogWarning("TEST");
        Debug.LogError(Native.fireman_add("Z2SE_GREEN_LUPY_GET"));
        Debug.LogError(Native.fireman_add("Z2SE_BLUE_LUPY_GET"));
        Debug.LogError(Native.fireman_add("Z2SE_RED_LUPY_GET"));*/
    }

    private void OnDestroy()
    {
        Debug.LogError("UNLOADING...");
        //Native.unload_samples();
        Debug.LogError("UNLOADING finished");
    }


    // Update is called once per frame
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