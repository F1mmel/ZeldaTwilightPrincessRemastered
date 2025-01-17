using System;
using Animancer;
using Cinemachine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using LoopType = JStudio.J3D.Animation.LoopType;

public class Chest : MonoBehaviour, IInteractable
{
    public string InteractionMessage { get; set; } = "Open";
    public float InteractionRange { get; set; } = 1.5f;
    
    private Animator animator;
    private AnimancerComponent animancer;

    private ZeldaAnimation stateOpen;
    private ZeldaAnimation linkOpen;

    private BMD item;

    public int ChestType;

    private void Start()
    {
        //stateOpen = null;
        
        InteractableManager.Instance.RegisterInteractable(this);
        
        animator = transform.AddComponent<Animator>();
        animancer = transform.AddComponent<AnimancerComponent>();
        animancer.Animator = animator;
        
        BMD bmd = GetComponent<BMD>();
        bmd.PrepareAnimation();

        if (ChestType == 0) // Small chest
        {
            stateOpen = bmd.LoadSpecificAnimation(bmd.Archive, "k_takaraa", LoopType.Once);
            
            if(Link.OpenChestA == null) Link.OpenChestA = Link.LoadAnimation("boxopkick", LoopType.Once);
            linkOpen = Link.OpenChestA;
        }
        else if (ChestType == 1) // Middle chest
        {
            stateOpen = bmd.LoadSpecificAnimation(bmd.Archive, "k_takarab", LoopType.Once);
            
            if(Link.OpenChestB == null) Link.OpenChestB = Link.LoadAnimation("boxopshort", LoopType.Once);
            linkOpen = Link.OpenChestB;
        }

        Link.GetItemFromChest = Link.LoadAnimation("geta", LoopType.Once);
        Link.GetItemFromChestWait = Link.LoadAnimation("getawait", LoopType.Loop);
        
        Actor actor = GetComponent<Actor>();

        ItemData itemData = ZeldaManager.GetItemData(actor.ChestItem);
        
        item = ZeldaManager.GetModelFromItemTable(itemData, transform);
        
        // Is rupee
        if (item.Name.Equals("f_gd_rupy.bmd"))
        {
            item.OverwriteMaterial(StageLoader.Instance.OverrideRupy);
            item.ChangeAlphaThreshold(0);
        }
        item.InitializeModelParams(itemData);
    }

    private CameraType type;
    public void Interact()
    {
        InteractableManager.Instance.currentInteractable = this;
        InteractableManager.Instance.UnregisterInteractable(this);
        
        // Disable controls
        Link.SetControls(Link.Controls.Frozen);
        
        Link.Teleport(transform.gameObject.FindChildrenTransform("GoToPoint"));
        
        // Change to camera
        Vector3 directionToCamera = (Camera.main.transform.position - transform.position).normalized;
        type = Vector3.Dot(transform.right, directionToCamera) > 0 ? CameraType.RIGHT : CameraType.LEFT;
        
        if(type == CameraType.RIGHT) transform.gameObject.FindChildrenTransform("OpenCameraRight")
            .GetComponent<CinemachineVirtualCamera>().enabled = true;
        else if(type == CameraType.LEFT) transform.gameObject.FindChildrenTransform("OpenCameraLeft")
            .GetComponent<CinemachineVirtualCamera>().enabled = true;
        
        //CinemachineVirtualCamera virtualCamera = transform.gameObject.FindChildrenTransform("OpenCameraRight").GetComponent<CinemachineVirtualCamera>();
        //virtualCamera.enabled = true;
        
        animancer.Play(stateOpen);
        
        //Link.OpenChestA = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "boxopkick", LoopType.Once);
        //Link.OpenChestB = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "boxopshort", LoopType.Once);
        
        Link.TakeItem = Link.LoadAnimation("take");
        Link.Instance.BmdLink.PlaySequencedAnimation(linkOpen, Link.GetItemFromChest, Link.GetItemFromChestWait);

        
        // Chest sound effect
        stateOpen.AddEvent(5, () =>
        {
            Link.PlaySound(ChestManager.Instance.OpenSound);
        });
        
        Link.GetItemFromChest.AddEvent(5, () =>
        {
            switch (type)
            {
                case CameraType.RIGHT:
                    transform.gameObject.FindChildrenTransform("ItemCameraRight")
                        .GetComponent<CinemachineVirtualCamera>().enabled = true;
                    break;
                case CameraType.LEFT:
                    transform.gameObject.FindChildrenTransform("ItemCameraLeft")
                        .GetComponent<CinemachineVirtualCamera>().enabled = true;
                    break;
            }
        });
        
        Link.GetItemFromChest.AddEvent(10, () => Link.RotatePlayer180());
        Link.GetItemFromChest.AddEvent(20, () =>
        {
            Link.FaceChestOpen = Link.LoadFaceAnimation("ff");
            Link.PlayFaceAnimation(Link.FaceChestOpen);
            
            // Check if item is epic
            Link.PlaySound(ChestManager.Instance.FanfareSound);
        });
        Link.GetItemFromChest.AddEvent(25, () =>
        {
            Transform itemHold = Link.Instance.gameObject.FindChildren("ItemHold").transform;
            itemHold.transform.localScale = Vector3.one;

            item.transform.SetParent(itemHold);
            item.transform.localPosition = Vector3.zero;
            item.transform.localEulerAngles = new Vector3(90f, 0, 0);

            // 3D item for dialogue camera
            Transform itemCamera = Instantiate(itemHold, DialogueManager.Instance.CameraItem.transform);
            itemCamera.gameObject.layer = LayerMask.NameToLayer("DialogueItemLayer");
            foreach (GameObject child in itemCamera.gameObject.GetAllChildren())
            {
                child.layer = LayerMask.NameToLayer("DialogueItemLayer");
            }
            itemCamera.transform.localPosition = Vector3.zero;
            itemCamera.transform.localEulerAngles = item.Dialogue3DRotation;
            Destroy(itemCamera.GetComponent<RotateObject>());

            itemHold.DOScale(new Vector3(90, 90, 90), 0.25f);
            
            //DialogueManager.ShowDialogue(164/*, TextureFetcher.LoadDialogueSprite(item)*/);
            DialogueManager.ShowDialogue(this, item);
        });
    }

    public void End()
    {
        Link.PlayAnimation(Link.TakeItem);
        
        // Enable controls
        Link.SetControls(Link.Controls.Default);
        
        // Hide cameras
        transform.gameObject.FindChildrenTransform("OpenCameraRight").GetComponent<CinemachineVirtualCamera>().enabled = false;
        transform.gameObject.FindChildrenTransform("ItemCameraRight").GetComponent<CinemachineVirtualCamera>().enabled = false;
        transform.gameObject.FindChildrenTransform("OpenCameraLeft").GetComponent<CinemachineVirtualCamera>().enabled = false;
        transform.gameObject.FindChildrenTransform("ItemCameraLeft").GetComponent<CinemachineVirtualCamera>().enabled = false;
        
        // Destroy items
        Destroy(Link.Instance.gameObject.FindChildren("ItemHold").transform.GetChild(0).gameObject);
        Destroy(DialogueManager.Instance.CameraItem.transform.transform.GetChild(1).gameObject);
        
        // Handle special items
        Actor actor = GetComponent<Actor>();
        if (actor.ChestItem is (int)ItemTable.WOOD_STICK or (int)ItemTable.LIGHT_SWORD or (int)ItemTable.MASTER_SWORD)
        {
            Link.EquipSword(actor.ChestItem);
        }

        InteractableManager.Instance.currentInteractable = null;

        Link.GetItemFromChest.ClearEvents();
    }

    private enum CameraType
    {
        RIGHT,
        LEFT
    }
}