using System;
using System.Collections.Generic;
using UnityEngine;
using JStudio.J3D.Animation;
using Unity.Collections;
using Unity.Jobs;

public class AnimationSetup
{
    private List<AnimationCurveHolder> _animationCurveHolders = new List<AnimationCurveHolder>();

    public static string GetFullPath(SkeletonJoint joint)
    {
        if (joint.Parent == null)
        {
            return joint.Name;
        }
        else
        {
            return GetFullPath(joint.Parent) + "/" + joint.Name;
        }
    }

    public class AnimationCurveHolder
    {
        public float LengthInFrames;
        public string BckName;
        public LoopType LoopType;
        public AnimationBehavior Behavior;

        public Dictionary<string, CustomCurve> PositionCurvesX { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> PositionCurvesY { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> PositionCurvesZ { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> RotationCurvesX { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> RotationCurvesY { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> RotationCurvesZ { get; private set; } =
            new Dictionary<string, CustomCurve>();

        public Dictionary<string, CustomCurve> RotationCurvesW { get; private set; } =
            new Dictionary<string, CustomCurve>();
    }

    public AnimationCurveHolder CreatePlayableFromBCK(BMD bmd, BCK bck, string startBone = "##STOP##")
    {
        List<SkeletonJoint> pose = bmd.JNT1Tag.AnimatedJoints;
        if (bck.m_animationData == null) return null;

        int numJoints = Mathf.Min(pose.Count, bck.m_animationData.Count);
        AnimationCurveHolder animationData = new AnimationCurveHolder();
        animationData.LengthInFrames = bck.AnimLengthInFrames;
        animationData.BckName = bck.Name;
        animationData.LoopType = bck.LoopMode;

        for (int i = 0; i < numJoints; i++)
        {
            string boneName = pose[i].Name;
            if (startBone != "##STOP##" && !IsDescendantOf(pose[i], startBone)) continue;

            string path = GetFullPath(pose[i]);

            animationData.PositionCurvesX[path] = new CustomCurve();
            animationData.PositionCurvesY[path] = new CustomCurve();
            animationData.PositionCurvesZ[path] = new CustomCurve();
            animationData.RotationCurvesX[path] = new CustomCurve();
            animationData.RotationCurvesY[path] = new CustomCurve();
            animationData.RotationCurvesZ[path] = new CustomCurve();
            animationData.RotationCurvesW[path] = new CustomCurve();
        }
        for (int j = 0; j <= bck.AnimLengthInFrames; j++)
        {
            float ftime = j;

            for (int i = 0; i < numJoints; i++)
            {
                if (startBone != "##STOP##" && !IsDescendantOf(pose[i], startBone)) continue;

                var jointAnim = bck.m_animationData[i];
                string path = GetFullPath(pose[i]);

                OpenTK.Vector3 translation = new OpenTK.Vector3(
                    bck.GetAnimValue(jointAnim.TranslationsX, ftime),
                    bck.GetAnimValue(jointAnim.TranslationsY, ftime),
                    bck.GetAnimValue(jointAnim.TranslationsZ, ftime)
                );

                OpenTK.Vector3 rot = new OpenTK.Vector3(
                    bck.GetAnimValue(jointAnim.RotationsX, ftime),
                    bck.GetAnimValue(jointAnim.RotationsY, ftime),
                    bck.GetAnimValue(jointAnim.RotationsZ, ftime)
                );

                pose[i].Rotation =
                    OpenTK.Quaternion.FromAxisAngle(new OpenTK.Vector3(0, 0, 1), WMath.DegreesToRadians(rot.Z)) *
                    OpenTK.Quaternion.FromAxisAngle(new OpenTK.Vector3(0, 1, 0), WMath.DegreesToRadians(rot.Y)) *
                    OpenTK.Quaternion.FromAxisAngle(new OpenTK.Vector3(1, 0, 0), WMath.DegreesToRadians(rot.X));

                animationData.PositionCurvesX[path].AddKey(ftime, translation.X);
                animationData.PositionCurvesY[path].AddKey(ftime, translation.Y);
                animationData.PositionCurvesZ[path].AddKey(ftime, translation.Z);
                animationData.RotationCurvesX[path].AddKey(ftime, pose[i].Rotation.X);
                animationData.RotationCurvesY[path].AddKey(ftime, pose[i].Rotation.Y);
                animationData.RotationCurvesZ[path].AddKey(ftime, pose[i].Rotation.Z);
                animationData.RotationCurvesW[path].AddKey(ftime, pose[i].Rotation.W);
            }
        }

        return animationData;
    }

// Hilfsfunktion zur rekursiven Überprüfung, ob ein Knochen ein Nachfahre von startBone ist
    private static bool IsDescendantOf(SkeletonJoint joint, string startBone)
    {
        while (joint != null)
        {
            if (joint.Name == startBone)
                return true;

            joint = joint.Parent;
        }

        return false;
    }
}