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
    public float rotationSpeed = 50f; // Basis Drehgeschwindigkeit
    public AnimationCurve rotationSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f); // Standardmäßig eine lineare Kurve (Modifikator)
    private float timeElapsed = 0f; // Die verstrichene Zeit

    // Update is called once per frame
    void FixedUpdate()
    {
        // Zeit basierend auf der verstrichenen Zeit (wird in jedem Frame aktualisiert)
        timeElapsed += Time.deltaTime;

        // Wiederhole die Zeit, falls sie den Bereich der Kurve überschreitet
        float loopedTime = timeElapsed % rotationSpeedCurve[rotationSpeedCurve.length - 1].time;

        // Berechne den modifizierten rotationSpeed basierend auf der AnimationCurve
        float modifiedRotationSpeed = rotationSpeed * rotationSpeedCurve.Evaluate(loopedTime);

        // Drehung basierend auf der ausgewählten Achse
        switch (rotationAxis)
        {
            case RotationAxis.X:
                transform.Rotate(Vector3.right * modifiedRotationSpeed);
                break;
            case RotationAxis.Y:
                transform.Rotate(Vector3.up * modifiedRotationSpeed);
                break;
            case RotationAxis.Z:
                transform.Rotate(Vector3.forward * modifiedRotationSpeed);
                break;
        }
    }
}