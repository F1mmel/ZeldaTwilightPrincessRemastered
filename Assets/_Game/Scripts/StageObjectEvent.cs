using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltEvents;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class StageObjectEvent : MonoBehaviour
{
    [HideInInspector] public List<StageEvent> StageEvents = new List<StageEvent>();

    public bool ToggleAll;
    public UltEvent Event;

    // Start is called before the first frame update
    void Start()
    {

        
    }

    public void Execute()
    {
        //Event.Invoke();
        
        /*foreach (string o in Objects)
        {
            StageLoader.GetModelByName(o).Keys.First().transform.eulerAngles = new Vector3(0, 70, 0);
            
            Dictionary<GameObject, BMD> bmds = StageLoader.GetModelByName(o);

            foreach (var item in bmds)
            {
                //bmds
                item.Key.transform.eulerAngles = new Vector3(0, 70, 0);
            }
        }*/

        foreach (StageEvent stageEvent in StageEvents)
        {
            foreach (string g in stageEvent.GameObjects)
            {
                GameObject obj = StageLoader.GetModelByName(g).Keys.First();

                if (stageEvent.EventType == StageEventType.MoveToOrigin)
                {
                    /*foreach (GameObject t in obj.GetAllChildren())
                    {
                        if (t.name.Equals(stageEvent.OriginName))
                        {
                            obj.transform.GetChild(0).parent = t.transform;
                            //obj.transform.localPosition = Vector3.zero;
                        }
                    }*/
                }

                if (stageEvent.EventType == StageEventType.RotateSmooth) StartCoroutine(_RotateSmooth(obj, Vector3.up, stageEvent.TargetRotation.y));
            }
        }
    }

    private IEnumerator _RotateSmooth(GameObject obj, Vector3 axis, float angle)
    {
        Quaternion startRotation = obj.transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.AngleAxis(angle, axis);

        float elapsedTime = 0.0f;
        float duration = Mathf.Abs(angle) / 15f;
        float currentSpeed = 0;

        while (elapsedTime < duration)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 3, .5f * Time.deltaTime);
            obj.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, currentSpeed);
            elapsedTime += Time.deltaTime * currentSpeed;
            yield return null;
        }

        obj.transform.rotation = targetRotation; // Ensure we reach the exact target rotation
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


[Serializable]
public class StageEvent
{
    [HideInInspector] public string Name;
    [HideInInspector] public StageEventType EventType;
    [HideInInspector] public bool Show;

    public string[] GameObjects;
    public Vector3 TargetRotation;
    
    public string OriginName;
}

public enum StageEventType
{
    MoveToOrigin,
    RotateSmooth,
}


#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(StageObjectEvent))]
public class StageObjectEvent_Inspector : Editor
{
    public StageObjectEvent StageObjectEvent;

    private ReorderableList reorderableList;

    void OnEnable()
    {
        StageObjectEvent = (StageObjectEvent) target;
        
        reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("StageEvents"), true, true, true, true);
        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "StageEvents");
        };
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            StageEvent process = StageObjectEvent.StageEvents[index];
            
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            int typeOffset = 25;
            if (!process.Name.Equals("")) typeOffset = 200;
            
            EditorGUI.PropertyField(new Rect(rect.x + typeOffset, rect.y + EditorGUIUtility.singleLineHeight * 0 +
                                                                  EditorGUIUtility.standardVerticalSpacing * 1, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("EventType"), GUIContent.none);

            process.Show = EditorGUI.Foldout(new Rect(rect.x + 15, rect.y, rect.width, EditorGUIUtility.singleLineHeight), process.Show, process.Name);
            if (process.Show)
            {
                /*EditorGUI.PropertyField(
                    new Rect(rect.x,
                        rect.y + EditorGUIUtility.singleLineHeight * 1 +
                        EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                        EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Name"));*/
                
                process.EventType = (StageEventType)element.FindPropertyRelative("EventType").enumValueIndex;
                switch (process.EventType)
                {
                    case StageEventType.MoveToOrigin:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("OriginName"));
                        
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 2 +
                                EditorGUIUtility.standardVerticalSpacing * 3, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("GameObjects"));
                        break;
                    case StageEventType.RotateSmooth:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("TargetRotation"));
                        
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 2 +
                                EditorGUIUtility.standardVerticalSpacing * 3, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("GameObjects"));
                        break;
                }
            }
        };
        reorderableList.elementHeightCallback = (int index) =>
        {
            StageEvent process = StageObjectEvent.StageEvents[index];
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            StageEventType processType = (StageEventType)element.FindPropertyRelative("EventType").enumValueIndex;

            if (!process.Show)
            {
                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 1;
            }
            
            SerializedProperty unityEventProperty = element.FindPropertyRelative("GameObjects");
            float height = EditorGUI.GetPropertyHeight(unityEventProperty);
            switch (processType)
            {
                case StageEventType.MoveToOrigin:
                    return height + EditorGUIUtility.standardVerticalSpacing * 20;
                case StageEventType.RotateSmooth:
                    //return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
                    // Calculate height for UnityEvent field
                    //SerializedProperty unityEventProperty = element.FindPropertyRelative("GameObjects");
                    return height + EditorGUIUtility.standardVerticalSpacing * 20;
                default:
                    return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (StageObjectEvent.ToggleAll)
        {
            if(GUILayout.Button("Hide all")) {
                foreach (StageEvent process in StageObjectEvent.StageEvents)
                    process.Show = false;
                
                StageObjectEvent.ToggleAll = false;
            }

        }
        else
        {
            if (GUILayout.Button("Show all"))
            {
                foreach (StageEvent process in StageObjectEvent.StageEvents) 
                    process.Show = true;
                
                StageObjectEvent.ToggleAll = true;
            }
        } 
        
        reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
        
        GUILayout.Space(15);
        if (GUILayout.Button("Execute"))
        {
            StageObjectEvent.Execute();
        }
    }
}
#endif