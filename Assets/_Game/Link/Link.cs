using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Animancer;
using Animancer.Samples.InverseKinematics;
using Animancer.Samples.Jobs;
using DG.Tweening;
using DiasGames.Abilities;
using DiasGames.Components;
using DiasGames.Controller;
using GameFormatReader.Common;
using JStudio.J3D.Animation;
using RootMotion;
using RootMotion.FinalIK;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using WiiExplorer;
using Debug = UnityEngine.Debug;
using LoopType = JStudio.J3D.Animation.LoopType;

public class Link : MonoBehaviour
{
    public LinkType LinkType;
    
    [Header("Animations")] public static ZeldaAnimation OpenChestA; 
    public static ZeldaAnimation OpenChestB;
    public static ZeldaAnimation GetItemFromChest;
    public static ZeldaAnimation GetItemFromChestWait;
    public static ZeldaAnimation FaceChestOpen;
    public static ZeldaAnimation TakeItem;
    
    // Sword
    public static ZeldaAnimation TakeSword;
    
    // Door
    public static ZeldaAnimation DoorOpenLeft;
    public static ZeldaAnimation DoorOpenRight;

    [Header("IK's")] public Transform FootIK;
    public Transform HandIK;

    [Header("References")] public LimbIK HandLeft;
    public LimbIK HandRight;
    
    public static Transform SwordLocation;
    

    public Archive ArchiveAnimations;
    
    [Space]
    public TPSPlayerController PlayerController;
    
    public BMD BmdLink;
    public BMD BmdFace;
    
    private Animator animator;
    public AnimancerComponent Animancer;
    private Animator animatorFace;
    public AnimancerComponent AnimancerFace;

    [Header("Animations")] public LinearMixerState animationMixer;

    private AudioSource audioSource;

    public Dictionary<string, AnimancerState> _allAnims = new Dictionary<string, AnimancerState>();
    
    public static Link Instance;
    [HideInInspector] public Camera _camera;
    
    private void ClearStaticAnimations()
    {
        // Hole alle statischen Felder vom Typ ZeldaAnimation
        FieldInfo[] fields = typeof(Link).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType == typeof(ZeldaAnimation))
            {
                field.SetValue(null, null); // Setze das statische Feld auf null
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        ClearStaticAnimations();
        Instance = this;
        _camera = Camera.main;
        PlayerController = transform.parent.parent.GetComponent<TPSPlayerController>();
        
        //BmdLink = BMDCreator.CreateModel("Kmdl", "al", transform);
        //BMD face = BMDCreator.CreateModel("Kmdl", "al_face", transform);
        //BMD head = BMDCreator.CreateModel("Kmdl", "al_head", transform);
        
        string typeValue = LinkType.ToEnumMember();
        string archiveName = typeValue.Split(", ")[0];
        string bmdName = typeValue.Split(", ")[1];

        string faceModel = "";
        if (LinkType == LinkType.Ordon) faceModel = "al_face";
        if (LinkType == LinkType.Zora) faceModel = "zl_face";
        if (LinkType == LinkType.Hero) faceModel = "al_face";
        if (LinkType == LinkType.MagicArmor) faceModel = "al_face";
        
        BmdLink = BMDCreator.CreateModel(archiveName, bmdName, transform);
        BMD head = BMDCreator.CreateModel(archiveName, bmdName + "_head", transform);
        if (LinkType == LinkType.Sumo)
        {
            archiveName = "Bmdl";
            faceModel = "al_face";
        }
        BmdFace = BMDCreator.CreateModel(archiveName, faceModel, transform);
        BmdLink.PrepareAnimation();
        BmdFace.PrepareAnimation();
        BmdLink.IsLink = true;

        Animancer = BmdLink._animancer;
        Animancer.Animator = BmdLink._animator;
        
        audioSource = transform.AddComponent<AudioSource>();

        animatorFace = BmdFace._animator;
        AnimancerFace = BmdFace._animancer;
        AnimancerFace.Animator = animatorFace;

        BmdLink.CompressedAnimations = true;
        BmdFace.CompressedAnimations = true;
        head.CompressedAnimations = true;
        
        // Adjust scales
        BmdLink.transform.localScale = Vector3.one;
        BmdFace.transform.localScale = Vector3.one;
        head.transform.localScale = Vector3.one;

        BmdFace.SetParentJoint(BmdLink, "head");
        head.SetParentJoint(BmdLink, "head");

        BmdLink.transform.localEulerAngles = new Vector3(0, 0, 0);
        
        if(LinkType != LinkType.Ordon && LinkType != LinkType.Sumo)
            BmdLink.transform.AddComponent<CapPhysics>().AddBone();

        AddHairAnimation("hairL1");
        AddHairAnimation("hairL2");
        AddHairAnimation("hairR");

        //InitializeFinalIK();
        SwordLocation = BmdLink.gameObject.FindChildren("backbone2").transform;
        
        ArchiveAnimations = ArcReader.Read(@"Assets\GameFiles\res\Object\AlAnm.arc");

        BmdLink.LoadAnimationsOnMainThread = true;
        ZeldaAnimation state = LoadAnimation("waits", LoopType.Loop);
        //ZeldaAnimation walk = LoadAnimation("walks", LoopType.Loop);

        AnimationSetup.AnimationCurveHolder curve1 = BmdLink.LoadAnimationCurves(ArchiveAnimations, "dashs");
        AnimationSetup.AnimationCurveHolder curve2 = BmdLink.LoadAnimationCurves(ArchiveAnimations, "dasha", "backbone1");
        ZeldaAnimation walk = BmdLink.MergeCurvesToAnimation(curve1, curve2, LoopType.Loop);
        ZeldaAnimation sprint = LoadAnimation("dashb");
        sprint.Speed = 1.24f;
        
        animationMixer = new LinearMixerState();
        animationMixer.Add(state, 0);
        animationMixer.Add(walk, 0.5f);
        animationMixer.Add(sprint, 1.0f);
        BmdLink.LoadAnimationsOnMainThread = false;
        
        animationMixer.DontSynchronize(animationMixer.GetChild(0));
        
        // Jede Animation rauß nehem und schauen ob mit keiner geht
        Animancer.Play(animationMixer);
        
        // Fill animations
        //OpenChestB = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "boxopshort", LoopType.Once, AnimationBehavior.GoBack);

        //Task.Run(() =>
        {
            try
            {
            //OpenChestA = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "boxopkick", LoopType.Once);
            //OpenChestB = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "boxopshort", LoopType.Once);
            //GetItemFromChest = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "geta", LoopType.Once);
            //GetItemFromChestWait = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "getawait", LoopType.Loop);
            
            //FaceChestOpen = face.LoadSpecificAnimation(ArchiveAnimations, "fboxop", LoopType.Once);
            //FaceChestOpen = BmdFace.LoadSpecificAnimation(ArchiveAnimations, "ff", LoopType.Once);
            //TakeItem = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "take", LoopType.Once, AnimationBehavior.GoBack);
        
            // Sword
            AnimationSetup.AnimationCurveHolder curve3 = BmdLink.LoadAnimationCurves(ArchiveAnimations, "waits");
            AnimationSetup.AnimationCurveHolder curve4 = BmdLink.LoadAnimationCurves(ArchiveAnimations, "waitatos", "backbone1");
            TakeSword = BmdLink.MergeCurvesToAnimation(curve3, curve4);
            TakeSword.SingleBoneConstraint = SingleBoneConstraint.ShoulderL;
            //TakeSword = BmdLink.LoadSpecificAnimation(ArchiveAnimations, "waitatos", LoopType.Once, AnimationBehavior.GoBack);

            // Door


            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in Task: {ex}");
            }
        };
        

        
        // Adjust limb ik's
        HandLeft.solver.bendModifier = IKSolverLimb.BendModifier.Target;
        HandLeft.solver.bone1.transform = BmdLink.gameObject.FindChildren("armL1").transform;
        HandLeft.solver.bone2.transform = BmdLink.gameObject.FindChildren("armL2").transform;
        HandLeft.solver.bone3.transform = BmdLink.gameObject.FindChildren("handL").transform;
        HandLeft.solver.bone3.transform = BmdLink.gameObject.FindChildren("weaponL").transform;
        HandLeft.transform.SetParent(BmdLink.transform);

        CreateLookIK();
        
        BMD sword = EquipSword((int)ItemTable.WOOD_STICK);
        //StartCoroutine(S(sword.gameObject));

        // Preview all animations
        /*foreach (ArcFile file in ArchiveAnimations.Files)
        {
            if (file.Name.EndsWith(".bck"))
            {
                _allAnims.Add(file.Name, BmdLink.LoadSpecificAnimation(ArchiveAnimations, file.Name, LoopType.Loop));
            }
        }*/

        /*HumanDescription description = AvatarUtils.CreateHumanDescription(BmdLink.gameObject);
        Avatar avatar = AvatarBuilder.BuildHumanAvatar(BmdLink.gameObject, description);
        avatar.name = gameObject.name;
        animator.avatar = avatar;*/
    }

    private void CreateLookIK()
    {
        GameObject look = new GameObject("LookIK");
        look.transform.SetParent(BmdLink.transform);
        look.transform.localPosition = Vector3.zero;
        look.transform.localRotation = Quaternion.identity;
        
        AimIK ik = look.AddComponent<AimIK>();
        ik.solver.target = GameObject.Find("Cube").transform;
        ik.solver.transform = BmdLink.gameObject.FindChildren("head").transform;
        
        ik.solver.axis = new Vector3(1, 0, 0);
        ik.solver.poleAxis = new Vector3(0, -1, 0);

        ik.solver.bones = new[]
        {
            new IKSolver.Bone()
            {
                transform = BmdLink.gameObject.FindChildren("backbone1").transform,
                rotationLimit = look.AddComponent<RotationLimitAngle>(),
                weight = 0
            },
            new IKSolver.Bone()
            {
                transform = BmdLink.gameObject.FindChildren("backbone2").transform,
                rotationLimit = look.AddComponent<RotationLimitAngle>(),
                weight = 0.6f
            },
            new IKSolver.Bone()
            {
                transform = BmdLink.gameObject.FindChildren("neck").transform,
                rotationLimit = look.AddComponent<RotationLimitAngle>(),
                weight = 0.4f
            },
            new IKSolver.Bone()
            {
                transform = BmdLink.gameObject.FindChildren("head").transform,
                rotationLimit = look.AddComponent<RotationLimitAngle>(),
                weight = 1
            }
        };
    }

    public static ZeldaAnimation LoadAnimation(string name, LoopType loopType = LoopType.UseDefined)
    {
        AnimationBehavior behavior = AnimationBehavior.Stay;
        if (loopType == LoopType.Loop) behavior = AnimationBehavior.GoBack;
        else if (loopType == LoopType.Once) behavior = AnimationBehavior.Stay;
        return Instance.BmdLink.LoadSpecificAnimation(Instance.ArchiveAnimations, name, loopType, behavior);
    }

    public static ZeldaAnimation LoadFaceAnimation(string name, LoopType loopType = LoopType.UseDefined)
    {
        return Instance.BmdFace.LoadSpecificAnimation(Instance.ArchiveAnimations, name, loopType);
    }

    private IEnumerator S(GameObject o)
    {
        yield return new WaitForSeconds(2.5f);
        EnableLeftHandIK(o.transform);
    }
    
    private void AddHairAnimation(string referenceBone)
    {
        Transform center = BmdLink.gameObject.FindChildren("center").transform;

        DynamicBone bone = center.AddComponent<DynamicBone>();
        bone.m_Root = BmdLink.gameObject.FindChildren(referenceBone).transform;
        bone.m_Damping = 0.78f;
        bone.m_Elasticity = 0.05f;
        bone.m_Stiffness = 0.6f;
        bone.m_Friction = 0.14f;
        bone.m_Force = new Vector3(1, 1, 1);
        bone.m_Gravity = new Vector3(1, 1, 1);
    }

    private void InitializeFinalIK()
    {
        /*GameObject grounderIK = new GameObject();
        grounderIK.name = "GrounderIK";
        grounderIK.transform.SetParent(BmdLink.transform);
        GrounderIK ik = grounderIK.AddComponent<GrounderIK>();
        ik.pelvis = BmdLink.gameObject.FindChildren("center").transform;
        ik.characterRoot = BmdLink.transform;
        
        GameObject legL = new GameObject();
        legL.name = "LegL";
        legL.transform.SetParent(grounderIK.transform);
        LimbIK ikLeft = legL.AddComponent<LimbIK>();
        ikLeft.solver.bone1.transform = BmdLink.gameObject.FindChildren("legL1").transform;
        ikLeft.solver.bone2.transform = BmdLink.gameObject.FindChildren("legL2").transform;
        ikLeft.solver.bone3.transform = BmdLink.gameObject.FindChildren("footL").transform;
        
        GameObject legR = new GameObject();
        legR.name = "LegR";
        legR.transform.SetParent(grounderIK.transform);
        LimbIK ikRight = legR.AddComponent<LimbIK>();
        ikRight.solver.bone1.transform = BmdLink.gameObject.FindChildren("legR1").transform;
        ikRight.solver.bone2.transform = BmdLink.gameObject.FindChildren("legR2").transform;
        ikRight.solver.bone3.transform = BmdLink.gameObject.FindChildren("footR").transform;


        ik.legs = new[] { ikLeft, ikRight };
        ik.solver.layers = 1;*/
        
        GameObject ikObject = new GameObject();
        ikObject.name = "FinalIK";
        ikObject.transform.SetParent(BmdLink.transform);
        
        FullBodyBipedIK ik = ikObject.AddComponent<FullBodyBipedIK>();
        ik.fixTransforms = true;

        BipedReferences references = new BipedReferences();
        references.root = BmdLink.transform;
        references.pelvis = BmdLink.gameObject.FindChildren("center").transform;
        references.leftThigh = BmdLink.gameObject.FindChildren("legL1").transform;
        references.leftCalf = BmdLink.gameObject.FindChildren("legL2").transform;
        references.leftFoot = BmdLink.gameObject.FindChildren("footL").transform;
        references.rightThigh = BmdLink.gameObject.FindChildren("legR1").transform;
        references.rightCalf = BmdLink.gameObject.FindChildren("legR2").transform;
        references.rightFoot = BmdLink.gameObject.FindChildren("footR").transform;
        references.leftUpperArm = BmdLink.gameObject.FindChildren("armL1").transform;
        references.leftForearm = BmdLink.gameObject.FindChildren("armL2").transform;
        references.leftHand = BmdLink.gameObject.FindChildren("handL").transform;
        references.rightUpperArm = BmdLink.gameObject.FindChildren("armR1").transform;
        references.rightForearm = BmdLink.gameObject.FindChildren("armR2").transform;
        references.rightHand = BmdLink.gameObject.FindChildren("handR").transform;
        references.head = BmdLink.gameObject.FindChildren("head").transform;

        references.spine = new[] { BmdLink.gameObject.FindChildren("backbone2").transform};
        ik.SetReferences(references, BmdLink.gameObject.FindChildren("backbone2").transform);
        ik.solver.IKPositionWeight = 1;
    }

    // Diese Methode erstellt ein Rig für den Kopf des Charakters
    private void AddHeadRig(GameObject rigGameObject, GameObject targetModel)
    {
        // Erstelle ein GameObject für den Kopf-Aim Constraint
        GameObject headAimGameObject = new GameObject("HeadAim");
        headAimGameObject.transform.parent = rigGameObject.transform;

        // Füge das Multi-Aim Constraint hinzu
        MultiAimConstraint headAimConstraint = headAimGameObject.AddComponent<MultiAimConstraint>();

        // Setup für die SourceObject-Liste: Das Ziel, auf das der Kopf zielen soll
        var data = headAimConstraint.data.sourceObjects;
        var sourceObject = new WeightedTransform(targetModel.transform, 1.0f); // Ziel des HeadAim Constraints (bspw. Kamera)
        data.Add(sourceObject);
        headAimConstraint.data.sourceObjects = data;

        // Wähle den Kopfknochen des Charakters als Transform, der bewegt werden soll
        headAimConstraint.data.constrainedObject = targetModel.gameObject.FindChildrenTransform("head"); // Setze hier den Kopf-Knochen

        // Richte die Achsen aus, z.B. wenn du den Kopf nur auf der Y-Achse rotieren willst
        headAimConstraint.data.aimAxis = MultiAimConstraintData.Axis.Y;
    }

public enum Controls {
    Frozen,
    Default
}

public static void SetControls(Controls controls)
{
    TPSPlayerController controller = Instance.transform.parent.parent.GetComponent<TPSPlayerController>();
    if (controls == Controls.Frozen)
    {
        controller.LockCameraPosition = true;
        controller.LockMovement = true;
        controller.Move = Vector2.zero;

        // Eingabe-Reset
        Input.ResetInputAxes();

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = false;
        Instance.transform.parent.parent.GetComponent<Locomotion>().enabled = false;
        Instance.transform.parent.parent.GetComponent<CharacterController>().enabled = false;
        controller._scheduler.enabled = false;
    } 
    else if (controls == Controls.Default)
    {
        controller.LockCameraPosition = false;
        controller.LockMovement = false;

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = true;
        Instance.transform.parent.parent.GetComponent<Locomotion>().enabled = true;
        Instance.transform.parent.parent.GetComponent<CharacterController>().enabled = true;
        controller._scheduler.enabled = true;
    }
}

public static void SetControlsWithoutResetInput(Controls controls)
{
    TPSPlayerController controller = Instance.transform.parent.parent.GetComponent<TPSPlayerController>();
    if (controls == Controls.Frozen)
    {
        controller.LockCameraPosition = true;
        controller.LockMovement = true;

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = false;
        Instance.transform.parent.parent.GetComponent<Locomotion>().enabled = false;
        Instance.transform.parent.parent.GetComponent<CharacterController>().enabled = false;
        controller._scheduler.enabled = false;
    } 
    else if (controls == Controls.Default)
    {
        controller.LockCameraPosition = false;
        controller.LockMovement = false;

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = true;
        Instance.transform.parent.parent.GetComponent<Locomotion>().enabled = true;
        Instance.transform.parent.parent.GetComponent<CharacterController>().enabled = true;
        controller._scheduler.enabled = true;
    }
}

public static void DisableMovementAllowCamera(Controls controls)
{
    TPSPlayerController controller = Instance.transform.parent.parent.GetComponent<TPSPlayerController>();
    if (controls == Controls.Frozen)
    {
        controller.LockMovement = true;
        controller.Move = Vector2.zero;

        // Eingabe-Reset
        Input.ResetInputAxes();

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = false;
    } 
    else if (controls == Controls.Default)
    {
        controller.LockMovement = false;

        Instance.transform.parent.parent.GetComponent<Mover>().enabled = true;
    }
}


public static void Teleport(Vector3 pos)
{
    Instance.transform.parent.parent.GetComponent<TPSPlayerController>().Move = Vector2.zero;
    Input.ResetInputAxes();
    Instance.transform.parent.parent.position = pos;
}

public static void Teleport(Transform target, bool autoLook = true)
{
    Instance.transform.parent.parent.GetComponent<TPSPlayerController>().Move = Vector2.zero;
    Input.ResetInputAxes();
    Instance.transform.parent.parent.position = target.position;
    if(autoLook) Instance.transform.parent.parent.rotation = Quaternion.LookRotation(target.forward);
}
public static void Look(Vector3 target)
{
    Instance.transform.parent.parent.rotation = Quaternion.LookRotation(target);
}

public static void PlaySound(AudioClip clip)
{
    Instance.audioSource.PlayOneShot(clip, 0.5f);
}

    public class AnimationData
    {
        public AnimationClip Clip;
        public AnimationSetup AnimationSetup;
    }

    private float blendValue; // Dieser Wert wird zwischen 0 und 1 interpoliert
    private AnimancerState currentState;

    private bool IsMoving()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }

    private ZeldaAnimation _previousState;
    public static AnimancerState PlayAnimation(ZeldaAnimation state, float fadeDuration = 0.25f)
    {
        Instance.BmdLink.PlaySequencedAnimation(state);
        return null;
        
        //Instance._previousState?.Stop();
        //Instance.Animancer.Stop();
        Instance._previousState?.Stop();
        Instance._previousState = state;
        var s = Instance.Animancer.Play(state, fadeDuration, FadeMode.NormalizedSpeed);
        //state.Speed = 30;
        //Instance._previousState?.Stop();
        //Instance._previousState = s;

        s.Events(Instance).OnEnd = () =>
        {
            //Debug.Log("STOP WITHOUT EVENT");
            PlayIdleAnimation(true);
        };
        //Instance.StartCoroutine(Instance.StopPreviousAnimation(s));
        
        //Instance.BmdLink.PlaySequencedAnimation(state);

        return s;
    }

    private IEnumerator StopPreviousAnimation(AnimancerState s)
    {
        yield return new WaitForSeconds(0.25f);
        if (_previousState != null)
        {
            _previousState.Stop();
        }

        //_previousState = s;
    }
    
    private AnimancerState _previousFaceState;
    public static AnimancerState PlayFaceAnimation(AnimancerState state)
    {
        Instance._previousFaceState?.Stop();
        //Instance.Animancer.Stop();
        Instance._previousFaceState = Instance.AnimancerFace.Play(state, 0.25f, FadeMode.FromStart);
        //state.Speed = 30;

        Instance._previousFaceState.Events(Instance).OnEnd = () =>
        {
            //Debug.Log("STOP WITHOUT EVENT");
            //PlayIdleAnimation(true);
        };

        return Instance._previousFaceState;
    }

    public static void PlayIdleAnimation(bool instant = false)
    {
        //AnimancerState state = Instance.Animancer.Play(Instance.animationMixer, 0.25f, FadeMode.FromStart);
        var state = instant ? Instance.Animancer.Play(Instance.animationMixer, 2f, FadeMode.FromStart) : Instance.Animancer.Play(Instance.animationMixer, 0.25f);
        //state.Speed = 30;
        /*state.Events(Instance).OnEnd = () =>
        {
            state.Time = 0;
        };*/
        state.ApplyFootIK = true;
    }

    private void Update()
    {
        float targetBlendValue = 0f;
        //return;

        if (Input.GetKey(KeyCode.E))
        {
            /*AnimancerState state = _component.Play(
                rollAnimation,
                0.2f,
                FadeMode.FromStart);
            state.Speed = 30;
            state.Events(this).OnEnd = () =>
            {
                AnimancerState state = _component.Play(
                    animationMixer);
                    //0.2f,
                    //FadeMode.FromStart);
                state.Speed = 30;
            };*/

            /*AnimancerState state = _component.Play(
                rollAnimation);
            state.Speed = 30;
            
            state.Events(this).OnEnd = () =>
            {
                if (IsMoving())
                {
                    
                }
                else
                {
                    AnimancerState state = _component.Play(
                        animationMixer,
                    0.2f,
                    FadeMode.FromStart);
                    state.Speed = 30;
                }
            };*/
        }

        // Check for forward movement
        //if (Input.GetKey(KeyCode.W)) // Walk or Sprint forward
        if (IsMoving()) // Walk or Sprint forward
        {
            // If shift is pressed, sprint (blendValue = 1)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                targetBlendValue = 1.0f; // Walk
            }
            else
            {
                targetBlendValue = 0.5f; // Sprint
            }
        }

        // Gradually interpolate the blend value based on the target blend
        if (blendValue < targetBlendValue)
        {
            blendValue = Mathf.MoveTowards(blendValue, targetBlendValue, Time.deltaTime * 2);
        }
        else if (blendValue > targetBlendValue)
        {
            blendValue = Mathf.MoveTowards(blendValue, targetBlendValue, Time.deltaTime * 2);
        }

        // Blend zwischen den Animationen
        //BlendAnimations(blendValue);
        if(animationMixer != null) animationMixer.Parameter = blendValue;
        //sprint.Speed = transform.parent.parent.GetComponent<TPSPlayerController>().Move.x + transform.parent.parent.GetComponent<TPSPlayerController>().Move.y;
    }
    
    public static void RotatePlayer180()
    {
        Instance.transform.parent.parent.GetComponent<TPSPlayerController>().Move = Vector2.zero;
        Input.ResetInputAxes();
        
        // Hole die aktuelle Rotation
        Transform playerTransform = Link.Instance.PlayerController.transform;
        Vector3 currentRotation = playerTransform.localEulerAngles;

        // Berechne die Zielrotation (180 Grad um die Y-Achse drehen)
        Vector3 targetRotation = new Vector3(
            currentRotation.x,
            currentRotation.y + 180f,
            currentRotation.z
        );

        // Sorge dafür, dass die Y-Werte zwischen 0 und 360 bleiben
        //targetRotation.y = Mathf.Repeat(targetRotation.y, 360f);

        // Tween die Rotation (z. B. über 1 Sekunde)
        ShortcutExtensions.DOLocalRotate(playerTransform, targetRotation, .4f, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad); // Glatte Übergänge
    }

    public static BMD EquipSword(int id)
    {
        BMD model = null;
        if (id is (int)ItemTable.WOOD_STICK or (int)ItemTable.LIGHT_SWORD or (int)ItemTable.MASTER_SWORD)
        {

            if (id is (int)ItemTable.WOOD_STICK) model = BMDCreator.CreateModel("Bmdl", "al_swb", Link.SwordLocation);

            model.transform.localPosition = new Vector3(30, -12, 18.7f);
            model.transform.localEulerAngles = new Vector3(-6.6f, -210.8f, -5.3f);
            model.transform.localScale = Vector3.one;

            Sword sword = model.AddComponent<Sword>();
            sword.Id = id;
            // Equip sword
        }

        return model;
    }

    public static void DisableFootIK()
    {
        Instance.FootIK.gameObject.SetActive(false);
    }

    public static void EnableFootIK()
    {
        Instance.FootIK.gameObject.SetActive(true);
    }

    public static void EnableLeftHandIK(Transform target)
    {
        Instance.HandLeft.gameObject.SetActive(true);
        Instance.HandLeft.solver.target = target;
        
        Instance.HandLeft.solver.IKPositionWeight = 0f;
        Instance.HandLeft.solver.IKRotationWeight = 0f;

        Instance.StartCoroutine(Instance.SmoothFloat(Instance.HandLeft, 1f));
    }

    private IEnumerator SmoothFloat(LimbIK source, float target)
    {
        float duration = 1.0f; // Dauer der Animation in Sekunden
        float elapsed = 0f;    // Verstrichene Zeit
        float startWeight = source.solver.IKPositionWeight;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth Interpolation
            source.solver.IKPositionWeight = Mathf.Lerp(startWeight, target, t);
            source.solver.IKRotationWeight = Mathf.Lerp(startWeight, target, t);

            yield return null; // Warte bis zum nächsten Frame
        }

        // Setze sicherheitshalber den Zielwert
        source.solver.IKPositionWeight = target;
        source.solver.IKRotationWeight = target;
    }
    
    public static Transform GetClosestGoToPoint(Transform pointA, Transform pointB)
    {
        if (Instance.transform == null || pointA == null || pointB == null)
        {
            Debug.LogError("One or more Transforms are null. Ensure all inputs are valid.");
            return null;
        }

        float distanceToA = Vector3.Distance(Instance.transform.position, pointA.position);
        float distanceToB = Vector3.Distance(Instance.transform.position, pointB.position);

        return distanceToA < distanceToB ? pointA : pointB;
    }
}