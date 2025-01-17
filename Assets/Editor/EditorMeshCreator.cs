using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorMeshCreator
{
    
    [MenuItem("Custom/Apply Mesh From Selected GameObject")]
    public static void ApplyMeshFromSelectedGameObject()
    {
        GameObject selectedGameObject = Selection.activeGameObject;

        if (selectedGameObject == null)
        {
            Debug.LogError("No GameObject selected!");
            return;
        }

        MeshFilter meshFilter = selectedGameObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError("Selected GameObject does not have a MeshFilter component!");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;

        string exportPath = "Assets/" + selectedGameObject.name + ".asset";
        string fullPath = "C:/Users/finne/Desktop/_Test/cache/" + selectedGameObject.name + ".asset";

        string folderPath = "C:/Users/finne/Desktop/_Test/cache/";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        AssetDatabase.CreateAsset(mesh, exportPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        System.IO.File.Copy(exportPath, fullPath, true);

        AssetDatabase.DeleteAsset(exportPath);
        AssetDatabase.Refresh();

        Debug.Log("Mesh exported to: " + fullPath);
    }
}public class TextureAtlasGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Texture Atlas")]
    static void GenerateAtlas()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogError("Kein GameObject ausgewählt!");
            return;
        }

        MeshRenderer meshRenderer = selectedObject.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogError("Kein MeshRenderer gefunden!");
            return;
        }

        Material[] materials = meshRenderer.sharedMaterials;
        Texture2D[] textures = new Texture2D[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].mainTexture is Texture2D texture)
            {
                textures[i] = texture;
            }
        }

        if (textures.Length == 0)
        {
            Debug.LogError("Keine Texturen im Material gefunden!");
            return;
        }

        Texture2D atlas = new Texture2D(2048, 2048);
        Rect[] rects = atlas.PackTextures(textures, 0, 2048);

        byte[] atlasBytes = atlas.EncodeToPNG();
        string atlasPath = "Assets/Atlas.png";
        System.IO.File.WriteAllBytes(atlasPath, atlasBytes);
        AssetDatabase.Refresh();

        Material newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        newMaterial.mainTexture = atlas;

        meshRenderer.sharedMaterial = newMaterial;

        AdjustMeshUVs(selectedObject, rects);

        Debug.Log("Texture Atlas und Material wurden erfolgreich erstellt und angewendet.");
    }

    static void AdjustMeshUVs(GameObject selectedObject, Rect[] rects)
    {
        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("Kein MeshFilter gefunden!");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;

        if (mesh == null || mesh.uv == null || mesh.uv.Length == 0)
        {
            Debug.LogError("Keine UV-Koordinaten im Mesh gefunden!");
            return;
        }

        Vector2[] uvs = mesh.uv;

        int vertexOffset = 0;
        int triangleOffset = 0;
        var combinedVertices = new System.Collections.Generic.List<Vector3>();
        var combinedTriangles = new System.Collections.Generic.List<int>();
        var combinedUVs = new System.Collections.Generic.List<Vector2>();

        int subMeshCount = mesh.subMeshCount;

        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvsSubMesh = mesh.uv;

            foreach (var index in triangles)
            {
                combinedTriangles.Add(index + vertexOffset);
            }

            combinedVertices.AddRange(vertices);

            foreach (var uv in uvsSubMesh)
            {
                Rect rect = rects[subMeshIndex];
                Vector2 newUV = new Vector2(
                    uv.x * rect.width + rect.x,
                    uv.y * rect.height + rect.y
                );
                combinedUVs.Add(newUV);
            }

            vertexOffset += vertices.Length;
        }

        Mesh combinedMesh = new Mesh
        {
            vertices = combinedVertices.ToArray(),
            triangles = combinedTriangles.ToArray(),
            uv = combinedUVs.ToArray()
        };

        meshFilter.sharedMesh = combinedMesh;

        EditorUtility.SetDirty(combinedMesh);
    }
}