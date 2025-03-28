using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZeldaTP : MonoBehaviour
{
    public int FPS = 60;

    [Header("Animation")] public float AnimationStiffness = 0.01f;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FPS;
        
        List<DisplayInfo> displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        if (displays?.Count > 1) // don't bother running if only one display exists...
        {
            var moveOperation = Screen.MoveMainWindowTo(displays[0], new Vector2Int(displays[0].width / 2, displays[0].height / 2));
        }
    }

    public static ZeldaTP Instance;
}
