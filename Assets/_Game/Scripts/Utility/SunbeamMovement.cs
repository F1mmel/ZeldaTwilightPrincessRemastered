using UnityEngine;

public class SunbeamMovement : MonoBehaviour
{
    public float movementRange = 0.25f; // Bereich der Bewegung entlang der X- und Z-Achse
    public float movementSpeed = 0.25f; // Geschwindigkeit der Bewegung
    public float rotationAngle = 0.25f; // Rotationswinkel
    public float rotationSpeed = 0.25f; // Geschwindigkeit der Rotation

    private Vector3 initialPosition; // Ausgangsposition der Sunbeams
    private Quaternion initialRotation; // Ausgangsrotation der Sunbeams

    void Start()
    {
        // Speichere die Ausgangsposition und -rotation der Sunbeams
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        // Berechne die Zielposition auf der X- und Z-Achse
        float newX = initialPosition.x + Mathf.Sin(Time.time * movementSpeed) * movementRange;
        float newZ = initialPosition.z + Mathf.Cos(Time.time * movementSpeed) * movementRange;

        // Setze die neue Position der Sunbeams
        transform.position = new Vector3(newX, initialPosition.y, newZ);

        // Berechne den Rotationswinkel basierend auf der Zeit
        float rotationAmount = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;

        // Setze die Rotation der Sunbeams um die Y-Achse
        transform.rotation = initialRotation * Quaternion.Euler(0f, rotationAmount, 0f);
    }
}