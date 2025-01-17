using System;
using System.Collections.Generic;
using System.IO;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class SaveDetails
{
    public string PlayerName = "";
    public string HorseName = "";
    public int HeartCount = 3;
    public string SaveTime = "";
    public string TotalPlayTime = "";
}

[Serializable]
public class SaveData
{
    public string CurrentStage = "";
    public SerializableVector3 PlayerLocation = new SerializableVector3(Vector3.zero);
    public SerializableQuaternion PlayerRotation = new SerializableQuaternion(Quaternion.identity);
    public SerializedDictionary<string, StageLayerFlagData> StageData = new SerializedDictionary<string, StageLayerFlagData>();
}

[Serializable]
public class StageLayerFlagData
{
    public int CurrentLayer = 1;
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
}

[Serializable]
public class SaveGame
{
    public SaveDetails SaveDetails = new SaveDetails();
    public SaveData SaveData = new SaveData();
}

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public struct SerializableQuaternion
{
    public float x, y, z, w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

public class SaveManager : MonoBehaviour
{
    [Range(1, 3)]
    public int SaveGameIndex = 1;
    
    private const string SAVE_PATH = "/savegame.json";
    public List<SaveGame> SaveGames = new List<SaveGame>();

    // Dictionary für die Stage Flags
    public SerializedDictionary<string, List<string>> StageFlags = new SerializedDictionary<string, List<string>>();

    [HideInInspector]
    public SaveGame currentSavegame;

    public static SaveManager Instance;

    public static int GetCurrentLayerOfStage()
    {
        return Instance.currentSavegame.SaveData.StageData[StageLoader.Instance.Stage.ToString()].CurrentLayer;
    }

    public static void SetFlag(string flag)
    {
        Instance.currentSavegame.SaveData.StageData[StageLoader.Instance.Stage.ToString()].Flags[flag] = true;        
        //Instance.SaveSaves();
    }

    public static void UnsetFlag(string flag)
    {
        Instance.currentSavegame.SaveData.StageData[StageLoader.Instance.Stage.ToString()].Flags[flag] = false;
        //Instance.SaveSaves();
    }

    public static bool IsValid(string flag)
    {
        return !Instance.currentSavegame.SaveData.StageData[StageLoader.Instance.Stage.ToString()].Flags[flag];
    }

    public static void HandleChilds(string flag, Transform o)
    {            
        o.gameObject.SetActive(!Instance.currentSavegame.SaveData.StageData[StageLoader.Instance.Stage.ToString()].Flags[flag]);
    }
    
    public void Awake()
    {
        Instance = this;
        LoadSaves();

        currentSavegame = SaveGames[SaveGameIndex - 1];
    }

    // Initialisiere SaveGames mit Standardwerten
    public void InitializeSaveGames()
    {
        SaveGames = new List<SaveGame>
        {
            CreateNewSaveGame(),
            CreateNewSaveGame(),
            CreateNewSaveGame()
        };
        SaveSaves();
    }

    // Erstellen eines neuen SaveGame mit Standardwerten und Flags
    private SaveGame CreateNewSaveGame()
    {
        SaveGame newSave = new SaveGame();
        newSave.SaveData.StageData = InitializeFlags();
        return newSave;
    }

    // Beispielmethode zur Initialisierung der Flags für alle Stages
    private SerializedDictionary<string, StageLayerFlagData> InitializeFlags()
    {
        SerializedDictionary<string, StageLayerFlagData> flags = new SerializedDictionary<string, StageLayerFlagData>();

        foreach (Stage stage in Enum.GetValues(typeof(Stage)))
        {
            string stageName = stage.ToString();
            StageLayerFlagData stageFlags = new StageLayerFlagData();
            string[] existingFlags = stage.GetFlags();

            if (existingFlags == null) continue;

            foreach (string flag in existingFlags)
            {
                stageFlags.Flags.Add(flag, false); // Flag wird direkt hinzugefügt
            }

            flags.Add(stageName, stageFlags); // Direkt auf Stage-Ebene speichern
        }

        return flags;
    }

    public void SaveSaves()
    {
        string path = Application.persistentDataPath + SAVE_PATH;
        string json = JsonConvert.SerializeObject(SaveGames, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log("Game Saved to " + path);
    }

    public void LoadSaves()
    {
        string path = Application.persistentDataPath + SAVE_PATH;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveGames = JsonConvert.DeserializeObject<List<SaveGame>>(json);
            Debug.Log("Game Loaded from " + path);

            // Aktualisiere die Flags für alle geladenen Spielstände
            foreach (var saveGame in SaveGames)
            {
                UpdateFlags(saveGame);
            }
            SaveSaves(); // Sicherstellen, dass die neuen Flags gespeichert werden
        }
        else
        {
            Debug.Log("No save file found. Creating new save...");
            InitializeSaveGames();
        }
    }

    // Methode zur Aktualisierung der Flags bei bestehenden Saves
    // Methode zur Aktualisierung der Flags bei bestehenden Saves
private void UpdateFlags(SaveGame saveGame)
{
    // Verwende die StageFlags aus dem Inspector als Standard
    foreach (var stageFlagEntry in StageFlags)
    {
        string stageName = stageFlagEntry.Key;
        List<string> stageFlags = stageFlagEntry.Value;

        // Wenn die Stage im aktuellen Save existiert
        if (saveGame.SaveData.StageData.ContainsKey(stageName))
        {
            // Gehe durch die standardmäßigen Flags und füge neue hinzu, falls sie fehlen
            foreach (string flag in stageFlags)
            {
                if (!saveGame.SaveData.StageData[stageName].Flags.ContainsKey(flag))
                {
                    saveGame.SaveData.StageData[stageName].Flags.Add(flag, false); // Neuer Flag mit Standardwert hinzufügen
                }
            }

            // Entferne Flags, die im aktuellen Save vorhanden sind, aber nicht mehr in den StageFlags aus dem Inspector
            List<string> flagsToRemove = new List<string>();
            foreach (var existingFlag in saveGame.SaveData.StageData[stageName].Flags.Keys)
            {
                if (!stageFlags.Contains(existingFlag))
                {
                    flagsToRemove.Add(existingFlag); // Markiere zum Entfernen
                }
            }

            foreach (var flagToRemove in flagsToRemove)
            {
                saveGame.SaveData.StageData[stageName].Flags.Remove(flagToRemove); // Entferne nicht mehr existierende Flags
            }
        }
        else
        {
            // Wenn die Stage noch nicht existiert, füge sie mit den Flags aus dem Inspector hinzu
            StageLayerFlagData newStageFlags = new StageLayerFlagData();
            foreach (string flag in stageFlags)
            {
                newStageFlags.Flags.Add(flag, false);
            }

            newStageFlags.CurrentLayer = 0;
            saveGame.SaveData.StageData.Add(stageName, newStageFlags);
        }
    }

    // Entferne komplette Stages, die nicht mehr im Inspector vorhanden sind
    /*List<string> stagesToRemove = new List<string>();
    foreach (var existingStage in saveGame.SaveData.Flags.Keys)
    {
        
        foreach (var a in saveGame.SaveData.StageData[existingStage].Flags.Keys)
        {
            if (!StageFlags.ContainsKey(existingStage))
            {
                stagesToRemove.Add(existingStage); // Markiere Stage zum Entfernen
            }
        }
        
        if (!StageFlags.ContainsKey(existingStage))
        {
            //stagesToRemove.Add(existingStage); // Markiere Stage zum Entfernen
        }
    }

    foreach (var stageToRemove in stagesToRemove)
    {
        saveGame.SaveData.Flags.Remove(stageToRemove); // Entferne nicht mehr existierende Stages
    }*/
    }

    private void OnDestroy()
    {
        foreach (var saveGame in SaveGames)
        {
            UpdateFlags(saveGame);
        }
        SaveSaves();
    }

    // Beispielmethode, um ein Flag zu ändern
    public void UpdateFlag(string stageName, string flagName, bool value)
    {
        /*foreach (var saveGame in SaveGames)
        {
            if (saveGame.SaveData.Flags.ContainsKey(stageName))
            {
                if (saveGame.SaveData.Flags[stageName].ContainsKey(flagName))
                {
                    saveGame.SaveData.Flags[stageName][flagName] = value;
                }
            }
        }
        SaveSaves();*/
    }
}
