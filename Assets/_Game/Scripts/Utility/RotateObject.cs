using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    // Enum für die möglichen Drehachsen
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public RotationAxis rotationAxis = RotationAxis.Y; // Die standardmäßig ausgewählte Drehachse
    public float rotationSpeed = 50f; // Die Drehgeschwindigkeit

    // Update is called once per frame
    void FixedUpdate()
    {
        // Drehung basierend auf der ausgewählten Achse
        switch (rotationAxis)
        {
            case RotationAxis.X:
                transform.Rotate(Vector3.right * rotationSpeed);
                break;
            case RotationAxis.Y:
                transform.Rotate(Vector3.up * rotationSpeed);
                break;
            case RotationAxis.Z:
                transform.Rotate(Vector3.forward * rotationSpeed);
                break;
        }
    }
}