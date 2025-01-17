using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

public enum Stage {
    ___HYRULEFIELD___,
    [StageData("F_SP121",  false, "", 0, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15)]
    HyruleField,

    ___ORDON___,
    [StageData("F_SP103", false, "Ordon Village")]
    [StageFlag(
        "monkey_saved",
        "got_fishingRod",
        "shoot_bees"
        )]
    OrdonVillage,
    [StageData("F_SP103", false, "", 1)]
    OutsideLinksHouse,
    [StageData("F_SP00", false)]
    OrdonRanch,
    [StageData("F_SP104", false, "", 1)]
    OrdonSpring,
    [StageData("R_SP01", false, "", 0)]
    BosHouse,
    [StageData("R_SP01",false, "",  1)]
    SeraSundries,
    [StageData("R_SP01", false, "", 2)]
    JagglesHouse,
    [StageData("R_SP01", false, "", 4, 7)]
    LinksHouse,
    [StageData("R_SP01",false, "",  5)]
    RuslsHouse,

    ___FARON___,
    [StageData("F_SP108", false, "", 0, 1, 2, 3, 4, 5, 8, 11, 14)]
    SouthFaronWoods,
    [StageData("F_SP108", false, "", 6)]
    NorthFaronWoods,
    [StageData("F_SP117",false, "",  3)]
    LostWoods,
    [StageData("F_SP117", false, "", 1)]
    SacredGrove,
    [StageData("F_SP117", false, "", 2)]
    TempleofTimePast,
    [StageData("D_SB10",false,  "")]
    FaronWoodsCave,
    [StageData("R_SP108", false, "")]
    CorosHouse,

    ___ELDIN___,
    [StageData("F_SP109", false, "")]
    KakarikoVillage,
    [StageData("F_SP110", false, "", 0, 1, 2, 3)]
    DeathMountainTrail,
    [StageData("F_SP111",false,  "")]
    KakarikoGraveyard,
    [StageData("F_SP128", false, "")]
    HiddenVillage,
    [StageData("R_SP109", false, "", 0)]
    RenadosSanctuary,
    [StageData("R_SP209", false, "", 7)]
    SanctuaryBasement,
    [StageData("R_SP109",false, "",  1)]
    BarnesBombs,
    [StageData("R_SP109",false, "",  2)]
    EldeInn,
    [StageData("R_SP109",false, "",  3)]
    MaloMart,
    [StageData("R_SP109",false, "",  4)]
    LookoutTower,
    [StageData("R_SP109", false, "", 5)]
    BombWarehouse,
    [StageData("R_SP109", false, "", 6)]
    AbandonedHouse,
    [StageData("R_SP110", false, "")]
    GoronEldersHall,

    ___LANAYRU___,
    [StageData("F_SP122", false, "", 8)]
    OutsideCastleTownWest,
    [StageData("F_SP122", false, "", 16)]
    OutsideCastleTownSouth,
    [StageData("F_SP122",false, "",  17)]
    OutsideCastleTownEast,
    [StageData("F_SP116",false, "",  0, 1, 2, 3, 4)]
    CastleTown,
    [StageData("F_SP112", false, "", 1)]
    ZorasRiver,
    [StageData("F_SP113", false, "", 0, 1)]
    ZorasDomain,
    [StageData("F_SP115", false, "")]
    LakeHylia,
    [StageData("F_SP115", false, "", 1)]
    LanayruSpring,
    [StageData("F_SP126", false, "", 0)]
    UpperZorasRiver,
    [StageData("F_SP127", false, "", 0)]
    FishingPond,
    [StageData("R_SP107", false, "", 0, 1, 2, 3)]
    CastleTownSewers,
    [StageData("R_SP116", false, "", 5, 6)]
    TelmasBarSecretPassage,
    [StageData("R_SP127", false, "", 0)]
    HenasCabin,
    [StageData("R_SP128",false,  "", 0)]
    ImpazsHouse,
    [StageData("R_SP160", false, "", 0)]
    MaloMartTown,
    [StageData("R_SP160", false, "", 1)]
    FanadisPalace,
    [StageData("R_SP160", false, "", 2)]
    MedicalClinic,
    [StageData("R_SP160", false, "", 3)]
    AgithasCastle,
    [StageData("R_SP160",false,  "", 4)]
    GoronShop,
    [StageData("R_SP160",false,  "", 5)]
    JovanisHouse,
    [StageData("R_SP161",false,  "", 7)]
    STARTent,

    ___GERUDODESERT___,
    [StageData("F_SP118", false, "", 0, 1, 3)]
    BulblinCamp,
    [StageData("F_SP118",false,  "", 2)]
    BulblinCampBetaRoom,
    [StageData("F_SP124", false, "", 0)]
    GerudoDesert,
    [StageData("F_SP125", false, "", 4)]
    MirrorChamber,

    ___SNOWPEAK___,
    [StageData("F_SP114", false, "", 0, 1, 2)]
    SnowpeakMountain,

    ___FORESTTEMPLE___,
    [StageData("D_MN05",true, "",  0, 1, 2, 3, 4, 5, 7, 9, 10, 11, 12, 19, 22)]
    ForestTemple,
    [StageData("D_MN05A", true, "", 50)]
    DiababaArena,
    [StageData("D_MN05B",true, "",  51)]
    OokArena,

    ___GORONMINES___,
    [StageData("D_MN04", true, "", 1, 3, 4, 5, 6, 7, 9, 11, 12, 13, 14, 16, 17)]
    GoronMines,
    [StageData("D_MN04A", true, "", 50)]
    FyrusArena,
    [StageData("D_MN04B", true, "", 51)]
    DangoroArena,

    ___LAKEBEDTEMPLE___,
    [StageData("D_MN01", true, "", 0, 1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13)]
    LakebedTemple,
    [StageData("D_MN01A", true, "", 50)]
    MorpheelArena,
    [StageData("D_MN01B",true, "",  51)]
    DekuToadArena,

    ___ARBITERGROUNDS___,
    [StageData("D_MN10", true, "", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)]
    ArbitersGrounds,
    [StageData("D_MN10A", true, "", 50)]
    StallordArena,
    [StageData("D_MN10B", true, "", 51)]
    DeathSwordArena,

    ___SNOWPEAKRUINS___,
    [StageData("D_MN11",true,  "", 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 13)]
    SnowpeakRuins,
    [StageData("D_MN11A",true, "",  50)]
    BlizzetaArena,
    [StageData("D_MN11B",true, "",  51)]
    DarkhammerArena,
    [StageData("D_MN11B",true, "",  49)]
    DarkhammerBetaArena,

    ___TEMPLEOFTIME___,
    [StageData("D_MN06",true, "",  0, 1, 2, 3, 4, 5, 6, 7, 8)]
    TempleofTime,
    [StageData("D_MN06A", true, "", 50)]
    ArmogohmaArena,
    [StageData("D_MN06B", true, "", 51)]
    DarknutArena,

    ___CITYINTHESKY___,
    [StageData("D_MN07", true, "", 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16)]
    CityintheSky,
    [StageData("D_MN07A",true, "",  50)]
    ArgorokArena,
    [StageData("D_MN07B", true, "", 51)]
    AeralfosArena,

    ___PALACEOFTWILIGHT___,
    [StageData("D_MN08", true, "", 0, 1, 2, 4, 5, 7, 8, 9, 10, 11)]
    PalaceofTwilight,
    [StageData("D_MN08A", true, "", 10)]
    PalaceofTwilightThroneRoom,
    [StageData("D_MN08B",true, "",  51)]
    PhantomZantArena1,
    [StageData("D_MN08C", true, "", 52)]
    PhantomZantArena2,
    [StageData("D_MN08D",true, "",  50, 53, 54, 55, 56, 57, 60)]
    ZantArenas,

    ___HYRULECASTLE___,
    [StageData("D_MN09", true, "", 1, 2, 3, 4, 5, 6, 8, 9, 11, 12, 13, 14, 15)]
    HyruleCastle,
    [StageData("D_MN09A",true, "",  50, 51)]
    HyruleCastleThroneRoom,
    [StageData("D_MN09B", true, "", 0)]
    HorsebackGanondorfArena,
    [StageData("D_MN09C", true, "", 0)]
    DarkLordGanondorfArena,

    ___GROTTOS___,
    [StageData("D_SB00",false, "",  0)]
    IceCavern,
    [StageData("D_SB01",false, "",  0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49)]
    CaveOfOrdeals,
    [StageData("D_SB02",false, "",  0)]
    KakarikoGorgeCavern,
    [StageData("D_SB03", false, "", 0)]
    LakeHyliaCavern,
    [StageData("D_SB04",false,  "", 10)]
    GoronStockcave,
    [StageData("D_SB05", false)]
    Grotto1,
    [StageData("D_SB06", false, "", 1)]
    Grotto2,
    [StageData("D_SB07",false, "",  2)]
    Grotto3,
    [StageData("D_SB08",false, "",  3)]
    Grotto4,
    [StageData("D_SB09", false, "", 4)]
    Grotto5,

    ___MISC___,
    [StageData("F_SP102", false)]
    TitleScreenKingBulblin1,
    [StageData("F_SP123", false, "", 13)]
    KingBulblin2,
    [StageData("F_SP200", false)]
    WolfHowlingCutsceneMap,
    [StageData("R_SP301", false)]
    CutsceneHyruleCastleThroneRoom,

}



[System.Serializable]
public class LocalizationData
{
    public string language;
    public List<LocalizationString> localizations;
}

[System.Serializable]
public class LocalizationString
{
    public string key;
    public string value;
    public string valuePlural;
}

public static class EnumerationExtensions
{
    
    public static string? ToEnumMember<T>(this T value) where T : Enum
    {
        return typeof(T)
            .GetTypeInfo()
            .DeclaredMembers
            .SingleOrDefault(x => x.Name == value.ToString())?
            .GetCustomAttribute<EnumMemberAttribute>(false)?
            .Value;
    }
    
    public static string GetStageFile(this Enum enumeration)
    {
        var attribute = GetText<StageDataAttribute>(enumeration);
        if (attribute == null) return null;
        return attribute.StageFile;
    }

    public static int[] GetRooms(this Enum enumeration)
    {
        var attribute = GetText<StageDataAttribute>(enumeration);
        return attribute.Rooms;
    }

    public static string GetBackgroundMusic(this Enum enumeration)
    {
        var attribute = GetText<StageDataAttribute>(enumeration);
        return attribute.BackgroundMusic;
    }

    public static bool IsDungeon(this Enum enumeration)
    {
        var attribute = GetText<StageDataAttribute>(enumeration);
        return attribute.IsDungeon;
    }

    public static string[] GetFlags(this Enum enumeration)
    {
        var attribute = GetText<StageFlagAttribute>(enumeration);
        if (attribute == null) return null;
        return attribute.Flags;
    }

    /*public static void SetLocalizationPath(this Enum enumeration, string value)
    {
        var attribute = GetText<StageDataAttribute>(enumeration);
        attribute.DisplayName = value;
    }*/

    public static T GetText<T>(Enum enumeration) where T : Attribute
    {
        var type = enumeration.GetType();

        var memberInfo = type.GetMember(enumeration.ToString());

        if (!memberInfo.Any())
            throw new ArgumentException($"No public members for the argument '{enumeration}'.");

        var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);

        if (attributes.Length == 0) return null;

        if (attributes == null || attributes.Length != 1)
            throw new ArgumentException($"Can't find an attribute matching '{typeof(T).Name}' for the argument '{enumeration}'");

        if (attributes.Single() == null) return null;
        return attributes.Single() as T;
    }
}
[AttributeUsage(AttributeTargets.Field)]
public class StageDataAttribute : Attribute
{

    public static List<StageDataAttribute> attrs = new List<StageDataAttribute>();
    //public static Dictionary<string, string> a = new Dictionary<string, string>();
    public static List<string> a = new List<string>();

    public StageDataAttribute(string stageFile, bool isDungeon, string backgroundMusic = "", params int[] rooms)         // Stage name? Eigentlich pro Raum neuer Name
    {
        StageFile = stageFile;
        Rooms = rooms;
        BackgroundMusic = backgroundMusic;
        IsDungeon = isDungeon;
    }

    public string StageFile { get; set; }
    public string BackgroundMusic { get; set; }
    public bool IsDungeon {get;set;}
    public int[] Rooms { get; set; }
}
[AttributeUsage(AttributeTargets.Field)]
public class StageFlagAttribute : Attribute
{
    public static List<StageFlagAttribute> attrs = new List<StageFlagAttribute>();
    public static List<string> a = new List<string>();

    public StageFlagAttribute(params string[] flags)         // Stage name? Eigentlich pro Raum neuer Name
    {
        Flags = flags;
    }

    public string[] Flags { get; set; }
}