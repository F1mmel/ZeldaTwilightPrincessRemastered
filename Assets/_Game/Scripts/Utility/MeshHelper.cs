using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshHelper
{
    static List<Vector3> vertices;

    static List<Vector3> normals;
    // [... all other vertex data arrays you need]

    static List<int> indices;
    static Dictionary<uint, int> newVectices;
    
    public static List<GameObject> SplitMesh(GameObject original, int maxVertices)
{
    // Liste für die Sub-Mesh GameObjects
    List<GameObject> subMeshes = new List<GameObject>();

    // Hole das ursprüngliche Mesh und Material
    MeshFilter meshFilter = original.GetComponent<MeshFilter>();
    if (meshFilter == null) throw new System.Exception("GameObject must have a MeshFilter.");
    Mesh originalMesh = meshFilter.sharedMesh;
    Material sharedMaterial = original.GetComponent<MeshRenderer>().sharedMaterial;

    // Extrahiere Daten aus dem ursprünglichen Mesh
    Vector3[] vertices = originalMesh.vertices;
    int[] triangles = originalMesh.triangles;
    Vector3[] normals = originalMesh.normals;
    Vector2[] uv = originalMesh.uv;
    Color[] colors = originalMesh.colors; // Vertex Colors extrahieren

    // Aufteilen der Dreiecke basierend auf der maxVertices-Grenze
    int triangleCount = triangles.Length / 3;
    for (int i = 0; i < triangleCount; i += maxVertices / 3)
    {
        // Bereich der Dreiecke für das Sub-Mesh
        int subTriangleCount = Mathf.Min(maxVertices / 3, triangleCount - i);
        int[] subTriangles = new int[subTriangleCount * 3];
        Vector3[] subVertices = new Vector3[subTriangleCount * 3];
        Vector3[] subNormals = new Vector3[subTriangleCount * 3];
        Vector2[] subUV = new Vector2[subTriangleCount * 3];
        Color[] subColors = new Color[subTriangleCount * 3]; // Sub-Mesh-Farben

        Dictionary<int, int> vertexMap = new Dictionary<int, int>();
        int vertexIndex = 0;

        for (int j = 0; j < subTriangleCount; j++)
        {
            for (int k = 0; k < 3; k++)
            {
                int originalIndex = triangles[(i + j) * 3 + k];
                if (!vertexMap.ContainsKey(originalIndex))
                {
                    vertexMap[originalIndex] = vertexIndex;
                    subVertices[vertexIndex] = vertices[originalIndex];
                    subNormals[vertexIndex] = normals[originalIndex];
                    if (uv.Length > 0) subUV[vertexIndex] = uv[originalIndex];
                    if (colors.Length > 0) subColors[vertexIndex] = colors[originalIndex];
                    vertexIndex++;
                }
                subTriangles[j * 3 + k] = vertexMap[originalIndex];
            }
        }

        // Neues Sub-Mesh erstellen
        Mesh subMesh = new Mesh();
        subMesh.vertices = subVertices;
        subMesh.triangles = subTriangles;
        subMesh.normals = subNormals;
        if (uv.Length > 0) subMesh.uv = subUV;
        if (colors.Length > 0) subMesh.colors = subColors; // Vertex Colors hinzufügen

        // Neues GameObject erstellen
        GameObject subMeshObject = new GameObject(original.name + "_SubMesh_" + (subMeshes.Count + 1));
        subMeshObject.transform.SetParent(original.transform);
        subMeshObject.transform.localPosition = Vector3.zero;
        subMeshObject.transform.localRotation = Quaternion.identity;
        subMeshObject.transform.localScale = Vector3.one;

        // Mesh und Material anwenden
        MeshFilter subMeshFilter = subMeshObject.AddComponent<MeshFilter>();
        subMeshFilter.mesh = subMesh;

        MeshRenderer subMeshRenderer = subMeshObject.AddComponent<MeshRenderer>();
        subMeshRenderer.sharedMaterial = sharedMaterial;

        subMeshes.Add(subMeshObject);
    }

    // Ursprüngliches Objekt deaktivieren, wenn die Sub-Meshes erstellt wurden
    original.GetComponent<MeshRenderer>().enabled = false;

    return subMeshes;
}


    static int GetNewVertex(int i1, int i2)
    {
        // We have to test both directions since the edge
        // could be reversed in another triangle
        uint t1 = ((uint)i1 << 16) | (uint)i2;
        uint t2 = ((uint)i2 << 16) | (uint)i1;
        if (newVectices.ContainsKey(t2))
            return newVectices[t2];
        if (newVectices.ContainsKey(t1))
            return newVectices[t1];
        // generate vertex:
        int newIndex = vertices.Count;
        newVectices.Add(t1, newIndex);

        // calculate new vertex
        vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
        normals.Add((normals[i1] + normals[i2]).normalized);
        // [... all other vertex data arrays]

        return newIndex;
    }


    public static void Subdivide(Mesh mesh)
    {
        newVectices = new Dictionary<uint, int>();

        vertices = new List<Vector3>(mesh.vertices);
        normals = new List<Vector3>(mesh.normals);
        // [... all other vertex data arrays]
        indices = new List<int>();

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i + 0];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            int a = GetNewVertex(i1, i2);
            int b = GetNewVertex(i2, i3);
            int c = GetNewVertex(i3, i1);
            indices.Add(i1);
            indices.Add(a);
            indices.Add(c);
            indices.Add(i2);
            indices.Add(b);
            indices.Add(a);
            indices.Add(i3);
            indices.Add(c);
            indices.Add(b);
            indices.Add(a);
            indices.Add(b);
            indices.Add(c); // center triangle
        }

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        // [... all other vertex data arrays]
        mesh.triangles = indices.ToArray();


        // since this is a static function and it uses static variables
        // we should erase the arrays to free them:
        newVectices = null;
        vertices = null;
        normals = null;
        // [... all other vertex data arrays]

        indices = null;
    }
}