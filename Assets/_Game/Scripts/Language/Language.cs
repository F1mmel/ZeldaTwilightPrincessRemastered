using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using UnityEngine;
using WiiExplorer;

public class Language : MonoBehaviour
{
    public LangType LangType;

    public static List<BmgReader.Message> Messages = new List<BmgReader.Message>();

    public static Language Instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Instance = this;
        Messages = new List<BmgReader.Message>();

        string path = "Assets/GameFiles/res/" + LangType.ToEnumMember();
        
        Task.Run(() =>
        {
            Archive archive = ArcReader.Read(path + "/bmgres.arc");
            ArcFile file = ArcReader.GetFile(archive, "zel_00.bmg");

            BmgReader reader = new BmgReader();
            reader.ParseBmgToJson(file.Buffer);
        });
    }
}

public enum LangType
{
    ___EU___,
    [EnumMember(Value = "Msgde")]
    DE,
    [EnumMember(Value = "Msguk")]
    UK,
    [EnumMember(Value = "Msgfr")]
    FR,
    [EnumMember(Value = "Msgit")]
    IT,
    [EnumMember(Value = "Msgsp")]
    SP,
    ___US___,
    [EnumMember(Value = "Msgus")]
    US,
}