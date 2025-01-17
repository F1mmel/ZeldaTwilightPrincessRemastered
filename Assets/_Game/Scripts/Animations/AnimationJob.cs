using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using JStudio.J3D.Animation;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using WiiExplorer;

public class AnimationJobManager : MonoBehaviour
{
    private NativeArray<BoneAnimationData> boneArray;
    private PlayableGraph graph;
    private float duration;

    public struct BoneAnimationData
    {
        public TransformStreamHandle handle;

        public NativeArray<float> posTimes;
        public NativeArray<float> posValuesX;
        public NativeArray<float> posValuesY;
        public NativeArray<float> posValuesZ;

        public NativeArray<float> rotTimes;
        public NativeArray<float> rotValuesX;
        public NativeArray<float> rotValuesY;
        public NativeArray<float> rotValuesZ;
        public NativeArray<float> rotValuesW;

        public int posKeyCount;
        public int rotKeyCount;

        public NativeArray<char> BoneName;
    }

    public struct AnimationCurveJob : IAnimationJob
    {
        public NativeArray<BoneAnimationData> bones;

        public float duration;
        //public float time;
        public NativeArray<float> Time;
        public NativeArray<float> NormalizedTime;
        public LoopType LoopType;
        public AnimationBehavior Behavior;
        public bool IgnoreTranslation;
        public SingleBoneConstraint SingleBoneConstraint;

        private bool first;
        public bool inPlace;

        public Vector3 _startPosition;
        public Vector3 _currentPosition;
        public float _currentY;

        public NativeArray<char> BckName;

        public void ProcessAnimation(AnimationStream stream)
        {
            //if (!IsCurrent) return;
            if (Time.Length == 0) return;
         
            if (LoopType == LoopType.Once && first && Behavior == AnimationBehavior.Stay)
            {
                // Directly set bone transformations to their final state
                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];
                    if (!bone.handle.IsValid(stream))
                        continue;

                    string boneNameString = new string(bone.BoneName.ToArray());
                    if (inPlace)
                    {
                        if (boneNameString.Equals("center"))
                        {
                            if (Time[0] >= duration)
                            {
                                continue;
                            }

                            float posX = EvaluateCurve(Time[0], bone.posTimes, bone.posValuesX, bone.posKeyCount);
                            float posY = EvaluateCurve(Time[0], bone.posTimes, bone.posValuesY, bone.posKeyCount);
                            float posZ = EvaluateCurve(Time[0], bone.posTimes, bone.posValuesZ, bone.posKeyCount);
                            _currentPosition = new Vector3(posX, posY, posZ);
                            _currentY = posY;

                            continue;
                        }
                    }

                    Debug.Log(new string(BckName.ToArray()) + " :: " + IgnoreTranslation);
                    if (!IgnoreTranslation)
                        bone.handle.SetLocalPosition(stream, new Vector3(
                            bone.posValuesX[bone.posKeyCount - 1],
                            bone.posValuesY[bone.posKeyCount - 1],
                            bone.posValuesZ[bone.posKeyCount - 1]
                        ));
                    bone.handle.SetLocalRotation(stream, new Quaternion(
                        bone.rotValuesX[bone.rotKeyCount - 1],
                        bone.rotValuesY[bone.rotKeyCount - 1],
                        bone.rotValuesZ[bone.rotKeyCount - 1],
                        bone.rotValuesW[bone.rotKeyCount - 1]
                    ));
                }

                return; // Stop further processing
            }
            
            float time = Time[0];

            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (!bone.handle.IsValid(stream))
                    continue;

                // Evaluate position
                float posX = EvaluateCurve(time, bone.posTimes, bone.posValuesX, bone.posKeyCount);
                float posY = EvaluateCurve(time, bone.posTimes, bone.posValuesY, bone.posKeyCount);
                float posZ = EvaluateCurve(time, bone.posTimes, bone.posValuesZ, bone.posKeyCount);
                if (!IgnoreTranslation) bone.handle.SetLocalPosition(stream, new Vector3(posX, posY, posZ));

                // Evaluate rotation
                float rotX = EvaluateCurve(time, bone.rotTimes, bone.rotValuesX, bone.rotKeyCount);
                float rotY = EvaluateCurve(time, bone.rotTimes, bone.rotValuesY, bone.rotKeyCount);
                float rotZ = EvaluateCurve(time, bone.rotTimes, bone.rotValuesZ, bone.rotKeyCount);
                float rotW = EvaluateCurve(time, bone.rotTimes, bone.rotValuesW, bone.rotKeyCount);
                bone.handle.SetLocalRotation(stream, new Quaternion(rotX, rotY, rotZ, rotW));
            }
        }
        public float EvaluateCurve(float t, NativeArray<float> times, NativeArray<float> values, int keyCount)
        {
            if (keyCount < 2)
                return values[0];

            if (LoopType != LoopType.Once)
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




        public void ProcessRootMotion(AnimationStream stream)
        {
            // Root motion remains empty
        }
    }

    public string currentAnimation;
    public void PlayAnimation(string anim, Archive archive = null)
    {
        currentAnimation = anim;
        BMD bmd = GetComponent<BMD>();
        ZeldaAnimation state = bmd.LoadSpecificAnimation(archive ?? bmd.Archive, anim);

        Animator animator = GetComponent<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        AnimancerComponent animancer = GetComponent<AnimancerComponent>();
        if (animancer == null) animancer = gameObject.AddComponent<AnimancerComponent>();

        animancer.Play(state);
    }

    public static void ConvertCustomCurveToNativeArrays(CustomCurve curve, out NativeArray<float> times, out NativeArray<float> values)
    {
        int keyCount = curve.Keyframes.Count(); // Annahme: CustomCurve hat eine Eigenschaft KeyCount
        times = new NativeArray<float>(keyCount, Allocator.Persistent);
        values = new NativeArray<float>(keyCount, Allocator.Persistent);

        for (int i = 0; i < keyCount; i++)
        {
            var key = curve.Keyframes[i]; // Annahme: CustomCurve hat eine Methode GetKey(int index), die Zeit und Wert zurückgibt
            times[i] = key.Time;       // Annahme: GetKey liefert ein Objekt mit der Eigenschaft Time
            values[i] = key.Value;     // Annahme: GetKey liefert ein Objekt mit der Eigenschaft Value
        }
    }


    private void OnDestroy()
    {
        if (boneArray.IsCreated)
        {
            for (int i = 0; i < boneArray.Length; i++)
            {
                boneArray[i].posTimes.Dispose();
                boneArray[i].posValuesX.Dispose();
                boneArray[i].posValuesY.Dispose();
                boneArray[i].posValuesZ.Dispose();
                boneArray[i].rotTimes.Dispose();
                boneArray[i].rotValuesX.Dispose();
                boneArray[i].rotValuesY.Dispose();
                boneArray[i].rotValuesZ.Dispose();
                boneArray[i].rotValuesW.Dispose();
            }
            boneArray.Dispose();
        }

        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}

[System.Serializable]
public class ZeldaAnimation : AnimancerState, IUpdatable
{

    private AnimationScriptPlayable _scriptPlayable;
    
        private readonly AnimancerComponent animancer;
        public AnimationJobManager.AnimationCurveJob job;
        public float duration;

        public LoopType loopType;
        public AnimationBehavior behavior;

        public AnimationSetup.AnimationCurveHolder Holder;

        public SingleBoneConstraint SingleBoneConstraint;

        public bool InPlace;
        public bool IgnoreTranslation;

        public List<AnimationEvent> Events { get; } = new List<AnimationEvent>();

        public void ClearEvents()
        {
            Events.Clear();
        }

        public string BckName;
        public NativeArray<AnimationJobManager.BoneAnimationData> BoneArray;

        public ZeldaAnimation(AnimancerComponent animancer)
        {
            this.animancer = animancer;
            //this.Speed = ZeldaManager.Instance.Speed;
        }

        private JobBoneData _jobBoneData;
        public void GetHandles(BMD bmd)
        {
            _jobBoneData = new JobBoneData();
            
            foreach (var joint in bmd.JNT1Tag.AnimatedJoints) _jobBoneData.Paths[joint] = AnimationSetup.GetFullPath(joint);
            foreach (string path in _jobBoneData.Paths.Values) _jobBoneData.Transforms[path] = bmd._animator.BindStreamTransform(bmd.transform.Find(path));
        }

        public void FillJobData(BMD bmd, params AnimationSetup.AnimationCurveHolder[] holder)
{
    Holder = holder[0];
    try
    {
        List<SkeletonJoint> pose = bmd.JNT1Tag.AnimatedJoints;

        NativeArray<AnimationJobManager.BoneAnimationData> boneArray =
            new NativeArray<AnimationJobManager.BoneAnimationData>(pose.Count, Allocator.Persistent);

        float duration = 0;
        for (int i = 0; i < pose.Count; i++)
        {
            string path = _jobBoneData.Paths[pose[i]];

            AnimationJobManager.BoneAnimationData boneData = new AnimationJobManager.BoneAnimationData();
            try
            {
                var h = _jobBoneData.Transforms[path];
                var posKey = holder[0].PositionCurvesX[path].Keyframes.Count;
                var rotKey = holder[0].RotationCurvesX[path].Keyframes.Count;
                var boneN = new NativeArray<char>(pose[i].Name.ToCharArray(), Allocator.Persistent);
                boneData = new AnimationJobManager.BoneAnimationData
                {
                    handle = h,
                    posKeyCount = posKey,
                    rotKeyCount = rotKey,
                    BoneName = boneN
                };

                // Position
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].PositionCurvesX[path],
                    out boneData.posTimes, out boneData.posValuesX);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].PositionCurvesY[path],
                    out boneData.posTimes, out boneData.posValuesY);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].PositionCurvesZ[path],
                    out boneData.posTimes, out boneData.posValuesZ);

                // Rotation
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].RotationCurvesX[path],
                    out boneData.rotTimes, out boneData.rotValuesX);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].RotationCurvesY[path],
                    out boneData.rotTimes, out boneData.rotValuesY);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].RotationCurvesZ[path],
                    out boneData.rotTimes, out boneData.rotValuesZ);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[0].RotationCurvesW[path],
                    out boneData.rotTimes, out boneData.rotValuesW);
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError(
                    $"KeyNotFoundException: The key '{path}' was not found in one of the dictionaries. " +
                    $"Please verify that 'curveHolder.PositionCurvesX' and 'curveHolder.RotationCurvesX' contain this key.");
            }

            boneArray[i] = boneData;
        }
        duration = holder[0].LengthInFrames;

        // Für Merging
        if (holder.Length == 2)
        {
            for (int i = 0; i < pose.Count; i++)
            {
                string path = _jobBoneData.Paths[pose[i]];

                if (!holder[1].PositionCurvesX.ContainsKey(path)) continue;

                AnimationJobManager.BoneAnimationData boneData = new AnimationJobManager.BoneAnimationData
                {
                    handle = _jobBoneData.Transforms[path],
                    posKeyCount = holder[1].PositionCurvesX[path].Keyframes.Count,
                    rotKeyCount = holder[1].RotationCurvesX[path].Keyframes.Count,
                    BoneName = new NativeArray<char>(pose[i].Name.ToCharArray(), Allocator.Persistent)
                };

                // Position
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].PositionCurvesX[path],
                    out boneData.posTimes, out boneData.posValuesX);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].PositionCurvesY[path],
                    out boneData.posTimes, out boneData.posValuesY);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].PositionCurvesZ[path],
                    out boneData.posTimes, out boneData.posValuesZ);

                // Rotation
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].RotationCurvesX[path],
                    out boneData.rotTimes, out boneData.rotValuesX);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].RotationCurvesY[path],
                    out boneData.rotTimes, out boneData.rotValuesY);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].RotationCurvesZ[path],
                    out boneData.rotTimes, out boneData.rotValuesZ);
                AnimationJobManager.ConvertCustomCurveToNativeArrays(holder[1].RotationCurvesW[path],
                    out boneData.rotTimes, out boneData.rotValuesW);

                boneArray[i] = boneData;
            }
            duration += holder[1].LengthInFrames;
        }

        loopType = holder[0].LoopType;
        behavior = holder[0].Behavior;
        this.duration = duration;
        job = new AnimationJobManager.AnimationCurveJob()
        {
            bones = boneArray,
            duration = duration,
            LoopType = loopType,
            Behavior = behavior,
            IgnoreTranslation = IgnoreTranslation,
            BckName = new NativeArray<char>(BckName.ToCharArray(), Allocator.Persistent),
            Time = new NativeArray<float>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
            NormalizedTime = new NativeArray<float>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
        };

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            try
            {
                _scriptPlayable.SetJobData(job);
            }
            catch (ArgumentNullException e)
            {
                
            }
        });

        UpdateJob = true;
    }
    catch (Exception e)
    {
        Debug.LogError(e);
    }
}


        public void SetInPlace(bool inPlace)
        {
            InPlace = inPlace;
            job.inPlace = inPlace;
        }
        
        public void AddEvent(int frame, Action callback)
        {
            Events.Add(new AnimationEvent { Frame = frame, Callback = callback });
            Events.Sort((a, b) => a.Frame.CompareTo(b.Frame)); // Optional, falls Events geordnet sein sollen
        }

        public override void Destroy() {
            base.Destroy();
            if (animancer == null) return;
            animancer.Graph.CancelPreUpdate(this);
        }

        public override AnimancerState Clone(CloneContext context)
        {
            return this;
        }

        protected override void CreatePlayable(out Playable playable)
        {
            _scriptPlayable = AnimationScriptPlayable.Create(Graph, job);
            playable = _scriptPlayable;
            
            if (UpdatableIndex == IUpdatable.List.NotInList)
            {
                animancer.Graph.RequirePreUpdate(this);
            }
        }

        public int UpdatableIndex { get; set; } = IUpdatable.List.NotInList;

        public bool AllowAnimation = true;
        public Vector3 PlayerPosition;
        
        private int lastTriggeredFrame = -1;
        private bool UpdateJob = false;
        public void Update()
        {
            //job.Time[0] = CurrentFrame / BCK.kAnimFramerate;
            //Debug.Log(job.Time[0] + " :: " + duration);
            //return;

            if (!UpdateJob) return;
            if (!IsCurrent)
            {
                return;
            }
            
            job.SingleBoneConstraint = SingleBoneConstraint;
            
            if (!AllowAnimation)
            {
                return;
            }

            if (Time > duration / BCK.kAnimFramerate && loopType == LoopType.Loop)
            {
                Time = 0f;
            }
            
            //Debug.Log("Updating " + BckName);
            //var jobData = _scriptPlayable.GetJobData<AnimationJobManager.AnimationCurveJob>();
            //jobData.BckName = BckName;
            job.Time[0] = Time * ZeldaManager.Instance.Speed;
            job.NormalizedTime[0] = NormalizedTime;

            //job.Time[0] = Time;
            //job.NormalizedTime[0] = NormalizedTime;
            
            // Update position if in place
            if (InPlace)
            {
                float actualTime = job.Time[0] * BCK.kAnimFramerate;
                if (actualTime < job.duration)
                {
                    for (int i = 0; i < BoneArray.Length; i++)
                    {
                        var bone = BoneArray[i];

                        string boneNameString = new string(bone.BoneName.ToArray());
                        if (boneNameString.Equals("center")) 
                        {
                            // Berechnung für alle drei Achsen: X, Y, Z
                            float startPosX = bone.posValuesX[0];
                            float startPosY = bone.posValuesY[0];
                            float startPosZ = bone.posValuesZ[0];

                            float dynamicOffsetX = startPosX / 100;
                            float dynamicOffsetY = startPosY / 100;
                            float dynamicOffsetZ = startPosZ / 100;

                            float posX = job.EvaluateCurve(actualTime, bone.posTimes, bone.posValuesX, bone.posKeyCount);
                            float posY = job.EvaluateCurve(actualTime, bone.posTimes, bone.posValuesY, bone.posKeyCount);
                            float posZ = job.EvaluateCurve(actualTime, bone.posTimes, bone.posValuesZ, bone.posKeyCount);

                            Vector3 currentPosition = PlayerPosition;

// Berechnung der neuen Position
                            //currentPosition.x = posX / 100;
                            //currentPosition.x -= dynamicOffsetX;
                            //currentPosition.x += PlayerPosition.x;

                            currentPosition.y = posY / 100;
                            currentPosition.y -= dynamicOffsetY;
                            currentPosition.y += PlayerPosition.y;

                            Vector3 forwardDirection = Link.Instance.PlayerController.transform.forward.normalized;
                            float forwardOffsetZ = (posZ / 100 + dynamicOffsetZ);
                            currentPosition += forwardDirection * forwardOffsetZ;

// Setzen der aktualisierten Position
                            Link.Instance.PlayerController.transform.localPosition = currentPosition;

                        }
                    }
                }
                /*else
                {
                    job.Time[0] = 0;
                    job.NormalizedTime[0] = 0;
                }*/
            }

            // Berechne den aktuellen Frame basierend auf der Zeit
            int currentFrame = Mathf.FloorToInt(Time * BCK.kAnimFramerate);

            // Überprüfe, ob ein Event für den aktuellen Frame registriert ist
            foreach (var animationEvent in Events)
            {
                if (animationEvent.Frame > lastTriggeredFrame && animationEvent.Frame <= currentFrame)
                {
                    animationEvent.Callback?.Invoke();
                }
            }

            lastTriggeredFrame = currentFrame;
            
            if (loopType == LoopType.Once && NormalizedTime >= 1)
            {
                //Link.PlayIdleAnimation();
                //Destroy();
            }

            if (loopType == LoopType.Once && NormalizedTime >= 1 && behavior == AnimationBehavior.GoBack)
            {
                Link link = animancer.transform.parent.GetComponent<Link>();
                if (link != null)
                {
                    Link.PlayIdleAnimation();
                }
                else
                {
                    Stop();

                }
                
            }
        }
        
        public override float Length => duration;
    }

public class AnimationEvent
{
    public int Frame; // Der Frame, an dem das Event ausgelöst wird
    public Action Callback; // Der Code, der ausgeführt wird
}

// Enum Definition
public enum SingleBoneConstraint
{
    [BoneName("###")]
    All,
    [BoneName("Chest Bone")]
    Chest,
    [BoneName("Head Bone")]
    Head,
    [BoneName("Right Shoulder")]
    ShoulderR,
    [BoneName("Right Upper Arm")]
    UpperArmR,
    [BoneName("Right Lower Arm")]
    LowerArmR,
    [BoneName("Right Hand")]
    HandR,
    [BoneName("Left Shoulder")]
    ShoulderL,
    [BoneName("Left Upper Arm")]
    UpperArmL,
    [BoneName("Left Lower Arm")]
    LowerArmL,
    [BoneName("Left Hand")]
    HandL,
    [BoneName("Left Upper Leg")]
    UpperLegL,
    [BoneName("Left Lower Leg")]
    LowerLegL,
    [BoneName("Left Foot")]
    FootL,
    [BoneName("Right Upper Leg")]
    UpperLegR,
    [BoneName("Right Lower Leg")]
    LowerLegR,
    [BoneName("Right Foot")]
    FootR
}

// Custom Attribute Definition
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class BoneNameAttribute : Attribute
{
    public string Name { get; }

    public BoneNameAttribute(string name)
    {
        Name = name;
    }
}

public class JobBoneData
{
    public Dictionary<SkeletonJoint, string> Paths = new Dictionary<SkeletonJoint, string>();
    public Dictionary<string, TransformStreamHandle> Transforms = new Dictionary<string, TransformStreamHandle>();
}

public class CustomCurve
{
    public List<CustomKeyframe> Keyframes { get; private set; } = new List<CustomKeyframe>();

    public void AddKey(float time, float value)
    {
        Keyframes.Add(new CustomKeyframe { Time = time, Value = value });
        Keyframes.Sort((a, b) => a.Time.CompareTo(b.Time)); // Stellen Sie sicher, dass die Keyframes sortiert sind
    }

    public float Evaluate(float time)
    {
        if (Keyframes.Count == 0) return 0f;

        // Suchen Sie die benachbarten Keyframes
        CustomKeyframe prev = Keyframes[0];
        CustomKeyframe next = Keyframes[Keyframes.Count - 1];

        foreach (var key in Keyframes)
        {
            if (key.Time > time)
            {
                next = key;
                break;
            }
            prev = key;
        }

        // Lineare Interpolation
        float t = (time - prev.Time) / (next.Time - prev.Time);
        return Mathf.Lerp(prev.Value, next.Value, t);
    }
}

public class CustomKeyframe
{
    public float Time { get; set; }
    public float Value { get; set; }
}
