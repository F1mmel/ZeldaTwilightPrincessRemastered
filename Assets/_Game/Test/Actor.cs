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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Rückgabe eines festen Parameters (entspricht `fopAcM_GetParam`)
    private uint GetParam()
    {
        return (uint)Parameter; // Der festgelegte Wert
    }

    // Verschiebt und maskiert die Bits basierend auf den Parametern shift und bit
    private uint GetParamBit(byte shift, byte bit)
    {
        return (GetParam() >> shift) & ((1u << bit) - 1);
    }

    // Liefert die gewünschte Ausgabe basierend auf der Verschiebung und Bitmaske
    public byte GetFRoomNo()
    {
        return (byte)GetParamBit(13, 6);
    }

    // Liefert die gewünschte Ausgabe basierend auf der Verschiebung und Bitmaske
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
