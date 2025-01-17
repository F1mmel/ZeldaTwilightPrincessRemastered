using System;
using Animancer;
using Cinemachine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using LoopType = JStudio.J3D.Animation.LoopType;

public class Door : MonoBehaviour, IInteractable
{
    public string InteractionMessage { get; set; } = "Open";
    public float InteractionRange { get; set; } = 1.5f;

    private ZeldaAnimation stateOpen;

    public int DoorType;
    public int TargetRoom;

    private void Start()
    {
        InteractableManager.Instance.RegisterInteractable(this);
        
        BMD bmd = GetComponent<BMD>();
        bmd.PrepareAnimation();

        stateOpen = bmd.LoadSpecificAnimation(ArcReader.ReadObject("static"), "fdoor" + (DoorType == 0 ? "a" : "b"), LoopType.Once);
        stateOpen.IgnoreTranslation = true;
        
        uint param = (uint)GetComponent<Actor>().Parameter;
        TargetRoom = GetThirdByte(param);
        
        if (Link.DoorOpenLeft == null) Link.DoorOpenLeft = Link.LoadAnimation("dooropa", LoopType.Once);
        if (Link.DoorOpenRight == null) Link.DoorOpenRight = Link.LoadAnimation("dooropb", LoopType.Once);     
        
        Link.DoorOpenLeft.AddEvent(40, () =>
        {
            TransitionManager.Fade(() =>
            {
            });
        });
        
        Link.DoorOpenRight.AddEvent(30, () =>
        {
            TransitionManager.Fade(() =>
            {
            });
        });
    }

    static byte GetThirdByte(uint value)
    {
        return (byte)((value >> 8) & 0xFF);
    }

    public void Interact()
    {
        InteractableManager.Instance.UnregisterInteractable(this);

        Transform closestPoint = Link.GetClosestGoToPoint(gameObject.FindChildrenTransform("GoToPointA"),
            gameObject.FindChildrenTransform("GoToPointB"));
        
        Link.SetControls(Link.Controls.Frozen);
        
        Vector3 directionToPlayer = (Link.Instance.transform.position - closestPoint.position).normalized;
        float dot = Vector3.Dot(closestPoint.forward, directionToPlayer);
        if(closestPoint.name == "GoToPointB") Link.Look(-closestPoint.forward);
        else if(closestPoint.name == "GoToPointA") Link.Look(closestPoint.forward);
        
        Link.Teleport(closestPoint, false);
        
        CinemachineVirtualCamera virtualCamera = closestPoint.Find("OpenCamera").GetComponent<CinemachineVirtualCamera>();
        virtualCamera.enabled = true;
        
        GetComponent<BMD>()._animancer.Play(stateOpen);

        if (closestPoint.name == "GoToPointB")
        {
        }
        else if (closestPoint.name == "GoToPointA")
        {
        }
        Link.PlayAnimation(DoorType == 0 ? Link.DoorOpenLeft : Link.DoorOpenRight);
        

        
        Link.DoorOpenLeft.AddEvent((int)Link.DoorOpenLeft.duration, () =>
        {
            End();
        });
    }

    public void End()
    {
        Link.SetControls(Link.Controls.Default);
    }
}