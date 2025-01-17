using GameFormatReader.Common;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JStudio.J3D;
using Debug = UnityEngine.Debug;


public class ShapeVertexAttribute
    {
        public VertexArrayType ArrayType;
        public VertexDataType DataType;

        public ShapeVertexAttribute(VertexArrayType arrayType, VertexDataType dataType)
        {
            ArrayType = arrayType;
            DataType = dataType;
        }

        public override string ToString()
        {
            return string.Format("ArrayType: {0} DataType: {1}", ArrayType, DataType);
        }
    }

    public class SHP1
    {
        public class Shape
        {
            public class SkinDataTable
            {
                public int Unknown0 { get; private set; }
                public int FirstRelevantVertexIndex;
                public int LastRelevantVertexIndex;
                public List<ushort> MatrixTable { get; private set; }

                public SkinDataTable(int unknown0)
                {
                    Unknown0 = unknown0;
                    MatrixTable = new List<ushort>();
                }
            }

            public byte MatrixType { get; set; }
            public float BoundingSphereDiameter { get; set; }
            public FAABox BoundingBox { get; set; }
            public List<ShapeVertexAttribute> Attributes { get; internal set; }
            public MeshVertexHolder VertexData { get; internal set; }
            public List<int> Indexes { get; internal set; }
            public VertexDescription VertexDescription { get; private set; }
            public List<Vector3> OverrideVertPos { get; set; }
            public List<Vector3> OverrideNormals { get; set; }

            public List<SkinDataTable> MatrixDataTable { get; private set; }

            public int[] m_glBufferIndexes;
            public int m_glIndexBuffer;

            private bool m_hasBeenDisposed = false;

            public int MaterialIndex = -1;

            public Shape()
            {
                Attributes = new List<ShapeVertexAttribute>();
                VertexData = new MeshVertexHolder();
                Indexes = new List<int>();
                VertexDescription = new VertexDescription();
                MatrixDataTable = new List<SkinDataTable>();
                OverrideVertPos = new List<Vector3>();
                OverrideNormals = new List<Vector3>();

                m_glBufferIndexes = new int[15];
                for (int i = 0; i < m_glBufferIndexes.Length; i++)
                    m_glBufferIndexes[i] = -1;

            }

            public void UploadBuffersToGPU(bool onlyOverrides)
            {
            }


        }

        public short ShapeCount { get; private set; }
        public List<Shape> Shapes { get; private set; }
        public List<short> ShapeRemapTable;

        public SHP1()
        {
            Shapes = new List<Shape>();
        }

        public void ReadSHP1FromStream(EndianBinaryReader reader, long tagStart, MeshVertexHolder compressedVertexData)
        {
            ShapeCount = reader.ReadInt16();
            Trace.Assert(reader.ReadUInt16() == 0xFFFF);
            int shapeOffset = reader.ReadInt32();

            int remapTableOffset = reader.ReadInt32();

            Trace.Assert(reader.ReadInt32() == 0);
            int attributeOffset = reader.ReadInt32();
            
            int matrixTableOffset = reader.ReadInt32();

            int primitiveDataOffset = reader.ReadInt32();
            int matrixDataOffset = reader.ReadInt32();
            int packetLocationOffset = reader.ReadInt32();

            reader.BaseStream.Position = tagStart + remapTableOffset;
            ShapeRemapTable = new List<short>();
            for (int i = 0; i < ShapeCount; i++)
                ShapeRemapTable.Add(reader.ReadInt16());

            for (int s = 0; s < ShapeCount; s++)
            {

                reader.BaseStream.Position = tagStart + shapeOffset + (0x28 * s)  ;
            
                long shapeStart = reader.BaseStream.Position;
                Shape shape = new Shape();
                shape.MatrixType = reader.ReadByte();
                reader.ReadByte();

                ushort packetCount = reader.ReadUInt16();

                ushort batchAttributeOffset = reader.ReadUInt16();

                ushort firstMatrixIndex = reader.ReadUInt16();
                ushort firstPacketIndex = reader.ReadUInt16();
                Trace.Assert(reader.ReadUInt16() == 0xFFFF);

                float boundingSphereDiameter = reader.ReadSingle();
                Vector3 bboxMin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Vector3 bboxMax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                reader.BaseStream.Position = tagStart + attributeOffset + batchAttributeOffset;
                List<ShapeVertexAttribute> attributes = new List<ShapeVertexAttribute>();
                do
                {
                    ShapeVertexAttribute attribute = new ShapeVertexAttribute((VertexArrayType)reader.ReadInt32(), (VertexDataType)reader.ReadInt32());
                    if (attribute.ArrayType == VertexArrayType.NullAttr) 
                        break;

                    attributes.Add(attribute);
                } while (true);

                shape.BoundingSphereDiameter = boundingSphereDiameter;
                shape.BoundingBox = new FAABox(bboxMin, bboxMax);
                shape.Attributes = attributes;
                Shapes.Add(shape);

                int numVertexRead = 0;
                for (ushort p = 0; p < packetCount; p++)
                {
                    reader.BaseStream.Position = tagStart + packetLocationOffset + ((firstPacketIndex + p) * 0x8);  

                    int packetSize = reader.ReadInt32();
                    int packetOffset = reader.ReadInt32();

                    reader.BaseStream.Position = tagStart + matrixDataOffset + (firstMatrixIndex + p) * 0x08;  
                    ushort matrixUnknown0 = reader.ReadUInt16();
                    ushort matrixCount = reader.ReadUInt16();
                    uint matrixFirstIndex = reader.ReadUInt32();

                    Shape.SkinDataTable matrixData = new Shape.SkinDataTable(matrixUnknown0);
                    shape.MatrixDataTable.Add(matrixData);
                    matrixData.FirstRelevantVertexIndex = shape.VertexData.Position.Count;

                    reader.BaseStream.Position = tagStart + matrixTableOffset + (matrixFirstIndex * 0x2);  
                    for (int m = 0; m < matrixCount; m++)
                        matrixData.MatrixTable.Add(reader.ReadUInt16());

                    reader.BaseStream.Position = tagStart + primitiveDataOffset + packetOffset;

                    uint numPrimitiveBytesRead = 0;
                    while(numPrimitiveBytesRead < packetSize)
                    {
                        GXPrimitiveType type = (GXPrimitiveType)reader.ReadByte();
                        if (type == 0 || numPrimitiveBytesRead >= packetSize)
                            break;

                        ushort vertexCount = reader.ReadUInt16();
                        numPrimitiveBytesRead += 0x3;

                        List<MeshVertexIndex> primitiveVertices = new List<MeshVertexIndex>();

                        for(int v = 0; v < vertexCount; v++)
                        {
                            MeshVertexIndex newVert = new MeshVertexIndex();
                            primitiveVertices.Add(newVert);

                            foreach (ShapeVertexAttribute curAttribute in attributes)
                            {
                                int index = 0;
                                uint numBytesRead = 0;

                                switch (curAttribute.DataType)
                                {
                                    case VertexDataType.Unsigned8:
                                    case VertexDataType.Signed8:
                                        index = reader.ReadByte();
                                        numBytesRead = 1;
                                        break;
                                    case VertexDataType.Unsigned16:
                                    case VertexDataType.Signed16:
                                        index = reader.ReadUInt16();
                                        numBytesRead = 2;
                                        break;
                                    case VertexDataType.Float32:
                                    case VertexDataType.None:
                                    default:
                                        System.Console.WriteLine("Unknown Data Type {0} for ShapeAttribute!", curAttribute.DataType);
                                        break;
                                }

                                switch (curAttribute.ArrayType)
                                {
                                    case VertexArrayType.Position: newVert.Position = index; break;
                                    case VertexArrayType.PositionMatrixIndex: newVert.PosMtxIndex = index; break;
                                    case VertexArrayType.Normal: newVert.Normal = index; break;
                                    case VertexArrayType.Color0: newVert.Color0 = index; break;
                                    case VertexArrayType.Color1: newVert.Color1 = index; break;
                                    case VertexArrayType.Tex0:  newVert.Tex0 = index; break;
                                    case VertexArrayType.Tex1:  newVert.Tex1 = index; break;
                                    case VertexArrayType.Tex2:  newVert.Tex2 = index; break;
                                    case VertexArrayType.Tex3:  newVert.Tex3 = index; break;
                                    case VertexArrayType.Tex4:  newVert.Tex4 = index; break;
                                    case VertexArrayType.Tex5:  newVert.Tex5 = index; break;
                                    case VertexArrayType.Tex6:  newVert.Tex6 = index; break;
                                    case VertexArrayType.Tex7:  newVert.Tex7 = index; break;
                                    default:
                                        System.Console.WriteLine("Unsupported ArrayType {0} for ShapeAttribute!", curAttribute.ArrayType);
                                        break;
                                }

                                numPrimitiveBytesRead += numBytesRead;
                            }
                        }

                        var triangleList = ConvertTopologyToTriangles(type, primitiveVertices);
                        for(int i = 0; i < triangleList.Count; i++)
                        {
                            shape.Indexes.Add(numVertexRead);
                            numVertexRead++;

                            var tri = triangleList[i];
                            if (tri.Position >= 0) shape.VertexData.Position.Add(compressedVertexData.Position[tri.Position]);
                            if (tri.Normal >= 0) shape.VertexData.Normal.Add(compressedVertexData.Normal[tri.Normal]);
                            if (tri.Binormal >= 0) shape.VertexData.Binormal.Add(compressedVertexData.Binormal[tri.Binormal]);
                            if (tri.Color0 >= 0) shape.VertexData.Color0.Add(compressedVertexData.Color0[tri.Color0]);
                            if (tri.Color1 >= 0) shape.VertexData.Color1.Add(compressedVertexData.Color1[tri.Color1]);
                            if (tri.Tex0 >= 0) shape.VertexData.Tex0.Add(compressedVertexData.Tex0[tri.Tex0]);
                            if (tri.Tex1 >= 0) shape.VertexData.Tex1.Add(compressedVertexData.Tex1[tri.Tex1]);
                            if (tri.Tex2 >= 0) shape.VertexData.Tex2.Add(compressedVertexData.Tex2[tri.Tex2]);
                            if (tri.Tex3 >= 0) shape.VertexData.Tex3.Add(compressedVertexData.Tex3[tri.Tex3]);
                            if (tri.Tex4 >= 0) shape.VertexData.Tex4.Add(compressedVertexData.Tex4[tri.Tex4]);
                            if (tri.Tex5 >= 0) shape.VertexData.Tex5.Add(compressedVertexData.Tex5[tri.Tex5]);
                            if (tri.Tex6 >= 0) shape.VertexData.Tex6.Add(compressedVertexData.Tex6[tri.Tex6]);
                            if (tri.Tex7 >= 0) shape.VertexData.Tex7.Add(compressedVertexData.Tex7[tri.Tex7]);

                            if (tri.PosMtxIndex >= 0) shape.VertexData.PositionMatrixIndexes.Add(tri.PosMtxIndex/3);
                            else shape.VertexData.PositionMatrixIndexes.Add(0);
                        }
                    }

                    matrixData.LastRelevantVertexIndex = shape.VertexData.Position.Count;
                }

                shape.UploadBuffersToGPU(false);
            }
        }

        public List<MeshVertexIndex> ConvertTopologyToTriangles(GXPrimitiveType fromType, List<MeshVertexIndex> indexes)
        {
            List<MeshVertexIndex> sortedIndexes = new List<MeshVertexIndex>();
            if(fromType == GXPrimitiveType.TriangleStrip)
            {
                for (int v = 2; v < indexes.Count; v++)
                {
                    bool isEven = v % 2 != 0;
                    MeshVertexIndex[] newTri = new MeshVertexIndex[3];

                    newTri[0] = indexes[v - 2];
                    newTri[1] = isEven ? indexes[v] : indexes[v - 1];
                    newTri[2] = isEven ? indexes[v - 1] : indexes[v];

                    if (newTri[0] != newTri[1] && newTri[1] != newTri[2] && newTri[2] != newTri[0])
                        sortedIndexes.AddRange(newTri);
                    else
                        System.Console.WriteLine("Degenerate triangle detected, skipping TriangleStrip conversion to triangle.");
                }
            }
            else if(fromType == GXPrimitiveType.TriangleFan)
            {
                for(int v = 1; v < indexes.Count-1; v++)
                {
                    MeshVertexIndex[] newTri = new MeshVertexIndex[3];
                    newTri[0] = indexes[v];
                    newTri[1] = indexes[v + 1];
                    newTri[2] = indexes[0];

                    if (newTri[0] != newTri[1] && newTri[1] != newTri[2] && newTri[2] != newTri[0])
                        sortedIndexes.AddRange(newTri);
                    else
                        System.Console.WriteLine("Degenerate triangle detected, skipping TriangleFan conversion to triangle.");
                }
            }
            else if(fromType == GXPrimitiveType.Triangles)
            {
                sortedIndexes.AddRange(indexes);
            }
            else
            {
                System.Console.WriteLine("Unsupported GXPrimitiveType: {0} in conversion to Triangle List.", fromType);
            }

            return sortedIndexes;
        }
    }
public class ShapeComparer : IComparer<SHP1.Shape>
{
    public int Compare(SHP1.Shape x, SHP1.Shape y)
    {
        return x.MaterialIndex.CompareTo(y.MaterialIndex);
    }
}