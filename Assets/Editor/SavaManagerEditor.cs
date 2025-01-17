using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    private SaveManager saveManager;
    private List<Stage> stages;
    private SerializedDictionary<Stage, ReorderableList> flagLists;

    private void OnEnable()
    {
        saveManager = (SaveManager)target;
        InitializeStageList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Zeichne die Reorderable Lists für die Stages
        foreach (var stage in stages)
        {
            DrawStage(stage);
        }

        // Speichere die Flags zurück ins SaveManager
        SaveFlags();

        // Speichere die Änderungen
        serializedObject.ApplyModifiedProperties();

        // Markiere das SaveManager-Objekt als dirty, damit Unity die Änderungen speichert
        EditorUtility.SetDirty(saveManager);
    }

    private void InitializeStageList()
    {
        // Alle Stages aus dem Enum abrufen
        stages = new List<Stage>((Stage[])System.Enum.GetValues(typeof(Stage)));
        flagLists = new SerializedDictionary<Stage, ReorderableList>();

        foreach (var stage in stages)
        {
            if (!stage.ToString().Contains("___"))
            {
                // Initialisiere die ReorderableList für die Flags
                List<string> flags = saveManager.StageFlags.ContainsKey(stage.ToString())
                    ? saveManager.StageFlags[stage.ToString()]
                    : new List<string>();

                var flagList = new ReorderableList(flags, typeof(string), true, true, true, true)
                {
                    drawHeaderCallback = (Rect rect) =>
                    {
                        EditorGUI.LabelField(rect, stage.ToString(), EditorStyles.boldLabel);
                    },
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        if (index < flags.Count)
                        {
                            // Flag-Namen anzeigen
                            flags[index] = EditorGUI.TextField(rect, flags[index]);
                        }
                    },
                    onAddCallback = (ReorderableList list) =>
                    {
                        flags.Add(""); // Neues leeres Flag hinzufügen
                    },
                    onRemoveCallback = (ReorderableList list) =>
                    {
                        if (list.index >= 0 && list.index < flags.Count)
                        {
                            flags.RemoveAt(list.index); // Flag entfernen
                        }
                    }
                };

                flagLists[stage] = flagList;
            }
        }
    }

    private void DrawStage(Stage stage)
    {
        // Überprüfen, ob die Stage ein Trennzeichen ist
        if (stage.ToString().Contains("___"))
        {
            // Platz für Trennzeichen schaffen
            EditorGUILayout.Space(10); // Abstand hinzufügen
            return; // Keine Reorderable List für Trennstages
        }

        // Reorderable List für die Flags zeichnen
        if (flagLists.TryGetValue(stage, out var flagList))
        {
            flagList.DoLayoutList();
        }
    }

    private void SaveFlags()
    {
        foreach (var stage in stages)
        {
            if (!stage.ToString().Contains("___") && flagLists.TryGetValue(stage, out var flagList))
            {
                List<string> s = new List<string>();
                foreach (var a in flagList.list)
                {
                    s.Add(a.ToString());
                }
                // Speichere die aktualisierten Flags zurück ins Dictionary
                saveManager.StageFlags[stage.ToString()] = s;
            }
        }
    }
}
