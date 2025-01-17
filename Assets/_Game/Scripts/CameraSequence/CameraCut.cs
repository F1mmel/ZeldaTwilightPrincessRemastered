using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineSmoothPath))]
public class CameraCut : MonoBehaviour
{
    public CinemachineDollyCart DollyCart;
    public Transform VirtualCameraTransform;
    public CinemachineVirtualCamera VirtualCamera;

    public CinemachineSmoothPath CinemachineSmoothPath;

    private void Reset()
    {
        if (DollyCart != null) return;
        
        // Assign references
        DollyCart = GetComponentInChildren<CinemachineDollyCart>();
        VirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        VirtualCameraTransform = VirtualCamera.transform.parent;
        CinemachineSmoothPath = GetComponent<CinemachineSmoothPath>();
    }

    private void OnEnable()
    {
        VirtualCameraTransform.gameObject.SetActive(false);
        DollyCart.gameObject.SetActive(false);
        CinemachineSmoothPath = GetComponent<CinemachineSmoothPath>();
    }
}