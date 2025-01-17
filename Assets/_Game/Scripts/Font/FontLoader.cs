using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FontLoader : MonoBehaviour
{
    public BFNData DefaultFont;
    public BFNData TitleFont;

    public static FontLoader Instance;
    
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;

        DefaultFont = new BFNLoader().LoadBFN("fontres", 100f, -30f);
        TitleFont = new BFNLoader().LoadBFN("rubyres", 130f, -20f);
    }
}
