using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Joint : MonoBehaviour
{
    public Vector3 OverridePosition;
    public Vector3 OverrideRotation;
    public Vector3 OverrideScale;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
    }
#endif
}
