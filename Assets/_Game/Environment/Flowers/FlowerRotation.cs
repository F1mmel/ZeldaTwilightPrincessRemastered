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
        if (Physics.Raycast(flowerTransform.position, Vector3.down, out hit, 1.0f))
        {
            flowerTransform.up = hit.normal;
        }
    }
}