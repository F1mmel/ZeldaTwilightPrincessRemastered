using System;
using System.Collections;
using Animancer;
using JStudio.J3D.Animation;
using UnityEngine;

public class LadderController : MonoBehaviour
{
    [SerializeField]
    private float raycastDistance = 1f;

    private bool isShowingUI = false;
    private bool isNearLadder = false;
    private bool isClimbing = false;
    private bool playLeftAnimation = true;

    private ZeldaAnimation ClimbStartUp;
    private ZeldaAnimation ClimbUpLeft;
    private ZeldaAnimation ClimbUpRight;
    private ZeldaAnimation ClimbUpFinishLeft;
    private ZeldaAnimation ClimbUpFinishRight;

    public LinearMixerState climbMixer;
    
    private CharacterController characterController;

    private void Start()
    {
        ClimbStartUp = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "ladupst", LoopType.Once);
        ClimbUpLeft = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "ladltor", LoopType.Once);
        ClimbUpRight = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "ladrtol", LoopType.Once);
        ClimbUpFinishLeft = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "ladupedl", LoopType.Once);
        ClimbUpFinishRight = Link.Instance.BmdLink.LoadSpecificAnimation(Link.Instance.ArchiveAnimations, "ladupedr", LoopType.Once);

        characterController = Link.Instance.PlayerController.GetComponent<CharacterController>();

        ClimbStartUp.SetInPlace(true);
        ClimbUpLeft.SetInPlace(true);
        ClimbUpRight.SetInPlace(true);
        ClimbUpFinishLeft.SetInPlace(true);
        ClimbUpFinishRight.SetInPlace(true);

        ClimbUpLeft.AllowAnimation = false;
        ClimbUpRight.AllowAnimation = false;
        ClimbUpFinishLeft.AllowAnimation = false;
        ClimbUpFinishRight.AllowAnimation = false;

        ClimbUpFinishLeft.Speed = 0.5f;
        ClimbUpFinishRight.Speed = 0.5f;
        
        climbMixer = new LinearMixerState();
        climbMixer.Add(ClimbStartUp, 0f);
        climbMixer.Add(ClimbUpLeft, 1f);
        climbMixer.Add(ClimbUpRight, 2f);

        ClimbStartUp.Events(this).OnEnd = () =>
        {
            EnumeratorHelper.Create(0.02f, () =>
            {
                climbStartUpFinished = true;
            });
        };
        
        ClimbUpFinishLeft.Events(this).OnEnd = () =>
        {
            AfterExitLadder();
        };
        ClimbUpFinishRight.Events(this).OnEnd = () =>
        {
            AfterExitLadder();
        };




    }
    
    public AnimationCurve accelerationCurve;

    public float maxDelay = 0.4f;
    public float minDelay = 0.15f;

    private float timeHeld = 0f;

    IEnumerator WaitAfterClimb(float dynamicDelay)
    {
        yield return new WaitForSeconds(dynamicDelay);
        isAnimationPlaying = false;
    }

    private bool StillOnLadder()
    {
        Vector3 meshPos = Link.Instance.BmdLink.transform.GetChild(0).position;
        Vector3 source = new Vector3(meshPos.x, meshPos.y + 1.4f, meshPos.z);
        Ray ray = new Ray(source, -transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;

            Debug.DrawLine(source, hit.point, Color.green);

            if (meshCollider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Climbable_Ladder"))
            {
                return true;
            }
        }

        Debug.DrawLine(source, source + -transform.forward * raycastDistance, Color.red);

        return false;
    }


    void Update()
    {
        if (isClimbing)
        {
            HandleLadderMovement();

            if (!StillOnLadder())
            {
                ExitLadder();
            }
            return;
        }

        Ray ray = new Ray(transform.position, -transform.forward);
        RaycastHit hit;

        isNearLadder = false;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (meshCollider != null)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Climbable_Ladder"))
                {
                    if (!isShowingUI)
                    {
                        InteractableManager.Instance.ShowUI("Climb", null);
                        isShowingUI = true;
                    }

                    isNearLadder = true;

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        EnterLadder(hit);
                    }

                    return;
                }
            }
        }

        if (isShowingUI)
        {
            InteractableManager.Instance.HideUI();
            isShowingUI = false;
        }
    }

    private Vector3 startPosition;
    private void EnterLadder(RaycastHit hit)
    {
        Debug.Log("Enter Ladder");
        Link.DisableFootIK();
        isClimbing = true;
        Link.SetControls(Link.Controls.Frozen);
        
        targetYPosition = Link.Instance.PlayerController.transform.localPosition.y;
        startPosition = Link.Instance.PlayerController.transform.localPosition;
        
        ClimbStartUp.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
        Link.Instance.Animancer.Play(climbMixer, 0.25f, FadeMode.FromStart);
        
    }
    private float targetYPosition = 0f;
    private float smoothMoveSpeed = 2f;

    private float blendValue;
    float targetBlendValue = 0f;
    private bool climbMixerPlaying = false;
    public AnimationCurve movementCurve;
    private float movementStartTime;
    private float movementDuration = 1f;
    private float startYPosition;
    private bool isMoving = false;
    private bool isAnimationPlaying = false;
    private bool climbStartUpFinished = false;
    private bool showExitAnimation;

private void HandleLadderMovement()
{
    if (Input.GetKey(KeyCode.W) && !isAnimationPlaying && climbStartUpFinished && !showExitAnimation)
    {
        timeHeld += Time.deltaTime * 30;

        float normalizedTime = Mathf.Clamp(timeHeld / 1f, 0f, 1f);
        float curveValue = accelerationCurve.Evaluate(normalizedTime);

        float dynamicDelay = Mathf.Lerp(maxDelay, minDelay, curveValue);

        isAnimationPlaying = true;

        if (playLeftAnimation)
        {
            targetBlendValue = 1f;
            
            ClimbUpRight.AllowAnimation = false;
            climbMixer.Stop();
            ClimbUpLeft.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Play();
            ClimbUpLeft.AllowAnimation = true;
        }
        else
        {
            targetBlendValue = 2f;
            
            ClimbUpLeft.AllowAnimation = false;
            climbMixer.Stop();
            ClimbUpRight.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Play();
            ClimbUpRight.AllowAnimation = true;
        }

        playLeftAnimation = !playLeftAnimation;

        StartCoroutine(WaitAfterClimb(dynamicDelay));
    }
    else if (Input.GetKey(KeyCode.W) == false)
    {
        timeHeld = 0f;
    }
    
    else if (Input.GetKey(KeyCode.S))
    {
        Link.Instance.PlayerController.transform.position += Vector3.down * Time.deltaTime;
    }
    
    blendValue = Mathf.MoveTowards(blendValue, targetBlendValue, Time.deltaTime * 3);
    climbMixer.Parameter = blendValue;
}

    private void SmoothMove()
    {
        float elapsedTime = Time.time - movementStartTime;
        float t = elapsedTime / movementDuration;

        if (t >= 1f)
        {
            t = 1f;
            isMoving = false;
        }

        float curveValue = movementCurve.Evaluate(t);
        float newYPosition = Mathf.Lerp(startYPosition, targetYPosition, curveValue);

        Vector3 currentPosition = Link.Instance.PlayerController.transform.localPosition;
        currentPosition.y = newYPosition;
        Link.Instance.PlayerController.transform.localPosition = currentPosition;
    }

    private void ExitLadder()
    {

        if (showExitAnimation) return;

        ClimbStartUp.AllowAnimation = false;
        ClimbUpLeft.AllowAnimation = false;
        ClimbUpRight.AllowAnimation = false;
        ClimbUpFinishLeft.AllowAnimation = false;
        if (playLeftAnimation)
        {
            
            ClimbUpFinishRight.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Stop();

            climbMixer.Add(ClimbUpFinishLeft, 3f);
            ClimbUpFinishLeft.AllowAnimation = true;
        }
        else
        {
            ClimbUpFinishRight.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Stop();

            climbMixer.Add(ClimbUpFinishRight, 3f);
            ClimbUpFinishRight.AllowAnimation = true;
        }

        targetBlendValue = 3;
        climbMixer.Parameter = 2;
            
        climbMixer.Play();
        
        showExitAnimation = true;
    }

    private void AfterExitLadder()
    {
        ClimbUpFinishRight.AllowAnimation = false;
        isClimbing = false;
            
        Vector3 pos = new Vector3(startPosition.x, Link.Instance.PlayerController.transform.localPosition.y, startPosition.z - 0.75f);
        pos.y += 0.3f;
        Link.Instance.PlayerController.transform.localPosition = pos;
            
        Link.PlayIdleAnimation();
        Link.EnableFootIK();
        Link.SetControls(Link.Controls.Default);
        
        EnumeratorHelper.Create(0.5f, () =>
        {
            climbMixer.Stop();
        });
    }
}


