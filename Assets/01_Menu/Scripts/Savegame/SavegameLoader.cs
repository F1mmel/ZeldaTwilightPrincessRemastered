using System;
using UnityEngine;
using System.IO;
using DG.Tweening;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SavegameLoader : MonoBehaviour
{
    public string jsonFilePath = "Assets/savegame.json";
    
    [Header("References")] public SaveFileReferences[] Files;
    
    public SaveSlot[] SaveSlots;

    public static SavegameLoader Instance;

    void Start()
    {
        Instance = this;
        LoadSaveData();
        
        ApplyDataToSavegames();
    }

    private void ApplyDataToSavegames()
    {
        

        for (int i = 0; i < SaveSlots.Length; i++)
        {
            SaveSlot data = SaveSlots[i];
            SaveFileReferences save = Files[i];

            if (data.SaveDetails.PlayerName.Equals(""))
            {
                // En-disable objects
                save.Save.gameObject.SetActive(false);
                save.ParticleFire.gameObject.SetActive(false);
                save.NoSave.gameObject.SetActive(true);
            }
            else
            {
                // En-disable objects
                save.Save.gameObject.SetActive(true);
                save.ParticleFire.gameObject.SetActive(true);
                save.NoSave.gameObject.SetActive(false);
                
                // Set details to save
                save.Name.GetComponent<TMP_Text>().text = data.SaveDetails.PlayerName;
                save.SaveTime.GetComponent<TMP_Text>().text = data.SaveDetails.SaveTime;
                save.TotalPlayTime.GetComponent<TMP_Text>().text = data.SaveDetails.TotalPlayTime;

                HeartManager heartManager = save.Hearts.GetComponent<HeartManager>();
                heartManager.Count = data.SaveDetails.HeartCount;
                heartManager.InitializeHearts();
            }
        }
    }

    public void CreateNewSave(int index, string playerName, string horseName)
    {
        SaveSlots[index].SaveDetails.PlayerName = playerName;
        SaveSlots[index].SaveDetails.HorseName = horseName;
        SaveSlots[index].SaveDetails.SaveTime = DateTime.Now.ToString();
        SaveSlots[index].SaveDetails.TotalPlayTime = "00:00";
        
        ApplyDataToSavegames();
        
        // Save file
        File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(SaveSlots, Formatting.Indented));
    }
    
    void LoadSaveData()
    {
        if (File.Exists(jsonFilePath))
        {
            SaveSlots = JsonConvert.DeserializeObject<SaveSlot[]>(File.ReadAllText(jsonFilePath));
        }
        else
        {
            // Create default save slots
            SaveSlots = new SaveSlot[3];
            for (int i = 0; i < SaveSlots.Length; i++)
            {
                SaveSlots[i] = new SaveSlot
                {
                    SaveDetails = new SaveDetails
                    {
                        PlayerName = "",
                        HorseName = "",
                        HeartCount = 3,
                        SaveTime = "",
                        TotalPlayTime = ""
                    },
                    SaveData = new SaveData
                    {
                        CurrentStage = ""
                    }
                };
            }
            // Save the default save slots to a new file
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(SaveSlots, Formatting.Indented));
        }
    }

    [System.Serializable]
    public class SaveDetails
    {
        public string PlayerName;
        public string HorseName;
        public int HeartCount;
        public string SaveTime;
        public string TotalPlayTime;
    }

    // Datenstruktur für das Laden der JSON-Daten
    [System.Serializable]
    public class SaveData
    {
        public string CurrentStage;
    }

    [System.Serializable]
    public class SaveSlot
    {
        public SaveDetails SaveDetails;
        public SaveData SaveData;
    }

    [System.Serializable]
    public class SaveFileReferences
    {
        public Transform Parent;
        public Transform Save;
        public Transform NoSave;
        public Transform ParticleFire;

        [Space] public Transform Name;
        public Transform SaveTime;
        public Transform TotalPlayTime;

        [Space] public Transform Hearts;
    }
}