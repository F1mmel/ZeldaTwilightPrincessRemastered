using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShineEffect : MonoBehaviour
{
    public Material ShineMaterial;

    [Range(0.001f, 0.01f)] public float Speed = 0.005f;
    public float Delay = 5f;
    
    // Start is called before the first frame update
    void Start()
    {
        ShineMaterial.SetFloat("_ShineLocation", 0f);
        
        StartCoroutine(ShineLoop());
    }

    IEnumerator ShineLoop()
    {
        yield return new WaitForSecondsRealtime(Delay);

        while (true)
        {
            //ShineMaterial.SetFloat("_ShineDensity", 0.263f);
            //ShineMaterial.SetFloat("_ShineWidth", 0.069f);

            float location = 0f;

            while (location < 1f)
            {
                location += Speed;
                ShineMaterial.SetFloat("_ShineLocation", location);

                yield return null;
            }
            
            ShineMaterial.SetFloat("_ShineLocation", 0f);
            yield return new WaitForSecondsRealtime(Delay);
        }
    }

    public void Shine()
    {
        StartCoroutine(_Shine());
    }

    private IEnumerator _Shine()
    {
        float location = 0f;

        while (location < 1f)
        {
            location += Speed;
            ShineMaterial.SetFloat("_ShineLocation", location);

            yield return null;
        }
            
        ShineMaterial.SetFloat("_ShineLocation", 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
