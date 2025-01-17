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

    void FixedUpdate()
    {
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