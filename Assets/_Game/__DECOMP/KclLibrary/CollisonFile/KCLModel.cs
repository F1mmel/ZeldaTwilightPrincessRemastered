using System;
using System.Collections.Generic;
using System.Linq;
using Syroot.BinaryData;
using System.Runtime.InteropServices;
using System.Numerics;

namespace KclLibrary
{
    public class KCLModel
    {

        public KCLModel() { }

        public KCLModel(List<Triangle> triangleList, uint baseTriCount,
            FileVersion version, CollisionImportSettings settings)
        {
            Vector3 minCoordinate = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
            Vector3 maxCoordinate = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);

            List<KclPrism> prismList = new List<KclPrism>();
            Dictionary<ushort, Triangle> triangles = new Dictionary<ushort, Triangle>();

            Positions = new List<Vector3>();
            Normals = new List<Vector3>();
            Prisms = new KclPrism[0];
            Version = version;

            PrismThickness = settings.PrismThickness;
            SphereRadius = settings.SphereRadius;

            Dictionary<string, int> positionTable = new Dictionary<string, int>();
            Dictionary<string, int> normalTable = new Dictionary<string, int>();

            ushort triindex = 0;
            for (int i = 0; i < triangleList.Count; i++)
            {
                var triangle = triangleList[i];

                Vector3 direction = Vector3.Cross(
                    triangle.Vertices[1] - triangle.Vertices[0],
                    triangle.Vertices[2] - triangle.Vertices[0]);

                if ((direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z) < 0.01) continue;
                direction = Vector3.Normalize(direction);

                for (int j = 0; j < 3; j++)
                {
                    Vector3 position = triangle.Vertices[j];
                    minCoordinate.X = Math.Min(position.X, minCoordinate.X);
                    minCoordinate.Y = Math.Min(position.Y, minCoordinate.Y);
                    minCoordinate.Z = Math.Min(position.Z, minCoordinate.Z);
                    maxCoordinate.X = Math.Max(position.X, maxCoordinate.X);
                    maxCoordinate.Y = Math.Max(position.Y, maxCoordinate.Y);
                    maxCoordinate.Z = Math.Max(position.Z, maxCoordinate.Z);
                }

                Vector3 normalA = Vector3.Cross(direction,
                    triangle.Vertices[2] - triangle.Vertices[0]);

                Vector3 normalB = (-(Vector3.Cross(direction,
                     triangle.Vertices[1] - triangle.Vertices[0])));

                Vector3 normalC = Vector3.Cross(direction,
                     triangle.Vertices[1] - triangle.Vertices[2]);

                normalA = Vector3.Normalize(normalA);
                normalB = Vector3.Normalize(normalB);
                normalC = Vector3.Normalize(normalC);

                KclPrism face = new KclPrism()
                {
                    PositionIndex = (ushort)IndexOfVertex(triangle.Vertices[0], Positions, positionTable),
                    DirectionIndex = (ushort)IndexOfVertex(direction, Normals, normalTable),
                    Normal1Index = (ushort)IndexOfVertex(normalA, Normals, normalTable),
                    Normal2Index = (ushort)IndexOfVertex(normalB, Normals, normalTable),
                    Normal3Index = (ushort)IndexOfVertex(normalC, Normals, normalTable),
                    GlobalIndex = baseTriCount + (uint)prismList.Count,
                    CollisionFlags = triangle.Attribute,
                };

                triangles.Add((ushort)triindex++, triangle);

                float length = Vector3.Dot(triangle.Vertices[1] - triangle.Vertices[0], normalC);
                face.Length = length;

                prismList.Add(face);
            }

            positionTable.Clear();
            normalTable.Clear();

            if (prismList.Count == 0) return;

            minCoordinate += settings.PaddingMin;
            maxCoordinate += settings.PaddingMax;

            MinCoordinate = minCoordinate;
            Prisms = prismList.ToArray();

            Vector3 size = maxCoordinate - minCoordinate;
            Vector3U exponents = new Vector3U(
                (uint)Maths.GetNext2Exponent(size.X),
                (uint)Maths.GetNext2Exponent(size.Y),
                (uint)Maths.GetNext2Exponent(size.Z));
            int cubeSizePower = Maths.GetNext2Exponent(Math.Min(Math.Min(size.X, size.Y), size.Z));
            if (cubeSizePower > Maths.GetNext2Exponent(settings.MaxRootSize))
                cubeSizePower = Maths.GetNext2Exponent(settings.MaxRootSize);

            int cubeSize = 1 << cubeSizePower;
            CoordinateShift = new Vector3U(
               (uint)cubeSizePower,
               (uint)(exponents.X - cubeSizePower),
               (uint)(exponents.X - cubeSizePower + exponents.Y - cubeSizePower));
            CoordinateMask = new Vector3U(
                (uint)(0xFFFFFFFF << (int)exponents.X),
                (uint)(0xFFFFFFFF << (int)exponents.Y),
                (uint)(0xFFFFFFFF << (int)exponents.Z));
            Vector3U cubeCounts = new Vector3U(
                (uint)Math.Max(1, (1 << (int)exponents.X) / cubeSize),
                (uint)Math.Max(1, (1 << (int)exponents.Y) / cubeSize),
                (uint)Math.Max(1, (1 << (int)exponents.Z) / cubeSize));
            PolygonOctreeRoots = new PolygonOctree[cubeCounts.X * cubeCounts.Y * cubeCounts.Z];


            int cubeBlow = SphereRadius > 0 ? (int)(SphereRadius * 2) : 50;

            DebugLogger.WriteLine($"Octree Distance Bias {cubeBlow}");
            DebugLogger.WriteLine($"Creating Octrees {cubeCounts}");

            int index = 0;
            for (int z = 0; z < cubeCounts.Z; z++) {
                for (int y = 0; y < cubeCounts.Y; y++) {
                    for (int x = 0; x < cubeCounts.X; x++) {
                        Vector3 cubePosition = minCoordinate + ((float)cubeSize) * new Vector3(x, y, z);
                        PolygonOctreeRoots[index++] = new PolygonOctree(triangles, cubePosition, cubeSize,
                            settings.MaxTrianglesInCube, settings.MaxCubeSize, 
                            settings.MinCubeSize, cubeBlow, settings.MaxOctreeDepth);
                    }
                }
            }
            DebugLogger.WriteLine($"Finished Octree");
        }


        public Vector3 MinCoordinate { get; internal set; }

        public Vector3U CoordinateMask { get; internal set; }

        public Vector3U CoordinateShift { get; internal set; }

        public List<Vector3> Positions { get; internal set; }

        public List<Vector3> Normals { get; internal set; }

        public KclPrism[] Prisms { get; internal set; }

        public float PrismThickness { get; set; }

        public PolygonOctree[] PolygonOctreeRoots { get; private set; }

        public float SphereRadius { get; set; } = 1f;

        public FileVersion Version { get; private set; } = FileVersion.Version2;

        public List<KclPrism> HitPrisms { get; internal set; } = new List<KclPrism>();

        public List<PolygonOctree> HitOctrees { get; internal set; } = new List<PolygonOctree>();


        public Triangle GetTriangle(KclPrism prism)
        {
            Vector3 A = Positions[prism.PositionIndex];
            Vector3 CrossA = Vector3.Cross(Normals[prism.Normal1Index], Normals[prism.DirectionIndex]);
            Vector3 CrossB = Vector3.Cross(Normals[prism.Normal2Index], Normals[prism.DirectionIndex]);
            Vector3 B = A + CrossB * (prism.Length / Vector3.Dot(CrossB, Normals[prism.Normal3Index]));
            Vector3 C = A + CrossA * (prism.Length / Vector3.Dot(CrossA, Normals[prism.Normal3Index]));
            return new Triangle(A, B, C) { Attribute = prism.CollisionFlags };
        }

        public KCLHit CheckHit(Vector3 point)
        {
            HitPrisms.Clear();
            var hit = CollisionHandler.CheckPoint(this, point, 1, 1);
            return hit;
        }

        public uint GetMaxTriangleCount()
        {
            uint maxTriangleCount = uint.MinValue;

            var boundings = GetOctreeBoundings();
            for (int i = 0; i < boundings.Count; i++)
            {
                if (boundings[i].Octree.TriangleIndices == null)
                    continue;

                int size = boundings[i].Octree.TriangleIndices.Count;
                maxTriangleCount = (uint)Math.Max(maxTriangleCount, size);
            }
            return maxTriangleCount;
        }

        public float GetMinCubeSize()
        {
            float minCubeSize = float.MaxValue;

            var boundings = GetOctreeBoundings();
            for (int i = 0; i < boundings.Count; i++)
            {
                float size = boundings[i].Size;
                minCubeSize = Math.Min(minCubeSize, size);
            }
            return minCubeSize;
        }

        public int GetMaxOctreeDepth()
        {
            int maxDepth = int.MinValue;

            Vector3 cubeCounts = new Vector3(
                  (~(int)CoordinateMask.X >> (int)CoordinateShift.X) + 1,
                  ((~(int)CoordinateMask.Y >> (int)CoordinateShift.X) + 1),
                  ((~(int)CoordinateMask.Z >> (int)CoordinateShift.X) + 1));

            int cubeSize = 1 << (int)CoordinateShift.X;

            int index = 0;
            for (int z = 0; z < cubeCounts.Z; z++)
            {
                for (int y = 0; y < cubeCounts.Y; y++)
                {
                    for (int x = 0; x < cubeCounts.X; x++)
                    {
                        Vector3 cubePosition = MinCoordinate + ((float)cubeSize) * new Vector3(x, y, z);
                        maxDepth = Math.Max(maxDepth, GetPolygonOctreeDepth(PolygonOctreeRoots[index++],
                            cubePosition, (float)cubeSize));
                    }
                }
            }
            return maxDepth;
        }

        public Vector3 GetCoordinatePadding()
        {
            Vector3 minCoordinate = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
            Vector3 maxCoordinate = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);

            for (int i = 0; i < Positions.Count; i++)
            {
                minCoordinate.X = Math.Min(Positions[i].X, minCoordinate.X);
                minCoordinate.Y = Math.Min(Positions[i].Y, minCoordinate.Y);
                minCoordinate.Z = Math.Min(Positions[i].Z, minCoordinate.Z);
                maxCoordinate.X = Math.Max(Positions[i].X, maxCoordinate.X);
                maxCoordinate.Y = Math.Max(Positions[i].Y, maxCoordinate.Y);
                maxCoordinate.Z = Math.Max(Positions[i].Z, maxCoordinate.Z);
            }
            return MinCoordinate - minCoordinate;
        }

        public List<OctreeBounding> GetOctreeBoundings()
        {
            List<OctreeBounding> boundings = new List<OctreeBounding>();

            Vector3 cubeCounts = new Vector3(
                      (~(int)CoordinateMask.X >> (int)CoordinateShift.X) + 1,
                      ((~(int)CoordinateMask.Y >> (int)CoordinateShift.X) + 1),
                      ((~(int)CoordinateMask.Z >> (int)CoordinateShift.X) + 1));

            int cubeSize = 1 << (int)CoordinateShift.X;

            int index = 0;
            for (int z = 0; z < cubeCounts.Z; z++) {
                for (int y = 0; y < cubeCounts.Y; y++)  {
                    for (int x = 0; x < cubeCounts.X; x++) {
                        Vector3 cubePosition = MinCoordinate + ((float)cubeSize) * new Vector3(x, y, z);
                        GetOctreeBounding(boundings, PolygonOctreeRoots[index++], cubePosition, (float)cubeSize);
                    }
                }
            }
            return boundings;
        }


        internal void Read(BinaryDataReader reader, FileVersion version)
        {
            Version = version;

            long modelPosition = reader.Position;
            int positionArrayOffset = reader.ReadInt32();
            int normalArrayOffset = reader.ReadInt32();
            int prismArrayOffset = reader.ReadInt32();
            int octreeOffset = reader.ReadInt32();

            if (version == FileVersion.VersionDS)
            {
                PrismThickness = reader.ReadFx32();
                MinCoordinate = reader.ReadVector3Fx32();
                CoordinateMask = reader.ReadVector3U();
                CoordinateShift = reader.ReadVector3U();
                SphereRadius = reader.ReadFx32();
                prismArrayOffset += 0x10;

                reader.Position = modelPosition + positionArrayOffset;
                int positionCount = (normalArrayOffset - positionArrayOffset) / 12;
                Positions = new List<Vector3>(reader.ReadVector3Fx32s(positionCount));

                reader.Position = modelPosition + normalArrayOffset;
                int normalCount = (prismArrayOffset - normalArrayOffset) / 6;
                Normals = new List<Vector3>(reader.ReadVector3Fx16s(normalCount));

                reader.Position = modelPosition + prismArrayOffset;
                int PrismCount = (octreeOffset - prismArrayOffset) / (version >= FileVersion.Version2 ? 20 : 16);
                Prisms = reader.ReadPrisms(PrismCount, version);
            }
            else
            {
                PrismThickness = reader.ReadSingle();
                MinCoordinate = reader.ReadVector3F();
                CoordinateMask = reader.ReadVector3U();
                CoordinateShift = reader.ReadVector3U();
                if (positionArrayOffset > 56)
                    SphereRadius = reader.ReadSingle();

                if (version < FileVersion.Version2)
                    prismArrayOffset += 0x10;

                reader.Position = modelPosition + positionArrayOffset;
                int positionCount = (normalArrayOffset - positionArrayOffset) / 12;
                Positions = new List<Vector3>(reader.ReadVector3Fs(positionCount));

                reader.Position = modelPosition + normalArrayOffset;
                int normalCount = (prismArrayOffset - normalArrayOffset) / 12;
                Normals = new List<Vector3>(reader.ReadVector3Fs(normalCount));

                reader.Position = modelPosition + prismArrayOffset;
                int PrismCount = (octreeOffset - prismArrayOffset) / (version >= FileVersion.Version2 ? 20 : 16);
                Prisms = reader.ReadPrisms(PrismCount, version);
            }

            reader.Position = modelPosition + octreeOffset;
            int nodeCount
                 = ((~(int)CoordinateMask.X >> (int)CoordinateShift.X) + 1)
                 * ((~(int)CoordinateMask.Y >> (int)CoordinateShift.X) + 1)
                 * ((~(int)CoordinateMask.Z >> (int)CoordinateShift.X) + 1);

            PolygonOctreeRoots = new PolygonOctree[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                PolygonOctreeRoots[i] = new PolygonOctree(reader, modelPosition + octreeOffset, version);
            }
        }

        internal void Write(BinaryDataWriter writer, FileVersion version)
        {
            long modelPosition = writer.Position;

            Offset positionArrayOffset = writer.ReserveOffset();
            Offset normalArrayOffset = writer.ReserveOffset();
            Offset triangleArrayOffset = writer.ReserveOffset();
            Offset octreeOffset = writer.ReserveOffset();
            if (version == FileVersion.VersionDS)
            {
                writer.WriteFx32(PrismThickness);
                writer.WriteVector3Fx32(MinCoordinate);
                writer.Write(CoordinateMask);
                writer.Write(CoordinateShift);
                writer.WriteFx32(SphereRadius);

                positionArrayOffset.Satisfy((int)(writer.Position - modelPosition));
                writer.WriteVector3Fx32s(Positions.ToArray());

                normalArrayOffset.Satisfy((int)(writer.Position - modelPosition));
                writer.WriteVector3Fx16s(Normals.ToArray());

                triangleArrayOffset.Satisfy((int)(writer.Position - modelPosition - 0x10));
                writer.Write(Prisms, version);
            }
            else
            {
                writer.Write(PrismThickness);
                writer.Write(MinCoordinate);
                writer.Write(CoordinateMask);
                writer.Write(CoordinateShift);
                if (version > FileVersion.VersionGC)
                    writer.Write(SphereRadius);

                positionArrayOffset.Satisfy((int)(writer.Position - modelPosition));
                writer.Write(Positions.ToArray());

                normalArrayOffset.Satisfy((int)(writer.Position - modelPosition));
                writer.Write(Normals.ToArray());

                if (version < FileVersion.Version2)
                    triangleArrayOffset.Satisfy((int)(writer.Position - modelPosition - 0x10));
                else
                    triangleArrayOffset.Satisfy((int)(writer.Position - modelPosition));
                writer.Write(Prisms, version);
            }

            int octreeOffsetValue = (int)(writer.Position - modelPosition);
            octreeOffset.Satisfy(octreeOffsetValue);

            int triangleListPos = GetNodeCount(PolygonOctreeRoots) * sizeof(uint);
            Queue<PolygonOctree[]> queuedNodes = new Queue<PolygonOctree[]>();
            Dictionary<ushort[], int> indexPool = CreateIndexBuffer(queuedNodes);

            queuedNodes.Enqueue(PolygonOctreeRoots);
            while (queuedNodes.Count > 0)
            {
                PolygonOctree[] nodes = queuedNodes.Dequeue();
                long offset = writer.Position - modelPosition - octreeOffsetValue;
                foreach (PolygonOctree node in nodes)
                {
                    if (node.Children == null)
                    {
                        ushort[] indices = node.TriangleIndices.ToArray();
                        int listPos = triangleListPos + indexPool[indices];
                        node.Key = (uint)ModelOctreeNode.Flags.Values | (uint)(listPos - offset - sizeof(ushort));
                    }
                    else
                    {
                        node.Key = (uint)(nodes.Length + queuedNodes.Count * 8) * sizeof(uint);
                        queuedNodes.Enqueue(node.Children);
                    }
                    writer.Write(node.Key);
                }
            }

            foreach (var ind in indexPool)
            {
                if (ind.Key.Length == 0)
                    break;

                if (version < FileVersion.Version2)
                {
                    for (int i = 0; i < ind.Key.Length; i++)
                        writer.Write((ushort)(ind.Key[i] + 1));
                    writer.Write((ushort)0);
                }
                else
                {
                    writer.Write(ind.Key);
                    writer.Write((ushort)0xFFFF);
                }
            }
        }


        private Dictionary<ushort[], int> CreateIndexBuffer(Queue<PolygonOctree[]> queuedNodes)
        {
            Dictionary<ushort[], int> indexPool = new Dictionary<ushort[], int>(new IndexEqualityComparer());
            int offset = 0;
            queuedNodes.Enqueue(PolygonOctreeRoots);
            while (queuedNodes.Count > 0)
            {
                PolygonOctree[] nodes = queuedNodes.Dequeue();
                foreach (PolygonOctree node in nodes)
                {
                    if (node.Children == null)
                    {
                        ushort[] indices = node.TriangleIndices.ToArray();
                        if (node.TriangleIndices.Count > 0 && !indexPool.ContainsKey(indices))
                        {
                            indexPool.Add(indices, offset);
                            offset += (node.TriangleIndices.Count + 1) * sizeof(ushort);
                        }
                    }
                    else
                    {
                        queuedNodes.Enqueue(node.Children);
                    }
                }
            }
            indexPool.Add(new ushort[0], offset - sizeof(ushort));
            return indexPool;
        }

        private void GetOctreeBounding(List<OctreeBounding> boundings, PolygonOctree octree, Vector3 cubePosition, float cubeSize)
        {
            OctreeBounding bounding = new OctreeBounding();
            bounding.Position = cubePosition;
            bounding.Size = cubeSize;
            bounding.Octree = octree;
            boundings.Add(bounding);

            if (octree.Children != null)
            {
                float childCubeSize = cubeSize / 2f;
                int i = 0;
                for (int z = 0; z < 2; z++) {
                    for (int y = 0; y < 2; y++) {
                        for (int x = 0; x < 2; x++) {
                            Vector3 childCubePosition = cubePosition + childCubeSize * new Vector3(x, y, z);
                            GetOctreeBounding(boundings, octree.Children[i++], childCubePosition, childCubeSize);
                        }
                    }
                }
            }
        }

        private int GetPolygonOctreeDepth(PolygonOctree octree, Vector3 cubePosition, float cubeSize, int depth = 0)
        {
            if (octree.Children != null)
            {
                float childCubeSize = cubeSize / 2f;
                int i = 0;
                for (int z = 0; z < 2; z++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int x = 0; x < 2; x++)
                        {
                            Vector3 childCubePosition = cubePosition + childCubeSize * new Vector3(x, y, z);
                            return GetPolygonOctreeDepth(octree.Children[i++], childCubePosition, childCubeSize, depth + 1);
                        }
                    }
                }
            }
            return depth;
        }

        public class OctreeBounding
        {
            public PolygonOctree Octree;
            public Vector3 Position { get; set; }
            public float Size { get; set; }
        }

        private class IndexEqualityComparer : IEqualityComparer<ushort[]>
        {
            public bool Equals(ushort[] x, ushort[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(ushort[] obj)
            {
                int result = 17;
                for (int i = 0; i < obj.Length; i++)
                {
                    unchecked
                    {
                        result = result * 23 + obj[i];
                    }
                }
                return result;
            }
        }

        private int GetNodeCount(PolygonOctree[] nodes)
        {
            int count = nodes.Length;
            foreach (PolygonOctree node in nodes)
            {
                if (node.Children != null)
                {
                    count += GetNodeCount(node.Children);
                }
            }
            return count;
        }

        private int IndexOfVertex(Vector3 value, List<Vector3> valueList, Dictionary<string, int> lookupTable)
        {
            string key = value.ToString();
            if (!lookupTable.ContainsKey(key))
            {
                valueList.Add(value);
                lookupTable.Add(key, lookupTable.Count);
            }

            return lookupTable[key];
        }

        private static int ContainsVector3(Vector3 a, List<Vector3> b)
        {
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].X == a.X && b[i].Y == a.Y && b[i].Z == a.Z)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
