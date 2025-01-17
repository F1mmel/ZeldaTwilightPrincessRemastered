using UnityEngine;

public class Sword : MonoBehaviour
{
    public int Id;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // FinalIK Rigging, check if player is idle -> doThis
            // If sprinting -> doOther
            //Link.PlayAnimation(Link.TakeSword);
        }
    }
}
