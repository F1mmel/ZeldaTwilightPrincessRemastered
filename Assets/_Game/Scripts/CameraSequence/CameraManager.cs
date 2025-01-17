using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UltEvents;
using UnityEngine.Events;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

public class CameraManager : MonoBehaviour
{
    [HideInInspector] public List<CameraProcess> Processes;
    [HideInInspector] public bool ToggleAll;

    private CinemachineVirtualCamera PreviousCamera;
    private CinemachineDollyCart CurrentTrack;
    private CinemachineTransposer DollyTransposer;

    public int CurrentProcess;
    public CameraProcess CurrentTrackProcess;
    
    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(StartCameraTrack());
        //StartCoroutine(StartProcesses());

        CurrentProcess = 0;

        foreach (CinemachineVirtualCamera camera in GetComponentsInChildren<CinemachineVirtualCamera>())
        {
            camera.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartProcesses()
    {
        /*foreach (CameraProcess process in Processes)
        {
            if (CurrentTrack != null) yield return null;
            
            if(process.ProcessType == ProcessType.Delay) yield return new WaitForSeconds(process.Delay);
            else if (process.ProcessType == ProcessType.SwitchToCamera)
            {
                if(PreviousCamera != null) PreviousCamera.gameObject.SetActive(false);
                process.Camera.gameObject.SetActive(true);
                
                PreviousCamera = process.Camera;
            }
            else if (process.ProcessType == ProcessType.StartTrack)
            {
                process.DollyCart.gameObject.SetActive(true);
                process.DollyCart.m_Speed = 1;
                
                CurrentTrack = process.DollyCart;
            }
            else if (process.ProcessType == ProcessType.LookAtObject)
            {
                if (PreviousCamera != null)
                    PreviousCamera.m_LookAt = process.LookAt;
            }
        }*/

        yield break;
    }

    private float timer = 0f;

    private float dampingTimer = 0f;
    private float blendTimer = 0f;
    // Update is called once per frame
    private void FixedUpdate()
    {

        if (DollyTransposer != null)
        {
            if (dampingTimer >= 50)
            {
                DollyTransposer.m_XDamping = 0.5f;
                DollyTransposer.m_YDamping = 0.5f;
                DollyTransposer.m_ZDamping = 0.5f;
            }
            else dampingTimer += 1;
        }

        if (CurrentTrack != null)
        {
            if (blendTimer >= 300)
            {
                //CurrentTrackProcess.CameraCut.DollyCart.m_Speed = CurrentTrackProcess.TrackSpeed;
            }
            else
            {
                blendTimer += 1;
                //return;
            }
            
            if (CurrentTrack.m_Position >= CurrentTrack.m_Path.PathLength)
            {
                CurrentTrack = null;
            }
            else
            {
                if (!CurrentTrackProcess.ContinueProcesses)
                {
                    return;
                }
            }
        }

        if (Processes.Count - 1 < CurrentProcess)
        {
            return;
        }
        CameraProcess process = Processes[CurrentProcess];

        if (process.ProcessType == ProcessType.Delay)
        {
            timer += Time.deltaTime;
            if (timer >= process.Delay)
            {
                timer = 0f;
                
                CurrentProcess++;
            }
            else
            {
                return;
            }
        }
        else if (process.ProcessType == ProcessType.SwitchToCamera)
        {
            if(PreviousCamera != null) PreviousCamera.gameObject.SetActive(false);
            process.Camera.gameObject.SetActive(true);
            PreviousCamera = process.Camera;
            
            //CinemachineBrain brain = 
                //cinemachineBrain.enabled = false;
                if (process.SwitchType == CameraSwitchType.Snap)
                    Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Style =
                        CinemachineBlendDefinition.Style.Cut;
            else if (process.SwitchType == CameraSwitchType.Move) 
                Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            
            CurrentProcess++;
        }
        else if (process.ProcessType == ProcessType.StartTrack)
        {
            CameraCut cameraCut = process.CameraCut;
            
            // Enable components
            cameraCut.VirtualCameraTransform.gameObject.SetActive(true);
            cameraCut.DollyCart.gameObject.SetActive(true);

            CinemachineTransposer transposer =
                cameraCut.VirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (process.SwitchType == CameraSwitchType.Snap)
            {
                Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Style =
                    CinemachineBlendDefinition.Style.Cut;
                transposer.m_XDamping = 0f;
                transposer.m_YDamping = 0f;
                transposer.m_ZDamping = 0f;
            }
            else
            {
                Camera.main.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Style =
                    CinemachineBlendDefinition.Style.EaseInOut;
                transposer.m_XDamping = 0.5f;
                transposer.m_YDamping = 0.5f;
                transposer.m_ZDamping = 0.5f;
            }

            dampingTimer = 0;
            blendTimer = 0;
            
            cameraCut.DollyCart.m_Speed = process.TrackSpeed;

            if (process.LookAt != null)
            {
                cameraCut.VirtualCamera.LookAt = process.LookAt;
                cameraCut.VirtualCamera.AddCinemachineComponent<CinemachineHardLookAt>();
            }

            CurrentTrack = cameraCut.DollyCart;
            CurrentTrackProcess = process;
            PreviousCamera = cameraCut.VirtualCamera;
            DollyTransposer = transposer;
            
            CurrentProcess++;
        }
        else if (process.ProcessType == ProcessType.LookAtObject)
        {
            if (PreviousCamera != null)
            {
                PreviousCamera.LookAt = process.LookAt;
                PreviousCamera.AddCinemachineComponent<CinemachineHardLookAt>();
            }

            CurrentProcess++;
        }
        else if (process.ProcessType == ProcessType.Event)
        {
            process.Event.Invoke();
            
            CurrentProcess++;
        }
    }
}

[Serializable]
public class CameraProcess
{
    [HideInInspector] public ProcessType ProcessType;
    
    [HideInInspector] public CinemachineVirtualCamera Camera;
    [HideInInspector] public float TrackSpeed = 1;
    [HideInInspector] public CameraSwitchType SwitchType;
    [HideInInspector] public bool ContinueProcesses;
    [HideInInspector] public CameraCut CameraCut;
    [HideInInspector] public float Delay;
    [HideInInspector] public Transform LookAt;
    //[HideInInspector] public UnityEvent Event;
    [HideInInspector] public UltEvent Event;
    [HideInInspector] public bool Show = true;
    [HideInInspector] public string Name;
}

public enum ProcessType
{
    Delay,
    SwitchToCamera,
    StartTrack,
    LookAtObject,
    Event,
}

public enum CameraSwitchType
{
    Snap,
    Move,
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(CameraManager))]
public class CameraProcess_Inspector : Editor
{
    public CameraManager CameraManager;

    private ReorderableList reorderableList;

    void OnEnable()
    {
        CameraManager = (CameraManager) target;
        
        reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("Processes"), true, true, true, true);
        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Processes");
        };
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            CameraProcess process = CameraManager.Processes[index];
            
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            int typeOffset = 25;
            if (!process.Name.Equals("")) typeOffset = 200;
            
            EditorGUI.PropertyField(new Rect(rect.x + typeOffset, rect.y + EditorGUIUtility.singleLineHeight * 0 +
                                                                  EditorGUIUtility.standardVerticalSpacing * 1, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("ProcessType"), GUIContent.none);

            process.Show = EditorGUI.Foldout(new Rect(rect.x + 15, rect.y, rect.width, EditorGUIUtility.singleLineHeight), process.Show, process.Name);
            if (process.Show)
            {
                /*EditorGUI.PropertyField(
                    new Rect(rect.x,
                        rect.y + EditorGUIUtility.singleLineHeight * 1 +
                        EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                        EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Name"));*/
                
                process.ProcessType = (ProcessType)element.FindPropertyRelative("ProcessType").enumValueIndex;
                switch (process.ProcessType)
                {
                    case ProcessType.Delay:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Delay"));
                        break;
                    case ProcessType.SwitchToCamera:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Camera"));
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 2 +
                                EditorGUIUtility.standardVerticalSpacing * 3, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("SwitchType"));
                        break;
                    case ProcessType.StartTrack:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("CameraCut"));
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 2 +
                                EditorGUIUtility.standardVerticalSpacing * 3, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("LookAt"));
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 3 +
                                EditorGUIUtility.standardVerticalSpacing * 4, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("TrackSpeed"));
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 4 +
                                EditorGUIUtility.standardVerticalSpacing * 5, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("SwitchType"));
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 5 +
                                EditorGUIUtility.standardVerticalSpacing * 6, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("ContinueProcesses"));
                        break;
                    case ProcessType.LookAtObject:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("LookAt"));
                        break;
                    case ProcessType.Event:
                        EditorGUI.PropertyField(
                            new Rect(rect.x,
                                rect.y + EditorGUIUtility.singleLineHeight * 1 +
                                EditorGUIUtility.standardVerticalSpacing * 2, rect.width,
                                EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("Event"));
                        break;
                }
            }
        };
        reorderableList.elementHeightCallback = (int index) =>
        {
            CameraProcess process = CameraManager.Processes[index];
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            ProcessType processType = (ProcessType)element.FindPropertyRelative("ProcessType").enumValueIndex;

            if (!process.Show)
            {
                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 1;
            }
            
            switch (processType)
            {
                case ProcessType.Delay:
                    return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
                case ProcessType.SwitchToCamera:
                    return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
                case ProcessType.LookAtObject:
                    return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
                case ProcessType.StartTrack:
                    return EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing * 6;
                case ProcessType.Event:
                    // Calculate height for UnityEvent field
                    SerializedProperty unityEventProperty = element.FindPropertyRelative("Event");
                    float height = EditorGUI.GetPropertyHeight(unityEventProperty);
                    return height + EditorGUIUtility.standardVerticalSpacing * 20;
                default:
                    return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (CameraManager.ToggleAll)
        {
            if(GUILayout.Button("Hide all")) {
                foreach (CameraProcess process in CameraManager.Processes)
                    process.Show = false;
                
                CameraManager.ToggleAll = false;
            }

        }
        else
        {
            if (GUILayout.Button("Show all"))
            {
                foreach (CameraProcess process in CameraManager.Processes) 
                    process.Show = true;
                
                CameraManager.ToggleAll = true;
            }
        } 
        
        reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif