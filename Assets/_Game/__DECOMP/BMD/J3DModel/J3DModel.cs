using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFormatReader.Common;
using JStudio.J3D;
using OpenTK;
using Unity.VisualScripting;
using UnityEngine;
using WiiExplorer;
using ZeldaTesting;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

//public class J3DModel : BMD
public class J3DModel : MonoBehaviour
{    
    
    public string SelectedArcFile = "B_dr.arc";
    public string SelectedBMD = "dr.bmd";
    public string SelectedBCK;
    
    public string Name;
    public List<BTI> ExternalTextures = new List<BTI>();

    public bool IsModel;

    private static string path = "Assets/GameFiles/res/Stage/";
    private static string pathHD = "Assets/GameFilesHD/res/Object/";
    
    public INF1 INF1Tag;
    public VTX1 VTX1Tag;
    public EVP1 EVP1Tag;
    public DRW1 DRW1Tag;
    public JNT1 JNT1Tag;
    public SHP1 SHP1Tag;
    public MAT3 MAT3Tag;
    public TEX1 TEX1Tag;
    
    private List<GameObject> Childs = new List<GameObject>();
    
    private List<MeshFilter> meshFilters = new List<MeshFilter>();
    private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    private List<Material> meshMaterials = new List<Material>();
    public List<TexMatrix[]> TexMatrixIndexes = new List<TexMatrix[]>();

    private byte[] buffer;

    public void Parse(string name, byte[] Buffer)
    {
        buffer = Buffer;
        Name = name;
        
        using (EndianBinaryReader reader = new EndianBinaryReader(Buffer, Endian.Big))
        {
            // Read the J3D Header
            reader.Skip(8);
            int Size = reader.ReadInt32();
            int NumChunks = reader.ReadInt32();

            // Skip over an unused tag ("SVR3") which is consistent in all models.
            reader.Skip(16);

            GetCheckStarts(reader, NumChunks);
        }
        
        // What is needed first, erst das laden -> WhatsApp
        if(IsModel) CreateHierarchy(transform);
        
        _ = LoadRemainingTagsAsync();
    }

    public void Parse(ArcFile file)
    {
        Parse(file.Name, file.Buffer);
    }
    
    private async Task LoadRemainingTagsAsync()
    {
        await Task.Run(() =>
        {
            using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
            {
                try
                {
                    // Read VTX1
                    reader.BaseStream.Position = VTX1Start + 4;
                    int VTX1Size = reader.ReadInt32();
                    VTX1Tag = new VTX1();
                    VTX1Tag.LoadVTX1FromStream(reader, VTX1Start, VTX1Size);

                    // Read SHP1
                    reader.BaseStream.Position = SHP1Start + 8;
                    SHP1Tag = new SHP1();
                    SHP1Tag.ReadSHP1FromStream(reader, SHP1Start, VTX1Tag.VertexData);

                    // Read EVP1
                    reader.BaseStream.Position = EVP1Start + 8;
                    EVP1Tag = new EVP1();
                    EVP1Tag.LoadEVP1FromStream(reader, EVP1Start);

                    // Read MAT3
                    reader.BaseStream.Position = MAT3Start + 8;
                    MAT3Tag = new MAT3();
                    MAT3Tag.LoadMAT3FromStream(reader, MAT3Start);

                    // Read DRW1
                    reader.BaseStream.Position = DRW1Start + 8;
                    DRW1Tag = new DRW1();
                    DRW1Tag.LoadDRW1FromStream(reader, DRW1Start);

                    // Read TEX1
                    reader.BaseStream.Position = TEX1Start + 8;
                    TEX1Tag = new TEX1();
                    TEX1Tag.LoadTEX1FromStreamRaw(reader, TEX1Start, ExternalTextures);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            INF1Tag.AssignMaterialIndexes(SHP1Tag);
        });

        List<GameObject> shapeObjects = CreateShapeObjects();
        await Task.Run(() =>
        {
            try
            {
                CreateShapes(shapeObjects);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        });
        
        if(ZeldaManager.Instance.CombineMeshes)
            CombineMeshes();
        else
        {
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
        }
    }

    private void CreateHierarchy(Transform parent)
    {
        Dictionary<string, GameObject> jointGameObjects = new Dictionary<string, GameObject>();
        foreach (SkeletonJoint joint in JNT1Tag.BindJoints)
        {
            GameObject jointGameObject = new GameObject(joint.Name);
            jointGameObjects.Add(joint.Name, jointGameObject);
            jointGameObject.transform.parent = parent;
            jointGameObject.tag = "Joint";
        }
        
        foreach (SkeletonJoint joint in JNT1Tag.BindJoints)
        {
            GameObject jointGameObject = jointGameObjects[joint.Name];

            // Setze den Eltern-Joint, falls vorhanden.
            if (joint.Parent != null)
            {
                GameObject parentGameObject = jointGameObjects[joint.Parent.Name];
                jointGameObject.transform.parent = parentGameObject.transform;

                jointGameObject.transform.localPosition =
                    new Vector3(joint.Translation.X, joint.Translation.Y, joint.Translation.Z);
                jointGameObject.transform.localRotation = new Quaternion(joint.Rotation.X, joint.Rotation.Y,
                    joint.Rotation.Z, joint.Rotation.W);
                jointGameObject.transform.localScale = new Vector3(joint.Scale.X, joint.Scale.Y, joint.Scale.Z);
            }
            else
            {
                // Root
                jointGameObject.transform.localPosition =
                    new Vector3(joint.Translation.X, joint.Translation.Y, joint.Translation.Z);
                jointGameObject.transform.localRotation = new Quaternion(joint.Rotation.X, joint.Rotation.Y,
                    joint.Rotation.Z, joint.Rotation.W);
                jointGameObject.transform.localScale = new Vector3(joint.Scale.X, joint.Scale.Y, joint.Scale.Z);
            }
        }
    }

    private List<GameObject> CreateShapeObjects()
    {
        List<GameObject> objects = new List<GameObject>();
        for (int i = 0; i < SHP1Tag.Shapes.Count; i++)
        {
            GameObject o = new GameObject("Shape" + i);
            o.transform.parent = transform;

            o.transform.localPosition = Vector3.zero;
            o.transform.localEulerAngles = Vector3.zero;
            o.transform.localScale = Vector3.one;
            
            objects.Add(o);
        }

        return objects;
    }

    private void CreateShapes(List<GameObject> shapeObjects)
    {
        IList<SkeletonJoint> boneList = JNT1Tag.AnimatedJoints;

        Matrix4[] boneTransforms = new Matrix4[boneList.Count];
        ApplyBonePositionsToAnimationTransforms(boneList, boneTransforms);
        
        for (int i = 0; i < SHP1Tag.Shapes.Count; i++)
        {
            int shapeIndex = i;
            SHP1.Shape shape = SHP1Tag.Shapes[i];
            
            if (IsModel)
            {
                var transformedPositions = new List<OpenTK.Vector3>(shape.VertexData.Position.Count);
                var transformedNormals = new List<OpenTK.Vector3>(shape.VertexData.Normal.Count);

                for (int j = 0; j < shape.VertexData.Position.Count; j++)
                {
                    // This is relative to the vertex's original packet's matrix table.  
                    ushort posMtxIndex = (ushort)(shape.VertexData.PositionMatrixIndexes[j]);

                    // We need to calculate which packet data table that is.
                    int originalPacketIndex = 0;
                    for (int p = 0; p < shape.MatrixDataTable.Count; p++)
                    {
                        if (j >= shape.MatrixDataTable[p].FirstRelevantVertexIndex &&
                            j < shape.MatrixDataTable[p].LastRelevantVertexIndex)
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

                    bool isPartiallyWeighted = DRW1Tag.IsPartiallyWeighted[matrixTableIndex];
                    ushort indexFromDRW1 = DRW1Tag.TransformIndexTable[matrixTableIndex];

                    Matrix4 finalMatrix = Matrix4.Zero;
                    if (isPartiallyWeighted)
                    {
                        EVP1.Envelope envelope = EVP1Tag.Envelopes[indexFromDRW1];
                        for (int b = 0; b < envelope.NumBones; b++)
                        {
                            Matrix4 sm1 = EVP1Tag.InverseBindPose[envelope.BoneIndexes[b]];
                            Matrix4 sm2 = boneTransforms[envelope.BoneIndexes[b]];

                            finalMatrix += Matrix4.Mult(Matrix4.Mult(sm1, sm2), envelope.BoneWeights[b]);
                        }
                    }
                    else
                    {
                        // If the vertex is not weighted then we use a 1:1 movement with the bone matrix.
                        finalMatrix = boneTransforms[indexFromDRW1];
                    }

                    // Multiply the data from the model file by the finalMatrix to put it in the correct (skinned) position
                    transformedPositions.Add(OpenTK.Vector3.Transform(shape.VertexData.Position[j], finalMatrix));

                    if (shape.VertexData.Normal.Count > 0)
                    {
                        OpenTK.Vector3 transformedNormal =
                            OpenTK.Vector3.TransformNormal(shape.VertexData.Normal[j], finalMatrix);
                        transformedNormals.Add(transformedNormal);
                    }

                    //colorOverride.Add(isPartiallyWeighted ? WLinearColor.Black : WLinearColor.White);
                }

                // Re-upload to the GPU.
                shape.OverrideVertPos = transformedPositions;
                //shape.VertexData.Color0 = colorOverride;
                if (transformedNormals.Count > 0)
                    shape.OverrideNormals = transformedNormals;
            }

            //string materialName = MAT3Tag.MaterialNameTable[shapeIndex];
            Material3 material = MAT3Tag.MaterialList[MAT3Tag.MaterialRemapTable[shape.MaterialIndex]];

            BinaryTextureImage texture = null;
            BinaryTextureImage vertexColorTexture = null;
            
            int baseTexture = material.TextureIndexes[0];
            if (baseTexture >= 0) texture = TEX1Tag.BinaryTextureImages[MAT3Tag.TextureRemapTable[baseTexture]];
            else if (baseTexture == -1)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    DestroyImmediate(shapeObjects[shapeIndex]);
                });
                continue;
            }

            int vertexTexture = material.TextureIndexes[1];
            if (vertexTexture >= 0)
            {
                int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                if (remap == 1 && material.TextureIndexes[2] != -1)
                    vertexTexture = material.TextureIndexes[2]; // Get second texture, first probably fake cloud
                vertexColorTexture = TEX1Tag.BinaryTextureImages[MAT3Tag.TextureRemapTable[vertexTexture]];
            }

            if (shapeIndex == 22)
            {
                Debug.LogError(material.TextureIndexes.Length);

                foreach (var a in material.TextureIndexes)
                {
                    Debug.LogError(a);
                }
            }

            List<OpenTK.Vector3> vertPos =
                shape.OverrideVertPos.Count > 0 ? shape.OverrideVertPos : shape.VertexData.Position;
            List<OpenTK.Vector3> vertNormal =
                shape.OverrideNormals.Count > 0 ? shape.OverrideNormals : shape.VertexData.Normal;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            foreach (OpenTK.Vector3 pos in vertPos) verts.Add(new Vector3(pos.X, pos.Y, pos.Z)); 
            foreach (OpenTK.Vector3 pos in vertNormal) normals.Add(new Vector3(pos.X, pos.Y, pos.Z));

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                shapeObjects[shapeIndex].name = shapeIndex + "_" + material.Name + "_" + texture?.Name;
                CreateMesh(shapeIndex, shapeObjects[shapeIndex], verts, normals, texture, vertexColorTexture, material);
            });
        }
    }
    
    public void CreateMesh(int shapeIndex, GameObject child, List<Vector3> vertices, List<Vector3> normals, BinaryTextureImage bti,
        BinaryTextureImage vertexBti, Material3 mat)
    {
        SHP1.Shape shape = SHP1Tag.Shapes[shapeIndex];
        
        bool forceDoubleSided = false;
        if (mat.BlendModeIndex.Type == GXBlendMode.Blend)
        {
            if (mat.TEVKonstAlphaSelectors[0] == GXKonstAlphaSel.KASel_1)
            {
                forceDoubleSided = true;
            }
        }
        
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();

        int numVertices = vertices.Count;
        int numTriangles = numVertices / 3;

        if (ZeldaManager.Instance.DoubleSided || IsModel || forceDoubleSided)
        {
            int[] triangles = new int[numTriangles * 3 * 2];            // Double sided
            for (int i = 0; i < numTriangles; i++)
            {
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;

                // Backside of triangles
                triangles[(numTriangles + i) * 3] = i * 3 + 2;
                triangles[(numTriangles + i) * 3 + 1] = i * 3 + 1;
                triangles[(numTriangles + i) * 3 + 2] = i * 3;
            }
            mesh.triangles = triangles;
        }
        else
        {
            int[] triangles = new int[numTriangles * 3];            // Single sided
            for (int i = 0; i < numTriangles; i++)
            {
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }
            mesh.triangles = triangles;
        }

        mesh.normals = normals.ToArray();

        MeshFilter meshFilter = child.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        mesh.uv = shape.VertexData.Tex0.ToArray();        
        mesh.uv2 = shape.VertexData.Tex1.ToArray();
        mesh.uv3 = shape.VertexData.Tex2.ToArray();
        mesh.uv4 = shape.VertexData.Tex3.ToArray();
        mesh.uv5 = shape.VertexData.Tex4.ToArray();
        mesh.uv6 = shape.VertexData.Tex5.ToArray();
        mesh.uv7 = shape.VertexData.Tex6.ToArray();
        mesh.uv8 = shape.VertexData.Tex7.ToArray();

        List<Color> colors = new List<Color>();
        foreach (WLinearColor linearColor in shape.VertexData.Color0) colors.Add(new Color(linearColor.R, linearColor.G, linearColor.B, linearColor.A));
        mesh.colors = colors.ToArray();


        MeshRenderer meshRenderer = null;
        if (child.GetComponent<MeshRenderer>()) meshRenderer = child.GetComponent<MeshRenderer>();
        else meshRenderer = child.AddComponent<MeshRenderer>();

        Texture2D texture = bti.SkiaToTexture();
        Texture2D vertexTexture = vertexBti?.SkiaToTexture();

        // Default material
        Material material = new Material(StageLoader.Instance.DefaultMaterial);
        material.mainTexture = texture;
        material.mainTextureOffset = new Vector2(0, 0);
        material.SetVector("_Offset", new Vector4(0, 0, 0, 0));

        if (child.name == "22_dd_MA01_Mizusoko_v_x_I_mizusoko")
        {
            Debug.Log(mat.BlendModeIndex.Type);
            Debug.Log(mat.TEVKonstAlphaSelectors[0]);
        }

        if (vertexBti != null)
        {
            //if (!vertexBti.Name.Contains("_kumo") || !vertexBti.Name.Contains("M_AllShadow_mori3"))           // Wenn ja, dann immer gras?
            if ((mat.BlendModeIndex.Type != GXBlendMode.Blend && !vertexBti.Name.Contains("_kumo") &&
                 !vertexBti.Name.Contains("mip")) || mat.Name.Contains("AllShadow")) // Wenn ja, dann immer gras?
            {
                if (!mat.Name.Contains("eye") && !vertexBti.Name.Contains("C_SM_Kokage01") &&
                    !vertexBti.Name.Contains("kokage") && !vertexBti.Name.Contains("C_SM_Kokage00") &&
                    !vertexBti.Name.Contains("M_AllShadow_mori3"))
                {
                    material = new Material(Shader.Find("Zelda/GameVertex 1"));
                    material.SetTexture("_BlendTexture", vertexTexture);
                    material.SetFloat("_AlphaClip", 1);
                    material.EnableKeyword("_ALPHATEST_ON");

                    // Magnet material
                    if (material.name.Contains("Magnet_v_x"))
                    {
                        material.SetFloat("_Min", 1f);
                    }
                }
            }
            else
            {
                if (mat.BlendModeIndex.Type == GXBlendMode.Blend)
                {
                    if (mat.TEVKonstAlphaSelectors[0] == GXKonstAlphaSel.KASel_1)
                    {
                        material = new Material(StageLoader.Instance.TransparentSurface);

                        material.SetFloat("_AlphaClip", 0);
                        material.EnableKeyword("_ALPHATEST_OFF");
                        material.SetFloat("_Smoothness", 0f);
                    }
                }
            }
        }
        else
        {
            if (mat.BlendModeIndex.Type == GXBlendMode.Blend)
            {
                material = new Material(StageLoader.Instance.TransparentSurface);

                material.SetFloat("_AlphaClip", 0);
                material.EnableKeyword("_ALPHATEST_OFF");
                material.SetFloat("_Smoothness", 0f);
            }
        }

        // Leaves
        List<string> shaderWind = new List<string>
            { "nukiset", "leaf", "iriguchi", "Sida_v", "PlantSet", "kiyane" /*, "tuta_v"*/ };
        List<string> notAllowedWind = new List<string>
            { "leafwall", "bb_iriguchi_v" };

        foreach (string wind in shaderWind)
        foreach (string notAllowed in notAllowedWind)
            if (mat.Name.ToLower().Contains(wind.ToLower()) && !mat.Name.ToLower().Contains(notAllowed.ToLower()))
            {
                material = new Material(Shader.Find("Shader Graphs/Wind"));
            
                // Remove collider
                //DestroyImmediate(child.GetComponent<MeshCollider>());
                
            }

        // Water
        /*if (mat.IsTranslucent)
        {
            child.SetActive(false);
            return;
        }*/

        // Crystal
        if (child.name.Contains("ee_omote_v_x"))
        {
            material = new Material(StageLoader.Instance.TransparentSurface);

            material.SetFloat("_AlphaClip", 0);
            material.EnableKeyword("_ALPHATEST_OFF");
            material.SetFloat("_Smoothness", 0f);
        }
        
        if (mat.Name.Contains("Sunbeam"))
        {
            material = new Material(StageLoader.Instance.Sunbeam);
            //material.mainTexture = bti.Texture;
            //material.mainTexture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[1]].Texture;        // Use generic texture

            // Add sunbeam movement
            /*child.*/transform.AddComponent<SunbeamMovement>();
        }

        // Warp portal
        if (Name.Equals("ef_brportal"))
        {
            material = new Material(StageLoader.Instance.DefaultMaterial);

            material.mainTexture = texture;
            //material.mainTextureOffset = new Vector2(0, 1);
            //material.SetVector("_Offset", new Vector4(0, 1, 0, 0));
        }

        if (material.name.Equals("mat_1polyplane"))
        {
            //material.SetFloat("_AlphaClip", 1);
            //material.SetFloat("_Cutoff", 0.05f);
            //material.EnableKeyword("_ALPHATEST_ON");
        }

        if (child.name.ToLower().Contains("kage"))
        {
            material = new Material(StageLoader.Instance.KageInterior);
        }

        // Disable certain shapes
        if (StageLoader.Instance != null)
        {
            foreach (string disabled in StageLoader.Instance.DisabledShapes.ShapeNames)
                if (child.name.ToLower().Contains(disabled.ToLower()))
                {
                    child.SetActive(false);
                    return;
                }
        }

        if (material.mainTexture == null)
            material.mainTexture = texture;

        if (child.name.ToLower().Contains("water") /* && child.name.ToLower().Contains("mip")*/ && child)
        {
            // Not allowed
            if (!child.name.Contains("waterwall") && !Name.Contains("onsen") &&
                !child.name.Contains("MA09_MeraWater_v_x") && !child.name.Contains("MA19_SW_v_x_watertest"))
                child.SetActive(false);
        }
        else
        {
            if (bti != null)
            {
                if (bti.Name.Equals("MODEL_BEHIND_DOOR"))
                {
                    material.mainTexture = StageLoader.Instance.TextureBehindDoor;
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                    float factor = Mathf.Pow(2, 6);
                    Color color = new Color(0.2264151f * factor, 0.2264151f * factor, 0.2264151f * factor);
                    material.SetColor("_EmissionColor", color);
                } 
            }


            if (material.mainTexture == null) material.mainTexture = StageLoader.Instance.FallbackTexture;

            if (bti != null)
            {
                if (bti.WrapS == BinaryTextureImage.WrapModes.Repeat)
                    material.mainTexture.wrapModeU = TextureWrapMode.Repeat;
                else if (bti.WrapS == BinaryTextureImage.WrapModes.MirroredRepeat)
                    material.mainTexture.wrapModeU = TextureWrapMode.Mirror;
                else if (bti.WrapS == BinaryTextureImage.WrapModes.ClampToEdge)
                    material.mainTexture.wrapModeU = TextureWrapMode.Clamp;

                if (bti.WrapT == BinaryTextureImage.WrapModes.Repeat)
                    material.mainTexture.wrapModeV = TextureWrapMode.Repeat;
                else if (bti.WrapT == BinaryTextureImage.WrapModes.MirroredRepeat)
                    material.mainTexture.wrapModeV = TextureWrapMode.Mirror;
                else if (bti.WrapT == BinaryTextureImage.WrapModes.ClampToEdge)
                    material.mainTexture.wrapModeV = TextureWrapMode.Clamp;
            }
            
            /*
            if (material.mainTexture.wrapModeV != TextureWrapMode.Mirror)
            {
                Vector2[] uvs = mesh.uv;
                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] = new Vector2(uvs[i].x, 1 - uvs[i].y);
                }

                mesh.uv = uvs;
            }*/

            // Real model? Not any stage model
            if (IsModel && material.mainTexture.wrapModeU == TextureWrapMode.Clamp &&
                material.mainTexture.wrapModeV == TextureWrapMode.Clamp)
            {
                material.mainTexture.wrapMode = TextureWrapMode.Repeat;
            }

            // Special cases
            if (Name.Equals("m_takaradai_top"))
            {
                material.mainTexture.wrapModeU = TextureWrapMode.Repeat;
                material.mainTexture.wrapModeV = TextureWrapMode.Mirror;
            }
        }

        if (child.name.Contains("fbtex_dummy") || child.name.Contains("water02") || child.name.Contains("Water_mip") ||
            child.name.Contains("Nigori") || child.name.Contains("watertest"))
        {
            if (StageLoader.Instance.Stage == Stage.DiababaArena)
                material = new Material(StageLoader.Instance.ToxicPurple);
            else if (StageLoader.Instance.Stage == Stage.SacredGrove)
            {
                material = new Material(StageLoader.Instance.GrayscaleWater);

                // Create mesh again, now with different material
                GameObject go = GameObject.Instantiate(child) as GameObject;
                go.transform.parent = child.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.SetActive(true);

                go.GetComponent<MeshRenderer>().sharedMaterial = StageLoader.Instance.Grayscale;

                child.transform.localScale = new Vector3(1.1f, 1f, 1f);
            }
            else
            {
                // Check if water is hot water
                if (Name.Contains("onsen")) material = new Material(StageLoader.Instance.HotWater);
                else if (mat.Name.Contains("Oil")) material = new Material(StageLoader.Instance.LanternOil);
                else if (mat.Name.Contains("MA19_SW")) material = new Material(StageLoader.Instance.IceWater);
                else material = new Material(StageLoader.Instance.Water);
            }

            child.SetActive(true);

            //GetComponent<Collider>().enabled = false;

            //child.SetActive(false);
            //return;
        }

        if (child.name.Contains("yogan"))
        {
            material = new Material(StageLoader.Instance.Lava);
            child.SetActive(true);

            //GetComponent<Collider>().enabled = false;
        }

        //if (Name.Equals("ef_yoganbashira")) 
        //material = new Material(StageLoader.Instance.Lava);

        if (StageLoader.Instance != null)
        {
            if (StageLoader.Instance.Stage == Stage.PalaceofTwilight ||
                StageLoader.Instance.Stage == Stage.PalaceofTwilightThroneRoom ||
                StageLoader.Instance.Stage == Stage.ZantArenas ||
                StageLoader.Instance.Stage == Stage.PhantomZantArena1 ||
                StageLoader.Instance.Stage == Stage.PhantomZantArena2)
            {
                material.SetFloat("_AlphaClip", 0);
                material.SetFloat("_Cutoff", 0f);
                material.SetFloat("_AlphaThreshold", 0f);
            }
        }


        if (material.mainTexture == null && !child.name.ToLower().Contains("water") &&
            !child.name.ToLower().Contains("fbtex_dummy") && !child.name.ToLower().Contains("yogan"))
        {
            Debug.LogError("FALLBACK: " + mat.Name);
            material.mainTexture = StageLoader.Instance.FallbackTexture;
            child.SetActive(false);
            return;
        }

        material.SetFloat("_ReceiveShadows", 0);
        material.DisableKeyword("_RECEIVE_SHADOWS_ON");  // Deaktiviere Schattenempfang
        material.name = mat.Name;
        
        material.enableInstancing = true;
        material.renderQueue -= shapeIndex;

        meshRenderer.material = material;

        //MeshHelper.Subdivide(mesh);

        //if(GetComponent<Collider>() != null) GetComponent<Collider>().sharedMesh = mesh;


        if (!material.shader.name.Contains("Stylized Water"))
        {
            meshFilters.Add(meshFilter);
            meshRenderers.Add(meshRenderer);
            meshMaterials.Add(material);
            Childs.Add(child);
        }
        

    }

    private void CombineMeshes()
    {
            // Combine meshes
            List<Mesh> submeshes = new List<Mesh>();
            foreach (MeshFilter filter in meshFilters)
            {
                List<CombineInstance> combiners = new List<CombineInstance>();

                Material[] localMaterials = meshRenderers[meshFilters.IndexOf(filter)].sharedMaterials;
                for (int j = 0; j < localMaterials.Length; j++)
                {
                    CombineInstance instance = new CombineInstance();
                    instance.mesh = filter.sharedMesh;
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

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(finalCombiners.ToArray(), false);

            // Remove filter and renderer
            foreach (GameObject child in Childs)
            {
                //TexMatrixIndexes.Add(child.GetComponent<MaterialData>().Material3.TexMatrixIndexes);
                DestroyImmediate(child);
            }

            // Assign combined mesh
            MeshFilter finalFilter = gameObject.AddComponent<MeshFilter>();
            finalFilter.sharedMesh = combinedMesh;

            Renderer renderer = null;
            if (IsModel)
            {
                renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
                CreateBoneWeights((SkinnedMeshRenderer)renderer, combinedMesh);
            }
            else
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterials = meshMaterials.ToArray();
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
            
            gameObject.isStatic = true;
    }

    private void CreateBoneWeights(SkinnedMeshRenderer skinnedMeshRenderer, Mesh combinedMesh)
    {
        Transform[] Bones = FindBones(transform);
        
        skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = combinedMesh;
        skinnedMeshRenderer.sharedMaterials = meshMaterials.ToArray();
        skinnedMeshRenderer.quality = SkinQuality.Bone2; // ???

        Vector3[] verts = combinedMesh.vertices;
        BoneWeight[] weights = new BoneWeight[verts.Length];

        int vertexCount = 0;
        foreach (var shape in SHP1Tag.Shapes)
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

                bool isPartiallyWeighted = DRW1Tag.IsPartiallyWeighted[matrixTableIndex];
                ushort indexFromDRW1 = DRW1Tag.TransformIndexTable[matrixTableIndex];

                //Matrix4 finalMatrix = Matrix4.Zero;
                if (isPartiallyWeighted)
                {
                    EVP1.Envelope envelope = EVP1Tag.Envelopes[indexFromDRW1];
                    
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

    private Transform[] FindBones(Transform parent)
    {
        List<Transform> bonesList = new List<Transform>();
        foreach (GameObject bone in parent.gameObject.GetAllChildren())
        {
            if(bone.CompareTag("Joint")) bonesList.Add(bone.transform);
        }
        
        return bonesList.ToArray();
    }

    public long INF1Start;
    public long VTX1Start;
    public long EVP1Start;
    public long DRW1Start;
    public long JNT1Start;
    public long SHP1Start;
    public long MAT3Start;
    public long TEX1Start;
    public long MDL3Start;
    
    public void GetCheckStarts(EndianBinaryReader reader, int tagCount)
    {
        for (int i = 0; i < tagCount; i++)
        {
            long tagStart = reader.BaseStream.Position;
            string tagName = reader.ReadString(4);
            int tagSize = reader.ReadInt32();

            switch (tagName)
            {
                case "INF1":
                    INF1Start = tagStart;
                    INF1Tag = new INF1();
                    INF1Tag.LoadINF1FromStream(reader, INF1Start);
                    break;
                case "VTX1":
                    VTX1Start = tagStart;
                    break;
                case "EVP1":
                    EVP1Start = tagStart;
                    break;
                case "DRW1":
                    DRW1Start = tagStart;
                    break;
                case "JNT1":
                    JNT1Start = tagStart;
                    JNT1Tag = new JNT1();
                    JNT1Tag.LoadJNT1FromStream(reader, JNT1Start);
                    JNT1Tag.CalculateParentJointsForSkeleton(INF1Tag.HierarchyRoot);
                    break;
                case "SHP1":
                    SHP1Start = tagStart;
                    break;
                case "MAT3":
                    MAT3Start = tagStart;
                    break;
                case "TEX1":
                    TEX1Start = tagStart;
                    break;
                case "MDL3":
                    MDL3Start = tagStart;
                    break;
            }

            // Überspringe den Rest dieses Tags
            reader.BaseStream.Position = tagStart + tagSize;
        }
    }
    
    private void ApplyBonePositionsToAnimationTransforms(IList<SkeletonJoint> boneList, Matrix4[] boneTransforms)
    {
        for (int i = 0; i < boneList.Count; i++)
        {
            SkeletonJoint curJoint, origJoint;
            curJoint = origJoint = boneList[i];

            Matrix4 cumulativeTransform = Matrix4.Identity;
            while (true)
            {
                Matrix4 jointMatrix = Matrix4.CreateScale(curJoint.Scale) *
                                      Matrix4.CreateFromQuaternion(curJoint.Rotation) *
                                      Matrix4.CreateTranslation(curJoint.Translation);
                cumulativeTransform *= jointMatrix;
                if (curJoint.Parent == null)
                    break;

                curJoint = curJoint.Parent;
            }

            boneTransforms[i] = cumulativeTransform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
