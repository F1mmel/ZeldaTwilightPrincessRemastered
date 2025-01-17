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

        foreach (var stage in stages)
        {
            DrawStage(stage);
        }

        SaveFlags();

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(saveManager);
    }

    private void InitializeStageList()
    {
        stages = new List<Stage>((Stage[])System.Enum.GetValues(typeof(Stage)));
        flagLists = new SerializedDictionary<Stage, ReorderableList>();

        foreach (var stage in stages)
        {
            if (!stage.ToString().Contains("___"))
            {
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
                            flags[index] = EditorGUI.TextField(rect, flags[index]);
                        }
                    },
                    onAddCallback = (ReorderableList list) =>
                    {
                        flags.Add("");
                    },
                    onRemoveCallback = (ReorderableList list) =>
                    {
                        if (list.index >= 0 && list.index < flags.Count)
                        {
                            flags.RemoveAt(list.index);
                        }
                    }
                };

                flagLists[stage] = flagList;
            }
        }
    }

    private void DrawStage(Stage stage)
    {
        if (stage.ToString().Contains("___"))
        {
            EditorGUILayout.Space(10);
            return;
        }

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
                saveManager.StageFlags[stage.ToString()] = s;
            }
        }
    }
}
