using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiasGames.Climbing;
using GameFormatReader.Common;
using KclLibrary;
using Syroot.BinaryData;
using Unity.VisualScripting;
using UnityEngine;
using WiiExplorer;

public class KCL : MonoBehaviour
{
    public KCLFile KclFile;

    public async void LoadFromStream(GameObject roomObj, Archive archive, ArcFile file, byte[] buffer)
    {

        using (MemoryStream stream = new MemoryStream(buffer))
        {
            KclFile = new KCLFile(stream);
        }
        
        // Load plc
        ArcFile plcFile = ArcReader.GetFile(archive, file.Name.Replace("kcl", "plc"));
        PLC plc = transform.AddComponent<PLC>();
        plc.Buffer = plcFile.Buffer;
        plc.LoadFromStream();
        
        CreateGameObjectsForModels(roomObj, KclFile, plc);
        return;
    }

    private async Task Load(byte[] buffer)
    {
        await Task.Run(() =>
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                KclFile = new KCLFile(stream);
            }
        });
    }

    
void CreateGameObjectsForModels(GameObject roomObj, KCLFile kclFile, PLC plc)
{
    Dictionary<ushort, List<Triangle>> meshDiv = new Dictionary<ushort, List<Triangle>>();
    foreach (var model in kclFile.Models)
    {
        foreach (var prism in model.Prisms)
        {
            var tri = model.GetTriangle(prism);
            if (!meshDiv.ContainsKey(prism.CollisionFlags))
                meshDiv.Add(prism.CollisionFlags, new List<Triangle>());

            meshDiv[prism.CollisionFlags].Add(tri);
        }
    }
    
    GameObject parent = new GameObject("Collision_KCL");
    parent.transform.SetParent(roomObj.transform);
    parent.transform.localPosition = Vector3.zero;
    parent.transform.localRotation = Quaternion.identity;

    foreach (var model in meshDiv)
    {
        GameObject modelObject = new GameObject("Model_" + model.Key);
        modelObject.transform.SetParent(parent.transform);
        modelObject.transform.position = Vector3.zero;
        modelObject.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
        
        PLC.sBgPc code = plc.Codes[model.Key];
        if (code.WallCode == PLC.WallCode.InvisibleWall) continue;

        MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = modelObject.AddComponent<MeshRenderer>();
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (var tri in model.Value)
        {
            int startIndex = vertices.Count;

            vertices.Add(new Vector3(tri.Vertices[2].X, tri.Vertices[2].Y, tri.Vertices[2].Z)); // v0
            vertices.Add(new Vector3(tri.Vertices[1].X, tri.Vertices[1].Y, tri.Vertices[1].Z)); // v1
            vertices.Add(new Vector3(tri.Vertices[0].X, tri.Vertices[0].Y, tri.Vertices[0].Z)); // v2

            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);

            vertices.Add(new Vector3(tri.Vertices[0].X, tri.Vertices[0].Y, tri.Vertices[0].Z)); // v2
            vertices.Add(new Vector3(tri.Vertices[1].X, tri.Vertices[1].Y, tri.Vertices[1].Z)); // v1
            vertices.Add(new Vector3(tri.Vertices[2].X, tri.Vertices[2].Y, tri.Vertices[2].Z)); // v0

            triangles.Add(startIndex + 3);
            triangles.Add(startIndex + 4);
            triangles.Add(startIndex + 5);
        }
        
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!IsValid(vertices[i]))
            {
                vertices[i] = Vector3.zero;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.bounds = new Bounds();
        
        bool IsValid(Vector3 vertex)
        {
            return !float.IsNaN(vertex.x) && !float.IsNaN(vertex.y) && !float.IsNaN(vertex.z) &&
                   !float.IsInfinity(vertex.x) && !float.IsInfinity(vertex.y) && !float.IsInfinity(vertex.z);
        }


        meshFilter.sharedMesh = mesh;
        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateNormals();

        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        modelObject.transform.position = Vector3.zero;
        
        MeshCollider collider = modelObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        if (code.CameraThrough)
        {
            modelObject.layer = LayerMask.NameToLayer("TransparentFX");
        }

        if (code.WallCode == PLC.WallCode.Climbable_Ladder)
        {
            modelObject.layer = LayerMask.NameToLayer("Climbable_Ladder");
            //collider.convex = true;
            //collider.isTrigger = true;
        } else if (code.WallCode == PLC.WallCode.Climbable_Generic)
        {
            modelObject.layer = LayerMask.NameToLayer("Climbable_Generic");
        }

        if (code.Att0Code == PLC.Att0Code.Water)
        {
            modelObject.layer = LayerMask.NameToLayer("Water");
            collider.convex = true;
            collider.isTrigger = true;
        }

        meshRenderer.enabled = false;
    }
}

}