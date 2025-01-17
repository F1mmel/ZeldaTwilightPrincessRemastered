using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CapPhysics : MonoBehaviour
{
    public static CapPhysics Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void AddBone(bool cine = false)
    {
        GameObject capObj = gameObject.FindChildren("z_cap1");

        DynamicBone dynamicBone = transform.AddComponent<DynamicBone>();
        dynamicBone.m_Root = capObj.transform;
        dynamicBone.m_Damping = 0.5f;
        dynamicBone.m_Elasticity = 0.05f;
        dynamicBone.m_Stiffness = 0.25f;
        dynamicBone.m_Inert = 0.6f;
        dynamicBone.m_Radius = 0.15f;
        if(!cine) dynamicBone.m_Force = new Vector3(0, -3, 0);
        else dynamicBone.m_Force = new Vector3(0, -2.75f, 0);
        dynamicBone.m_Colliders = new List<DynamicBoneColliderBase>();
        
        BMD bmd = gameObject.GetComponent<BMD>();
        GameObject referenceCollider = new GameObject("CapCollider");
        referenceCollider.transform.SetParent(bmd.gameObject.FindChildren("backbone1").transform);
        referenceCollider.transform.localPosition = new Vector3(24f, -37.2f, -0.2f);
        referenceCollider.transform.localEulerAngles = new Vector3(108.3f, 254.3f, 164.5f);
        referenceCollider.transform.localScale = Vector3.one;

        DynamicBoneCollider collider = referenceCollider.AddComponent<DynamicBoneCollider>();
        if (!cine)
        {
            collider.m_Center = new Vector3(0, 0.2f, -6.34f);
            collider.m_Bound = DynamicBoneColliderBase.Bound.Inside;
            collider.m_Radius = 14.63f;
            collider.m_Height = 72.3f;
            collider.m_Radius2 = 16.39f;
        }
        else
        {
            collider.m_Center = new Vector3(0, 133.6f, -8.01f);
            collider.m_Radius = 8.9f;
            collider.m_Height = 38.9f;
            collider.m_Radius2 = 5.48f;
        }
        
        dynamicBone.m_Colliders.Add(collider);
        
        lastCapBone = gameObject.FindChildren("z_cap2").transform;
        initialPosition = lastCapBone.localPosition;
        initialRotation = lastCapBone.localRotation;
    }

    private Transform lastCapBone;

    public float gravityForce = 9.81f;
    public float stiffness = 1.64f;
    public float damping = 0.9f;

    public Vector3 targetRotationEuler;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    private Vector3 boneVelocity;
    
    private Vector3 rotationVelocity;
    
    private void LateUpdate()
    {
        if (lastCapBone != null)
        {

            Vector3 gravity = Vector3.down * gravityForce * Time.deltaTime;

            boneVelocity *= damping;

            boneVelocity += gravity;

            Vector3 newPosition = lastCapBone.localPosition + boneVelocity;

            Vector3 springForce = (initialPosition - newPosition) * stiffness;
            boneVelocity += springForce;

            lastCapBone.localPosition = newPosition;


            Quaternion targetRotation = Quaternion.Euler(targetRotationEuler);

            rotationVelocity *= damping;

            Quaternion currentRotation = lastCapBone.localRotation;
            Quaternion rotationDifference = targetRotation * Quaternion.Inverse(currentRotation);

            Vector3 rotationDifferenceEuler = rotationDifference.eulerAngles;

            Vector3 springForceRotation = (rotationDifferenceEuler) * stiffness;
            rotationVelocity += springForceRotation;

            Vector3 newRotationEuler = currentRotation.eulerAngles + rotationVelocity;

            lastCapBone.localRotation = Quaternion.Euler(newRotationEuler);
        }
    }
}
