using UnityEngine;

public class CameraUnderwaterManager : MonoBehaviour
{
    public float rayDistance = 10f;
    public GameObject targetObject;
    public LayerMask waterLayer;
    public float waterSurfaceHeight = 0f;

    void Update()
    {
        bool isBelowWater = transform.position.y < waterSurfaceHeight;

        Ray ray = new Ray(transform.position, Vector3.up);
        RaycastHit hit;

        bool hitWater = false;
        if (Physics.Raycast(ray, out hit, rayDistance, waterLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject != null && hitObject.layer == LayerMask.NameToLayer("Water"))
            {
                hitWater = true;
            }
        }

        if (isBelowWater || hitWater)
        {
            targetObject.SetActive(true);
        }
        else
        {
            targetObject.SetActive(false);
        }
    }
}