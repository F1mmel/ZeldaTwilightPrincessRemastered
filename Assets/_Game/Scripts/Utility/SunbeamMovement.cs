using UnityEngine;

public class SunbeamMovement : MonoBehaviour
{
    public float movementRange = 0.25f;
    public float movementSpeed = 0.25f;
    public float rotationAngle = 0.25f;
    public float rotationSpeed = 0.25f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        float newX = initialPosition.x + Mathf.Sin(Time.time * movementSpeed) * movementRange;
        float newZ = initialPosition.z + Mathf.Cos(Time.time * movementSpeed) * movementRange;

        transform.position = new Vector3(newX, initialPosition.y, newZ);

        float rotationAmount = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;

        transform.rotation = initialRotation * Quaternion.Euler(0f, rotationAmount, 0f);
    }
}