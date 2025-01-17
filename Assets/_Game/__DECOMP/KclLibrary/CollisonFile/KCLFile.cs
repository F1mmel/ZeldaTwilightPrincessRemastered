using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Syroot.BinaryData;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace KclLibrary
{
    public class KCLFile
    {

        private const int _version2 = 0x02020000;

        private static readonly int MaxModelPrismCount = 65535 / 4;


        public KCLFile(List<Triangle> triangles, FileVersion version, 
            bool isBigEndian, CollisionImportSettings settings = null)
        {
            if (settings == null) settings = new CollisionImportSettings();

            Version = version;
            ByteOrder = isBigEndian ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            Replace(triangles, settings);
        }

        public KCLFile(string fileName) : base()
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                Load(stream);
            }
        }

        public KCLFile()
        {
            
        }

        public KCLFile(Stream stream) : base()
        {
            Load(stream);
        }


        public FileVersion Version { get; set; }

        public Vector3 MinCoordinate { get; private set; }

        public Vector3 MaxCoordinate { get; private set; }

        public Vector3U CoordinateShift { get; private set; }

        public int PrismCount { get; private set; }

        public ModelOctreeNode ModelOctreeRoot { get; private set; }

        public List<KCLModel> Models { get; private set; }

        public ByteOrder ByteOrder { get; set; }

        public Matrix4x4 Transform = Matrix4x4.Identity;


        public ObjModel CreateGenericModel()
        {
            ObjModel objModel = new ObjModel();

            Dictionary<string, ObjMesh> meshes = new Dictionary<string, ObjMesh>();

            var mesh = new ObjMesh($"Mesh");
            objModel.Meshes.Add(mesh);

            bool spltByMaterial = true;

            foreach (KCLModel model in Models)
            {
                var distinctPrisms = model.Prisms
                       .GroupBy(p => p.CollisionFlags)
                       .Select(g => g.First())
                       .ToList().Count;

                if (distinctPrisms == model.Prisms.Length)
                    spltByMaterial = false;
            }

            foreach (KCLModel model in Models) {
                foreach (var face in model.Prisms)
                {
                    var triangle = model.GetTriangle(face);
                    var normal = triangle.Normal;

                    ObjFace objFace = new ObjFace();
                    objFace.Material = $"COL_{face.CollisionFlags.ToString("X")}";
                    objFace.Vertices = new ObjVertex[3];
                    for (int i = 0; i < 3; i++) {
                        objFace.Vertices[i] = new ObjVertex()
                        {
                            Position = triangle.Vertices[i],
                            Normal = normal,
                        };
                    }

                    if (spltByMaterial)
                    {
                        if (!meshes.ContainsKey(objFace.Material)) {
                            meshes.Add(objFace.Material, new ObjMesh(objFace.Material));
                            objModel.Meshes.Add(meshes[objFace.Material]);
                        }
                        meshes[objFace.Material].Faces.Add(objFace);
                    }
                    else
                        mesh.Faces.Add(objFace);
                }
            }
            return objModel;
        }


        public void Load(Stream stream, bool leaveOpen = false)
        {
            using (BinaryDataReader reader = new BinaryDataReader(stream, leaveOpen))
            {
                Read(reader);
            }
        }

        public void Save(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write)) {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                Write(writer);
            }
        }

        public void Replace(List<Triangle> triangles, CollisionImportSettings settings)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Vector3 minCoordinate = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);
            Vector3 maxCoordinate = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);

            DebugLogger.WriteLine($"Replacing Collision...");

            DebugLogger.WriteLine($"Settings:");
            DebugLogger.WriteLine($"-MaxRootSize {settings.MaxRootSize}");
            DebugLogger.WriteLine($"-MaxTrianglesInCube {settings.MaxTrianglesInCube}");
            DebugLogger.WriteLine($"-MinCubeSize {settings.MinCubeSize}");
            DebugLogger.WriteLine($"-PaddingMax {settings.PaddingMax}");
            DebugLogger.WriteLine($"-PaddingMin {settings.PaddingMin}");
            DebugLogger.WriteLine($"-PrismThickness {settings.PrismThickness}");
            DebugLogger.WriteLine($"-SphereRadius {settings.SphereRadius}");

            DebugLogger.WriteLine($"Calculating bounding sizes...");

            for (int i = 0; i < triangles.Count; i++) {
                for (int v = 0; v < triangles[i].Vertices.Length; v++) {
                    Vector3 position = triangles[i].Vertices[v];
                    minCoordinate.X = Math.Min(position.X, minCoordinate.X);
                    minCoordinate.Y = Math.Min(position.Y, minCoordinate.Y);
                    minCoordinate.Z = Math.Min(position.Z, minCoordinate.Z);
                    maxCoordinate.X = Math.Max(position.X, maxCoordinate.X);
                    maxCoordinate.Y = Math.Max(position.Y, maxCoordinate.Y);
                    maxCoordinate.Z = Math.Max(position.Z, maxCoordinate.Z);
                }
            }

            MinCoordinate = minCoordinate + settings.PaddingMin;
            MaxCoordinate = maxCoordinate + settings.PaddingMax;

            DebugLogger.WriteLine($"MinCoordinate: {MinCoordinate}");
            DebugLogger.WriteLine($"MaxCoordinate: {MaxCoordinate}");

            Vector3 size = MaxCoordinate - MinCoordinate;
            int worldLengthExp = Maths.GetNext2Exponent(Math.Min(Math.Min(size.X, size.Y), size.Z));
            int cubeSize = 1 << worldLengthExp;
            Vector3 exponents = new Vector3(
                Maths.GetNext2Exponent(size.X),
                Maths.GetNext2Exponent(size.Y),
                Maths.GetNext2Exponent(size.Z));
            CoordinateShift = new Vector3U(
                (uint)(exponents.X),
                (uint)(exponents.Y),
                (uint)(exponents.Z));

            Models = new List<KCLModel>();
            Vector3 boxSize = new Vector3(
                1 << (int)CoordinateShift.X,
                1 << (int)CoordinateShift.Y,
                1 << (int)CoordinateShift.Z);

            DebugLogger.WriteLine($"Model Octree Bounds: {boxSize}");

            var modelRoots = CreateModelDivision(MinCoordinate, triangles, boxSize / 2f);

            ModelOctreeRoot = new ModelOctreeNode();
            ModelOctreeRoot.Children = new ModelOctreeNode[ModelOctreeNode.ChildCount];
            for (int i = 0; i < ModelOctreeNode.ChildCount; i++) {
                ModelOctreeRoot.Children[i] = new ModelOctreeNode();
            }
            Models.Clear();

            CreateModelOctree(modelRoots, ModelOctreeRoot.Children, settings, 0);
            PrismCount = Models.Sum(x => x.Prisms.Length);

            stopWatch.Stop();

            DebugLogger.WriteLine($"Model Octree:");
            PrintModelOctree(ModelOctreeRoot.Children);

            DebugLogger.WriteLine($"Finished Collsion Generation {stopWatch.Elapsed}");
        }

        public void ResetHits()
        {
            for (int i = 0; i < Models.Count; i++) {
                Models[i].HitPrisms.Clear();
                Models[i].HitOctrees.Clear();
            }
        }

        public KCLHit CheckHit(Vector3 point)
        {
            ResetHits();

            point = CollisionHandler.ConvertLocalSpace(Transform, point);
            if (Models.Count == 1) return Models[0].CheckHit(point);

            bool inRange = (MinCoordinate.X < point.X && point.X < MaxCoordinate.X &&
                            MinCoordinate.Y < point.Y && point.Y < MaxCoordinate.Y &&
                            MinCoordinate.Z < point.Z && point.Z < MaxCoordinate.Z);
            if (!inRange)
                return null;

            Vector3 boxSize = new Vector3(
                1 << (int)CoordinateShift.X,
                1 << (int)CoordinateShift.Y,
                1 << (int)CoordinateShift.Z);

            var block = SearchModelBlock(ModelOctreeRoot.Children, point, MinCoordinate, boxSize);
            if (block != null && block.ModelIndex != null)
            {
                var hit = Models[(int)block.ModelIndex].CheckHit(point);
                return hit;
            }

            return null;
        }


        private ModelOctreeNode SearchModelBlock(ModelOctreeNode[] children, Vector3 point, Vector3 position, Vector3 boxSize)
        {
            int blockIdx = 0;
            for (int z = 0; z < 2; z++) {
                for (int y = 0; y < 2; y++) {
                    for (int x = 0; x < 2; x++) {
                        Vector3 cubePosition = position + boxSize * new Vector3(x, y, z);
                        Vector3 min = cubePosition - boxSize / 2f;
                        Vector3 max = cubePosition + boxSize / 2f;
                        bool inCube = (min.X < point.X && point.X < max.X &&
                                       min.Y < point.Y && point.Y < max.Y &&
                                       min.Z < point.Z && point.Z < max.Z);

                        if (inCube)
                        {
                            if (children[blockIdx].Children != null)
                                return SearchModelBlock(children[blockIdx].Children, point, cubePosition, boxSize / 2f);
                            else
                                return children[blockIdx];
                        }
                        blockIdx++;
                    }
                }
            }
            return null;
        }

        private void PrintModelOctree(ModelOctreeNode[] children, string indent = "") {
            int index = 0;
            foreach (var octree in children)
            {
                if (octree.ModelIndex.HasValue)
                    DebugLogger.WriteLine($"{indent}index {index} ModelIndex {octree.ModelIndex}");
                else if (octree.Children == null)
                    DebugLogger.WriteLine($"{indent}index {index} Empty Space");

                if (octree.Children != null)
                    PrintModelOctree(octree.Children, indent + "-");

                index++;
            }
        }

        private void CreateModelOctree(List<ModelGroup> modelRoots, ModelOctreeNode[] nodes,
            CollisionImportSettings settings,  uint baseTriCount, int level = 0)
        {
            for (int i = 0; i < modelRoots.Count; i++)
            {
                int nodeIndex = modelRoots[i].BlockIndex;

                if (modelRoots[i].Children.Count > 0)
                {
                    nodes[nodeIndex].Children = new ModelOctreeNode[ModelOctreeNode.ChildCount];
                    for (int j = 0; j < ModelOctreeNode.ChildCount; j++)
                    {
                        nodes[nodeIndex].Children[j] = new ModelOctreeNode();
                    }
                    CreateModelOctree(modelRoots[i].Children, nodes[nodeIndex].Children, settings, baseTriCount, level + 1);

                }
                else if (modelRoots[i].Triangles.Count > 0)
                {
                    var model = new KCLModel(modelRoots[i].Triangles, baseTriCount, Version, settings);
                    baseTriCount += (uint)modelRoots[i].Triangles.Count;

                    nodes[nodeIndex].ModelIndex = (uint)Models.Count;
                    foreach (var index in modelRoots[i].MergedBlockIndices)
                        nodes[index].ModelIndex = (uint)Models.Count;

                    Models.Add(model);
                }
            }
        }

        private List<ModelGroup> TryMergeModelGroups(List<ModelGroup> modelRoots)
        {
            List<ModelGroup> GlobalList = new List<ModelGroup>();
            for (int i = 0; i < modelRoots.Count; i++)
            {
                if (modelRoots[i].Children.Count == 0)
                {
                    bool isMerged = false;
                    foreach (var globalModel in GlobalList)
                    {
                        if (modelRoots[i].Triangles.Count + globalModel.Triangles.Count < MaxModelPrismCount)
                        {
                            globalModel.Triangles.AddRange(modelRoots[i].Triangles);
                            globalModel.MergedBlockIndices.Add(i);
                            isMerged = true;
                        }
                    }
                    if (!isMerged)
                        GlobalList.Add(modelRoots[i]);
                }
                else
                {
                    modelRoots[i].Children = TryMergeModelGroups(modelRoots[i].Children);
                    GlobalList.Add(modelRoots[i]);
                }


            }
            return GlobalList;
        }

        private List<ModelGroup> CreateModelDivision(Vector3 position, List<Triangle> triangles, Vector3 boxSize, int level = 0)
        {
            if (Version < FileVersion.Version2 || triangles.Count < MaxModelPrismCount && level == 0) {
                ModelGroup model = new ModelGroup();
                model.Triangles.AddRange(triangles);
                model.BlockIndex = 0;
                model.MergedBlockIndices = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
                return new List<ModelGroup>() { model };
            }

            ModelGroup[] modelRoots = new ModelGroup[8];
            int index = 0;
            for (int z = 0; z < 2; z++) {
                for (int y = 0; y < 2; y++) {
                    for (int x = 0; x < 2; x++) {
                        ModelGroup model = new ModelGroup();

                        Vector3 cubePosition = position + boxSize * new Vector3(x, y, z);

                        List<Triangle> containedTriangles = new List<Triangle>();
                        for (int i = 0; i < triangles.Count; i++)
                        {
                            if (TriangleBoxIntersect.TriBoxOverlap(triangles[i], cubePosition + boxSize / 2f, boxSize / 2f))
                                containedTriangles.Add(triangles[i]);
                        }

                        if (containedTriangles.Count >= MaxModelPrismCount)
                            DebugLogger.WriteLine($"Dividing model at {containedTriangles.Count} polygons.");

                        if (level > 2)
                            DebugLogger.WriteError($"Warning! Your KCL has over 3 division levels and may fall through!");

                        if (containedTriangles.Count >= MaxModelPrismCount)
                            model.Children = CreateModelDivision(cubePosition, containedTriangles, boxSize / 2f, level + 1);
                        else
                            model.Triangles = containedTriangles;

                        model.BlockIndex = index;
                        modelRoots[index] = model;
                        index++;
                    }
                }
            }
            return modelRoots.ToList();
        }

        private class ModelGroup
        {
            public List<Triangle> Triangles = new List<Triangle>();
            public List<ModelGroup> Children = new List<ModelGroup>();
            public int BlockIndex { get; set; }
            public List<int> MergedBlockIndices = new List<int>();
        }

        public void Read(BinaryDataReader reader)
        {
            ModelOctreeRoot = new ModelOctreeNode();
            ModelOctreeRoot.Children = new ModelOctreeNode[ModelOctreeNode.ChildCount];
            Models = new List<KCLModel>();

            reader.ByteOrder = ByteOrder.BigEndian;
            uint value = reader.ReadUInt32();
            Version = (FileVersion)value;

            ByteOrder = CheckByteOrder(reader);
            reader.ByteOrder = this.ByteOrder;

            if ((FileVersion)value != FileVersion.Version2)
            {
                if (value == 56)
                    Version = FileVersion.VersionGC;
                else
                {
                    using (reader.TemporarySeek(56, SeekOrigin.Begin)) {
                        if (reader.ReadInt32() == 102400)
                            Version = FileVersion.VersionDS;
                        else
                            Version = FileVersion.VersionWII;
                    }
                }

                reader.Seek(-4);

                KCLModel model = new KCLModel();
                model.Read(reader, Version);
                Models.Add(model);

                for (int i = 0; i < ModelOctreeNode.ChildCount; i++) {
                    ModelOctreeRoot.Children[i] = new ModelOctreeNode() { ModelIndex = 0 };
                }

                PrismCount = model.Prisms.Length;
                MinCoordinate = model.MinCoordinate;

            }
            else
            {
                int octreeOffset = reader.ReadInt32();
                int modelOffsetArrayOffset = reader.ReadInt32();
                int modelCount = reader.ReadInt32();
                MinCoordinate = reader.ReadVector3F();
                MaxCoordinate = reader.ReadVector3F();
                CoordinateShift = reader.ReadVector3U();
                PrismCount = reader.ReadInt32();

                reader.Position = octreeOffset;
                for (int i = 0; i < ModelOctreeNode.ChildCount; i++) {
                    ModelOctreeRoot.Children[i] = new ModelOctreeNode(reader, (uint)octreeOffset);
                }

                reader.Position = modelOffsetArrayOffset;
                int[] modelOffsets = reader.ReadInt32s(modelCount);
                Models = new List<KCLModel>(modelCount);
                foreach (int modelOffset in modelOffsets)
                {
                    reader.Position = modelOffset;
                    var model = new KCLModel();
                    model.Read(reader, Version);
                    Models.Add(model);
                }
            }
        }

        private ByteOrder CheckByteOrder(BinaryDataReader reader)
        {
            using (reader.TemporarySeek(0, SeekOrigin.Begin))
            {
                reader.ByteOrder = ByteOrder.BigEndian;

                uint value = reader.ReadUInt32();
                if (value == _version2)
                {
                    if (reader.ReadUInt32() == 56)
                        return ByteOrder.BigEndian;
                    else
                        return ByteOrder.LittleEndian;
                }
                else
                {
                    if (value == 60 || value == 56)
                        return ByteOrder.BigEndian;
                    else
                        return ByteOrder.LittleEndian;
                }
            }
        }

        private void Write(BinaryDataWriter writer)
        {
            DebugLogger.WriteLine($"Writing binary {this.ByteOrder} {Version}");

            writer.ByteOrder = this.ByteOrder;
            if (Version == FileVersion.Version2)
                WriteV2(writer);
            else
                WriteV1(writer);
        }

        private void WriteV1(BinaryDataWriter writer) {
            Models[0].Write(writer, Version);
        }

        private void WriteV2(BinaryDataWriter writer)
        {
            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write((uint)Version);
            writer.ByteOrder = this.ByteOrder;

            Offset octreeOffset = writer.ReserveOffset();
            Offset modelOffsetArrayOffset = writer.ReserveOffset();
            writer.Write(Models.Count);
            writer.Write(MinCoordinate);
            writer.Write(MaxCoordinate);
            writer.Write(CoordinateShift);
            writer.Write(PrismCount);

            octreeOffset.Satisfy();
            foreach (ModelOctreeNode rootChild in ModelOctreeRoot) 
                rootChild.Write(writer);

            int branchKey = 8;
            foreach (ModelOctreeNode rootChild in ModelOctreeRoot)
                rootChild.WriteChildren(writer, ref branchKey);

            modelOffsetArrayOffset.Satisfy();
            Offset[] modelOffsets = writer.ReserveOffset(Models.Count);

            for (int i = 0; i < Models.Count; i++)
            {
                modelOffsets[i].Satisfy();
                Models[i].Write(writer, Version);
                writer.Align(4);
            }
        }
    }
}
