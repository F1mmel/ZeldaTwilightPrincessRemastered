using UnityEngine;

public class FlowerRotation : MonoBehaviour
{
    private Transform flowerTransform;
    private RaycastHit hit;

    void Start()
    {
        flowerTransform = GetComponent<Transform>();
    }

    void Update()
    {
        // Cast a raycast downward from the flower
        if (Physics.Raycast(flowerTransform.position, Vector3.down, out hit, 1.0f))
        {
            // If the raycast hits the ground, rotate the flower so that it is parallel to the ground
            flowerTransform.up = hit.normal;
        }
    }
}