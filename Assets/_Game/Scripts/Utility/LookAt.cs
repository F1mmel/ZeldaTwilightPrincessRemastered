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
            // Calculate the look direction from the NPC's head to the target
            Vector3 lookDirection = target.position - transform.position;

            // Rotate the NPC's head to look at the target
            Joint.OverrideRotation = Quaternion.LookRotation(lookDirection).eulerAngles;
        }
    }
}