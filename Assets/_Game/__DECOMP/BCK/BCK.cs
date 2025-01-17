using GameFormatReader.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Quaternion = OpenTK.Quaternion;
using Vector3 = OpenTK.Vector3;

namespace JStudio.J3D.Animation
{
    public class BCK : BaseJ3DAnimation
    {
        public class JointAnim
        {
            public List<Key> ScalesX = new List<Key>();
            public List<Key> ScalesY = new List<Key>();
            public List<Key> ScalesZ = new List<Key>();

            public List<Key> RotationsX = new List<Key>();
            public List<Key> RotationsY = new List<Key>();
            public List<Key> RotationsZ = new List<Key>();

            public List<Key> TranslationsX = new List<Key>();
            public List<Key> TranslationsY = new List<Key>();
            public List<Key> TranslationsZ = new List<Key>();
        }

        public int BoneCount { get { return m_animationData.Count; } }

        public List<JointAnim> m_animationData;

        private string _animName;

        public BCK(string name) : base(name)
        {
            _animName = name;
        }

        public BMD model;
        public bool IsLoaded;

        public void LoadFromStream(EndianBinaryReader reader, BMD model, bool enqueue)
        {
            this.model = model;
            try
            {
                Magic = new string(reader.ReadChars(4));
                AnimType = new string(reader.ReadChars(4));
            }
            catch (ArgumentException e)
            {
                Debug.Log("Fehler beim Lesen des J3D Headers: " + e.Message);
                return;
            }

            int fileSize = reader.ReadInt32();
            int tagCount = reader.ReadInt32();


            reader.Skip(16);

            if (enqueue)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => LoadTagDataFromStream(reader, tagCount));
            }
            else
            {
                LoadTagDataFromStream(reader, tagCount);
            }
        }


        public void ApplyAnimationToPose(List<SkeletonJoint> pose, Dictionary<string, GameObject> jointGameObjects)
        {
            if (m_animationData == null) return;
            if (pose == null) return;

            if (pose.Count != m_animationData.Count)
                Debug.LogWarning("Mis-matched animation! Model: " + model.Name + ", animation=" + _animName);

            int numJoints = Math.Min(pose.Count, m_animationData.Count);

            float ftime = (m_timeSinceStartedPlaying * kAnimFramerate) % AnimLengthInFrames;

            for(int i = 0; i < numJoints; i++)
            {
                pose[i].Scale = new Vector3(GetAnimValue(m_animationData[i].ScalesX, ftime), GetAnimValue(m_animationData[i].ScalesY, ftime), GetAnimValue(m_animationData[i].ScalesZ, ftime));

                Vector3 rot = new Vector3(GetAnimValue(m_animationData[i].RotationsX, ftime), GetAnimValue(m_animationData[i].RotationsY, ftime), GetAnimValue(m_animationData[i].RotationsZ, ftime));
                pose[i].Rotation = Quaternion.FromAxisAngle(new Vector3(0, 0, 1), WMath.DegreesToRadians(rot.Z)) *
                                   Quaternion.FromAxisAngle(new Vector3(0, 1, 0), WMath.DegreesToRadians(rot.Y)) *
                                   Quaternion.FromAxisAngle(new Vector3(1, 0, 0), WMath.DegreesToRadians(rot.X));

                Vector3 translation = new Vector3(GetAnimValue(m_animationData[i].TranslationsX, ftime), GetAnimValue(m_animationData[i].TranslationsY, ftime), GetAnimValue(m_animationData[i].TranslationsZ, ftime));
                pose[i].Translation = translation;
                
                GameObject jointGameObject = jointGameObjects[pose[i].Name];
                
                jointGameObject.transform.localPosition = new UnityEngine.Vector3(translation.X, translation.Y, translation.Z); 
                jointGameObject.transform.localRotation = new UnityEngine.Quaternion(pose[i].Rotation.X, pose[i].Rotation.Y, pose[i].Rotation.Z, pose[i].Rotation.W); 
                jointGameObject.transform.localScale = new UnityEngine.Vector3(pose[i].Scale.X, pose[i].Scale.Y, pose[i].Scale.Z);
                
            }
            
        }

        private bool loopCounter = false;
        public void ApplyPoseForSkinnedMeshRenderer(List<SkeletonJoint> pose, Transform[] bones)
        {
            if (m_animationData == null) return;
            if (pose == null) return;

            if (pose.Count != m_animationData.Count)
                Debug.LogWarning("Mis-matched animation! model=" + model.Name + ", animation=" + _animName);

            int numJoints = Math.Min(pose.Count, m_animationData.Count);

            float ftime = (m_timeSinceStartedPlaying * kAnimFramerate) % AnimLengthInFrames;
            
            float stiffnessFactor = ZeldaTP.Instance.AnimationStiffness;

            for(int i = 0; i < numJoints; i++)
            {
                pose[i].Scale = new Vector3(GetAnimValue(m_animationData[i].ScalesX, ftime), GetAnimValue(m_animationData[i].ScalesY, ftime), GetAnimValue(m_animationData[i].ScalesZ, ftime));

                Vector3 rot = new Vector3(GetAnimValue(m_animationData[i].RotationsX, ftime), GetAnimValue(m_animationData[i].RotationsY, ftime), GetAnimValue(m_animationData[i].RotationsZ, ftime));
                pose[i].Rotation = Quaternion.FromAxisAngle(new Vector3(0, 0, 1), WMath.DegreesToRadians(rot.Z)) *
                                   Quaternion.FromAxisAngle(new Vector3(0, 1, 0), WMath.DegreesToRadians(rot.Y)) *
                                   Quaternion.FromAxisAngle(new Vector3(1, 0, 0), WMath.DegreesToRadians(rot.X));

                Vector3 translation = new Vector3(GetAnimValue(m_animationData[i].TranslationsX, ftime), GetAnimValue(m_animationData[i].TranslationsY, ftime), GetAnimValue(m_animationData[i].TranslationsZ, ftime));
                pose[i].Translation = translation;
                
                Transform bone = null;

                foreach (Transform b in bones)
                {
                    if (b.name.Equals(pose[i].Name)) bone = b;
                }



                Joint joint = bone.GetComponent<Joint>();

                UnityEngine.Vector3 targetPosition = new UnityEngine.Vector3(translation.X, translation.Y, translation.Z);
                UnityEngine.Quaternion targetRotation = new UnityEngine.Quaternion(pose[i].Rotation.X, pose[i].Rotation.Y, pose[i].Rotation.Z, pose[i].Rotation.W);

                bone.transform.localPosition = UnityEngine.Vector3.Lerp(bone.transform.localPosition, targetPosition, stiffnessFactor);

                bone.transform.localRotation = UnityEngine.Quaternion.Slerp(bone.transform.localRotation, targetRotation, stiffnessFactor);

                bone.transform.localScale = new UnityEngine.Vector3(pose[i].Scale.X, pose[i].Scale.Y, pose[i].Scale.Z);

                bone.transform.localPosition += joint.OverridePosition;
                bone.transform.localEulerAngles += joint.OverrideRotation;
                bone.transform.localScale += joint.OverrideScale;
                
                Debug.Log(bone);
            }
        }

        private void LoadTagDataFromStream(EndianBinaryReader reader, int tagCount)
        {
            for (int i = 0; i < tagCount; i++)
            {
                long tagStart = reader.BaseStream.Position;

                string tagName = reader.ReadString(4);
                int tagSize = reader.ReadInt32();

                switch (tagName)
                {
                    case "ANK1":

                            LoadANK1FromStream(reader, tagStart);

                        break;
                    default:
                        Debug.LogError("Unsupported section in BCK File: " + tagName); break;
                }

                reader.BaseStream.Position = tagStart + tagSize;
            }
            IsLoaded = true;
        }

        private void LoadANK1FromStream(EndianBinaryReader reader, long tagStart)
        {
            try
            {
                LoopMode = (LoopType)reader.ReadByte();
                byte angleMultiplier = reader.ReadByte();
                AnimLengthInFrames = reader.ReadInt16();
                short jointEntryCount = reader.ReadInt16();
                short numScaleFloatEntries = reader.ReadInt16();
                short numRotationShortEntries = reader.ReadInt16();
                short numTranslateFloatEntries = reader.ReadInt16();
                int jointDataOffset = reader.ReadInt32();
                int scaleDataOffset = reader.ReadInt32();
                int rotationDataOffset = reader.ReadInt32();
                int translateDataOffset = reader.ReadInt32();

                float[] scaleData = new float[numScaleFloatEntries];
                reader.BaseStream.Position = tagStart + scaleDataOffset;
                for (int j = 0; j < numScaleFloatEntries; j++)
                    scaleData[j] = reader.ReadSingle();

                float[] rotationData = new float[numRotationShortEntries];
                reader.BaseStream.Position = tagStart + rotationDataOffset;
                for (int j = 0; j < numRotationShortEntries; j++)
                    rotationData[j] = reader.ReadInt16();

                float[] translationData = new float[numTranslateFloatEntries];
                reader.BaseStream.Position = tagStart + translateDataOffset;
                for (int j = 0; j < numTranslateFloatEntries; j++)
                    translationData[j] = reader.ReadSingle();

                m_animationData = new List<JointAnim>();
                float rotScale = (float)Math.Pow(2f, angleMultiplier) * (180 / 32768f);

                reader.BaseStream.Position = tagStart + jointDataOffset;
                for (int j = 0; j < jointEntryCount; j++)
                {
                    AnimatedJoint animatedJoint = ReadAnimJoint(reader);
                    JointAnim joint = new JointAnim();

                    joint.ScalesX = ReadComp(scaleData, animatedJoint.X.Scale);
                    joint.ScalesY = ReadComp(scaleData, animatedJoint.Y.Scale);
                    joint.ScalesZ = ReadComp(scaleData, animatedJoint.Z.Scale);

                    joint.RotationsX = ReadComp(rotationData, animatedJoint.X.Rotation);
                    joint.RotationsY = ReadComp(rotationData, animatedJoint.Y.Rotation);
                    joint.RotationsZ = ReadComp(rotationData, animatedJoint.Z.Rotation);

                    ConvertRotation(joint.RotationsX, rotScale);
                    ConvertRotation(joint.RotationsY, rotScale);
                    ConvertRotation(joint.RotationsZ, rotScale);

                    joint.TranslationsX = ReadComp(translationData, animatedJoint.X.Translation);
                    joint.TranslationsY = ReadComp(translationData, animatedJoint.Y.Translation);
                    joint.TranslationsZ = ReadComp(translationData, animatedJoint.Z.Translation);

                    m_animationData.Add(joint);
                }
            } catch(System.OverflowException)
            {
                Debug.LogError("OVERFLOW EXCEPTION WHILE PARSING ANIMATION");
            }
        }
    }
}