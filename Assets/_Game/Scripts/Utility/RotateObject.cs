using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public RotationAxis rotationAxis = RotationAxis.Y;
    public float rotationSpeed = 50f;
    public AnimationCurve rotationSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    private float timeElapsed = 0f;

    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;

        float loopedTime = timeElapsed % rotationSpeedCurve[rotationSpeedCurve.length - 1].time;

        float modifiedRotationSpeed = rotationSpeed * rotationSpeedCurve.Evaluate(loopedTime);

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