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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        /*Gizmos.DrawWireSphere(transform.position, 0.05f);
        GUIStyle style = new GUIStyle();
        style.normal.textColor = UnityEngine.Color.green;
        Handles.Label(transform.position, gameObject.name, style);*/
    }
#endif
}
