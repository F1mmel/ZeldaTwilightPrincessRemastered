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
        
        //Debug.Log(GetComponent<Actor>().GetFRoomNo());
        //Debug.Log(GetComponent<Actor>().GetBRoomNo());
        uint param = (uint)GetComponent<Actor>().Parameter;
        TargetRoom = GetThirdByte(param);
        //uint f = (param >> 13) & ((1u << 6) - 1);
        //uint b =  (param >> 19) & ((1u << 6) - 1);
        
        //Debug.Log(f + " :: " + b);
        if (Link.DoorOpenLeft == null) Link.DoorOpenLeft = Link.LoadAnimation("dooropa", LoopType.Once);
        if (Link.DoorOpenRight == null) Link.DoorOpenRight = Link.LoadAnimation("dooropb", LoopType.Once);     
        
        // Push
        Link.DoorOpenLeft.AddEvent(40, () =>
        {
            TransitionManager.Fade(() =>
            {
                // Load new stage / room
            });
        });
        
        // Pull
        Link.DoorOpenRight.AddEvent(30, () =>
        {
            TransitionManager.Fade(() =>
            {
                // Load new stage / room
            });
        });
    }

    static byte GetThirdByte(uint value)
    {
        // Verschiebe die Bits um 8 nach rechts und maskiere das Ergebnis mit 0xFF
        return (byte)((value >> 8) & 0xFF);
    }

    public void Interact()
    {
        InteractableManager.Instance.UnregisterInteractable(this);

        Transform closestPoint = Link.GetClosestGoToPoint(gameObject.FindChildrenTransform("GoToPointA"),
            gameObject.FindChildrenTransform("GoToPointB"));
        
        // Disable controls
        Link.SetControls(Link.Controls.Frozen);
        
        // Calculate look rotation
        Vector3 directionToPlayer = (Link.Instance.transform.position - closestPoint.position).normalized;
        float dot = Vector3.Dot(closestPoint.forward, directionToPlayer);
        // if (dot > 0)
        if(closestPoint.name == "GoToPointB") Link.Look(-closestPoint.forward);
        else if(closestPoint.name == "GoToPointA") Link.Look(closestPoint.forward);
        
        Link.Teleport(closestPoint, false);
        
        // Change to camera
        CinemachineVirtualCamera virtualCamera = closestPoint.Find("OpenCamera").GetComponent<CinemachineVirtualCamera>();
        virtualCamera.enabled = true;
        
        GetComponent<BMD>()._animancer.Play(stateOpen);

        //ZeldaAnimation usedAnim = null;
        if (closestPoint.name == "GoToPointB")
        {
            //usedAnim = Link.DoorOpenLeft;
        }
        else if (closestPoint.name == "GoToPointA")
        {
            //usedAnim = Link.DoorOpenLeft;
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