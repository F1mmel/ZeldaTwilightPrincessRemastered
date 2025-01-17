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

        // Hier kannst du mit dem Mesh arbeiten, z.B. es in eine Datei exportieren oder auf ein anderes GameObject anwenden
        string exportPath = "Assets/" + selectedGameObject.name + ".asset";
        string fullPath = "C:/Users/finne/Desktop/_Test/cache/" + selectedGameObject.name + ".asset";

        // Erstelle den Ordner, falls er nicht existiert
        string folderPath = "C:/Users/finne/Desktop/_Test/cache/";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        AssetDatabase.CreateAsset(mesh, exportPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Kopiere das Asset an den Zielort
        System.IO.File.Copy(exportPath, fullPath, true);

        // Lösche das Asset aus den Assets
        AssetDatabase.DeleteAsset(exportPath);
        AssetDatabase.Refresh();

        Debug.Log("Mesh exported to: " + fullPath);
    }
}public class TextureAtlasGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Texture Atlas")]
    static void GenerateAtlas()
    {
        // Hole das aktuell ausgewählte GameObject im Editor
        GameObject selectedObject = Selection.activeGameObject;

        // Falls kein GameObject ausgewählt ist, gib eine Fehlermeldung aus
        if (selectedObject == null)
        {
            Debug.LogError("Kein GameObject ausgewählt!");
            return;
        }

        // Hole den MeshRenderer des ausgewählten Objekts
        MeshRenderer meshRenderer = selectedObject.GetComponent<MeshRenderer>();

        // Falls kein MeshRenderer gefunden wurde, gib eine Fehlermeldung aus
        if (meshRenderer == null)
        {
            Debug.LogError("Kein MeshRenderer gefunden!");
            return;
        }

        // Hole alle Materialien des MeshRenderers
        Material[] materials = meshRenderer.sharedMaterials;
        Texture2D[] textures = new Texture2D[materials.Length];

        // Sammle die Texturen aus den Materialien
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].mainTexture is Texture2D texture)
            {
                textures[i] = texture;
            }
        }

        // Überprüfe, ob Texturen gefunden wurden
        if (textures.Length == 0)
        {
            Debug.LogError("Keine Texturen im Material gefunden!");
            return;
        }

        // Kombiniere die Texturen in einem Atlas
        Texture2D atlas = new Texture2D(2048, 2048);
        Rect[] rects = atlas.PackTextures(textures, 0, 2048);

        // Speichere den Atlas
        byte[] atlasBytes = atlas.EncodeToPNG();
        string atlasPath = "Assets/Atlas.png";
        System.IO.File.WriteAllBytes(atlasPath, atlasBytes);
        AssetDatabase.Refresh();

        // Erstelle das neue Material und weise den Atlas als Textur zu
        Material newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        newMaterial.mainTexture = atlas;

        // Setze das neue Material für das MeshRenderer
        meshRenderer.sharedMaterial = newMaterial;

        // Anpassen der UV-Koordinaten des Meshes
        AdjustMeshUVs(selectedObject, rects);

        // Gib eine Nachricht aus, dass der Atlas und das Material erfolgreich erstellt wurden.
        Debug.Log("Texture Atlas und Material wurden erfolgreich erstellt und angewendet.");
    }

    // Funktion zur Anpassung der UV-Koordinaten für das Mesh, einschließlich der SubMeshes
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

        // Hole die aktuellen UV-Koordinaten des Meshes
        Vector2[] uvs = mesh.uv;

        // Sammle alle Vertex- und Triangulardaten aus den SubMeshes
        int vertexOffset = 0;
        int triangleOffset = 0;
        var combinedVertices = new System.Collections.Generic.List<Vector3>();
        var combinedTriangles = new System.Collections.Generic.List<int>();
        var combinedUVs = new System.Collections.Generic.List<Vector2>();

        int subMeshCount = mesh.subMeshCount;

        // Gehe durch jedes SubMesh und kombiniere die Geometrie und UVs
        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvsSubMesh = mesh.uv;

            // Verschiebe die Indizes für die Vertices und Triangles
            foreach (var index in triangles)
            {
                combinedTriangles.Add(index + vertexOffset);
            }

            // Füge die Vertices des SubMeshes hinzu
            combinedVertices.AddRange(vertices);

            // Füge die UVs des SubMeshes hinzu und passe sie an die Atlas-Region an
            foreach (var uv in uvsSubMesh)
            {
                Rect rect = rects[subMeshIndex]; // Bestimme die Region im Atlas für dieses SubMesh
                Vector2 newUV = new Vector2(
                    uv.x * rect.width + rect.x,
                    uv.y * rect.height + rect.y
                );
                combinedUVs.Add(newUV);
            }

            // Verschiebe den vertexOffset für das nächste SubMesh
            vertexOffset += vertices.Length;
        }

        // Erstelle das kombinierte Mesh
        Mesh combinedMesh = new Mesh
        {
            vertices = combinedVertices.ToArray(),
            triangles = combinedTriangles.ToArray(),
            uv = combinedUVs.ToArray()
        };

        // Setze das kombinierte Mesh in den MeshFilter
        meshFilter.sharedMesh = combinedMesh;

        // Markiere das Mesh als "dirty", damit die Änderungen gespeichert werden
        EditorUtility.SetDirty(combinedMesh);
    }
}