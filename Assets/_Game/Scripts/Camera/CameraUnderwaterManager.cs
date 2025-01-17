using UnityEngine;

public class CameraUnderwaterManager : MonoBehaviour
{
    public float rayDistance = 10f;
    public GameObject targetObject;
    public LayerMask waterLayer; // Layer für Wasser
    public float waterSurfaceHeight = 0f; // Höhe der Wasseroberfläche

    void Update()
    {
        // Prüfen, ob die Kamera unter der Wasseroberfläche ist
        bool isBelowWater = transform.position.y < waterSurfaceHeight;

        // Raycast nach oben
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

        // Aktivieren, wenn die Kamera unterhalb der Wasseroberfläche ist oder der Ray Wasser trifft
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