using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using UnityEngine;
using UnityEngine.Rendering;
using WiiExplorer;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class DZSLoader : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    
    public static DZS ParseDZSHeaders(byte[] buffer)
    {
        var chunkHeaders = new Dictionary<string, DZSChunkHeader>();
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Seek(0x03, SeekOrigin.Begin);
                int chunkCount = reader.ReadByte();
                int chunkTableIdx = 0x04;
                for (int i = 0; i < chunkCount; i++)
                {
                    stream.Seek(chunkTableIdx + 0x00, SeekOrigin.Begin);

                    string type = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    int numEntries = reader.ReadByte();
                    byte[] offset = reader.ReadBytes(0x04);
                    Array.Reverse(offset);
                    int offs = BitConverter.ToInt32(offset, 0);


                    chunkHeaders[type] = new DZSChunkHeader { type = type, count = numEntries, offs = offs };
                    chunkTableIdx += 0x0C;
                }
            }
        }
        return new DZS { headers = chunkHeaders, buffer = buffer };
    }
    
    public struct DZSChunkHeader
    {
        public string type;
        public int count;
        public int offs;
    }

    public class DZS
    {
        public Dictionary<string, DZSChunkHeader> headers;
        public byte[] buffer;

    public void Decode(Archive stage, GameObject actorObj)
    {
        GameObject always = new GameObject("Always");
        always.transform.parent = actorObj.transform;
        
        foreach(var header in headers)
        {
            int length = buffer.Length - header.Value.offs;
            byte[] slicedBytes = new byte[length];
            Array.Copy(buffer, header.Value.offs, slicedBytes, 0, length);
            
            GameObject o = new GameObject(header.Value.type);
            o.transform.parent = always.transform;
            if(header.Value.type == "ACTR")
            {
                dStage_actorInit(ActorType.ACTR, slicedBytes, header.Value.count, -1, stage, o);
            } 
            else if(header.Value.type == "TGOB")
            {
                dStage_actorInit(ActorType.ACTR, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "TRES")
            {
                dStage_actorInit(ActorType.TRES, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "TGSC")
            {
                dStage_actorInit(ActorType.TGSC, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "SCOB")
            {
                dStage_tgscInfoInit(ActorType.SCOB, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "SCLS")
            {
                dStage_sclsInfoInit(ActorType.SLCS, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "Door")
            {
                dStage_tgscInfoInit(ActorType.DOOR, slicedBytes, header.Value.count, -1, stage, o);
            }
            else if (header.Value.type == "TGDR")
            {
                dStage_tgscInfoInit(ActorType.TGDR, slicedBytes, header.Value.count, -1, stage, o);
            }
            
            if(o.transform.childCount == 0) Destroy(o);
        }

        if (StageLoader.Instance.Stage.IsDungeon())
        {
            for (int i = 0; i < 15; i++)
            {
                actorlayerLoader(stage, actorObj, i);
            }
        }

        actorlayerLoader(stage, actorObj, SaveManager.GetCurrentLayerOfStage());
        
        if(StageLoader.Instance != null) envLayerLoader(stage, actorObj);
        
        if(always.transform.childCount == 0) Destroy(always);
    }

    public void actorlayerLoader(Archive stage, GameObject actorObj, int i)
    {
        string[] actrLayer = {"ACT0", "ACT1", "ACT2", "ACT3", "ACT4", "ACT5", "ACT6", "ACT7", "ACT8", "ACT9", "ACTa", "ACTb", "ACTc", "ACTd", "ACTe"};
        string[] scobLayer = {"SCO0", "SCO1", "SCO2", "SCO3", "SCO4", "SCO5", "SCO6", "SCO7", "SCO8", "SCO9", "SCOa", "SCOb", "SCOc", "SCOd", "SCOe"};
        string[] doorLayer = {"Doo0", "Doo1", "Doo2", "Doo3", "Doo4", "Doo5", "Doo6", "Doo7", "Doo8", "Doo9", "Dooa", "Doob", "Dooc", "Dood", "Dooe"};

        {
            GameObject layerObj = new GameObject("Layer" + i);
            layerObj.transform.parent = actorObj.transform;
            
            
            foreach(var header in headers)
            {
                int length = buffer.Length - header.Value.offs;
                byte[] slicedBytes = new byte[length];
                Array.Copy(buffer, header.Value.offs, slicedBytes, 0, length);

            
                if(header.Value.type == actrLayer[i])
                {
                    GameObject o = new GameObject(actrLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_actorInit(ActorType.ACTR, slicedBytes, header.Value.count, i, stage, o);
                }
                else if (header.Value.type == scobLayer[i])
                {
                    GameObject o = new GameObject(scobLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_tgscInfoInit(ActorType.SCOB, slicedBytes, header.Value.count, i, stage, o);
                }
                else if (header.Value.type == doorLayer[i])
                {
                    GameObject o = new GameObject(doorLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_tgscInfoInit(ActorType.DOOR, slicedBytes, header.Value.count, i, stage, o);
                }
            }
        
            if(layerObj.transform.childCount == 0) Destroy(layerObj);
        }
    }

    public void envLayerLoader(Archive stage, GameObject actorObj)
    {
        return;
        
        string[] lgtLayer = {"LGT0", "LGT1", "LGT2", "LGT3", "LGT4", "LGT5", "LGT6", "LGT7", "LGT8", "LGT9", "LGTa", "LGTb", "LGTc", "LGTd", "LGTe"};
        string[] envrLayer = {"Env0", "Env1", "Env2", "Env3", "Env4", "Env5", "Env6", "Env7", "Env8", "Env9", "Enva", "Envb", "Envc", "Envd", "Enve"};
        string[] colLayer = {"Col0", "Col1", "Col2", "Col3", "Col4", "Col5", "Col6", "Col7", "Col8", "Col9", "Cola", "Colb", "Colc", "Cold", "Cole"};
        string[] palLayer = {"PAL0", "PAL1", "PAL2", "PAL3", "PAL4", "PAL5", "PAL6", "PAL7", "PAL8", "PAL9", "PALa", "PALb", "PALc", "PALd", "PALe"};
        string[] vrbLayer = {"VRB0", "VRB1", "VRB2", "VRB3", "VRB4", "VRB5", "VRB6", "VRB7", "VRB8", "VRB9", "VRBa", "VRBb", "VRBc", "VRBd", "VRBe"};

        int max = 1;

        {
        int i = SaveManager.GetCurrentLayerOfStage();
            GameObject layerObj = new GameObject("EnvLayer" + i);
            layerObj.transform.parent = actorObj.transform;
            
            
            foreach(var header in headers)
            {
                int length = buffer.Length - header.Value.offs;
                byte[] slicedBytes = new byte[length];
                Array.Copy(buffer, header.Value.offs, slicedBytes, 0, length);
            
                if(header.Value.type == lgtLayer[i])
                {
                    GameObject o = new GameObject(lgtLayer[i]);
                    o.transform.parent = layerObj.transform;
                }
                else if (header.Value.type == envrLayer[i]) 
                {
                    GameObject o = new GameObject(envrLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_envrInfoInit(slicedBytes, header.Value.count, i, stage, o);
                }
                else if (header.Value.type == colLayer[i])
                {
                    GameObject o = new GameObject(colLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_pselectInfoInit(slicedBytes, header.Value.count, i, stage, o);
                }
                else if (header.Value.type == palLayer[i])
                {
                    GameObject o = new GameObject(palLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_paletInfoInit(slicedBytes, header.Value.count, i, stage, o);
                }
                else if (header.Value.type == vrbLayer[i])
                {
                    GameObject o = new GameObject(vrbLayer[i]);
                    o.transform.parent = layerObj.transform;
                    dStage_vrboxInfoInit(slicedBytes, header.Value.count, i, stage, o);
                }
            }
        
            if(layerObj.transform.childCount == 0) Destroy(layerObj);
        }
    }

    public void dStage_sclsInfoInit(ActorType type, byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
{
    using (MemoryStream stream = new MemoryStream(buffer))
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            for (int i = 0; i < count; i++)
            {
                string rawName = Encoding.ASCII.GetString(reader.ReadBytes(8));
                string mapName = rawName.Split('\0')[0];

                byte room = reader.ReadByte();
                
                var data = reader.ReadBytes(4);
                Array.Reverse(data);
                uint param = BitConverter.ToUInt32(data, 0);



                GameObject exitObj = new GameObject(mapName);
                exitObj.transform.parent = actorObj.transform;
                Actor actor = exitObj.AddComponent<Actor>();

                actor.ActorType = type;
                actor.name = mapName;
                actor.MapName = mapName;
                actor.SpawnIndex = room;
                actor.Parameter = (int)param;
                actor.HexParameter = param.ToString("X8");
                
                StageLoader.Instance.SCLC.Add(actor);

                exitObj.transform.localPosition = Vector3.zero;
                exitObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }
    }
}

    public void dStage_tgscInfoInit(ActorType type, byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int offs = 0;
                for (int i = 0; i < count; i++)
                {
                    stream.Seek(offs + 0x00, SeekOrigin.Begin);

                    string rawName = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
                    string name = "";
                    foreach (char c in rawName)
                    {
                        if (char.IsLetter(c) || c == '_' || char.IsNumber(c))
                        {
                            name += c;
                        }
                    }
                    
                    byte[] offset = reader.ReadBytes(0x04);
                    Array.Reverse(offset);
                    int parameter = BitConverter.ToInt32(offset, 0);

                    float posX = ReadBigEndianFloat(reader);
                    float posY = ReadBigEndianFloat(reader);
                    float posZ = ReadBigEndianFloat(reader);

                    float angleX = ReadBigEndianHalf(reader);
                    float angleY = ReadBigEndianHalf(reader);
                    float angleZ = ReadBigEndianHalf(reader);
                    ushort enemyNo = reader.ReadUInt16();
                    
                    float scaleX = reader.ReadByte() / 10.0f;
                    float scaleY = reader.ReadByte() / 10.0f;
                    float scaleZ = reader.ReadByte() / 10.0f;
                    
                    GameObject cube = new GameObject(name);
                    cube.transform.parent = actorObj.transform;
                    Actor actor = cube.AddComponent<Actor>();

                    if (type == ActorType.SCOB)
                    {
                        actor.SCOB_targetIndex = parameter.GetByte(2);
                    } else if (type == ActorType.DOOR)
                    {
                        actor.SCOB_targetIndex = parameter.GetByte(3);
                    }

                    actor.ActorType = type;
                    actor.name = name;
                    actor.Parameter = parameter;
                    actor.HexParameter = parameter.ToString("X8");
                    actor.EnemyNo = enemyNo;
                    actor.Pos = new Vector3(posX / 100, posY / 100, -posZ / 100);
                    actor.Rot = new Vector3(angleX, angleY, angleZ);
                    actor.Scale = new Vector3(scaleX, scaleY, scaleZ);
                    
                    cube.transform.localPosition = new Vector3(posX / 100, posY / 100, -posZ / 100);
                    cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    
                    computeActorModelMatrix(cube, new Vector3(angleX, angleY, angleZ),
                        new Vector3(scaleX, scaleY, scaleZ), new Vector3(posX, posY, posZ));
                    
                    BMDFetcher.Fetch(name, cube, actor, stage);

                    offs += 0x24;
                }
            }
        }
    }

    public void dStage_envrInfoInit(byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        int i = SaveManager.GetCurrentLayerOfStage();
        {
            stage_envr_info_class info = new stage_envr_info_class();
            info.parse(buffer);
            StageLoader.Instance.StageData.Envr.Add(new StageValue<stage_envr_info_class>() {Layer = layer, Class = info});
        }
    }

    public void dStage_lgtvInfoInit(byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        int i = SaveManager.GetCurrentLayerOfStage();
        {
            stage_pure_lightvec_info_class info = new stage_pure_lightvec_info_class();
            info.parse(buffer);
            StageLoader.Instance.StageData.Lights.Add(new StageValue<stage_pure_lightvec_info_class>() {Layer = layer, Class = info});
            
            GameObject lightGameObject = new GameObject("The Light");
            lightGameObject.transform.parent = actorObj.transform;
            lightGameObject.transform.localPosition = info.pos / 1000;
            
            Light light = lightGameObject.AddComponent<Light>();
            light.color = Color.white;
            light.range = info.radius;
            light.intensity = 0.2f;
        }
    }

    public void dStage_paletInfoInit(byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        int offs = 0;
        int i = SaveManager.GetCurrentLayerOfStage();
        {
            stage_palet_info_class pale = new stage_palet_info_class();
            offs += pale.parse(BufferUtil.Slice(buffer, offs));
            StageLoader.Instance.StageData.Palets.Add(new StageValue<stage_palet_info_class>() {Layer = layer, Class = pale});
        }
    }

    public void dStage_pselectInfoInit(byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        int offs = 0;
        int i = SaveManager.GetCurrentLayerOfStage();
        {
            stage_pselect_info_class pale = new stage_pselect_info_class();
            offs += pale.parse(BufferUtil.Slice(buffer, offs));
            StageLoader.Instance.StageData.Selects.Add(new StageValue<stage_pselect_info_class>() {Layer = layer, Class = pale});
        }
    }

    public void dStage_vrboxInfoInit(byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        int offs = 0;
        int i = SaveManager.GetCurrentLayerOfStage();
        {
            stage_vrbox_info_class pale = new stage_vrbox_info_class();
            offs += pale.parse(BufferUtil.Slice(buffer, offs));
            StageLoader.Instance.StageData.Vrbox.Add(new StageValue<stage_vrbox_info_class>() {Layer = layer, Class = pale});
        }
    }

    public async void dStage_actorInit(ActorType type, byte[] buffer, int count, int layer, Archive stage, GameObject actorObj)
    {
        {
            
    using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int offs = 0;
                for (int i = 0; i < count; i++)
                {
                    stream.Seek(offs + 0x00, SeekOrigin.Begin);
                    
                    string rawName = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
                    string name = "";
                    foreach (char c in rawName)
                    {
                        if (char.IsLetter(c) || c == '_' || char.IsNumber(c))
                        {
                            name += c;
                        }
                    }
                    
                    byte[] offset = reader.ReadBytes(0x04);
                    Array.Reverse(offset);
                    int parameter = BitConverter.ToInt32(offset, 0);

                    float posX = ReadBigEndianFloat(reader);
                    float posY = ReadBigEndianFloat(reader);
                    float posZ = ReadBigEndianFloat(reader);

                    float angleX = ReadBigEndianHalf(reader);
                    float angleY = ReadBigEndianHalf(reader);
                    float angleZ = ReadBigEndianHalf(reader);
                    ushort enemyNo = reader.ReadUInt16();
                    
                    float scaleX = reader.ReadByte() / 10000.0f;
                    float scaleY = reader.ReadByte() / 10000.0f;
                    float scaleZ = reader.ReadByte() / 10000.0f;

                    GameObject cube = null;
                    Actor actor = null;
                    
                    cube = new GameObject(name);
                    cube.transform.parent = actorObj.transform;
                    actor = cube.AddComponent<Actor>();


                    actor.ActorType = type;
                    actor.name = name;
                    actor.Parameter = parameter;
                    actor.HexParameter = parameter.ToString("X");
                    actor.ItemNo = parameter & 0xFF;
                    actor.EnemyNo = enemyNo;
                    actor.Pos = new Vector3(posX / 100, posY / 100, -posZ / 100);
                    actor.Rot = new Vector3(angleX, angleY, angleZ);
                    actor.Scale = new Vector3(scaleX, scaleY, scaleZ);
                    
                    actor.Type = (parameter >> 0x18) & 0xF;
                    actor.SwitchNo = (parameter >> 0x10) & 0xFF;

                    if (type == ActorType.TRES)
                    {
                        stream.Seek(offs + 0x00, SeekOrigin.Begin);
                        
                        string name1 = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');

                        byte field_0x8 = reader.ReadByte();
                        byte typeFlag = reader.ReadByte();
                        byte field_0xa = reader.ReadByte();
                        byte appearType = reader.ReadByte();

                        float posX1 = ReadBigEndianFloat(reader);
                        float posY1 = ReadBigEndianFloat(reader);
                        float posZ1 = ReadBigEndianFloat(reader);

                        short roomNo = ReadBigEndianShort(reader);
                        short rotation = ReadBigEndianShort(reader);

                        byte item = reader.ReadByte();
                        byte flagId = reader.ReadByte();

                        byte field_0x1e = reader.ReadByte();
                        byte field_0x1f = reader.ReadByte();
                        
                        actor.ChestItem = item;
                    }
                    
                    cube.transform.localPosition = new Vector3(posX / 100, posY / 100, -posZ / 100);
                    cube.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                    computeActorModelMatrix(cube, new Vector3(angleX, angleY, angleZ),
                        new Vector3(scaleX, scaleY, scaleZ), new Vector3(posX, posY, posZ));
                    
                    BMDFetcher.Fetch(name, cube, actor, stage);

                    offs += 0x20;
                }
            }
        }
        } ;
        
    }
    
    
    private void computeActorModelMatrix(GameObject o, Vector3 rot, Vector3 scale, Vector3 pos)
    {
        double rotationX = rot.x / 0x7FFF * Math.PI;
        double rotationY = rot.y / 0x7FFF * Math.PI;
        double rotationZ = rot.z / 0x7FFF * Math.PI;
        
        computeModelMatrixSRT(o, new Vector3((float)rotationX, (float)rotationY , (float)rotationZ), scale, pos);
    }
    
    private void computeModelMatrixSRT(GameObject o, Vector3 rotation, Vector3 scale, Vector3 position)
    {
        double sinX = Math.Sin(rotation.x), cosX = Math.Cos(rotation.x);
        double sinY = Math.Sin(rotation.y), cosY = Math.Cos(rotation.y);
        double sinZ = Math.Sin(rotation.z), cosZ = Math.Cos(rotation.z);

        double scaleX =  scale.x * (cosY * cosZ);

        double scaleY =  scale.y * (sinX * cosZ * sinY - cosX * sinZ);

        double scaleZ =  scale.z * (cosX * cosZ * sinY + sinX * sinZ);

        o.transform.localEulerAngles = new Vector3(0, (float)(-rotation.y * 180 / Math.PI), 0);
        o.transform.localScale = new Vector3(0.01f, 0.01f, -0.01f);
    }


    private uint ReadBigEndianUInt(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt32(bytes, 0);
    }

    private short ReadBigEndianShort(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt16(bytes, 0);
    }
    public static float ReadBigEndianFloat(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

        public float ReadBigEndianHalf(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        private float HalfToFloat(ushort value)
        {
            int mantissa = value & 0x03FF;
            int exponent = value & 0x7C00;
            if (exponent == 0x7C00)
            {
                exponent = 0xFF << 23;
            }
            else if (exponent != 0)
            {
                exponent += 0x70 << 23;
            }
            else if (mantissa != 0)
            {
                exponent = 1 << 23;
                do
                {
                    exponent--;
                    mantissa <<= 1;
                } while ((mantissa & 0x0400) == 0);
                mantissa &= 0x03FF;
            }
            return BitConverter.ToSingle(BitConverter.GetBytes((value & 0x8000) << 16 | (exponent | mantissa) << 13), 0);
        }

    }
}

public enum ItemNo {
    GREEN_RUPEE = 1,
    BLUE_RUPEE = 2,
    YELLOW_RUPEE = 3,
    RED_RUPEE = 4,
    PURPLE_RUPEE = 5,
    ORANGE_RUPEE = 6,
    SILVER_RUPEE = 7,
}

public enum DistAttnFunction {
    OFF = 0x00,
    GENTLE = 0x01,
    MEDIUM = 0x02,
    STEEP = 0x03,
}

public enum SpotFunction {
    OFF = 0x00,
    FLAT = 0x01,
    COS = 0x02,
    COS2 = 0x03,
    SHARP = 0x04,
    RING1 = 0x05,
    RING2 = 0x06,
}

[Serializable]
public class stage_pure_lightvec_info_class
{
    public Vector3 pos;
    public float radius;
    public Vector2 dir;
    public SpotFunction spotFn = SpotFunction.OFF;
    public float spotCutoff;
    public DistAttnFunction distFn = DistAttnFunction.OFF;
    public float swtch;
    public float fluctuation;

    public int parse(byte[] buffer) {

        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                float posX = ReadBigEndianFloat(reader);
                float posY = ReadBigEndianFloat(reader);
                float posZ = ReadBigEndianFloat(reader);
                
                radius = ReadBigEndianFloat(reader);
                
                float dirX = ReadBigEndianFloat(reader);
                float dirY = ReadBigEndianFloat(reader);
                dKy_lightdir_set(dir, dirX, dirY);
                
                spotCutoff = ReadBigEndianFloat(reader);
                fluctuation = reader.ReadByte();
                spotFn = (SpotFunction) reader.ReadByte();
                distFn = (DistAttnFunction) reader.ReadByte();
                this.swtch = reader.ReadByte();

                pos = new Vector3(posX, posY, posZ);
                
                return 0x20;
            }
        }
    }
    
    private void dKy_lightdir_set(Vector3 dst, float x, float y) {
        computeUnitSphericalCoordinates(dst, (float) (x * (Math.PI / 180f)), (float) (y * (Math.PI / 180f)));
    }

    private void computeUnitSphericalCoordinates(Vector3 dst, float azimuthal, float polar) {

        float sinP = (float) Math.Sin(polar);
        dst[0] = (float) (sinP * Math.Cos(azimuthal));
        dst[1] = (float) (Math.Cos(polar));
        dst[2] = (float) (sinP * Math.Sin(azimuthal));
    }

    public static float ReadBigEndianFloat(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }
}

[Serializable]
public class stage_envr_info_class {
    public int[] pselIdx = new int[8];

    public int parse(byte[] buffer) {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int i = 0; i < 8; i++)
                {
                    pselIdx[i] = reader.ReadByte();
                }
                
                
                return 0x41;
            }
        }
    }
}

[Serializable]
public class stage_pselect_info_class
{
    public int[] palIdx = new int[8];
    public float changeRate;

    public int parse(byte[] buffer) {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int i = 0; i < 8; i++)
                {
                    palIdx[i] = reader.ReadByte();
                }
                
                changeRate = stage_pure_lightvec_info_class.ReadBigEndianFloat(reader);
                
                return 0x0C;
            }
        }
    }
}

[Serializable]
public class stage_palet_info_class {
    public Color actorAmbCol = Color.white;
    public Color[] bgAmbCol = new[] { Color.white, Color.white, Color.white, Color.white };
    public Color[] lightCol = new[] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white };
    public Color fogCol = Color.white;
    public float fogStartZ;
    public float fogEndZ;
    public float virtIdx;
    public int terrainLightInfluence;
    public int cloudShadowDensity;
    public int unk_2f;
    public int bloomTblIdx;
    public int bgAmbColor1A;
    public int bgAmbColor2A;
    public int bgAmbColor3A;

    public int parse(byte[] buffer) {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                colorFromRGB8(ref this.actorAmbCol, reader.ReadInt32());
                
                colorFromRGB8(ref this.bgAmbCol[0], reader.ReadInt32());
                colorFromRGB8(ref this.bgAmbCol[1], reader.ReadInt32());
                colorFromRGB8(ref this.bgAmbCol[2], reader.ReadInt32());
                colorFromRGB8(ref this.bgAmbCol[3], reader.ReadInt32());
                
                colorFromRGB8(ref this.lightCol[0], reader.ReadInt32());
                colorFromRGB8(ref this.lightCol[1], reader.ReadInt32());
                colorFromRGB8(ref this.lightCol[2], reader.ReadInt32());
                colorFromRGB8(ref this.lightCol[3], reader.ReadInt32());
                colorFromRGB8(ref this.lightCol[4], reader.ReadInt32());
                colorFromRGB8(ref this.lightCol[5], reader.ReadInt32());
                
                colorFromRGB8(ref this.fogCol, reader.ReadInt32());
                
                this.fogStartZ = stage_pure_lightvec_info_class.ReadBigEndianFloat(reader);
                this.fogEndZ = stage_pure_lightvec_info_class.ReadBigEndianFloat(reader);
                this.virtIdx = stage_pure_lightvec_info_class.ReadBigEndianFloat(reader);
                this.terrainLightInfluence = reader.ReadByte() / 100;
                this.cloudShadowDensity = reader.ReadByte() / 255;
                this.unk_2f = reader.ReadByte();
                this.bloomTblIdx = reader.ReadByte();
                this.bgAmbColor1A = reader.ReadByte() / 255;
                this.bgAmbColor2A = reader.ReadByte() / 255;
                this.bgAmbColor3A = reader.ReadByte() / 255;
                
                return 0x34;
            }
        }
    }

    public static void colorFromRGB8(ref Color dst, long n)
    {
        colorFromRGBA8(ref dst, (n & 0xFFFFFF00) | 0xFF);
    }

    public static void colorFromRGBA8(ref Color dst, long n)
    {
        dst.r = ((n >> 24) & 0xFF) / 255f;
        dst.g = ((n >> 16) & 0xFF) / 255f;
        dst.b = ((n >> 8) & 0xFF) / 255f;
        dst.a = (n & 0xFF) / 255f;
    }
}

[Serializable]
public class stage_vrbox_info_class {
    public Color skyCol = Color.white;
    public Color kumoCol = Color.white;
    public Color shitaGumoCol = Color.white;
    public Color shimoUneiCol = Color.white;
    public Color kasumiCol = Color.white;
    public Color okuKasumiCol = Color.white;

    public int parse(byte[] buffer) {
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stage_palet_info_class.colorFromRGB8(ref this.skyCol, reader.ReadInt32());
                stage_palet_info_class.colorFromRGB8(ref this.kumoCol, reader.ReadInt32());
                stage_palet_info_class.colorFromRGB8(ref this.shitaGumoCol, reader.ReadInt32());
                stage_palet_info_class.colorFromRGB8(ref this.shimoUneiCol, reader.ReadInt32());
                this.kumoCol.a = reader.ReadByte() / 0xFF;
                stage_palet_info_class.colorFromRGBA8(ref this.kasumiCol, reader.ReadInt32());
                stage_palet_info_class.colorFromRGBA8(ref this.okuKasumiCol, reader.ReadInt32());
                
                return 0x15;
            }
        }
    }
}