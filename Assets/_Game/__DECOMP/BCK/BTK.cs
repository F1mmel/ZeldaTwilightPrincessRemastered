using GameFormatReader.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = OpenTK.Vector3;

namespace JStudio.J3D.Animation
{
    public class BTK : BaseJ3DAnimation
    {
        private class MaterialAnim
        {
            public Vector3 Center;
            public string Name;
            public int TexMatrixIndex;

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

        private List<MaterialAnim> m_animationData;
		private short[] m_remapTable;

		public BTK(string name) : base(name) { }

        public void LoadFromStream(EndianBinaryReader reader)
        {
            Magic = new string(reader.ReadChars(4));
            AnimType = new string(reader.ReadChars(4));

            int fileSize = reader.ReadInt32();
            int tagCount = reader.ReadInt32();

            reader.Skip(16);

            LoadTagDataFromFile(reader, tagCount);
        }

        private bool _playing = false;
        public void PlayBTK(BMD bmd)
        {
            _playing = true;
            Start();
            ZeldaManager.Instance.StartCoroutine(_PlayBTK(bmd.MAT3Tag, bmd));
        }

        public void StopBTK()
        {
            _playing = false;
            Stop();
        }

        private IEnumerator _PlayBTK(MAT3 pose, BMD bmd)
        {
            while (_playing)
            {
                ApplyAnimationToMaterials(pose, bmd);

                Tick(0.05f);

                yield return new WaitForSeconds(1f / 30f);
            }
        }

        private void UpdateMeshData(List<GameObject> children)
        {
            foreach (GameObject child in children)
            {
                MaterialData data = child.GetComponent<MaterialData>();
                Material3 mat = data.Material3;

                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                Material material = renderer.material;
                Mesh mesh = child.GetComponent<Mesh>();

                Vector2 textureTranslation = new Vector2(-mat.TexMatrixIndexes[0].TranslateS,
                    -mat.TexMatrixIndexes[0].TranslateT);

                material.mainTextureOffset = new Vector2(mat.TexMatrixIndexes[0].TranslateS,
                    mat.TexMatrixIndexes[0].TranslateT);
                material.mainTextureScale =
                    new Vector2(mat.TexMatrixIndexes[0].ScaleS, mat.TexMatrixIndexes[0].ScaleT);

                if (material.shader.name.Contains("OffsetParticle"))
                {
                    Vector4 v = new Vector4(textureTranslation.x, textureTranslation.y, textureTranslation.x, textureTranslation.y);
                    
                    material.SetVector("_MainTex_ST", v);
                    Debug.Log(v);
                }
                else
                {
                    material.SetVector("_MainOffset", textureTranslation);
                    material.SetVector("_BaseMap_ST", textureTranslation);
                }
            }
        }

        private MeshRenderer _renderer;
        public void ApplyAnimationToMaterials(MAT3 pose, BMD bmd)
        {
            if(_renderer == null) _renderer = bmd.GetComponent<MeshRenderer>();
            float ftime = (m_timeSinceStartedPlaying * kAnimFramerate) % AnimLengthInFrames;

            for (int i = 0; i < m_animationData.Count; i++)
            {
                Material3 mat = null;

                foreach (var materal in pose.MaterialList)
                {
                    if (materal.Name == m_animationData[i].Name)
                    {
                        mat = materal;
                        break;
                    }
                }

                if (mat == null)
                    continue;

				var texMatrixIndex = m_animationData[i].TexMatrixIndex;
                var texMatrix = mat.TexMatrixIndexes[texMatrixIndex];

                Vector3 scale = new Vector3(GetAnimValue(m_animationData[i].ScalesX, ftime), GetAnimValue(m_animationData[i].ScalesY, ftime), GetAnimValue(m_animationData[i].ScalesZ, ftime));
                Vector3 rot = new Vector3(GetAnimValue(m_animationData[i].RotationsX, ftime), GetAnimValue(m_animationData[i].RotationsY, ftime), GetAnimValue(m_animationData[i].RotationsZ, ftime));
                Vector3 translation = new Vector3(GetAnimValue(m_animationData[i].TranslationsX, ftime), GetAnimValue(m_animationData[i].TranslationsY, ftime), GetAnimValue(m_animationData[i].TranslationsZ, ftime));

                texMatrix.CenterS = m_animationData[i].Center.X;
                texMatrix.CenterT = m_animationData[i].Center.Y;
                texMatrix.CenterW = m_animationData[i].Center.Z;

                texMatrix.ScaleS = scale.X;
                texMatrix.ScaleT = scale.Y;

                texMatrix.Rotation = rot.X;

                texMatrix.TranslateS = translation.X;
                texMatrix.TranslateT = translation.Y;

                Material m = null;
                TexMatrix[] matrices = null;
                foreach (Material material in _renderer.sharedMaterials)
                {
                    if (material.name == mat.Name)
                    {
                        m = material;
                        matrices = bmd.TexMatrixIndexes[_renderer.sharedMaterials.ToList().IndexOf(material)];
                    }
                }

                {
                    if(m == null) continue;
                        
                        var oriTexMatrix = matrices[texMatrixIndex];

                        Vector2 textureTranslation = new Vector2(-oriTexMatrix.TranslateS, -oriTexMatrix.TranslateT);

                        m.mainTextureOffset = new Vector2(oriTexMatrix.TranslateS, oriTexMatrix.TranslateT);
                        m.mainTextureScale = new Vector2(oriTexMatrix.ScaleS, oriTexMatrix.ScaleT);

                        if (m.shader.name.Contains("OffsetParticle"))
                        {
                            Vector4 v = new Vector4(oriTexMatrix.ScaleS, oriTexMatrix.ScaleT, textureTranslation.x, textureTranslation.y);

                            m.SetVector("_MainTex_ST", v);
                        }
                        else
                        {
                            m.SetVector("_MainOffset", textureTranslation);
                            m.SetVector("_BaseMap_ST", textureTranslation);
                        }


                }

			}
        }

        private void LoadTagDataFromFile(EndianBinaryReader reader, int tagCount)
        {
            for (int i = 0; i < tagCount; i++)
            {
                long tagStart = reader.BaseStream.Position;

                string tagName = reader.ReadString(4);
                int tagSize = reader.ReadInt32();

                switch (tagName)
                {
                    case "TTK1":
                        LoopMode = (LoopType)reader.ReadByte();
                        byte angleMultiplier = reader.ReadByte();
                        AnimLengthInFrames = reader.ReadInt16();
                        short textureAnimEntryCount = (short)(reader.ReadInt16() / 3);
                        short numScaleFloatEntries = reader.ReadInt16();
                        short numRotationShortEntries = reader.ReadInt16();
                        short numTranslateFloatEntries = reader.ReadInt16();
                        int animDataOffset = reader.ReadInt32();
                        int remapTableOffset = reader.ReadInt32();
                        int stringTableOffset = reader.ReadInt32();
                        int textureIndexTableOffset = reader.ReadInt32();
                        int textureCenterTableOffset = reader.ReadInt32();
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

                        m_remapTable = new short[textureAnimEntryCount];
                        reader.BaseStream.Position = tagStart + remapTableOffset;
                        for (int j = 0; j < textureAnimEntryCount; j++)
							m_remapTable[j] = reader.ReadInt16();

                        reader.BaseStream.Position = tagStart + stringTableOffset;
                        StringTable stringTable = StringTable.FromStream(reader);

                        byte[] texMtxIndexTable = new byte[textureAnimEntryCount];
                        reader.BaseStream.Position = tagStart + textureIndexTableOffset;
                        for (int j = 0; j < textureAnimEntryCount; j++)
                            texMtxIndexTable[j] = reader.ReadByte();

                        Vector3[] textureCenters = new Vector3[textureAnimEntryCount];
                        reader.BaseStream.Position = tagStart + textureCenterTableOffset;
                        for (int j = 0; j < textureAnimEntryCount; j++)
                            textureCenters[j] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                        m_animationData = new List<MaterialAnim>();
                        float rotScale = (float)Math.Pow(2f, angleMultiplier) * (180 / 32768f);

                        reader.BaseStream.Position = tagStart + animDataOffset;
                        for (int j = 0; j < textureAnimEntryCount; j++)
                        {
                            AnimComponent texU = ReadAnimComponent(reader);
                            AnimComponent texV = ReadAnimComponent(reader);
                            AnimComponent texW = ReadAnimComponent(reader);

                            MaterialAnim anim = new MaterialAnim();
                            anim.Name = stringTable[j];
                            anim.TexMatrixIndex = texMtxIndexTable[j];
                            anim.Center = textureCenters[j];

                            anim.ScalesX = ReadComp(scaleData, texU.Scale);
                            anim.ScalesY = ReadComp(scaleData, texV.Scale);
                            anim.ScalesZ = ReadComp(scaleData, texW.Scale);

                            anim.RotationsX = ReadComp(rotationData, texU.Rotation);
                            anim.RotationsY = ReadComp(rotationData, texV.Rotation);
                            anim.RotationsZ = ReadComp(rotationData, texW.Rotation);

                            ConvertRotation(anim.RotationsX, rotScale);
                            ConvertRotation(anim.RotationsY, rotScale);
                            ConvertRotation(anim.RotationsZ, rotScale);

                            anim.TranslationsX = ReadComp(translationData, texU.Translation);
                            anim.TranslationsY = ReadComp(translationData, texV.Translation);
                            anim.TranslationsZ = ReadComp(translationData, texW.Translation);

                            m_animationData.Add(anim);
                        }
                        break;
                }

                reader.BaseStream.Position = tagStart + tagSize;
            }
        }
    }
}
