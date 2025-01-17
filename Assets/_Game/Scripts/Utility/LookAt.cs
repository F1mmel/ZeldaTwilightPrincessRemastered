using System;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform target;

    private Joint Joint;

    private void Start()
    {
        Joint = GetComponent<Joint>();
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 lookDirection = target.position - transform.position;

            Joint.OverrideRotation = Quaternion.LookRotation(lookDirection).eulerAngles;
        }
    }
}