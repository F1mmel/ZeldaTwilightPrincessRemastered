using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Syroot.BinaryData;
using System.Numerics;

namespace KclLibrary
{
    public class PolygonOctree : OctreeNodeBase<PolygonOctree>
    {

        public PolygonOctree() : base(0) { }

        internal PolygonOctree(BinaryDataReader reader, long parentOffset, FileVersion version) : base(reader.ReadUInt32())
        {
            int terminator = version >= FileVersion.Version2 ? 0xFFFF : 0x0;

            long offset = parentOffset + Key & ~_flagMask;
            if ((Key >> 31) == 1)
            {
                using (reader.TemporarySeek(offset + sizeof(ushort), SeekOrigin.Begin))
                {
                    TriangleIndices = new List<ushort>();
                    ushort index;
                    while ((index = reader.ReadUInt16()) != terminator) {
                        if (version < FileVersion.Version2)
                            TriangleIndices.Add((ushort)(index - 1));
                        else
                            TriangleIndices.Add(index);
                    }
                }
            }
            else
            {
                using (reader.TemporarySeek(offset, SeekOrigin.Begin))
                {
                    PolygonOctree[] children = new PolygonOctree[ChildCount];
                    for (int i = 0; i < ChildCount; i++) {
                        children[i] = new PolygonOctree(reader, offset, version);
                    }
                    Children = children;
                }
            }
        }


        internal PolygonOctree(Dictionary<ushort, Triangle> triangles, Vector3 cubePosition, float cubeSize,
            int maxTrianglesInCube, int maxCubeSize, int minCubeSize, int cubeBlow, int maxDepth, int depth = 0) : base(0)
        {
            Vector3 cubeCenter = cubePosition + new Vector3(cubeSize / 2f, cubeSize / 2f, cubeSize / 2f);
            float newsize = cubeSize + cubeBlow;
            Vector3 newPosition = cubeCenter - new Vector3(newsize / 2f, newsize / 2f, newsize / 2f);

            Dictionary<ushort, Triangle> containedTriangles = new Dictionary<ushort, Triangle>();
            foreach (KeyValuePair<ushort, Triangle> triangle in triangles)
            {
                if (TriangleHelper.TriangleCubeOverlap(triangle.Value, newPosition, newsize)) {
                    containedTriangles.Add(triangle.Key, triangle.Value);
                }
            }

            float halfWidth = cubeSize / 2f;

            bool isTriangleList = cubeSize <= maxCubeSize && containedTriangles.Count <= maxTrianglesInCube ||
                                  cubeSize <= minCubeSize || depth > maxDepth;

            if (containedTriangles.Count > maxTrianglesInCube && halfWidth >= minCubeSize)
            {
                float childCubeSize = cubeSize / 2f;
                Children = new PolygonOctree[ChildCount];
                int i = 0;
                for (int z = 0; z < 2; z++) {
                    for (int y = 0; y < 2; y++) {
                        for (int x = 0; x < 2; x++) {
                            Vector3 childCubePosition = cubePosition + childCubeSize * new Vector3(x, y, z);
                            Children[i++] = new PolygonOctree(containedTriangles, childCubePosition, childCubeSize,
                                maxTrianglesInCube, maxCubeSize, minCubeSize, cubeBlow, maxDepth, depth + 1);
                        }
                    }
                }
            }
            else
            {
                TriangleIndices = containedTriangles.Keys.ToList();
            }
        }


        public List<ushort> TriangleIndices { get; internal set; }
    }

}
