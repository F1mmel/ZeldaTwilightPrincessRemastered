using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Numerics;
using System.Linq;
using System.Globalization;

namespace KclLibrary
{
    public class ObjModel
    {

        private static readonly char[] _argSeparators = new char[] { ' ' };
        private static readonly char[] _vertexSeparators = new char[] { '/' };


        public ObjModel()
        {
            Meshes = new List<ObjMesh>();
            Materials = new List<ObjMaterial>();
        }

        public ObjModel(Stream stream)
        {
            Load(stream);
        }

        public ObjModel(string fileName)
        {
            Load(fileName);
        }


        public List<ObjMesh> Meshes { get; set; }

        public List<ObjMaterial> Materials { get; set; }


        public List<Triangle> ToTriangles()
        {
            DebugLogger.WriteLine($"Creating triangle list....");

            List<ushort> attributes = new List<ushort>();

            List<Triangle> triangles = new List<Triangle>();
            foreach (var mesh in Meshes)
            {
                foreach (var face in mesh.Faces)
                {
                    if (!attributes.Contains(face.CollisionAttribute))
                        attributes.Add(face.CollisionAttribute);

                    var triangle = new Triangle();
                    triangle.Attribute = face.CollisionAttribute;
                    triangle.Vertices = new Vector3[3];
                    for (int i = 0; i < face.Vertices.Length; i++)
                        triangle.Vertices[i] = face.Vertices[i].Position;

                    triangles.Add(triangle);
                }
            }

            return triangles;
        }

        public string[] GetMeshNameList()
        {
            List<string> meshNames = new List<string>();
            for (int i = 0; i < Meshes.Count; i++)
            {
                if (!meshNames.Contains(Meshes[i].Name))
                    meshNames.Add(Meshes[i].Name);
            }
            return meshNames.ToArray();
        }

        public string[] GetMaterialNameList()
        {
            List<string> materialNames = new List<string>();
            for (int i = 0; i < Meshes.Count; i++)
            {
                for (int f = 0; f < Meshes[i].Faces.Count; f++)
                {
                    if (!materialNames.Contains(Meshes[i].Faces[f].Material))
                        materialNames.Add(Meshes[i].Faces[f].Material);
                }
            }
            return materialNames.ToArray();
        }

        public void Load(Stream stream)
        {
            DebugLogger.WriteLine($"Loading obj file....");

            Meshes = new List<ObjMesh>();
            Materials = new List<ObjMaterial>();

            ObjMesh currentMesh = new ObjMesh("Mesh");

            HashSet<string> faceHashes = new HashSet<string>();

            Dictionary<ObjFace, int> faceDupes = new Dictionary<ObjFace, int>();
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                List<Vector3> Positions = new List<Vector3>();
                List<Vector2> TexCoords = new List<Vector2>();
                List<Vector3> Normals = new List<Vector3>();

                var enusculture = new CultureInfo("en-US");
                string currentMaterial = null;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    line = line.Replace(",", ".");

                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] args = line.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length == 1)
                        continue;

                    switch (args[0])
                    {
                        case "o":
                        case "g":
                            currentMesh = new ObjMesh(args.Length > 1 ? args[1] : $"Mesh{Meshes.Count}");
                            Meshes.Add(currentMesh);
                            continue;
                        case "v":
                            Positions.Add(new Vector3(
                                Single.Parse(args[1], enusculture),
                                Single.Parse(args[2], enusculture),
                                Single.Parse(args[3], enusculture)));
                            continue;
                        case "vt":
                            TexCoords.Add(new Vector2(Single.Parse(args[1], enusculture), Single.Parse(args[2], enusculture)));
                            continue;
                        case "vn":
                            Normals.Add(new Vector3(Single.Parse(args[1], enusculture), Single.Parse(args[2], enusculture),
                                Single.Parse(args[3])));
                            continue;
                        case "f":
                            if (args.Length != 4)
                                throw new Exception("Obj must be trianglulated!");

                            int[] indices = new int[3 * 2];

                            ObjFace face = new ObjFace() { Vertices = new ObjVertex[3] };
                            face.Material = currentMaterial;
                            for (int i = 0; i < face.Vertices.Length; i++)
                            {
                                string[] vertexArgs = args[i + 1].Split(_vertexSeparators, StringSplitOptions.None);
                                int positionIndex = Int32.Parse(vertexArgs[0]) - 1;

                                face.Vertices[i].Position = Positions[positionIndex];

                                if (float.IsNaN(face.Vertices[i].Position.X) ||
                                    float.IsNaN(face.Vertices[i].Position.Y) ||
                                    float.IsNaN(face.Vertices[i].Position.Z))
                                {
                                    face.Vertices = null;
                                    break;
                                }

                                if (vertexArgs.Length > 1 && vertexArgs[1] != String.Empty)
                                {
                                    face.Vertices[i].TexCoord = TexCoords[Int32.Parse(vertexArgs[1]) - 1];
                                }
                                if (vertexArgs.Length > 2 && vertexArgs[2] != String.Empty)
                                {
                                    face.Vertices[i].Normal = Normals[Int32.Parse(vertexArgs[2]) - 1];
                                }
                            }

                            string faceStr = face.ToString();
                            if (faceHashes.Contains(faceStr))
                                continue;

                            faceHashes.Add(faceStr);

                            if (face.Vertices != null)
                                currentMesh.Faces.Add(face);
                            continue;
                        case "usemtl":
                            {
                                if (args.Length < 2) continue;
                                currentMaterial = args[1];
                                continue;
                            }
                    }
                }
            }

            Console.WriteLine($"FACE COUNT {currentMesh.Faces.Count}");

            faceDupes.Clear();

            if (Meshes.Count == 0)
                Meshes.Add(currentMesh);
        }

        public void LoadMTL(Stream stream, bool leaveOpen = false)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.Default, true, 81920, leaveOpen))
            {
                ObjMaterial currentMaterial = null;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] args = line.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);
                    switch (args[0])
                    {
                        case "newmtl":
                            currentMaterial = new ObjMaterial();
                            currentMaterial.Name = args[1];
                            break;
                        case "Ka":
                            currentMaterial.Ambient = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "Kd":
                            currentMaterial.Diffuse = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "Ks":
                            currentMaterial.Specular = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "map_Kd ":
                            currentMaterial.DiffuseTexture = args[1];
                            break;
                    }
                }
            }
        }


        public void SaveMTL(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.Default))
            {
                foreach (var material in Materials)
                {
                    writer.WriteLine($"newmtl {material.Name}");
                    if (material.Diffuse != null)
                        writer.WriteLine($"Kd {material.Diffuse.X} {material.Diffuse.Y} {material.Diffuse.Z}");
                    if (material.Ambient != null)
                        writer.WriteLine($"Ka {material.Ambient.X} {material.Ambient.Y} {material.Ambient.Z}");
                    if (material.Specular != null)
                        writer.WriteLine($"Ks {material.Specular.X} {material.Specular.Y} {material.Specular.Z}");
                    if (material.DiffuseTexture != null)
                        writer.WriteLine($"map_Kd  {material.DiffuseTexture}");
                }
            }
        }

        public void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.Default))
            {
                int positionShift = 1;
                int normalShift = 1;
                foreach (var mesh in Meshes)
                {
                    Dictionary<string, int> positionTable = new Dictionary<string, int>();
                    Dictionary<string, int> normalTable = new Dictionary<string, int>();

                    List<Vector3> positons = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();

                    writer.WriteLine($"o {mesh.Name}");
                    foreach (var face in mesh.Faces)
                    {
                        foreach (var v in face.Vertices)
                        {
                            string positionKey = v.Position.ToString();
                            string normalKey = v.Normal.ToString();
                            string texCoordKey = v.TexCoord.ToString();

                            if (!positionTable.ContainsKey(positionKey))
                            {
                                positionTable.Add(positionKey, positons.Count);
                                positons.Add(v.Position);
                            }

                            if (!normalTable.ContainsKey(normalKey))
                            {
                                normalTable.Add(normalKey, normals.Count);
                                normals.Add(v.Normal);
                            }
                        }
                    }

                    foreach (var pos in positons)
                        writer.WriteLine($"v {pos.X} {pos.Y} {pos.Z}");
                    foreach (var nrm in normals)
                        writer.WriteLine($"vn {nrm.X} {nrm.Y} {nrm.Z}");

                    string currentMaterial = "";
                    foreach (var face in mesh.Faces.OrderBy(x => x.Material))
                    {
                        if (face.Material != currentMaterial)
                        {
                            currentMaterial = face.Material;
                            writer.WriteLine($"usemtl {currentMaterial}");
                        }

                        string faceData = "f";
                        foreach (var v in face.Vertices)
                        {
                            int positionIndex = positionShift + positionTable[v.Position.ToString()];
                            int normalIndex = normalShift + normalTable[v.Normal.ToString()];
                            faceData += " " + string.Join("//", new string[] { positionIndex.ToString(), normalIndex.ToString() });
                        }
                        writer.WriteLine(faceData);
                    }

                    positionShift += positons.Count;
                    normalShift += normals.Count;
                }
            }
        }

        public void Load(string fileName) {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Load(stream);
            }
        }


        public void Save(string fileName, bool saveMTL = true) {
            if (saveMTL)
                SaveMTL(fileName.Replace(".obj", ".mtl"));
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Save(stream);
            }
        }

        public void SaveMTL(string fileName) {
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                SaveMTL(stream);
            }
        }
    }

    public class ObjMaterial
    {
        public string Name { get; set; }

        public string DiffuseTexture { get; set; }

        public Vector3 Diffuse { get; set; }

        public Vector3 Ambient { get; set; }

        public Vector3 Specular { get; set; }
    }

    public class ObjMesh
    {
        public List<ObjFace> Faces { get; set; }

        public string Name { get; set; }

        public ObjMesh(string name)
        {
            Name = name;
            Faces = new List<ObjFace>();
        }
    }

    public struct ObjFace
    {
        public string Material { get; set; }

        public ushort CollisionAttribute { get; set; }

        public ObjVertex[] Vertices;

        public override string ToString() {
            string f = CollisionAttribute.ToString();
            for (int i = 0; i < Vertices.Length; i++)
                f += Vertices[i].ToString();
            return f;
        }

        public ObjFace(ObjFace face, ushort value)
        {
            Vertices = face.Vertices;
            Material = face.Material;
            CollisionAttribute = value;
        }
    }

    public struct ObjVertex
    {

        public Vector3 Position;

        public Vector2 TexCoord;

        public Vector3 Normal;

        public override string ToString() {
            return $"{Position}_{Normal}";
        }
    }

    public class ObjFaceComparer : IEqualityComparer<ObjFace>
    {
        public bool Equals(ObjFace x, ObjFace y)
        {
            if (x.GetHashCode() != y.GetHashCode())
                return false;

            return true;
        }

        public int GetHashCode(ObjFace obj) {
            return obj.GetHashCode();
        }
    }
}
