using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class GMX
{
    private GMXHeader Header;

    public void LoadHeader(byte[] data)
    {
        Header = new GMXHeader();
        Header.Read(new FileReader(data));
    }

    // Start is called before the first frame update
    public void Create(Transform parent, List<Texture2D> textures, Material material)
    {
        int MeshIndex = 0;
        for (int i = 0; i < Header.Meshes.Count; i++)
        {
            if (Header.Meshes[i].VertexGroup != null)
            {
                List<GMXHeader.Vertex> vertices = Header.Meshes[i].VertexGroup.Vertices;
                
                GameObject meshObject = new GameObject("Mesh_" + i);
                meshObject.transform.SetParent(parent);
                meshObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

                Mesh mesh = new Mesh();

                // Vertices
                Vector3[] meshVertices = new Vector3[vertices.Count];
                for (int j = 0; j < vertices.Count; j++)
                {
                    meshVertices[j] = vertices[j].pos;
                }
                mesh.vertices = meshVertices;

                // Normals
                Vector3[] meshNormals = new Vector3[vertices.Count];
                for (int j = 0; j < vertices.Count; j++)
                {
                    meshNormals[j] = vertices[j].nrm;
                }
                mesh.normals = meshNormals;

                // UV
                Vector2[] meshUV = new Vector2[vertices.Count];
                for (int j = 0; j < vertices.Count; j++)
                {
                    meshUV[j] = vertices[j].uv0;
                }
                mesh.uv = meshUV;

                // Triangles
                int[] meshIndices = Array.ConvertAll(Header.Meshes[i].IndexGroup.Indices, item => (int)item);
                mesh.triangles = meshIndices;
                
                meshFilter.mesh = mesh;
                
                MaterialData data = meshObject.AddComponent<MaterialData>();
                data.TexturesIndexes = new short[] { (short)i };

                meshRenderer.material = new Material(material);
                meshRenderer.material.mainTexture = textures[i];

                /*if (i == 18)        // 10
                {
                    
                    Debug.LogError(Header.Meshes[i].VMapGroup.Indices.Length);
                    Debug.LogWarning(Header.Meshes[i].VMapGroup.Indices[0]);

                }
            

                if (i == 2) meshRenderer.material.mainTexture = textures[8];
                else if (i == 6) meshRenderer.material.mainTexture = textures[7];
                else if (i == 7) meshRenderer.material.mainTexture = textures[4];
                else if (i == 8) meshRenderer.material.mainTexture = textures[6];
                else if (i == 10) meshRenderer.material.mainTexture = textures[20];
                else if (i == 11) meshRenderer.material.mainTexture = textures[24];
                else if (i == 16) meshRenderer.material.mainTexture = textures[21];
                else if (i == 17) meshRenderer.material.mainTexture = textures[22];
                else if (i == 18) meshRenderer.material.mainTexture = textures[10];
                else if (i == 32) meshRenderer.material.mainTexture = textures[26];*/
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public class GMXHeader
    {
        public uint HeaderSize;

        public GMXHeader()
        {
            HeaderSize = 8;
        }

        public List<MESH> Meshes = new List<MESH>();

        public void Read(FileReader reader)
        {
            Debug.LogWarning("READ");
            reader.ReadSignature(4, "GMX2");
            reader.SetByteOrder(true);

            HeaderSize = reader.ReadUInt32();

            while (reader.Position < reader.BaseStream.Length)
            {
                long pos = reader.Position;

                string Magic = reader.ReadString(4);
                uint SectionSize = reader.ReadUInt32();

                reader.SeekBegin(pos);
                switch (Magic)
                {
                    case "PADX":
                        PADX padding = new PADX();
                        padding.Read(reader);
                        break;
                    case "INDX":
                        INDX indexGrp = new INDX();
                        indexGrp.Read(reader, GetLastMesh());
                        GetLastMesh().IndexGroup = indexGrp;
                        break;
                    case "VMAP":
                        VMAP vmap = new VMAP();
                        vmap.Read(reader);
                        GetLastMesh().VMapGroup = vmap;
                        break;
                    case "MESH":
                        MESH mesh = new MESH();
                        mesh.Read(reader);
                        Meshes.Add(mesh);
                        break;
                    case "VERT":
                        VERT vert = new VERT();
                        vert.Read(reader, GetLastMesh());
                        GetLastMesh().VertexGroup = vert;
                        break;
                    case "ENDX":
                        reader.ReadSignature(4, "ENDX");
                        uint EndSectionSize = reader.ReadUInt32();
                        break;
                    default:
                        throw new Exception("Unknown section! " + Magic);
                }

                reader.SeekBegin(pos + SectionSize);
            }
        }

        public MESH GetLastMesh()
        {
            return Meshes[Meshes.Count - 1];
        }

        public class Vertex
        {
            public Vector3 pos = Vector3.zero;
            public Vector3 nrm = Vector3.zero;
            public Vector4 col = Vector4.one;
            public Vector4 col2 = Vector4.one;

            public Vector2 uv0 = Vector2.zero;
            public Vector2 uv1 = Vector2.zero;
            public Vector2 uv2 = Vector2.zero;
            public Vector2 uv3 = Vector2.zero;

            public Vector4 tan = Vector4.zero;
            public Vector4 bitan = Vector4.zero;

            public List<int> boneIds = new List<int>();
            public List<float> boneWeights = new List<float>();

            public float normalW = 1;

            public List<string> boneNames = new List<string>();

            public List<Bone> boneList = new List<Bone>();

            public class Bone
            {
                public string Name;
                public int Index;
                public bool HasWeights;
                public List<BoneWeight> weights = new List<BoneWeight>();
            }

            public class BoneWeight
            {
                public float weight;
            }

            //For vertex morphing 
            public Vector3 pos1 = new Vector3();
            public Vector3 pos2 = new Vector3();

            public List<Vector4> Unknowns = new List<Vector4>();
        }

        public class VERT
        {
            public List<Vertex> Vertices = new List<Vertex>();
            public List<uint> Unknowns = new List<uint>();

            public void Read(FileReader reader, MESH mesh)
            {
                reader.ReadSignature(4, "VERT");
                uint SectionSize = reader.ReadUInt32();

                if (mesh.VertexSize == 32)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        vert.pos = reader.ReadVec3();
                        vert.nrm = reader.ReadVec3();
                        vert.uv0 = reader.ReadVec2();
                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 12)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        vert.pos = reader.ReadVec3();
                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 20)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        vert.pos = reader.ReadVec3();
                        vert.uv0 = reader.ReadVec2();
                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 24)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        vert.pos = reader.ReadVec3();
                        vert.nrm = reader.ReadVec3();
                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 36)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        if (mesh.Unknown1 != 0)
                        {
                            uint Unknown = reader.ReadUInt32(); //Bone index?
                            vert.pos = reader.ReadVec3();
                            vert.nrm = reader.ReadVec3();
                            vert.uv0 = reader.ReadVec2();
                            Unknowns.Add(Unknown);
                        }
                        else
                        {
                            vert.pos = reader.ReadVec3();
                            vert.nrm = reader.ReadVec3();
                            vert.col = ColorUtility.ToVector4(reader.ReadBytes(4));
                            vert.uv0 = reader.ReadVec2();
                        }

                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 40)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        if (mesh.Unknown1 != 0)
                        {
                            uint Unknown = reader.ReadUInt32(); //Bone index?
                            vert.pos = reader.ReadVec3();
                            vert.nrm = reader.ReadVec3();
                            vert.col = ColorUtility.ToVector4(reader.ReadBytes(4));
                            vert.uv0 = reader.ReadVec2();
                        }
                        else
                            throw new Exception($"Unsupported Vertex Size {mesh.VertexSize}");

                        Vertices.Add(vert);
                    }
                }
                else if (mesh.VertexSize == 44)
                {
                    for (int v = 0; v < mesh.VertexCount; v++)
                    {
                        Vertex vert = new Vertex();
                        vert.pos = reader.ReadVec3();
                        vert.nrm = reader.ReadVec3();
                        vert.col = ColorUtility.ToVector4(reader.ReadBytes(4));
                        vert.uv0 = reader.ReadVec2();
                        vert.uv1 = reader.ReadVec2();

                        Vertices.Add(vert);
                    }
                }
                else
                    throw new Exception($"Unsupported Vertex Size {mesh.VertexSize}");
            }
        }

        public class MESH
        {
            public INDX IndexGroup { get; set; }
            public VERT VertexGroup { get; set; }
            public VMAP VMapGroup { get; set; }

            public ushort VertexSize { get; set; }
            public ushort VertexCount { get; set; }
            public uint FaceCount { get; set; }

            public uint Unknown { get; set; }
            public uint Unknown1 { get; set; }
            public uint Unknown2 { get; set; }
            public uint Unknown3 { get; set; }

            public void Read(FileReader reader)
            {
                reader.ReadSignature(4, "MESH");
                uint SectionSize = reader.ReadUInt32();
                uint Padding = reader.ReadUInt32();
                VertexSize = reader.ReadUInt16();
                VertexCount = reader.ReadUInt16();
                uint Padding2 = reader.ReadUInt32();
                FaceCount = reader.ReadUInt32();
                Unknown = reader.ReadUInt32();
                Unknown1 = reader.ReadUInt32();
                Unknown2 = reader.ReadUInt32();
                Unknown3 = reader.ReadUInt32();
            }

            public void Write(FileWriter writer)
            {
                writer.WriteSignature("MESH");
                writer.Write(40);
                writer.Write(0);
                writer.Write(VertexSize);
                writer.Write(VertexCount);
                writer.Write(0);
                writer.Write(FaceCount);
                writer.Write(Unknown);
                writer.Write(Unknown1);
                writer.Write(Unknown2);
                writer.Write(Unknown3);
            }
        }

        public class INDX
        {
            public ushort[] Indices;

            public void Read(FileReader reader, MESH mesh)
            {
                reader.ReadSignature(4, "INDX");
                uint SectionSize = reader.ReadUInt32();

                Indices = new ushort[mesh.FaceCount];
                for (int i = 0; i < mesh.FaceCount; i++)
                {
                    Indices[i] = reader.ReadUInt16();
                }
            }

            public void Write(FileWriter writer)
            {
                writer.WriteSignature("INDX");
                writer.Write(Indices.Length * sizeof(ushort) + 8);
                for (int i = 0; i < Indices.Length; i++)
                    writer.Write(Indices[i]);
            }
        }

        public class VMAP
        {
            public ushort[] Indices;

            public void Read(FileReader reader)
            {
                reader.ReadSignature(4, "VMAP");
                uint SectionSize = reader.ReadUInt32();
                uint FaceCount = (SectionSize - 8) / sizeof(ushort);

                Indices = new ushort[FaceCount];
                for (int i = 0; i < FaceCount; i++)
                {
                    Indices[i] = reader.ReadUInt16();
                }
            }

            public void Write(FileWriter writer)
            {
                writer.WriteSignature("VMAP");
                writer.Write(Indices.Length * sizeof(ushort) + 8);
                for (int i = 0; i < Indices.Length; i++)
                    writer.Write(Indices[i]);
            }
        }

        public class PADX
        {
            public void Read(FileReader reader)
            {
                reader.ReadSignature(4, "PADX");
                uint SectionSize = reader.ReadUInt32();
            }

            public void Write(FileWriter writer, uint Alignment)
            {
                long pos = writer.Position;

                //Check if alignment is needed first!
                using (writer.TemporarySeek(pos + 8, System.IO.SeekOrigin.Begin))
                {
                    var startPos = writer.Position;
                    long position = writer.Seek((-writer.Position % Alignment + Alignment) % Alignment,
                        System.IO.SeekOrigin.Current);

                    if (startPos == position)
                        return;
                }

                writer.WriteSignature("PADX");
                writer.Write(uint.MaxValue);
                Align(writer, (int)Alignment);

                long endPos = writer.Position;
                using (writer.TemporarySeek(pos + 4, System.IO.SeekOrigin.Begin))
                {
                    writer.Write((uint)(endPos - pos));
                }
            }

            private void Align(FileWriter writer, int alignment)
            {
                var startPos = writer.Position;
                long position = writer.Seek((-writer.Position % alignment + alignment) % alignment,
                    System.IO.SeekOrigin.Current);

                writer.Seek(startPos, System.IO.SeekOrigin.Begin);
                while (writer.Position != position)
                {
                    writer.Write(byte.MaxValue);
                }
            }
        }

        public class ColorUtility
        {
            public static Vector3 ToVector3(Color color)
            {
                return new Vector3(color.r / 255.0f,
                    color.g / 255.0f,
                    color.b / 255.0f);
            }

            public static Vector4 ToVector4(byte[] color)
            {
                if (color == null || color.Length != 4)
                    throw new Exception("Invalid color length found! (ToVector4)");

                return new Vector4(color[0] / 255.0f,
                    color[1] / 255.0f,
                    color[2] / 255.0f,
                    color[3] / 255.0f);
            }

            public static Vector4 ToVector4(Color color)
            {
                return new Vector4(color.r / 255.0f,
                    color.g / 255.0f,
                    color.b / 255.0f,
                    color.a / 255.0f);
            }
        }
    }
}