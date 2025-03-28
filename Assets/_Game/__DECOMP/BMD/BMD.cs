using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Animancer;
using GameFormatReader.Common;
using JStudio.J3D;
using JStudio.J3D.Animation;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Unity.VisualScripting;
using UnityEngine;
using WiiExplorer;
using ZeldaTesting;
using Color = UnityEngine.Color;
using TextureWrapMode = UnityEngine.TextureWrapMode;
using UnityEngine.Rendering.Universal;
using GameObject = UnityEngine.GameObject;
using PrimitiveType = UnityEngine.PrimitiveType;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Random = System.Random;

public class BMD : MonoBehaviour
{
    public INF1 INF1Tag;
    public VTX1 VTX1Tag;
    public EVP1 EVP1Tag;
    public DRW1 DRW1Tag;
    public JNT1 JNT1Tag;
    public SHP1 SHP1Tag;
    public MAT3 MAT3Tag;
    public TEX1 TEX1Tag;

    public string Magic;
    public int Size;
    public int Offset;
    public int NumChunks;

    public string Name;
    public Actor Actor;
    
    //public string SpriteTexture;
    public Vector3 Dialogue3DRotation;
    public int DialogueId;
    
    public List<GameObject> Childs = new List<GameObject>();

    public List<TextureData> TextureDatas = new List<TextureData>();

    public Archive Archive;

    public Dictionary<string, GameObject> jointGameObjects = new Dictionary<string, GameObject>();

    public BCK CurrentAnimation;
    public BTK CurrentBTK;
    public BRK CurrentBRK;

    public bool IsLink;

    private TevColorOverride m_tevColorOverrides = new TevColorOverride();

    public List<BCK> PreloadedBCKs = new List<BCK>();

    public WeightDataGenerator WeightDataGenerator;

    public static BMD CopyModel(BMD originalBmd, GameObject parent)
    {
        BMD bmd = parent.AddComponent<BMD>();
        bmd.Actor = originalBmd.Actor;
        bmd.Archive = originalBmd.Archive;
        bmd.Name = originalBmd.Name;

        bmd.INF1Tag = originalBmd.INF1Tag;
        bmd.VTX1Tag = originalBmd.VTX1Tag;
        bmd.EVP1Tag = originalBmd.EVP1Tag;
        bmd.DRW1Tag = originalBmd.DRW1Tag;
        bmd.JNT1Tag = originalBmd.JNT1Tag;
        bmd.SHP1Tag = originalBmd.SHP1Tag;
        bmd.MAT3Tag = originalBmd.MAT3Tag;
        bmd.TEX1Tag = originalBmd.TEX1Tag;

        return bmd;
    }

    public static BMD CreateModelFromPath(Archive archive, string name, List<BTI> externalBTIs, GameObject o = null)
    {
        if (!name.EndsWith(".bmd")) name += ".bmd";

        BMD bmd = o.AddComponent<BMD>();
        bmd.Actor = o.GetComponent<Actor>();
        if (bmd.Actor == null) bmd.Actor = bmd.transform.parent.GetComponent<Actor>();

        byte[] buffer = ArcReader.GetBuffer(archive, name);

        //o.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);

        if (o == null)
        {
            o = new GameObject(name);
            o.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }

        bmd.Archive = archive;
        bmd.Parse(name, buffer, externalBTIs, true);
        
        // Check if model has joints, if so prepare weights and use SkinnedMeshRenderer
        if (bmd.JNT1Tag.BindJoints.Count != 0)
        {
            bmd.PrepareWeights();
            bmd.AddMeshColliderToSkinned();

            foreach (MeshFilter filter in bmd.GetComponentsInChildren<MeshFilter>())
            {
                //filter.gameObject.SetActive(false);
                DestroyImmediate(filter.gameObject);
            }
        }
        

        // Add script to calculate weights
        /*bmd.weightDataGenerator = bmd.AddComponent<WeightDataGenerator>();
        bmd.weightDataGenerator.bmd = bmd;
        bmd.weightDataGenerator.PrepareWeights();

        foreach (MeshFilter filter in bmd.GetComponentsInChildren<MeshFilter>())
        {
            filter.gameObject.SetActive(false);
        }*/

        return bmd;
    }

    public static BMD CreateModelFromPathInPlace(Archive archive, string name, List<BTI> externalBTIs, Transform parent,
        bool UseRigidbody)
    {
        if (!name.EndsWith(".bmd")) name += ".bmd";
        byte[] buffer = ArcReader.GetBuffer(archive, name);

        GameObject o = new GameObject(name);
        o.transform.parent = parent;
        o.transform.localPosition = Vector3.zero;
        o.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);

        BMD bmd = o.AddComponent<BMD>();
        bmd.Archive = archive;
        bmd.Parse(name, buffer, externalBTIs, true, UseRigidbody);

        return bmd;
    }

    public static void CreateStage(Archive archive, GameObject room, ArcFile file, string name, List<BTI> externalBTIs)
    {
        byte[] buffer = file.Buffer;

        GameObject o = new GameObject(name);
        o.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
        o.transform.parent = room.transform;

        BMD bmd = o.AddComponent<BMD>();
        bmd.Archive = archive;
        bmd.Parse(name, buffer, externalBTIs, false);

        // Get BTK animation
        bmd.LoadBTK(name);
        //bmd.LoadBRK(name);

        ArcFile btkFile = ArcReader.GetFile(archive, name + ".btk");
        if (btkFile != null)
        {
            BTK btk = new BTK(name);
            using (EndianBinaryReader reader = new EndianBinaryReader(btkFile.Buffer, Endian.Big))
            {
                btk.LoadFromStream(reader);
            }
            
            btk.PlayBTK(bmd);
        }
        
        //bmd.PrepareWeights();

        foreach (GameObject c in bmd.Childs)
        {
            //c.AddComponent<MeshCollider>().sharedMesh = c.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    private List<BTI> externalBTIs;
    private bool isModel;

    public void Parse(string name, byte[] buffer, List<BTI> externalBTIs, bool isModel = false,
        bool useRigidbody = false)
    {      
        //var watch = System.Diagnostics.Stopwatch.StartNew();
        
        Name = name;
        this.externalBTIs = externalBTIs;
        this.isModel = isModel;

            using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
            {
                // Read the J3D Header
                //Magic = new string(reader.ReadChars(4));
                //Magic = new string(reader.ReadChars(4));
                reader.Skip(8);
                Size = reader.ReadInt32();
                NumChunks = reader.ReadInt32();

                // Skip over an unused tag ("SVR3") which is consistent in all models.
                reader.Skip(16);

                LoadTagDataFromFile(buffer, reader, NumChunks, externalBTIs, isModel);
            }

        if (useRigidbody)
        {
            Rigidbody rigidbody = transform.AddComponent<Rigidbody>();
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            //rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ;
        }

        CreateShapes();
        
        //watch.Stop();
        //Debug.LogError("Model (" + name + "): " + watch.ElapsedMilliseconds);
    }
    
    List<MeshFilter> meshFilters = new List<MeshFilter>();
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    List<Material> meshMaterials = new List<Material>();
    public List<TexMatrix[]> TexMatrixIndexes = new List<TexMatrix[]>();
    public void CreateShapes(GameObject targetObject = null)
    {
        GameObject target = null;
        if (targetObject == null) target = transform.gameObject;
        else target = targetObject;

        int currentMaterialIndex = -1;
        foreach (INF1.InfoNode node in INF1Tag.InfoNodes)
        {
            if (node.Type == HierarchyDataType.Finish) break;
            else if (node.Type == HierarchyDataType.Material)
            {
                currentMaterialIndex = node.Value;
            }
            else if (node.Type == HierarchyDataType.Batch)
            {
                SHP1.Shape shape = SHP1Tag.Shapes[node.Value];

                if (shape.MaterialIndex != -1) continue;

                shape.MaterialIndex = currentMaterialIndex;
            }
        }

        IList<SkeletonJoint> boneList = (CurrentAnimation != null) ? JNT1Tag.AnimatedJoints : JNT1Tag.BindJoints;

        Matrix4[] boneTransforms = new Matrix4[boneList.Count];
        ApplyBonePositionsToAnimationTransforms(boneList, boneTransforms);

        for (int i = 0; i < SHP1Tag.Shapes.Count; i++)
        {
            PrepareShape(target, i, externalBTIs, boneList, boneTransforms, isModel);
        }



        if (!isModel)
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
                TexMatrixIndexes.Add(child.GetComponent<MaterialData>().Material3.TexMatrixIndexes);
                DestroyImmediate(child);
            }

            // Assign combined mesh
            MeshFilter finalFilter = gameObject.AddComponent<MeshFilter>();
            finalFilter.sharedMesh = combinedMesh;

            MeshRenderer finalRenderer = gameObject.AddComponent<MeshRenderer>();
            finalRenderer.sharedMaterials = meshMaterials.ToArray();
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
            _meshRenderers.Add(finalRenderer);
            
            gameObject.isStatic = true;
        }

        /*List<MeshFilter> meshFilters = new List<MeshFilter>();
        List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
        List<Material> meshMaterials = new List<Material>();
        foreach (SHP1.Shape shape in SHP1Tag.Shapes)
        {
            GameObject go = new GameObject("shape" + SHP1Tag.Shapes.IndexOf(shape));
            Childs.Add(go);

            Material3 material = MAT3Tag.MaterialList[MAT3Tag.MaterialRemapTable[shape.MaterialIndex]];

            BTI texture = null;
            BTI vertexColorTexture = null;
            int baseTexture = material.TextureIndexes[0];
            if (baseTexture >= 0) texture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[baseTexture]];
            else if (baseTexture == -1)
            {
                texture = new BTI("MODEL_BEHIND_DOOR", null, new BinaryTextureImage());
                return;
            }
            //texture.Compressed.TextureIndex = SHP1Tag.Shapes.IndexOf(shape);

            int vertexTexture = material.TextureIndexes[1];
            if (vertexTexture >= 0)
            {
                int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                if (remap == 1 && material.TextureIndexes[2] != -1)
                    vertexTexture = material.TextureIndexes[2]; // Get second texture, first propably fake cloud
                vertexColorTexture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[vertexTexture]];
            }

            if (texture == null) texture = new BTI("DUMMY", null, new BinaryTextureImage());

            List<OpenTK.Vector3> vertPos = shape.OverrideVertPos.Count > 0 ? shape.OverrideVertPos : shape.VertexData.Position;
            List<OpenTK.Vector3> vertNormal = shape.OverrideNormals.Count > 0 ? shape.OverrideNormals : shape.VertexData.Normal;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            foreach (OpenTK.Vector3 pos in vertPos) verts.Add(new Vector3(pos.X, pos.Y, pos.Z));
            foreach (OpenTK.Vector3 pos in vertNormal) normals.Add(new Vector3(pos.X, pos.Y, pos.Z));

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts.ToArray();

            int numVertices = verts.Count;
            int numTriangles = numVertices / 3;

            int[] triangles = new int[numTriangles * 3];            // Single sided
            for (int i = 0; i < numTriangles; i++)
            {
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }
            mesh.triangles = triangles;
            mesh.normals = normals.ToArray();

            mesh.uv = shape.VertexData.Tex0.ToArray();
            mesh.uv2 = shape.VertexData.Tex1.ToArray();
            mesh.uv3 = shape.VertexData.Tex2.ToArray();
            mesh.uv4 = shape.VertexData.Tex3.ToArray();
            mesh.uv5 = shape.VertexData.Tex4.ToArray();
            mesh.uv6 = shape.VertexData.Tex5.ToArray();
            mesh.uv7 = shape.VertexData.Tex6.ToArray();
            mesh.uv8 = shape.VertexData.Tex7.ToArray();

            List<UnityEngine.Color> colors = new List<UnityEngine.Color>();
            foreach (WLinearColor linearColor in shape.VertexData.Color0) colors.Add(new UnityEngine.Color(linearColor.R, linearColor.G, linearColor.B, linearColor.A));
            mesh.colors = colors.ToArray();

            Material mat = new Material(StageLoader.Instance.DefaultMaterial);
            mat.mainTexture = texture.Texture;
            mat.mainTextureOffset = new Vector2(0, 1);
            mat.SetVector("_Offset", new Vector4(0, 1, 0, 0));

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            meshFilters.Add(filter);
            meshRenderers.Add(renderer);
            meshMaterials.Add(mat);
        }



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
        combinedMesh.CombineMeshes(finalCombiners.ToArray(), false);

        // Remove filter and renderer
        foreach (GameObject child in Childs) DestroyImmediate(child);

        // Assign combined mesh
        MeshFilter finalFilter = gameObject.AddComponent<MeshFilter>();
        finalFilter.sharedMesh = combinedMesh;

        MeshRenderer finalRenderer = gameObject.AddComponent<MeshRenderer>();
        finalRenderer.sharedMaterials = meshMaterials.ToArray();
        gameObject.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);*/
        
        

        /*if (!isModel)
        {
            if (ZeldaManager.Instance.SplitMeshes.IsActive)
            {
                foreach (GameObject c in Childs)
                {
                    List<GameObject> subMeshes = MeshHelper.SplitMesh(c, int.Parse(ZeldaManager.Instance.SplitMeshes.Value));

                    foreach (GameObject subMesh in subMeshes)
                    {
                        _meshRenderers.Add(subMesh.GetComponent<MeshRenderer>());
                    }
                }
            }

        }*/


        if (isModel)
        {
            jointGameObjects = CreateJointGameObjects(JNT1Tag.BindJoints, target.transform);
            BuildHierarchy(JNT1Tag.BindJoints, jointGameObjects);
            // Add all eye textures to blink list
            foreach (BTI bti in TEX1Tag.BTIs)
            {
                if (bti.Name.Contains("_eye."))
                {
                    _eyeTextures.Add(new EyeTexture()
                    {
                        bti = bti
                    });
                }
            }
            if(_eyeTextures.Count >= 1)
                StartCoroutine(BlinkRoutine());
        }
    }

    public BMD OverwriteMaterial(Material material)
    {
//        CalculateWeights();
        // Force change to lit material
        SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
        Texture tex = renderer.sharedMaterial.mainTexture;

        renderer.sharedMaterial = new Material(material);
        renderer.sharedMaterial.mainTexture = tex;
        renderer.sharedMaterial.SetFloat("_Smoothness", 0f);

        return this;
    }

    public BMD OverwriteMaterial(int sourceMaterialIndex, Material material)
    {
        SkinnedMeshRenderer renderer = GetComponent<SkinnedMeshRenderer>();
        Texture tex = renderer.sharedMaterial.mainTexture;
        
        Debug.Log(renderer.materials[sourceMaterialIndex]);
        renderer.materials[sourceMaterialIndex] = material;
        renderer.materials[sourceMaterialIndex].mainTexture = tex;
        renderer.materials[sourceMaterialIndex].SetFloat("_Smoothness", 0f);
        Debug.Log(renderer.materials[sourceMaterialIndex]);
        
        /*MeshRenderer renderer = GetComponentsInChildren<MeshRenderer>()[sourceMaterialIndex];
        Texture tex = renderer.sharedMaterial.mainTexture;

        renderer.sharedMaterial = new Material(material);
        renderer.sharedMaterial.mainTexture = tex;
        renderer.sharedMaterial.SetFloat("_Smoothness", 0f);*/

        return this;
    }

    public BMD ChangeEmission(UnityEngine.Color color)
    {
        CalculateWeights();
        // Force change to lit material
        SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color);

        return this;
    }

    public BMD ChangeAlphaThreshold(float alphaThreshold)
    {
        // Force change to lit material
        SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        renderer.material.SetFloat("_AlphaThreshold", alphaThreshold);

        return this;
    }

    public BMD ChangeColor(UnityEngine.Color color)
    {
        CalculateWeights();
        // Force change to lit material
        SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        UnityEngine.Texture tex = renderer.material.mainTexture;

        renderer.material = new Material(StageLoader.Instance.DefaultMaterial);
        renderer.material.mainTexture = tex;
        renderer.material.SetFloat("_Smoothness", 0f);
        //renderer.material.SetColor("_BaseColor", new UnityEngine.Color(color.r / 255f, color.g /255f, color.b /255f));
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", color);

        return this;
    }

    private int frameCounter = 0;
    private int updateInterval = 30; // Anzahl der Frames zwischen den Aktualisierungen
    
    private bool IsRendererVisible(Renderer renderer)
    {
        // Holen des Bounds des Renderers
        Bounds bounds = renderer.bounds;

        // Prüfen, ob der Bounds im Sichtfeld der Kamera liegt
        Plane[] cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(Link.Instance != null ? Link.Instance._camera : Camera.main);
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, bounds);
    }

    private void FixedUpdate()
    {

            foreach (Renderer renderer in _meshRenderers)
            {
                if (renderer == null) continue;
                //if(renderer.gameObject.GetComponent<MaterialData>() != null) continue;

                // Sichtbarkeit des Renderers überprüfen
                bool isVisible = IsRendererVisible(renderer);        
                if (ZeldaManager.Instance.MeshCulling)
                {
                    renderer.enabled = isVisible;
                }
                else
                {
                    renderer.enabled = true;
                }
        }

        
        //return;

        /*if (CurrentAnimation != null)
        {
            if (CurrentAnimation.IsPlaying())
            {
                //frameCounter++;
                //if (frameCounter >= updateInterval)
                {
                    //CurrentAnimation.Tick(Time.deltaTime);
                    if (WeightDataGenerator != null)
                    {
                        CurrentAnimation.ApplyPoseForSkinnedMeshRenderer(JNT1Tag.AnimatedJoints,
                            WeightDataGenerator.Bones);
                        bool valid = CurrentAnimation.Tick(0.02f);
                        if (!valid)
                        {
                            CurrentAnimation.Stop();
                            CurrentAnimation = null;
                        }
                    }
                }
            }
        }*/

        if (CurrentBTK != null)
        {
            return;
            if (CurrentBTK.IsPlaying())
            {
                CurrentBTK.Tick(0.02f);
                //CurrentBTK.ApplyAnimationToMaterials(MAT3Tag);

                // ATTENTION: SOME TIMES MESSES UP OFFSET OF TEXTURE! => Hidden Village Impa Roof
                // Update materials when changed using BTK
                foreach (GameObject child in Childs)
                {
                    MaterialData data = child.GetComponent<MaterialData>();
                    Material3 mat = data.Material3;

                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                    Material material = renderer.material;
                    Mesh mesh = child.GetComponent<Mesh>();

                    Vector2 textureTranslation = new Vector2(-mat.TexMatrixIndexes[0].TranslateS,
                        -mat.TexMatrixIndexes[0].TranslateT);

                    material.mainTextureOffset = new Vector2(mat.TexMatrixIndexes[0].TranslateS,
                        mat.TexMatrixIndexes[0].TranslateT);
                    material.mainTextureScale =
                        new Vector2(mat.TexMatrixIndexes[0].ScaleS, mat.TexMatrixIndexes[0].ScaleT);

                    if (material.shader.name.Contains("OffsetParticle"))
                    {
                        material.SetTextureOffset("_MainTex", textureTranslation);
                    }
                    else
                    {
                        material.SetVector("_MainOffset", textureTranslation);
                    }

                    // In case particle shader
                    //material.SetVector("_BlendOffset", new Vector4(mat.TexMatrixIndexes[1].TranslateS, mat.TexMatrixIndexes[1].TranslateT, 0, 0));
                }
            }
        }

        if (CurrentBRK != null)
        {
            if (CurrentBRK.IsPlaying())
            {
                CurrentBRK.Tick(1f);
                CurrentBRK.ApplyAnimationToMaterials(MAT3Tag, m_tevColorOverrides);

                // Update materials when changed using BTK
                foreach (GameObject child in Childs)
                {

                    MaterialData data = child.GetComponent<MaterialData>();
                    Material3 mat = data.Material3;

                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                    Material material = renderer.material;
                    Mesh mesh = child.GetComponent<Mesh>();

                    /*List<BRK.RegisterAnim> colorAnimationData = CurrentBRK.GetColorAnimationData();
                    for (int i = 0; i < colorAnimationData.Count; i++)
                    {
                        WLinearColor color = mat.TevColorIndexes[colorAnimationData[i].ColorID];
                        Debug.LogWarning(color.R);
                        material.SetColor("_MainColor", new Color(color.R, color.G, color.B, color.A));
                    }

                    List<BRK.RegisterAnim> konstAnimationData = CurrentBRK.GetKonstAnimationData();
                    for (int i = 0; i < konstAnimationData.Count; i++)
                    {
                        WLinearColor color = mat.TevColorIndexes[konstAnimationData[i].ColorID];



                        Debug.LogError(color.R);
                        material.SetColor("_MainColor", new Color(color.R, color.G, color.B, color.A));
                    }*/

                    //Debug.Log(mat.TevColorIndexes[0]);
                    //material.SetColor("_MainColor", new Color(mat.TevColorIndexes[0].R, mat.TevColorIndexes[0].G, mat.TevColorIndexes[0].B, mat.TevColorIndexes[0].A));
                }
            }
        }

        // Is rupee?
        /*if (Name.Contains("o_g_rupy"))
        {
            transform.eulerAngles =
                new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + 2f, transform.eulerAngles.z);
        }*/
    }

    private void AddMeshColliderToSkinned()
    {
        MeshCollider collider = transform.AddComponent<MeshCollider>();
        collider.sharedMesh = transform.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        collider.convex = true;
    }
    
    public void CalculateWeights()
    {
        if (WeightDataGenerator == null)
        {
            WeightDataGenerator = transform.AddComponent<WeightDataGenerator>();
            WeightDataGenerator.bmd = this;
            WeightDataGenerator.PrepareWeights(this);

            foreach (MeshFilter filter in transform.GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.SetActive(false);
                DestroyImmediate(filter.gameObject);
            }
        }
        else
        {
            //WeightDataGenerator.PrepareWeightsAlreadyCombined(CombinedMesh, CombinedMaterials);
        }
    }

    public void LoadAnimation(string bckName)
    {
        // Stop previous animation
        if (CurrentAnimation != null)
        {
            CurrentAnimation.Stop();
            CurrentAnimation = null;
        }

        ArcFile animFile = ArcReader.GetFile(Archive, bckName + ".bck");
        if (animFile == null)
        {
            Debug.LogError("Animation not found! " + bckName);
            return;
        }

        byte[] buffer = animFile.Buffer;

        BCK bck = new BCK(animFile.Name);

        bck.Start();

        CurrentAnimation = bck;

        // Get first frame
        CurrentAnimation.ApplyAnimationToPose(JNT1Tag.AnimatedJoints, jointGameObjects);
        // Change mesh
        //UpdateMesh();
        // Add script to calculate weights
        CalculateWeights();
    }

    #region BTK

    public void LoadBTK(string btkName)
    {
        // Stop previous animation
        if (CurrentBTK != null)
        {
            CurrentBTK.Stop();
            CurrentBTK = null;
        }

        ArcFile animFile = ArcReader.GetFile(Archive, btkName + ".btk");
        if (animFile == null) return;

        byte[] buffer = animFile.Buffer;

        BTK btk = new BTK(animFile.Name);
        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            btk.LoadFromStream(reader);
        }

        btk.Start();

        CurrentBTK = btk;
    }

    #endregion

    #region BRK

    public void LoadBRK(string brkName)
    {
        // Stop previous animation
        if (CurrentBRK != null)
        {
            CurrentBRK.Stop();
            CurrentBRK = null;
        }


        ArcFile animFile = ArcReader.GetFile(Archive, brkName + ".brk");
        if (animFile == null) return;

        byte[] buffer = animFile.Buffer;

        BRK brk = new BRK(animFile.Name);
        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            brk.LoadFromStream(reader);
        }

        brk.Start();

        CurrentBRK = brk;
    }

    #endregion

    [HideInInspector] public int UpdateIndex;
    private int MaxUpdate;
    [HideInInspector] public bool CheckCached = false;
    [HideInInspector] public bool ForceCheck = false;
    [HideInInspector] public Dictionary<int, Vector3[]> CachedAnimationsVerts = new Dictionary<int, Vector3[]>();
    [HideInInspector] public Dictionary<int, Vector3[]> CachedAnimationsNormals = new Dictionary<int, Vector3[]>();

    private bool checkForExternalCache = true;

    [HideInInspector] public bool CacheMeshes = false;
    //public Dictionary<string, Dictionary<int, Vector3[]>> CachedVerts = new Dictionary<string, Dictionary<int, Vector3[]>>();
    //public Dictionary<string, Dictionary<int, Vector3[]>> CachedNormals = new Dictionary<string, Dictionary<int, Vector3[]>>();

    public bool IsAnimationPlaying()
    {
        if (CurrentAnimation == null) return false;
        else
        {
            return CurrentAnimation.IsPlaying();
        }
    }

    [HideInInspector] public Dictionary<string, AnimationClip> _animationClips = new Dictionary<string, AnimationClip>();

    public bool CompressedAnimations = false;
    
    public Dictionary<string, AnimationSetup.AnimationCurveHolder> specificAnimations = new Dictionary<string, AnimationSetup.AnimationCurveHolder>();
    public ZeldaAnimation LoadSpecificAnimationOnMain(Archive archive, string bckName, LoopType loopType = LoopType.Loop, AnimationBehavior behavior = AnimationBehavior.Stay)
    {
        if(_animancer == null) _animancer = transform.AddComponent<AnimancerComponent>();
        if(_animator == null) _animator = transform.AddComponent<Animator>();
        
        if(!bckName.EndsWith(".bck")) bckName = bckName + ".bck";
        
        ZeldaAnimation placeholderAnimation = new ZeldaAnimation(_animancer);
        placeholderAnimation.BckName = bckName;
        placeholderAnimation.GetHandles(this);
        if(specificAnimations.ContainsKey(bckName))
        {
            AnimationSetup.AnimationCurveHolder savedAnim = specificAnimations[bckName];
            //return PlayAnimationJob(savedAnim, savedAnim.LoopType, savedAnim.Behavior);   // Save behavior? Not needed
            placeholderAnimation.FillJobData(this, savedAnim);

            return placeholderAnimation;
        }
        
        AnimationSetup.AnimationCurveHolder holder = LoadAnimationCurves(archive, bckName);
        if (loopType != LoopType.UseDefined) holder.LoopType = loopType;
        holder.Behavior = behavior;
        
        specificAnimations.Add(bckName, holder);
        placeholderAnimation.FillJobData(this, holder);
        
        return placeholderAnimation;
    }

    public bool LoadAnimationsOnMainThread = false;
    public ZeldaAnimation LoadSpecificAnimation(Archive archive, string bckName, LoopType loopType = LoopType.Loop, AnimationBehavior behavior = AnimationBehavior.Stay)
    {
        if (LoadAnimationsOnMainThread) return LoadSpecificAnimationOnMain(archive, bckName, loopType, behavior);
        
        // Sicherstellen, dass der Name korrekt ist
        if (!bckName.EndsWith(".bck")) bckName = bckName + ".bck";
        
        if(_animancer == null) _animancer = transform.AddComponent<AnimancerComponent>();
        if(_animator == null) _animator = transform.AddComponent<Animator>();
        
        // Platzhalter-Objekt erstellen
        ZeldaAnimation placeholderAnimation = new ZeldaAnimation(_animancer);
        placeholderAnimation.BckName = bckName;
        placeholderAnimation.GetHandles(this);
        if (specificAnimations.ContainsKey(bckName))
        {
            AnimationSetup.AnimationCurveHolder savedAnim = specificAnimations[bckName];
            placeholderAnimation.FillJobData(this, savedAnim);
            
            return placeholderAnimation;
        }

        // Animation im Hintergrund laden
        _ = LoadAnimationCurvesInBackground(archive, bckName).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Failed to load animation curves for {bckName}: {task.Exception}");
                return;
            }

            // Daten aktualisieren, sobald der Task abgeschlossen ist
            AnimationSetup.AnimationCurveHolder holder = task.Result;
            if (holder != null)
            {
                /*while (holder.PositionCurvesX.Count == 0)
                {
                    Thread.Sleep(100);
                    Debug.LogError("Wating for finishing...");
                }*/
                //Thread.Sleep(250);
                
                //placeholderAnimation.Curves = holder; // Setze die geladenen Daten

                if (loopType != LoopType.UseDefined) holder.LoopType = loopType;
                holder.Behavior = behavior;

                specificAnimations[bckName] = holder; // Speichere den Holder für zukünftige Verwendung
                placeholderAnimation.FillJobData(this, holder);
            }
        });

        // Placeholder sofort zurückgeben
        placeholderAnimation.IsPlaying = false;
        return placeholderAnimation;
    }


    public async Task<AnimationSetup.AnimationCurveHolder> LoadAnimationCurvesInBackground(Archive archive, string bckName, string startBone = "##STOP##")
    {
        if(!bckName.EndsWith(".bck")) bckName = bckName + ".bck";
        
        /*BCK bck = new BCK(bckName);
        await EnumeratorHelper.CreateAsync(0, async () =>
        {
            // Unity-spezifischer Teil läuft im Hauptthread
            ArcFile file = ArcReader.GetFile(archive, bckName);
            byte[] buffer = file.Buffer;

            if (CompressedAnimations)
                buffer = YAZ0.Decompress(file.Buffer);

            using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
            {
                bck.LoadFromStream(reader, this, false);
            }

 
        });*/
        
        
        ArcFile file = ArcReader.GetFile(archive, bckName);
        BCK bck = new BCK(file.Name);
        byte[] buffer = file.Buffer;
                
        if(CompressedAnimations) buffer = YAZ0.Decompress(file.Buffer);
        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            bck.LoadFromStream(reader, this, false);
        }
        
        AnimationSetup setup = new AnimationSetup();
        //AnimationSetup.AnimationCurveHolder holder = setup.CreateAnimationCurves(this, bck, startBone);
        AnimationSetup.AnimationCurveHolder holder = null;
        //holder = setup.CreatePlayableFromBCK(this, bck, startBone);
        holder = await Task.Run(async () =>
        {
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(async () =>
            {
                holder = setup.CreatePlayableFromBCK(this, bck, startBone);
            });
            return holder;
        });
        return holder;
        
        
        /*BCK bck = new BCK(bckName);
        EndianBinaryReader reader = null;
        await EnumeratorHelper.CreateAsync(0, () =>
        {
            ArcFile file = ArcReader.GetFile(archive, bckName);
            byte[] buffer = file.Buffer;

            if (CompressedAnimations) buffer = YAZ0.Decompress(file.Buffer);
            reader = new EndianBinaryReader(buffer, Endian.Big);
            bck.LoadFromStream(reader, this, true);
        });
        

        
        // Warten auf den separaten Task, der die Animation verarbeitet
        AnimationSetup setup = new AnimationSetup();
        holder = await Task.Run(() =>
        {
            Thread.Sleep(1000);

            while (!bck.IsLoaded)
            {
                Thread.Sleep(10);
            }
            
            reader.Close();
            
            Debug.Log(bckName + " finished loading animation curves");
            return setup.CreatePlayableFromBCKInBackground(this, bck, startBone);
        });

        Debug.Log(bckName + " then return");
        return holder;*/
        
    
// Der Task wird ausgeführt und wartet auf die Fertigstellung
        /*AnimationSetup setup = new AnimationSetup();
        holder = await Task.Run(() =>
        {
            
            ArcFile file = ArcReader.GetFile(archive, bckName);
            BCK bck = new BCK(file.Name);
            byte[] buffer = file.Buffer;

            if (CompressedAnimations) buffer = YAZ0.Decompress(file.Buffer);
            EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big);
            bck.LoadFromStream(reader, this, true);

            while (!bck.IsLoaded)
            {
                Thread.Sleep(10);
            }
            
            reader.Close();
            
            return setup.CreatePlayableFromBCK(this, bck, startBone);
        });*/

        return holder;
    }

    public AnimationSetup.AnimationCurveHolder LoadAnimationCurves(Archive archive, string bckName, string startBone = "##STOP##")
    {
        if(!bckName.EndsWith(".bck")) bckName = bckName + ".bck";
        foreach (ArcFile file in archive.Files)
        {
            if (file.Name.EndsWith(".bck") && file.Name == bckName)
            {
                BCK bck = new BCK(file.Name);
                byte[] buffer = file.Buffer;
                
                if(CompressedAnimations) buffer = YAZ0.Decompress(file.Buffer);
                using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
                {
                    bck.LoadFromStream(reader, this, false);
                }
        
                AnimationSetup setup = new AnimationSetup();
                AnimationSetup.AnimationCurveHolder holder = setup.CreatePlayableFromBCK(this, bck, startBone);
                return holder;
            }
        }

        return null;
    }

public void PlaySequencedAnimation(params ZeldaAnimation[] states)
{
    // Falls keine States übergeben wurden, wird die Methode einfach beendet
    if (states.Length == 0)
    {
        Debug.LogWarning("Keine Animationsstates zum Abspielen übergeben.");
        return;
    }

    // Starte die erste Animation
    PlayNextState(states, 0);
}

private void PlayNextState(ZeldaAnimation[] states, int currentIndex)
{
    // Überprüfe, ob wir uns noch innerhalb der Grenzen des Arrays befinden
    if (currentIndex < states.Length)
    {
        // Hole den aktuellen AnimancerState
        ZeldaAnimation currentState = states[currentIndex];

        // Setze den OnEnd-Event, um die nächste Animation zu starten
        currentState.Events(this).OnEnd = () =>
        {
            // Nachdem diese Animation abgeschlossen ist, spiele die nächste ab
            //PlayNextState(states, currentIndex + 1);
        };
        currentState.IsPlaying = true;
        currentState.AddEvent((int)currentState.duration, () =>
        {
            PlayNextState(states, currentIndex + 1);
        });
        
        // Starte die aktuelle Animation
        //currentState.Play();

        _animancer.Play(currentState, 0.25f, FadeMode.FromStart);
    }
    else
    {
        if (IsLink)
        {
            Link.PlayIdleAnimation(true);
        }
    }
}


    public ZeldaAnimation MergeCurvesToAnimation(AnimationSetup.AnimationCurveHolder curve1,
        AnimationSetup.AnimationCurveHolder curve2, LoopType loopType = LoopType.Once, AnimationBehavior animationBehavior = AnimationBehavior.GoBack)
    {
        ZeldaAnimation placeholder = new ZeldaAnimation(_animancer);
        placeholder.GetHandles(this);
        placeholder.BckName = curve1.BckName +  "_" + curve2.BckName;
        placeholder.loopType = loopType;
        placeholder.behavior = animationBehavior;

        Task.Run(() =>
        {
            placeholder.FillJobData(this, curve1, curve2);
        });

        return placeholder;
        //return PlayAnimationJob(curve1, loopType, animationBehavior, curve2);
    }

    public void PrepareWeights()
    {
        if (WeightDataGenerator == null)
        {
            // Add weights
            WeightDataGenerator = transform.AddComponent<WeightDataGenerator>();
            WeightDataGenerator.bmd = this;
            WeightDataGenerator.PrepareWeights(this);

            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
            {
                filter.gameObject.SetActive(false);
                Destroy(filter.gameObject);
            }
        }
    }

    public Animator _animator;
    public AnimancerComponent _animancer;
    [HideInInspector] public string lastAnimationName = "";

    public void PrepareAnimation()
    {
        _animator = gameObject.GetComponent<Animator>();
        if (_animator == null) _animator = gameObject.AddComponent<Animator>();

        _animancer = gameObject.GetComponent<AnimancerComponent>();
        if (_animancer == null) _animancer = gameObject.AddComponent<AnimancerComponent>();
    }

    public Transform FindChildByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Root oder Pfad ist ungültig.");
            return null;
        }

        // Splitte den Pfad in einzelne Teile
        string[] parts = path.Split('/');

        // Iteriere über die Teile und suche das nächste Kind
        Transform current = root;
        foreach (var part in parts)
        {
            if (current == null)
            {
                Debug.LogWarning($"Pfad ungültig: {path}");
                return null;
            }

            current = current.Find(part);
        }

        return current;
    }

    public class CustomAnimancerState : AnimancerState {
      
        private Animator animator;
        private CustomAnimationJob job;
      
        public CustomAnimancerState(Animator animator, BMD bmd) {
            this.animator = animator;
            job = new CustomAnimationJob(animator, bmd.jointGameObjects);
        }

        protected override void CreatePlayable(out Playable playable) {
            PlayableGraph playableGraph = PlayableGraph.Create("MyPlayableGraph");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            playable = AnimationScriptPlayable.Create(playableGraph, job);
        }

        public void Update() {
            job.Time = Time;
        }
      
        public override float Length => 60;
        public override AnimancerState Clone(CloneContext context)
        {
            throw new NotImplementedException();
        }
    }

    public struct CustomAnimationJob : IAnimationJob {

        public float Time;
        //private TransformStreamHandle headHandle;
        private Transform headHandle;
      
        public CustomAnimationJob(Animator animator, Dictionary<string, GameObject> bones)
        {
            headHandle = bones["head"].transform;
            Time = 0;
        }

        public void ProcessAnimation(AnimationStream stream) {
            //headHandle.SetLocalRotation(stream, Quaternion.Euler(0, Mathf.Sin(Time) * 30, 0));
            headHandle.rotation = Quaternion.Euler(0, Mathf.Sin(Time) * 30, 0);
        }

        public void ProcessRootMotion(AnimationStream stream) {
        }
    }

    public struct MyAnimationJob : IAnimationJob
    {
        public AnimationData Data;
        public float Time;

        public Dictionary<string, GameObject> Bones;

        public void ProcessAnimation(AnimationStream stream)
        {
            // Beispiel: Position berechnen
            float posX = Data.PositionX.Evaluate(Time);
            float posY = Data.PositionY.Evaluate(Time);
            float posZ = Data.PositionZ.Evaluate(Time);

            // Rotation (Quaternion) berechnen
            float rotX = Data.RotationX.Evaluate(Time);
            float rotY = Data.RotationY.Evaluate(Time);
            float rotZ = Data.RotationZ.Evaluate(Time);
            float rotW = Data.RotationW.Evaluate(Time);

            foreach (var a in Bones)
            {
                a.Value.transform.SetPositionAndRotation(new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
            // Optional: Wurzelbewegung verarbeiten
        }
    }

    
    public struct AnimationData
    {
        public NativeCurve PositionX;
        public NativeCurve PositionY;
        public NativeCurve PositionZ;
        public NativeCurve RotationX;
        public NativeCurve RotationY;
        public NativeCurve RotationZ;
        public NativeCurve RotationW;
    }

    
    private string lastExternalArchiveForAnimation;
    public BMD PlayAnimationFromDifferentArchive(string archive, string name)
    {
        lastExternalArchiveForAnimation = archive;
        PrepareWeights();
        
        if (!name.EndsWith(".bck")) name += ".bck";
        string arcPath = BMDFetcher.OBJ_PATH + "/" + archive + ".arc";
        ZeldaAnimation animation = LoadSpecificAnimation(ArcReader.Read(arcPath), name);
        
        
        if (_animator == null) _animator = transform.AddComponent<Animator>();
        //_animator.speed = 30;
        
        if (_animancer == null) _animancer = transform.AddComponent<AnimancerComponent>();
        _animancer.Animator = _animator;

        //AnimationClip clip = _animationClips[name];

        AnimancerState state = _animancer.Play(animation);
        lastAnimationName = name;

        return this;
    }

    public BMD UseSameAnimationAsOriginalModelForCopy(BMD original)
    {
        PrepareWeights();

        if (original.lastAnimationName != "")
        {
            string arcPath = BMDFetcher.OBJ_PATH + "/" + original.lastExternalArchiveForAnimation + ".arc";
        
            if (!original.lastAnimationName.EndsWith(".bck")) original.lastAnimationName += ".bck";
        
            _animator = transform.GetComponent<Animator>();
            //_animator.speed = 30;
        
            _animancer = transform.GetComponent<AnimancerComponent>();
            _animancer.Animator = _animator;

            //AnimationClip clip = _animationClips[original.lastAnimationName];
            ZeldaAnimation clip = LoadSpecificAnimation(ArcReader.Read(arcPath), original.lastAnimationName);

            AnimancerState state = _animancer.Play(clip);
            /*state.Speed = 30;
            state.Events(this).OnEnd = () =>
            {
                state.Time = 0;
            };*/
        }
        else
        {
            AnimationJobManager originalJobManager = original.GetComponent<AnimationJobManager>();
            if (originalJobManager == null) return this;
            string currentAnim = originalJobManager.currentAnimation;
            
            AnimationJobManager job = transform.AddComponent<AnimationJobManager>();
            job.PlayAnimation(currentAnim);
        }
        


        return this;
        
        if (!name.EndsWith(".bck")) name += ".bck";
        
        if (_animator == null) _animator = transform.GetComponent<Animator>();
        _animator.speed = 30;
        
        if (_animancer == null) _animancer = transform.GetComponent<AnimancerComponent>();
        _animancer.Animator = _animator;
        
        //AnimationSetup.AnimationCurveHolder holder = specificAnimations[lastAnimationName];
        //AnimancerState s = PlayAnimationJob(holder);

        //AnimancerState state = _animancer.Play(s);
        //state.Speed = 30;
        //state.Events(this).OnEnd = () =>
        {
            //state.Time = 0;
        };

        return this;
    }

    public void SetParticles(int groupID, int subId)
    {
        //foreach(JPAC jpac in StageLoader.JPACs)
        {
            if(StageLoader.JPACs.Count == 0) return;
            
            JPAC jpac = StageLoader.JPACs[groupID];
            for (int i = 0; i < jpac.Effects.Count; i++)
            {
                JPAResourceRaw effect = jpac.Effects[i];

                if (effect.ResourceId == subId)
                {
                    Debug.Log(effect.ResourceId.ToString("X4"));
                    Debug.Log("SIZEOFEFFECTS: " + jpac.Effects.Count + " :: i=" + i);
                    Debug.Log("SIZEOFTEX: " + jpac.Textures.Count + " :: i=" + i);

                    //List<BTI> textures = effect.Textures;
                    //Debug.Log("DRAW: " + textures.Count);
                    
                    foreach (BTI bti in jpac.Textures)
                    {
                        //Debug.Log("DRAW THIS TEX: " + bti.Name);
                    }
                }
            }
        }
    }

    private BTP CurrentBTP;
    public void PlayFacialBTP(string btpName, string external = "")
    {
        string arcPath = BMDFetcher.OBJ_PATH + "/" + external + ".arc";
        
        Archive animationArchive = Archive;
        if (external != "") animationArchive = ArcReader.Read(arcPath);
        ArcFile animFile = ArcReader.GetFile(animationArchive, btpName + ".btp");
        if (animFile == null)
        {
            Debug.LogError("Animation not found! " + btpName);
            return;
        }

        byte[] buffer = animFile.Buffer;

        CurrentBTP = new BTP(animFile.Name);
        using (EndianBinaryReader reader = new EndianBinaryReader(buffer, Endian.Big))
        {
            CurrentBTP.ReadTPT1Chunk(reader);
        }

        /*foreach (BTPAnimationEntry entry in CurrentBTP.AnimationEntries)
        {
            foreach (StoredMaterial sMat in _storedMaterials)
            {
                if (sMat.name == entry.MaterialName)
                {
                    Debug.Log("0: " + entry.TextureIndices.Length);
                    Debug.Log("0: " + entry.TextureIndices[0]);
                    Debug.Log("1: " + entry.TextureIndices[1]);
                    Debug.Log("2: " + entry.TextureIndices[2]);
                    Debug.Log("3: " + entry.TextureIndices[3]);
                    Debug.Log("4: " + entry.TextureIndices[4]);
                    Debug.Log("5: " + entry.TextureIndices[5]);
                    Debug.Log("6: " + entry.TextureIndices[6]);
                    Debug.Log("7: " + entry.TextureIndices[7]);
                    Debug.Log("EDIT: " + TEX1Tag.BTIs.Count);

                    BTI texture = TEX1Tag.BTIs[sMat.mat3.TextureIndexes[entry.TextureIndices[0]]];

                        SkinnedMeshRenderer renderer = transform.GetComponent<SkinnedMeshRenderer>();
                    renderer.materials[0].mainTexture = texture.Texture;
                }
            }
        }*/
    }

    [HideInInspector] public int MaxEyeTexture = 2;
    IEnumerator BlinkRoutine()
    {
        // Eine Sekunde warten, bevor der Blinkzyklus startet
        yield return new WaitForSeconds(1);

        SkinnedMeshRenderer renderer = transform.GetComponent<SkinnedMeshRenderer>();
        int materialIndex = 0;
        
        foreach (Material m in renderer.materials)
        {
            if (!m.name.Contains("eyeball") && m.name.Contains("_eye"))
            {
                materialIndex = renderer.materials.ToList().IndexOf(m);
                m.SetFloat("_AlphaThreshold", .5f);
            }
        }

        while (true)
        {
            // Auge schließen (Texturen durchgehen und rendern)
            for (int i = 0; i <= MaxEyeTexture; i++)
            {
                renderer.materials[materialIndex].mainTexture = _eyeTextures[i].bti.Texture;

                // Warte 0,5 Sekunden zwischen den Texturwechseln (Auge schließt sich allmählich)
                yield return new WaitForSeconds(0.05f);
            }

            // Auge wieder öffnen (Texturen rückwärts durchgehen)
            for (int i = MaxEyeTexture; i >= 0; i--)
            {
                renderer.materials[materialIndex].mainTexture = _eyeTextures[i].bti.Texture;

                // Warte 0,5 Sekunden zwischen den Texturwechseln (Auge öffnet sich allmählich)
                yield return new WaitForSeconds(0.05f);
            }

            // Nachdem das Auge vollständig geschlossen und wieder geöffnet wurde, eine Sekunde warten
            float randomWaitTime = UnityEngine.Random.Range(1.5f, 2.5f);
            yield return new WaitForSeconds(randomWaitTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (transform.GetComponent<SkinnedMeshRenderer>() != null)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();
            Bounds bounds = skinnedMeshRenderer.bounds;

            // Center und Größe der transformierten Bounds
            Vector3 center = bounds.center; // Weltkoordinaten
            Vector3 size = bounds.size;

            // Gizmo-Farbe setzen
            Gizmos.color = Color.green;

            // Bounds als Wireframe-Box zeichnen
            Gizmos.DrawWireCube(center, size);
        }
    }

    public void CreateMesh(GameObject child, SHP1.Shape shape, List<Vector3> vertices, List<Vector3> normals, BTI bti,
        BTI vertexBti, MeshVertexHolder vertexHolder, Material3 mat, bool external, bool externalVertex, bool isModel,
        int shapeIndex, Material overwriteMaterial = null)
    {

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

            if (ZeldaManager.Instance.DoubleSided || isModel || forceDoubleSided)
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

        MeshFilter meshFilter = null;
        if (child.GetComponent<MeshFilter>()) meshFilter = child.GetComponent<MeshFilter>();
        else meshFilter = child.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        mesh.uv = vertexHolder.Tex0.ToArray();
        mesh.uv2 = vertexHolder.Tex1.ToArray();
        mesh.uv3 = vertexHolder.Tex2.ToArray();
        mesh.uv4 = vertexHolder.Tex3.ToArray();
        mesh.uv5 = vertexHolder.Tex4.ToArray();
        mesh.uv6 = vertexHolder.Tex5.ToArray();
        mesh.uv7 = vertexHolder.Tex6.ToArray();
        mesh.uv8 = vertexHolder.Tex7.ToArray();

        OpenTK.Vector3 center = shape.BoundingBox.Center;
        OpenTK.Vector3 size = shape.BoundingBox.Max;

        Bounds bounds = new Bounds(new Vector3(center.X, center.Y, center.Z), new Vector3(size.X, size.Y, size.Z));
        bounds.extents = new Vector3(shape.BoundingBox.Extents.X, shape.BoundingBox.Extents.Y,
            shape.BoundingBox.Extents.Z);
        bounds.min = new Vector3(shape.BoundingBox.Min.X, shape.BoundingBox.Min.Y, shape.BoundingBox.Min.Z);
        bounds.max = new Vector3(shape.BoundingBox.Max.X, shape.BoundingBox.Max.Y, shape.BoundingBox.Max.Z);

        mesh.bounds = bounds;
        mesh.RecalculateBounds();

        List<UnityEngine.Color> colors = new List<UnityEngine.Color>();
        foreach (WLinearColor linearColor in vertexHolder.Color0) colors.Add(new UnityEngine.Color(linearColor.R, linearColor.G, linearColor.B, linearColor.A));
        mesh.colors = colors.ToArray();


        MeshRenderer meshRenderer = null;
        if (child.GetComponent<MeshRenderer>()) meshRenderer = child.GetComponent<MeshRenderer>();
        else meshRenderer = child.AddComponent<MeshRenderer>();

        Material material = null;

        // Default material
        material = new Material(StageLoader.Instance.DefaultMaterial);
        material.mainTexture = bti.Texture;
        material.mainTextureOffset = new Vector2(0, 0);
        material.SetVector("_Offset", new Vector4(0, 0, 0, 0));

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
                    material.SetTexture("_BlendTexture", vertexBti.Texture);
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

            material.mainTexture = bti.Texture;
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
            material.mainTexture = bti.Texture;

        if (child.name.ToLower().Contains("water") /* && child.name.ToLower().Contains("mip")*/ && child)
        {
            // Not allowed
            if (!child.name.Contains("waterwall") && !Name.Contains("onsen") &&
                !child.name.Contains("MA09_MeraWater_v_x") && !child.name.Contains("MA19_SW_v_x_watertest"))
                child.SetActive(false);
        }
        else
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

            if (material.mainTexture == null) material.mainTexture = StageLoader.Instance.FallbackTexture;

            if (bti.Compressed != null)
            {
                if (bti.Compressed.WrapS == BinaryTextureImage.WrapModes.Repeat)
                    material.mainTexture.wrapModeU = TextureWrapMode.Repeat;
                else if (bti.Compressed.WrapS == BinaryTextureImage.WrapModes.MirroredRepeat)
                    material.mainTexture.wrapModeU = TextureWrapMode.Mirror;
                else if (bti.Compressed.WrapS == BinaryTextureImage.WrapModes.ClampToEdge)
                    material.mainTexture.wrapModeU = TextureWrapMode.Clamp;

                if (bti.Compressed.WrapT == BinaryTextureImage.WrapModes.Repeat)
                    material.mainTexture.wrapModeV = TextureWrapMode.Repeat;
                else if (bti.Compressed.WrapT == BinaryTextureImage.WrapModes.MirroredRepeat)
                    material.mainTexture.wrapModeV = TextureWrapMode.Mirror;
                else if (bti.Compressed.WrapT == BinaryTextureImage.WrapModes.ClampToEdge)
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
            if (isModel && material.mainTexture.wrapModeU == TextureWrapMode.Clamp &&
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

        if (bti.Texture != null)
            TextureDatas.Add(new TextureData(bti.Name, external, bti.Texture));
        if (vertexBti != null)
            TextureDatas.Add(new TextureData(vertexBti.Name, externalVertex, vertexBti.Texture));

        if (overwriteMaterial == null)
            meshRenderer.materials = new[] { material };
        else
            meshRenderer.materials = new[] { overwriteMaterial };

        //MeshHelper.Subdivide(mesh);

        //if(GetComponent<Collider>() != null) GetComponent<Collider>().sharedMesh = mesh;
        Childs.Add(child);

        if (!isModel)
        {
            //transform.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        // Add data
        MaterialData data = child.AddComponent<MaterialData>();
        data.MaterialName = mat.Name;
        data.TexturesIndexes = mat.TextureIndexes;
        data.TEX1Tag = TEX1Tag;
        data.MAT3Tag = MAT3Tag;
        data.Material3 = mat;
        if (bti.Texture != null)
            data.TextureDatas.Add(new TextureData(bti.Name, external, bti.Texture));
        if (vertexBti != null)
            data.TextureDatas.Add(new TextureData(vertexBti.Name, externalVertex, vertexBti.Texture));

        //mesh.Optimize();
        
        _storedMaterials.Add(new StoredMaterial()
        {
            name = mat.Name,
            mat3 = mat,
            textureIndexes = mat.TextureIndexes
        });
        
        _meshRenderers.Add(meshRenderer);
        meshFilters.Add(meshFilter);
        meshRenderers.Add(meshRenderer);
        meshMaterials.Add(material);
    }

    [HideInInspector] public List<Renderer> _meshRenderers = new List<Renderer>();

    private List<StoredMaterial> _storedMaterials = new List<StoredMaterial>();
    class StoredMaterial
    {
        public string name;
        public Material3 mat3;
        public short[] textureIndexes;
    }

    private List<EyeTexture> _eyeTextures = new List<EyeTexture>();
    class EyeTexture
    {
        public BTI bti;
    }

    public void LoadTagDataFromFile(byte[] buffer, EndianBinaryReader reader, int tagCount, List<BTI> externalBTIs,
        bool isModel)
    {
        for (int i = 0; i < tagCount; i++)
        {
            long tagStart = reader.BaseStream.Position;

            string tagName = reader.ReadString(4);
            int tagSize = reader.ReadInt32();

            switch (tagName)
            {
                // INFO - Vertex Count, Scene Hierarchy
                case "INF1":
                    INF1Tag = new INF1();
                    INF1Tag.LoadINF1FromStream(reader, tagStart);
                    break;
                // VERTEX - Stores vertex arrays for pos/normal/color0/tex0 etc.
                // Contains VertexAttributes which describe how the data is stored/laid out.
                case "VTX1":
                    VTX1Tag = new VTX1();
                    VTX1Tag.LoadVTX1FromStream(reader, tagStart, tagSize);
                    break;
                // ENVELOPES - Defines vertex weights for skinning
                case "EVP1":
                    EVP1Tag = new EVP1();
                    EVP1Tag.LoadEVP1FromStream(reader, tagStart);
                    break;
                // DRAW (Skeletal Animation Data) - Stores which matrices (?) are weighted, and which are used directly
                case "DRW1":
                    DRW1Tag = new DRW1();
                    DRW1Tag.LoadDRW1FromStream(reader, tagStart);
                    break;
                // JOINTS - Stores the skeletal joints (position, rotation, scale, etc...)
                case "JNT1":
                    JNT1Tag = new JNT1();
                    JNT1Tag.LoadJNT1FromStream(reader, tagStart);
                    JNT1Tag.CalculateParentJointsForSkeleton(INF1Tag.HierarchyRoot);
                    break;
                // SHAPE - Face/Triangle information for model.
                case "SHP1":
                    SHP1Tag = new SHP1();
                    SHP1Tag.ReadSHP1FromStream(reader, tagStart, VTX1Tag.VertexData);
                    break;
                // MATERIAL - Stores materials (which describes how textures, etc. are drawn)
                case "MAT3":
                    MAT3Tag = new MAT3();
                    MAT3Tag.LoadMAT3FromStream(reader, tagStart);
                    break;
                // TEXTURES - Stores binary texture images.
                case "TEX1":
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    //Debug.LogWarning("JETZT " + Name);
                    TEX1Tag = new TEX1();
                    if (!ZeldaManager.Instance.UseHDTextures.IsActive)
                        TEX1Tag.LoadTEX1FromStream(reader, tagStart, externalBTIs);
                    else TEX1Tag.BTIs = GTXFetcher.LoadHDTextures(Archive.Name, isModel);
                    //watch.Stop();
                    //Debug.LogWarning("TEX1 took " + watch.ElapsedMilliseconds + "ms");
                    break;
                // MODEL - Seems to be bypass commands for Materials and invokes GX registers directly.
                case "MDL3":
                    break;
            }

            // Skip the stream reader to the start of the next tag since it gets moved around during loading.
            reader.BaseStream.Position = tagStart + tagSize;
        }

        reader.Close();
    }

    public void ApplyBonePositionsToAnimationTransforms(IList<SkeletonJoint> boneList, Matrix4[] boneTransforms)
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

            if (origJoint.Parent != null)
            {
                OpenTK.Vector3 curPos = cumulativeTransform.ExtractTranslation();
                OpenTK.Vector3 parentPos = boneTransforms[boneList.IndexOf(origJoint.Parent)].ExtractTranslation();

                //m_lineBatcher.DrawLine(curPos, parentPos, WLinearColor.Red, 1, 0);
            }
        }
    }

    // Erstellt GameObjects für jeden Joint und speichert sie in einem Dictionary.
    Dictionary<string, GameObject> CreateJointGameObjects(List<SkeletonJoint> joints, Transform parent)
    {
        Dictionary<string, GameObject> jointGameObjects = new Dictionary<string, GameObject>();

        foreach (SkeletonJoint joint in joints)
        {
            GameObject jointGameObject = new GameObject(joint.Name);
            jointGameObjects.Add(joint.Name, jointGameObject);
            jointGameObject.transform.parent = parent;

            Joint j = jointGameObject.AddComponent<Joint>();
        }

        return jointGameObjects;
    }

    // Baut die Hierarchie zwischen den Joint-GameObjects basierend auf den parent-Joints.
    void BuildHierarchy(List<SkeletonJoint> joints, Dictionary<string, GameObject> jointGameObjects)
    {
        foreach (SkeletonJoint joint in joints)
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

    Transform FindJointGameObject(SkeletonJoint joint, Transform parentTransform)
    {
        // Suche nach dem GameObject mit dem Namen des Joints in der Hierarchie.
        Transform childTransform = parentTransform.Find(joint.Name);
        if (childTransform != null)
        {
            return childTransform;
        }
        else
        {
            // Durchsuche rekursiv die Kinder, falls das GameObject nicht direkt gefunden wurde.
            foreach (Transform child in parentTransform)
            {
                Transform foundTransform = FindJointGameObject(joint, child);
                if (foundTransform != null)
                {
                    return foundTransform;
                }
            }

            return null; // Das GameObject wurde nicht gefunden.
        }
    }

    private void PrepareShape(GameObject target, int shapeIndex, List<BTI> externalBTIs, IList<SkeletonJoint> boneList,
        Matrix4[] boneTransforms, bool isModel)
    {
        SHP1.Shape shape = SHP1Tag.Shapes[shapeIndex];

        if (isModel)
        {
            var transformedPositions = new List<OpenTK.Vector3>(shape.VertexData.Position.Count);
            var transformedNormals = new List<OpenTK.Vector3>(shape.VertexData.Normal.Count);
            //List<WLinearColor> colorOverride = new List<WLinearColor>();

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

                Matrix4 finalMatrix = Matrix4.Zero;
                if (isPartiallyWeighted)
                {
                    EVP1.Envelope envelope = EVP1Tag.Envelopes[indexFromDRW1];
                    for (int b = 0; b < envelope.NumBones; b++)
                    {
                        Matrix4 sm1 = EVP1Tag.InverseBindPose[envelope.BoneIndexes[b]];
                        Matrix4 sm2 = boneTransforms[envelope.BoneIndexes[b]];

                        finalMatrix = finalMatrix + Matrix4.Mult(Matrix4.Mult(sm1, sm2), envelope.BoneWeights[b]);
                    }
                }
                else
                {
                    // If the vertex is not weighted then we use a 1:1 movement with the bone matrix.
                    finalMatrix = boneTransforms[indexFromDRW1];
                }

                // Multiply the data from the model file by the finalMatrix to put it in the correct (skinned) position
                transformedPositions.Add(OpenTK.Vector3.Transform(shape.VertexData.Position[i], finalMatrix));

                if (shape.VertexData.Normal.Count > 0)
                {
                    OpenTK.Vector3 transformedNormal =
                        OpenTK.Vector3.TransformNormal(shape.VertexData.Normal[i], finalMatrix);
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

        BTI texture = null;
        BTI vertexColorTexture = null;
        bool external = false;
        bool externalVertex = false;

        if (ZeldaManager.Instance.UseHDTextures.IsActive)
        {
            if (isModel)
            {
                // Normal? _Testing.scene mit Drache, testen             
                int baseTexture = material.TextureIndexes[0];

                if (baseTexture == TEX1Tag.BTIs.Count) baseTexture -= 1;
                
                if (baseTexture >= 0 && baseTexture < MAT3Tag.TextureRemapTable.Count)
                {
                    texture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[baseTexture]];
                }
                else
                {
                    baseTexture = 0;
                    Debug.Log(TEX1Tag.BTIs.Count);
                    texture = new BTI("MODEL_BEHIND_DOOR", null, new BinaryTextureImage());
                }

                
                //if (baseTexture >= 0) texture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[baseTexture]];
                if (baseTexture == -1)
                {
                    texture = new BTI("MODEL_BEHIND_DOOR", null, new BinaryTextureImage());
                    return;
                }

                int vertexTexture = material.TextureIndexes[1];
                if (vertexTexture >= 0)
                {
                    int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                    if (remap == 1 && material.TextureIndexes[2] != -1)
                        vertexTexture = material.TextureIndexes[2]; // Get second texture, first propably fake cloud
                    
                    if(MAT3Tag.TextureRemapTable[vertexTexture] >= TEX1Tag.BTIs.Count) vertexColorTexture = TEX1Tag.BTIs[TEX1Tag.BTIs.Count-1];
                    else vertexColorTexture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[vertexTexture]];
                }
            }
            else
            {
                int baseTexture = material.TextureIndexes[0];
                //if (baseTexture >= 17) baseTexture++;
                if (baseTexture >= 0) texture = TEX1Tag.BTIs[baseTexture];
                else if (baseTexture == -1)
                {
                    texture = new BTI("MODEL_BEHIND_DOOR", null, new BinaryTextureImage());
                    return;
                }

                int vertexTexture = material.TextureIndexes[1];
                if (vertexTexture >= 0)
                {
                    int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                    if (remap == 1 && material.TextureIndexes[2] != -1)
                        vertexTexture = material.TextureIndexes[2]; // Get second texture, first propably fake cloud
                    vertexColorTexture = TEX1Tag.BTIs[vertexTexture];
                }
            }
        }
        else
        {
            int baseTexture = material.TextureIndexes[0];
            if (baseTexture >= 0) texture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[baseTexture]];
            else if (baseTexture == -1)
            {
                texture = new BTI("MODEL_BEHIND_DOOR", null, new BinaryTextureImage());
                return;
            }

            int vertexTexture = material.TextureIndexes[1];
            if (vertexTexture >= 0)
            {
                int remap = MAT3Tag.TextureRemapTable[vertexTexture];

                if (remap == 1 && material.TextureIndexes[2] != -1)
                    vertexTexture = material.TextureIndexes[2]; // Get second texture, first propably fake cloud
                vertexColorTexture = TEX1Tag.BTIs[MAT3Tag.TextureRemapTable[vertexTexture]];
            }
        }

        if (texture == null) texture = new BTI("DUMMY", null, new BinaryTextureImage());

        GameObject parent = CreateSubMesh(target, shapeIndex + "_" + material.Name + "_" + texture.Name);
        List<OpenTK.Vector3> vertPos =
            shape.OverrideVertPos.Count > 0 ? shape.OverrideVertPos : shape.VertexData.Position;
        List<OpenTK.Vector3> vertNormal =
            shape.OverrideNormals.Count > 0 ? shape.OverrideNormals : shape.VertexData.Normal;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        foreach (OpenTK.Vector3 pos in vertPos)
        {
            verts.Add(new Vector3(pos.X, pos.Y, pos.Z));
            globalVerts.Add(new Vector3(pos.X, pos.Y, pos.Z));
        }

        foreach (OpenTK.Vector3 pos in vertNormal)
        {
            normals.Add(new Vector3(pos.X, pos.Y, pos.Z));
            globalNormals.Add(new Vector3(pos.X, pos.Y, pos.Z));
        }

        CreateMesh(parent, shape, verts, normals, texture, vertexColorTexture, shape.VertexData, material, external, externalVertex, isModel, shapeIndex);
    }
    
    private List<Vector3> globalVerts = new List<Vector3>();
    private List<Vector3> globalNormals = new List<Vector3>();

    public GameObject CreateSubMesh(GameObject target, string name)
    {
        GameObject o = new GameObject(name);
        o.transform.parent = target.transform;

        o.transform.localPosition = Vector3.zero;
        o.transform.localEulerAngles = Vector3.zero;
        o.transform.localScale = Vector3.one;

        return o;
    }

    public GameObject GetWorldRoot()
    {
        return RecursiveFindChild(transform, "world_root").gameObject;
    }

    public BMD SetParentJoint(BMD parent, string jointName)
    {
        Transform parentTransform = RecursiveFindChild(parent.transform, jointName);
        transform.parent = parentTransform;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;

        return this;
    }

    public BMD SetPosition(Vector3 translation)
    {
        //transform.Translate(translation, Space.Self);
        transform.localPosition = translation;

        return this;
    }

    public void ApplyFirstTranslation()
    {
        transform.Translate(initTranslation);
    }

    public Vector3 initTranslation = Vector3.zero;

    public BMD Translate(Vector3 translation)
    {
        /*transform.localPosition = new Vector3(transform.localPosition.x + translation.x,
            transform.localPosition.y + translation.y, transform.localPosition.z + translation.z);*/

        if (initTranslation == Vector3.zero) initTranslation = translation;
        transform.Translate(translation);

        return this;
    }

    public BMD TranslateLocal(Vector3 translation)
    {
        transform.localPosition = new Vector3(transform.localPosition.x + translation.x,
            transform.localPosition.y + translation.y, transform.localPosition.z + translation.z);

        return this;
    }

    public BMD SetLocalPosition(Vector3 translation)
    {
        transform.localPosition = translation;

        return this;
    }

    public BMD SetLocalRotation(Vector3 translation)
    {
        transform.localEulerAngles = translation;

        return this;
    }

    public BMD RotateXShort(int shortAngle)
    {
        // Berechne die Rotation in Radianten
        float rotationInRadians = ShortToRadians(shortAngle);

        // Wandle Radianten in Grad um, da Quaternion.Euler mit Gradwerten arbeitet
        float rotationInDegrees = rotationInRadians * Mathf.Rad2Deg;

        // Erstelle eine Quaternion für die Rotation um die X-Achse
        Quaternion rotationX = Quaternion.Euler(rotationInDegrees, 0, 0);

        // Multipliziere die aktuelle lokale Rotation mit der neuen X-Rotation
        transform.localRotation *= rotationX;

        return this;
    }

    public void InitializeModelParams(ItemData data)
    {
        /*if (data.Btk != 0xFFFF) {
            const btk_anm = resCtrl.getObjectRes(ResType.Btk, arcName, data.Btk);
            this.btk = new mDoExt_btkAnm();
            this.btk.init(mdl_data, btk_anm, true, LoopMode.Repeat);
        }

        if (data.Bck != 0xFFFF) {
            const bck_anm = resCtrl.getObjectRes(ResType.Bck, arcName, data.Bck);
            this.bck = new mDoExt_bckAnm();
            this.bck.init(mdl_data, bck_anm, true, LoopMode.Repeat);
        }*/

        if (data.Brk != 0xFFFF)
        {

            RARC.File brkFile = ArcReader.GetFileById(Archive, data.Brk);
            BRK brk = new BRK(brkFile.Name);
            
            using (EndianBinaryReader reader = new EndianBinaryReader(brkFile.FileData, Endian.Big))
            {
                brk.LoadFromStream(reader);
            }
            
            bool play_anm = data.TevFrame == 0xFF ? true : false;
            if (play_anm) brk.ApplyFrame(MAT3Tag, GetComponent<SkinnedMeshRenderer>().materials, data.TevFrame);
        }
    }
    
    private float ShortToRadians(int shortValue)
    {
        // Konvertiere den Short-Wert in Radianten (Bereich -π bis +π)
        return shortValue * (Mathf.PI / 0x8000);  // 0x8000 entspricht 32768
    }


    public BMD RotateYShort(int shortAngle)
    {
        float angleInDegrees = shortAngle * (360.0f / 65535.0f);

        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
            transform.localEulerAngles.y + angleInDegrees, transform.localEulerAngles.z);

        return this;
    }

    public BMD RotateY(int angle)
    {
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + angle,
            transform.localEulerAngles.z);

        return this;
    }

    public BMD ResetRotation()
    {
        transform.localEulerAngles = Vector3.zero;

        return this;
    }

    public BMD SetRotation(Vector3 rotation)
    {
        transform.localEulerAngles = rotation;

        return this;
    }

    public BMD SetBaseScale(Vector3 scale)
    {
        transform.localScale = new Vector3(0.01f * scale.x, 0.01f * scale.y, 0.01f * scale.y);

        return this;
    }

    public BMD SetLocalScale(Vector3 scale)
    {
        transform.localScale = scale;

        return this;
    }

    public void RotateByPivot(float target)
    {
        StartCoroutine(_RotateByPivot(target));
    }

    public BMD ToUrpLitShader()
    {
        foreach (Material material in transform.GetComponent<SkinnedMeshRenderer>().sharedMaterials)
        {
            Texture mainTex = material.GetTexture("_MainTex");
            Debug.Log(mainTex.width);

            material.shader = Shader.Find("Universal Render Pipeline/Lit");

            if (mainTex != null)
            {
                material.SetTexture("_BaseMap", mainTex);

                material.DisableKeyword("_ALPHATEST_ON");
            }
        }

        return this;
    }


    public void AddClothPhysics(float maxCoefficients)
    {
        Mesh mesh = transform.GetComponent<SkinnedMeshRenderer>().sharedMesh;

        // Remove double sided vertices
        Vector3[] vertices = mesh.vertices;

        int numVertices = vertices.Length;
        int numTriangles = numVertices / 3;
        int[] triangles = new int[numTriangles * 3];
        for (int i = 0; i < numTriangles; i++)
        {
            triangles[i * 3] = i * 3;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;
        }

        mesh.triangles = triangles;

        Cloth cloth = transform.AddComponent<Cloth>();
        //cloth.useGravity = false;

        //int[] vertexIndices = GetTopVertexIndices(model.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices);
        ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[cloth.coefficients.Length];

        for (int i = 0; i < coefficients.Length; i++) coefficients[i] = cloth.coefficients[i];

        /*for (int i = 0; i < coefficients.Length; i++)
        {
            //bool isFixed = Array.Exists(vertexIndices, index => index == i);
            //coefficients[i].maxDistance = isFixed ? 0 : cloth.coefficients[i].maxDistance;
        }*/

        for (int i = 0; i < maxCoefficients; i++)
        {
            coefficients[coefficients.Length - (i + 1)].maxDistance = 0;
        }

        cloth.coefficients = coefficients;

        float acceleration = -1f;

        cloth.externalAcceleration = new Vector3(acceleration, acceleration, acceleration);
        cloth.randomAcceleration = new Vector3(acceleration, acceleration, acceleration);

        SkinnedMeshRenderer skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.updateWhenOffscreen = true;
    }

    private float currentSpeed;

    private IEnumerator _RotateByPivot(float target)
    {
        float rotationY = transform.GetChild(0).eulerAngles.y;
        float rotationYTarget = rotationY + target;
        Debug.LogError(rotationYTarget - rotationY);
        while (rotationYTarget - rotationY <= 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 2f, 1f * Time.deltaTime);
            transform.parent.eulerAngles = Vector3.MoveTowards(transform.GetChild(0).eulerAngles,
                new Vector3(transform.GetChild(0).eulerAngles.x, rotationYTarget, transform.GetChild(0).eulerAngles.z),
                currentSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void MoveToTarget(Transform target, float moveSpeed, float acceleration, float deceleration)
    {
        _forceStopMove = false;
        StopCoroutine(_MoveToTarget(null, 0, 0, 0));
        StartCoroutine(_MoveToTarget(target, moveSpeed, acceleration, deceleration));
    }

    private bool _forceStopMove = false;

    public void InterruptMove()
    {
        _forceStopMove = true;
        //StopCoroutine(_MoveToTarget(null, 0));
    }

    private IEnumerator _MoveToTarget(Transform target, float maxMoveSpeed, float acceleration, float deceleration)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = target.position;

        // Berechne die Richtung zum Ziel
        Vector3 direction = (targetPosition - startPosition).normalized;

        // Berechne die Drehung, um die Richtung zum Ziel zu erreichen
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Start- und Zielrotation bestimmen
        Quaternion startRotation = transform.rotation;

        //float acceleration = 3f, deceleration = 3f;

        float currentSpeed = 0f;
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            if (_forceStopMove)
            {
                Debug.LogError("RETURN");
                break;
            }

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, currentSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            if (_forceStopMove)
            {
                Debug.LogError("RETURN");
                StopCoroutine(_MoveToTarget(null, 0, 0, 0));
                break;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, maxMoveSpeed, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
            Debug.LogWarning("MACHE OBWOIHL: " + _forceStopMove);
            yield return null;
        }

        transform.position = targetPosition;

        //_forceStopMove = false;
    }


    private Transform RecursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            else
            {
                Transform found = RecursiveFindChild(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }
}