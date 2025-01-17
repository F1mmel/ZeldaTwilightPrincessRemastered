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
        //climbMixer.Add(ClimbUpFinishRight, 3f);

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


        //ClimbStartUp.Speed = .2f;
        //ClimbUpLeft.Speed = 1.5f;
        //ClimbUpRight.Speed = 1.5f;

        /*ClimbUpLeft.Events(Link.Instance.BmdLink).OnEnd = () =>
        {
            isAnimationPlaying = false;
        };
        ClimbUpRight.Events(Link.Instance.BmdLink).OnEnd = () =>
        {
            isAnimationPlaying = false;
        };*/

        //StartCoroutine(StopAnim());
    }
    
// AnimationCurve im Inspector
    public AnimationCurve accelerationCurve;

// Variablen zur Steuerung des Delay
    public float maxDelay = 0.4f; // Maximaler Wert für das Delay
    public float minDelay = 0.15f; // Minimaler Wert für das Delay

    private float timeHeld = 0f; // Zeit, die die Taste gehalten wird

    IEnumerator WaitAfterClimb(float dynamicDelay)
    {
        yield return new WaitForSeconds(dynamicDelay);
        isAnimationPlaying = false;
    }

// Prüft, ob der Spieler sich noch in der Nähe einer Leiter befindet
    private bool StillOnLadder()
    {
        Vector3 meshPos = Link.Instance.BmdLink.transform.GetChild(0).position;
        Vector3 source = new Vector3(meshPos.x, meshPos.y + 1.4f, meshPos.z);
        Ray ray = new Ray(source, -transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;

            // Zeichne Gizmos, wenn getroffen
            Debug.DrawLine(source, hit.point, Color.green);

            if (meshCollider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Climbable_Ladder"))
            {
                return true;
            }
        }

        // Zeichne Gizmos bis zur Maximaldistanz, wenn kein Treffer
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
                // AUFRÄUMEN VON ALLEN PARAMETERN; ÖFTER HINTEREINANDER GEHTG NICHT
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
    private float smoothMoveSpeed = 2f; // Geschwindigkeit, mit der die Y-Position geändert wird

    private float blendValue;
    float targetBlendValue = 0f;
    private bool climbMixerPlaying = false;
    public AnimationCurve movementCurve; // Zum Einstellen im Inspector
    private float movementStartTime;     // Zeitpunkt, zu dem die Bewegung begonnen hat
    private float movementDuration = 1f; // Dauer der Bewegung
    private float startYPosition;        // Anfangsposition der Bewegung
    private bool isMoving = false;       // Bewegung aktiv?
    private bool isAnimationPlaying = false; // Überprüft, ob eine Animation läuft
    private bool climbStartUpFinished = false;
    private bool showExitAnimation;

private void HandleLadderMovement()
{
    if (Input.GetKey(KeyCode.W) && !isAnimationPlaying && climbStartUpFinished && !showExitAnimation)
    {
        // Berechne die Zeit, wie lange die Taste gedrückt wird
        timeHeld += Time.deltaTime * 30;

        // Verwende die AnimationCurve, um den Delay dynamisch zu berechnen
        // Interpoliere den Delay-Wert basierend auf der Zeit, die die Taste gedrückt wird
        float normalizedTime = Mathf.Clamp(timeHeld / 1f, 0f, 1f); // Stelle sicher, dass die Zeit zwischen 0 und 1 bleibt
        float curveValue = accelerationCurve.Evaluate(normalizedTime);

        // Berechne den dynamischen Delay-Wert mit der Kurve
        float dynamicDelay = Mathf.Lerp(maxDelay, minDelay, curveValue);

        // Setze isAnimationPlaying auf true und führe die Animation aus
        isAnimationPlaying = true;

        if (playLeftAnimation)
        {
            targetBlendValue = 1f; // Left
            
            ClimbUpRight.AllowAnimation = false;
            climbMixer.Stop();
            ClimbUpLeft.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Play();
            ClimbUpLeft.AllowAnimation = true;
        }
        else
        {
            targetBlendValue = 2f; // Right
            
            ClimbUpLeft.AllowAnimation = false;
            climbMixer.Stop();
            ClimbUpRight.PlayerPosition = Link.Instance.PlayerController.transform.localPosition;
            climbMixer.Play();
            ClimbUpRight.AllowAnimation = true;
        }

        playLeftAnimation = !playLeftAnimation;

        // Starte die Coroutine mit dem dynamischen Delay
        StartCoroutine(WaitAfterClimb(dynamicDelay));
    }
    else if (Input.GetKey(KeyCode.W) == false) // Wenn die W-Taste losgelassen wird
    {
        // Setze timeHeld zurück, wenn W losgelassen wurde
        timeHeld = 0f;
    }
    
    else if (Input.GetKey(KeyCode.S))
    {
        Link.Instance.PlayerController.transform.position += Vector3.down * Time.deltaTime;
    }
    
    blendValue = Mathf.MoveTowards(blendValue, targetBlendValue, Time.deltaTime * 3);
    climbMixer.Parameter = blendValue;
    //Debug.Log(climbMixer.Parameter);
}

    private void SmoothMove()
    {
        float elapsedTime = Time.time - movementStartTime;
        float t = elapsedTime / movementDuration; // Normalisierter Wert (0 bis 1)

        if (t >= 1f)
        {
            t = 1f; // Bewegung abgeschlossen
            isMoving = false;
        }

        // Bewegungskurve anwenden
        float curveValue = movementCurve.Evaluate(t);
        float newYPosition = Mathf.Lerp(startYPosition, targetYPosition, curveValue);

        Vector3 currentPosition = Link.Instance.PlayerController.transform.localPosition;
        currentPosition.y = newYPosition;
        Link.Instance.PlayerController.transform.localPosition = currentPosition;
    }

    private void ExitLadder()
    {
        //isClimbing = false;
        //Link.SetControls(Link.Controls.Default);

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
        //climbMixer.Stop();
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


/*
using System;
using System.Collections;
using Animancer;
using JStudio.J3D.Animation;
using Unity.Collections;
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
        
        climbMixer = new LinearMixerState();
        climbMixer.Add(ClimbStartUp, 0f);
        climbMixer.Add(ClimbUpLeft, 1f);
        climbMixer.Add(ClimbUpRight, 2f);

        ClimbUpLeft.AllowPlay = false;
        ClimbUpRight.AllowPlay = false;
        
        //ClimbUpLeft.SetAdditionInPlaceData(climbMixer, 0.01f, 1f);
        //ClimbUpRight.SetAdditionInPlaceData(climbMixer, 1.01f, 2f);

        ClimbStartUp.Events(this).OnEnd = () =>
        {
            climbStartUpFinished = true;
        };
        
        ClimbUpFinishLeft.Events(this).OnEnd = () =>
        {
            /*Link.Instance.PlayerController.transform.position = new Vector3(Link.Instance.BmdLink.transform.GetChild(0).transform.position.x / 100, 
                Link.Instance.BmdLink.transform.GetChild(0).transform.position.y, 
                Link.Instance.BmdLink.transform.GetChild(0).transform.position.z / -100);
            Link.Instance.BmdLink.transform.GetChild(0).transform.localPosition = Vector3.zero;* /
            
            Link.PlayIdleAnimation();
            Link.EnableFootIK();
        };
        ClimbUpFinishRight.Events(this).OnEnd = () =>
        {
            // Setze die Position des Modells auf (0,0,0) lokal.
            Link.Instance.BmdLink.transform.GetChild(0).transform.localPosition = Vector3.zero;

            // Teleportiere den PlayerController zur Position des Models
            Link.Instance.PlayerController.transform.position = new Vector3(Link.Instance.BmdLink.transform.GetChild(0).transform.position.x + 0.3f,
                Link.Instance.BmdLink.transform.GetChild(0).transform.position.y + 3f,
                Link.Instance.BmdLink.transform.GetChild(0).transform.position.z - 1.1f);
            
            // IN PLACE SETZEN UND NEU MACHEN DIE TIMING

            // Spiele die Idle-Animation ab
            Link.PlayIdleAnimation();
            Link.EnableFootIK();

            // Optional: Setze die Steuerung auf Default (wenn benötigt)
            // Link.SetControls(Link.Controls.Default);
        };


        //ClimbStartUp.Speed = .2f;
        //ClimbUpLeft.Speed = 1.5f;
        //ClimbUpRight.Speed = 1.5f;

        /*ClimbUpLeft.Events(Link.Instance.BmdLink).OnEnd = () =>
        {
            isAnimationPlaying = false;
        };
        ClimbUpRight.Events(Link.Instance.BmdLink).OnEnd = () =>
        {
            isAnimationPlaying = false;
        };* /

        //StartCoroutine(StopAnim());
    }

    IEnumerator WaitAfterClimb()
    {
        yield return new WaitForSeconds(0.35f);
        isAnimationPlaying = false;
    }

// Prüft, ob der Spieler sich noch in der Nähe einer Leiter befindet
    private bool StillOnLadder()
    {
        Vector3 meshPos = Link.Instance.BmdLink.transform.GetChild(0).position;
        Vector3 source = new Vector3(meshPos.x, meshPos.y + 1.4f, meshPos.z);
        Ray ray = new Ray(source, -transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;

            // Zeichne Gizmos, wenn getroffen
            Debug.DrawLine(source, hit.point, Color.green);

            if (meshCollider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("Climbable_Ladder"))
            {
                return true;
            }
        }

        // Zeichne Gizmos bis zur Maximaldistanz, wenn kein Treffer
        Debug.DrawLine(source, source + -transform.forward * raycastDistance, Color.red);

        return false;
    }


    void Update()
    {
        if (isClimbing)
        {
            HandleLadderMovement();

            if (!StillOnLadder() && false)
            {
                Debug.Log("EXIT!!!");
                ExitLadder();
                // AUFRÄUMEN VON ALLEN PARAMETERN; ÖFTER HINTEREINANDER GEHTG NICHT
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
    private AnimationJobManager.BoneAnimationData LeftData;
    private AnimationJobManager.BoneAnimationData RightData;
    public AnimationCurve Left;
    public AnimationCurve Right;
    private void EnterLadder(RaycastHit hit)
    {
        Debug.Log("Enter Ladder");
        Link.DisableFootIK();
        isClimbing = true;
        Link.SetControls(Link.Controls.Frozen);
        
        smoothMoveSpeed = .3f;
        
        isMoving = true;
        startYPosition = Link.Instance.PlayerController.transform.localPosition.y;
        targetYPosition = startYPosition + 0.3f;

        foreach (var a in ClimbUpLeft.BoneAnimationData)
        {
            string boneNameString = new string(a.BoneName.ToArray());
            if (boneNameString == "center")
            {
                LeftData = a;
                Left = ZeldaAnimation.ConvertToAnimationCurve(a.posValuesY);
            }
        }

        foreach (var a in ClimbUpRight.BoneAnimationData)
        {
            string boneNameString = new string(a.BoneName.ToArray());
            if (boneNameString == "center")
            {
                RightData = a;
                Right = ZeldaAnimation.ConvertToAnimationCurve(a.posValuesY);
            }
        }
        
        Link.Instance.Animancer.Play(climbMixer);
        
    }
    private float targetYPosition = 0f;
    private float smoothMoveSpeed;
    //private AnimationJobManager.BoneAnimationData _data;

    private float blendValue;
    float targetBlendValue = 0f;
    private bool climbMixerPlaying = false;
    private float startYPosition;        // Anfangsposition der Bewegung
    private bool isMoving = false;       // Bewegung aktiv?
    private bool isAnimationPlaying = false; // Überprüft, ob eine Animation läuft
    private bool climbStartUpFinished = false;

    private void HandleLadderMovement()
    {
        if (Input.GetKey(KeyCode.W) && !isAnimationPlaying && climbStartUpFinished)
        {
            smoothMoveSpeed = 2f;
            isAnimationPlaying = true;
            
            ClimbStartUp.AllowPlay = false;

            if (playLeftAnimation)
            {
                targetBlendValue = 1f; // Left
                ClimbUpRight.AllowPlay = false;
                ClimbUpLeft.AllowPlay = true;
            }
            else
            {
                targetBlendValue = 2f; // Right
                ClimbUpLeft.AllowPlay = false;
                ClimbUpRight.AllowPlay = true;
            }

            playLeftAnimation = !playLeftAnimation;

            StartCoroutine(WaitAfterClimb());
        }
        
        else if (Input.GetKey(KeyCode.S))
        {
            Link.Instance.PlayerController.transform.position += Vector3.down * Time.deltaTime;
        }
        
        blendValue = Mathf.MoveTowards(blendValue, targetBlendValue, Time.deltaTime * 3);
        climbMixer.Parameter = blendValue;
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            ExitLadder();
        }

        return;
    }

    private float EvaluateCurve(float t, NativeArray<float> times, NativeArray<float> values, int keyCount, LoopType loopType)
    {
        if (keyCount < 2)
            return values[0];

        if (loopType != LoopType.Once)
        {
            // Normalize time only if looping
            t = Mathf.Repeat(t, times[keyCount - 1]);
        }

        for (int i = 0; i < keyCount - 1; i++)
        {
            if (t >= times[i] && t < times[i + 1])
            {
                float delta = (t - times[i]) / (times[i + 1] - times[i]);
                return Mathf.Lerp(values[i], values[i + 1], delta);
            }
        }

        return values[keyCount - 1];
    }

    private void ExitLadder()
    {
        isClimbing = false;
        climbStartUpFinished = false;
        //Link.SetControls(Link.Controls.Default);

        if (playLeftAnimation)
        {
            Link.PlayAnimation(ClimbUpFinishLeft);
        }
        else
        {
            Link.PlayAnimation(ClimbUpFinishRight);
        }
    }
}

*/