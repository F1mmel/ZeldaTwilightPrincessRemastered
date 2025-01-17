using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class Actor : MonoBehaviour
{
    public ActorType ActorType = ActorType.ACTR;
    
    [ActorGroup("SLCS")]
    public string MapName;
    public byte SpawnIndex;

    [ActorGroup("SCOB")] public int SCOB_targetIndex;
    [ActorGroup("DOOR")] public int DOOR_targetIndex;
    
    [Space]
    
    [ActorGroup("Always")]
    public string Name;
    public int Parameter;
    public string HexParameter;
    
    [ActorGroup("ACTR", "DOOR")]
    public int EnemyNo;
    public int ItemNo;
    public int Type;
    public int SwitchNo;
    public Vector3 Pos;
    public Vector3 Rot;
    public Vector3 Scale;
    
    [ActorGroup("TRES")] 
    public int ChestItem;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
    
    private uint GetParam()
    {
        return (uint)Parameter;
    }

    private uint GetParamBit(byte shift, byte bit)
    {
        return (GetParam() >> shift) & ((1u << bit) - 1);
    }

    public byte GetFRoomNo()
    {
        return (byte)GetParamBit(13, 6);
    }

    public byte GetBRoomNo()
    {
        return (byte)GetParamBit(19, 6);
    }
}

public enum ActorType
{
    ACTR,
    TRES,
    TGSC,
    SLCS,
    DOOR,
    TGDR,
    SCOB
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class ActorGroupAttribute : Attribute
{
    public string[] Groups { get; }

    public ActorGroupAttribute(params string[] groups)
    {
        Groups = groups;
    }
}
