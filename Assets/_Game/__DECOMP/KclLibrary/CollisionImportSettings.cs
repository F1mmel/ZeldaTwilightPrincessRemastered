using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KclLibrary
{
    public class CollisionImportSettings
    {
        public int MaxRootSize = 2048;
        public int MinRootSize = 128;
        public int MaxCubeSize = 0x100000;
        public int MinCubeSize = 32;
        public int MaxOctreeDepth = 10;
        public int MaxTrianglesInCube = 10;
        public float PrismThickness = 30;
        public float SphereRadius = 25;
        public Vector3 PaddingMin = new Vector3(-50, -50, -50);

        public Vector3 PaddingMax = new Vector3(50, 50, 50);
    }
}
