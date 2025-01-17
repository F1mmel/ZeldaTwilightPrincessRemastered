using System;
using System.Collections.Generic;
using GameFormatReader.Common;
using JStudio.J3D.Animation;
using OpenTK;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using WiiExplorer;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

public class WeightDataGenerator : MonoBehaviour
{
    private List<Mesh> meshes;
    private List<Material> materials;
    private List<MeshFilter> filters;
    public Transform[] Bones;

    private Mesh combinedMesh;
    private Vector3[] combinedMeshVertices;

    public bool VisualizeBones;

    public void PrepareWeights(BMD bmd)
    {
        meshes = new List<Mesh>();
        materials = new List<Material>();
        filters = new List<MeshFilter>();

        FindMeshesAndMaterials(transform);

        List<Mesh> submeshes = new List<Mesh>();
        for (int i = 0; i < meshes.Count; i++)
        {
            List<CombineInstance> combiners = new List<CombineInstance>();

            MeshRenderer renderer = filters[i].GetComponent<MeshRenderer>();
            Material[] localMaterials = renderer.sharedMaterials;
            for (int j = 0; j < localMaterials.Length; j++)
            {
                CombineInstance instance = new CombineInstance();
                instance.mesh = filters[i].sharedMesh;
                instance.subMeshIndex = j;
                instance.transform = Matrix4x4.identity;
                    
                combiners.Add(instance);
            }

            Mesh submesh = new Mesh();
            submesh.CombineMeshes(combiners.ToArray(), true);
            submeshes.Add(submesh);
        }

        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        foreach (Mesh mesh in submeshes)
        {
            CombineInstance instance = new CombineInstance();
            instance.mesh = mesh;
            instance.subMeshIndex = 0;
            instance.transform = Matrix4x4.identity;
                    
            finalCombiners.Add(instance);
        }

        combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(finalCombiners.ToArray(), false);
        combinedMeshVertices = combinedMesh.vertices;

        // Füge den kombinierten Mesh zum SkinnedMeshRenderer hinzu
        Bones = FindBones(transform);
        GenerateBoneWeights(combinedMesh, bmd);
    }
    
    void Start()
    {
        //PrepareWeights();
    }

    private void FindMeshesAndMaterials(Transform parent)
    {
        foreach (Transform child in parent)
        {
            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();

            if (meshFilter != null && meshRenderer != null)
            {
                if(!child.gameObject.activeSelf) continue;
                
                filters.Add(meshFilter);
                meshes.Add(meshFilter.sharedMesh);
                materials.Add(meshRenderer.sharedMaterial);
            }

            // Rekursiv in die Tiefe gehen
            FindMeshesAndMaterials(child);
        }
    }

    private BCK bck;
    public BMD bmd;
    private void FixedUpdate()
    {
        if (bck != null)
        {
            /*if (bck == null)
            {
                ArcFile animFile = ArcReader.GetFile(bmd.Archive, "ba_fly.bck");
                byte[] buffer = animFile.Buffer;

                bck = new BCK(animFile.Name);
                using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
                {
                    bck.LoadFromStream(reader);
                }
                bck.Start();
            }
            else*/
            {
                //bck.ApplyPoseForSkinnedMeshRenderer(bmd.JNT1Tag.AnimatedJoints, Bones);
                //bck.Tick(0.02f);
            }
        }
    }

    private Transform[] FindBones(Transform parent)
    {
        List<Transform> bonesList = new List<Transform>();
        /*foreach (Transform child in parent)
        {
            if (child.childCount >= 1)
            {
                bonesList.Add(child);
                Transform[] childBones = FindBones(child);
                if (childBones != null && childBones.Length > 0)
                {
                    bonesList.AddRange(childBones);
                }
            }
        }*/

        foreach (GameObject bone in parent.gameObject.GetAllChildren())
        {
            //if(!bone.GetComponent<MeshFilter>() && !bone.name.Contains("al.bmd"))
                //bonesList.Add(bone.transform);
                
                if(bone.GetComponent<Joint>() != null) bonesList.Add(bone.transform);
        }
        
        // Always remove world_root?
        //bonesList.RemoveAt(0);
        
        return bonesList.ToArray();
    }

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private void GenerateBoneWeights(Mesh combinedMesh, BMD bmd)
    {
        if (skinnedMeshRenderer == null) skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null) skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        
        skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = combinedMesh;
        skinnedMeshRenderer.sharedMaterials = materials.ToArray();
        skinnedMeshRenderer.quality = SkinQuality.Bone4; // ???
        
        bmd._meshRenderers.Add(skinnedMeshRenderer);

        Vector3[] verts = combinedMesh.vertices;
        BoneWeight[] weights = new BoneWeight[verts.Length];
        
        
        /*IList<SkeletonJoint> boneList = bmd.JNT1Tag.AnimatedJoints;

        Matrix4[] boneTransforms = new Matrix4[boneList.Count];
        bmd.ApplyBonePositionsToAnimationTransforms(boneList, boneTransforms);*/

        int vertexCount = 0;
        foreach (var shape in bmd.SHP1Tag.Shapes)
        {
            // Iterate through each vertex
            for (int i = 0; i < shape.VertexData.Position.Count; i++)
            {
                // This is relative to the vertex's original packet's matrix table.  
                ushort posMtxIndex = (ushort)(shape.VertexData.PositionMatrixIndexes[i]);

                // We need to calculate which packet data table that is.
                int originalPacketIndex = 0;
                for (int p = 0; p < shape.MatrixDataTable.Count; p++)
                {
                    if (i >= shape.MatrixDataTable[p].FirstRelevantVertexIndex &&
                        i < shape.MatrixDataTable[p].LastRelevantVertexIndex)
                    {
                        originalPacketIndex = p;
                        break;
                    }
                }

                // Now that we know which packet this vertex belongs to, we can get the index from it.
                // If the Matrix Table index is 0xFFFF then it means "use previous", and we have to
                // continue backwards until it is no longer 0xFFFF.
                ushort matrixTableIndex;
                do
                {
                    matrixTableIndex = shape.MatrixDataTable[originalPacketIndex].MatrixTable[posMtxIndex];
                    originalPacketIndex--;
                } while (matrixTableIndex == 0xFFFF);

                bool isPartiallyWeighted = bmd.DRW1Tag.IsPartiallyWeighted[matrixTableIndex];
                ushort indexFromDRW1 = bmd.DRW1Tag.TransformIndexTable[matrixTableIndex];

                //Matrix4 finalMatrix = Matrix4.Zero;
                if (isPartiallyWeighted)
                {
                    EVP1.Envelope envelope = bmd.EVP1Tag.Envelopes[indexFromDRW1];
                    
                    BoneWeight weight = new BoneWeight();
                    float totalWeight = 0f;

                    if (envelope.NumBones == 1)
                    {
                        weight.boneIndex0 = envelope.BoneIndexes[0];
                        weight.weight0 = envelope.BoneWeights[0];
                        totalWeight = weight.weight0;
                    }
                    else if (envelope.NumBones == 2)
                    {
                        weight.boneIndex0 = envelope.BoneIndexes[0];
                        weight.weight0 = envelope.BoneWeights[0];

                        weight.boneIndex1 = envelope.BoneIndexes[1];
                        weight.weight1 = envelope.BoneWeights[1];

                        totalWeight = weight.weight0 + weight.weight1;
                    }
                    else if (envelope.NumBones == 3)
                    {
                        weight.boneIndex0 = envelope.BoneIndexes[0];
                        weight.weight0 = envelope.BoneWeights[0];

                        weight.boneIndex1 = envelope.BoneIndexes[1];
                        weight.weight1 = envelope.BoneWeights[1];

                        weight.boneIndex2 = envelope.BoneIndexes[2];
                        weight.weight2 = envelope.BoneWeights[2];

                        totalWeight = weight.weight0 + weight.weight1 + weight.weight2;
                    }
                    else if (envelope.NumBones == 4)
                    {
                        weight.boneIndex0 = envelope.BoneIndexes[0];
                        weight.weight0 = envelope.BoneWeights[0];

                        weight.boneIndex1 = envelope.BoneIndexes[1];
                        weight.weight1 = envelope.BoneWeights[1];

                        weight.boneIndex2 = envelope.BoneIndexes[2];
                        weight.weight2 = envelope.BoneWeights[2];

                        weight.boneIndex3 = envelope.BoneIndexes[3];
                        weight.weight3 = envelope.BoneWeights[3];

                        totalWeight = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
                    }

// Normalize the weights if the total weight is greater than zero
                    if (totalWeight > 0f)
                    {
                        weight.weight0 /= totalWeight;
                        weight.weight1 /= totalWeight;
                        weight.weight2 /= totalWeight;
                        weight.weight3 /= totalWeight;
                    }
                    else
                    {
                        // Handle case where all weights are zero to prevent errors
                        weight.weight0 = 1f;
                        weight.weight1 = 0f;
                        weight.weight2 = 0f;
                        weight.weight3 = 0f;
                    }

                    weights[vertexCount] = weight;

                }
                else
                {
                    // If the vertex is not weighted then we use a 1:1 movement with the bone matrix.
                    BoneWeight weight = new BoneWeight();
                    weight.boneIndex0 = indexFromDRW1;
                    weight.weight0 = 1f;
                    //weights[vertexCount] = weight;
                    
                    if (vertexCount >= 0 && vertexCount < weights.Length)
                    {
                        weights[vertexCount] = weight;
                    }
                }
                
                vertexCount++;
            }
        }
        combinedMesh.boneWeights = weights;

        Matrix4x4[] bindPoses = new Matrix4x4[Bones.Length];
        for (int i = 0; i < bindPoses.Length; i++)
        {
            bindPoses[i] = Bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        combinedMesh.bindposes = bindPoses;

        skinnedMeshRenderer.bones = Bones;
        skinnedMeshRenderer.sharedMesh = combinedMesh;
    }
    
    private void GenerateBoneWeightsForLink(Mesh combinedMesh, params BMD[] bmds)
    {
        if (skinnedMeshRenderer == null) skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        
        skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = combinedMesh;
        skinnedMeshRenderer.sharedMaterials = materials.ToArray();
        skinnedMeshRenderer.quality = SkinQuality.Bone4;

        Vector3[] verts = combinedMesh.vertices;
        BoneWeight[] weights = new BoneWeight[verts.Length];

        int vertexCount = 0;
        foreach (BMD bmd in bmds)
        {
            foreach (var shape in bmd.SHP1Tag.Shapes)
            {
                for (int i = 0; i < shape.VertexData.Position.Count; i++)
                {
                    ushort posMtxIndex = (ushort)(shape.VertexData.PositionMatrixIndexes[i]);

                    int originalPacketIndex = 0;
                    for (int p = 0; p < shape.MatrixDataTable.Count; p++)
                    {
                        if (i >= shape.MatrixDataTable[p].FirstRelevantVertexIndex &&
                            i < shape.MatrixDataTable[p].LastRelevantVertexIndex)
                        {
                            originalPacketIndex = p;
                            break;
                        }
                    }

                    ushort matrixTableIndex;
                    do
                    {
                        matrixTableIndex = shape.MatrixDataTable[originalPacketIndex].MatrixTable[posMtxIndex];
                        originalPacketIndex--;
                    } while (matrixTableIndex == 0xFFFF);

                    bool isPartiallyWeighted = bmd.DRW1Tag.IsPartiallyWeighted[matrixTableIndex];
                    ushort indexFromDRW1 = bmd.DRW1Tag.TransformIndexTable[matrixTableIndex];

                    if (isPartiallyWeighted)
                    {
                        EVP1.Envelope envelope = bmd.EVP1Tag.Envelopes[indexFromDRW1];

                        BoneWeight weight = new BoneWeight();
                        float totalWeight = 0f;

                        if (envelope.NumBones == 1)
                        {
                            weight.boneIndex0 = envelope.BoneIndexes[0];
                            weight.weight0 = envelope.BoneWeights[0];
                            totalWeight = weight.weight0;
                        }
                        else if (envelope.NumBones == 2)
                        {
                            weight.boneIndex0 = envelope.BoneIndexes[0];
                            weight.weight0 = envelope.BoneWeights[0];

                            weight.boneIndex1 = envelope.BoneIndexes[1];
                            weight.weight1 = envelope.BoneWeights[1];

                            totalWeight = weight.weight0 + weight.weight1;
                        }
                        else if (envelope.NumBones == 3)
                        {
                            weight.boneIndex0 = envelope.BoneIndexes[0];
                            weight.weight0 = envelope.BoneWeights[0];

                            weight.boneIndex1 = envelope.BoneIndexes[1];
                            weight.weight1 = envelope.BoneWeights[1];

                            weight.boneIndex2 = envelope.BoneIndexes[2];
                            weight.weight2 = envelope.BoneWeights[2];

                            totalWeight = weight.weight0 + weight.weight1 + weight.weight2;
                        }
                        else if (envelope.NumBones == 4)
                        {
                            weight.boneIndex0 = envelope.BoneIndexes[0];
                            weight.weight0 = envelope.BoneWeights[0];

                            weight.boneIndex1 = envelope.BoneIndexes[1];
                            weight.weight1 = envelope.BoneWeights[1];

                            weight.boneIndex2 = envelope.BoneIndexes[2];
                            weight.weight2 = envelope.BoneWeights[2];

                            weight.boneIndex3 = envelope.BoneIndexes[3];
                            weight.weight3 = envelope.BoneWeights[3];

                            totalWeight = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
                        }

                        if (totalWeight > 0f)
                        {
                            weight.weight0 /= totalWeight;
                            weight.weight1 /= totalWeight;
                            weight.weight2 /= totalWeight;
                            weight.weight3 /= totalWeight;
                        }
                        else
                        {
                            weight.weight0 = 1f;
                            weight.weight1 = 0f;
                            weight.weight2 = 0f;
                            weight.weight3 = 0f;
                        }

                        weights[vertexCount] = weight;

                    }
                    else
                    {
                        BoneWeight weight = new BoneWeight();
                        weight.boneIndex0 = indexFromDRW1;
                        weight.weight0 = 1f;
                        weights[vertexCount] = weight;
                    }

                    vertexCount++;
                }
            }
        }

        combinedMesh.boneWeights = weights;

        Matrix4x4[] bindPoses = new Matrix4x4[Bones.Length];
        for (int i = 0; i < bindPoses.Length; i++)
        {
            bindPoses[i] = Bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        combinedMesh.bindposes = bindPoses;

        skinnedMeshRenderer.bones = Bones;
        skinnedMeshRenderer.sharedMesh = combinedMesh;
    }

    Transform FindClosestBone(Vector3 vertex)
    {
        Transform closestBone = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform bone in Bones)
        {
            if(bone.name.Equals("world_root") || bone.name.Equals("center")) continue;
            
            float distance = Vector3.Distance(vertex, bone.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBone = bone;
            }
        }
        return closestBone;
    }

    Transform FindClosestBoneExclude(Vector3 vertex, params Transform[] excludeBone)
    {
        Transform closestBone = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform bone in Bones)
        {
            if(bone.name.Equals("world_root") || bone.name.Equals("center")) continue;
            foreach(Transform ex in excludeBone) if (bone == ex) continue;

            float distance = Vector3.Distance(vertex, bone.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBone = bone;
            }
        }
        return closestBone;
    }

    void OnDrawGizmos()
    {
        if(!VisualizeBones) return;
        
        if (Bones == null || combinedMesh == null)
            return;

        Vector3[] verts = combinedMeshVertices;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 vertex = transform.TransformPoint(verts[i]);

            Transform closestBone = FindClosestBone(vertex);
            //Transform secondClosestBone = FindSecondClosestBone(vertex, closestBone);

            if (closestBone != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(vertex, closestBone.position);
            }

            //if (secondClosestBone != null)
            {
                Gizmos.color = Color.blue;
                //Gizmos.DrawLine(vertex, secondClosestBone.position);
            }
        }
    }
}
