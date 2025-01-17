using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

[System.Serializable]
public class CameraSequence : MonoBehaviour
{
    public List<CameraPoint> CameraPoints = new List<CameraPoint>();

    public float MoveSpeed = 1.0f;
    public float RotationMultiplier = 5.0f;

    public int currentIndex = 0;
    public CameraState State;

    [HideInInspector] public Camera Cam;

    void Start()
    {
        if (Cam == null) Cam = Camera.main;
    }

    void Update()
    {
        
    }

    public void StartCameraTrack()
    {
        if (CameraPoints.Count > 0)
        {
            currentIndex = 0;
            MoveCameraToNextPoint();
        }
    }

    private void MoveCameraToNextPoint()
    {
        if (currentIndex < CameraPoints.Count)
        {
            CameraPoint nextPoint = CameraPoints[currentIndex];
            Vector3 targetPosition = nextPoint.Location;
            Quaternion targetRotation = Quaternion.Euler(nextPoint.RotationEuler);

            Cam.transform.position = targetPosition;
            Cam.transform.rotation = targetRotation;

            Debug.LogWarning("START ");

            State = CameraState.PLAYING;

            StartCoroutine(Play());
        }
    }

    IEnumerator Play()
    {
        while (true)
        {
            if (State == CameraState.PLAYING)
            {
                CameraPoint nextPoint = CameraPoints[currentIndex];
                Vector3 targetPosition = nextPoint.Location;
                Quaternion targetRotation = Quaternion.Euler(nextPoint.RotationEuler);

                if (nextPoint.PointMode == PointMode.SNAP)
                {
                    yield return new WaitForSeconds(nextPoint.SnapDelay);
                
                    Cam.transform.position = targetPosition;
                    Cam.transform.rotation = targetRotation;
                
                    currentIndex++;
                    if (CameraPoints.Count <= currentIndex) State = CameraState.STOPPED;
                } else if (nextPoint.PointMode == PointMode.ANIMATE)
                {

                    Cam.transform.position =
                        Vector3.MoveTowards(Cam.transform.position, targetPosition, MoveSpeed * Time.deltaTime);
                    Cam.transform.rotation = Quaternion.RotateTowards(Cam.transform.rotation, targetRotation,
                        MoveSpeed * Time.deltaTime *
                        RotationMultiplier);

                    if (Vector3.Distance(Cam.transform.position, targetPosition) < 0.01f &&
                        Quaternion.Angle(Cam.transform.rotation, targetRotation) < 0.01f)
                    {
                        currentIndex++;
                        if (CameraPoints.Count <= currentIndex) State = CameraState.STOPPED;
                    }
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    private void FixedUpdate()
    {

    }

    private IEnumerator MoveCameraCoroutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        Debug.LogError("ER: " + Cam.transform.position + " :: " + targetPosition);
        while (Vector3.Distance(Cam.transform.position, targetPosition) > 0.01f || Quaternion.Angle(Cam.transform.rotation, targetRotation) > 0.01f)
        {
            Cam.transform.position = Vector3.MoveTowards(Cam.transform.position, targetPosition, MoveSpeed * Time.deltaTime);
            Cam.transform.rotation = Quaternion.RotateTowards(Cam.transform.rotation, targetRotation, MoveSpeed * Time.deltaTime * RotationMultiplier);

            yield return new WaitForFixedUpdate();
        }

        currentIndex++;
        MoveCameraToNextPoint();
    }
}

[System.Serializable]
public class CameraPoint
{
    public Vector3 Location;
    public int Index;
    public Vector3 RotationEuler;
    public PointMode PointMode;
    public float SnapDelay = 1;
}

public enum CameraState
{
    STOPPED,
    PLAYING,
    PAUSED
}

public enum PointMode
{
    SNAP,
    ANIMATE
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraSequence))]
public class CameraSequence_Inspector : Editor
{
    public CameraSequence CameraSequence;

    private string[] pointModeOptions = new string[] { "SNAP", "ANIMATE" };

    void OnEnable()
    {
        CameraSequence = (CameraSequence)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        for (int i = 0; i < CameraSequence.CameraPoints.Count; i++)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + CameraSequence.CameraPoints[i].Index, GUILayout.Width(20));

            CameraSequence.CameraPoints[i].Location = EditorGUILayout.Vector3Field("Position: ", CameraSequence.CameraPoints[i].Location);

            if (GUILayout.Button("\u25A0", GUILayout.Width(20)))
            {
                SceneView.lastActiveSceneView.LookAtDirect(CameraSequence.CameraPoints[i].Location, Quaternion.Euler(CameraSequence.CameraPoints[i].RotationEuler));
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                CameraSequence.CameraPoints.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();

            CameraSequence.CameraPoints[i].RotationEuler = EditorGUILayout.Vector3Field("Rotation: ", CameraSequence.CameraPoints[i].RotationEuler);

            CameraSequence.CameraPoints[i].PointMode = (PointMode)EditorGUILayout.Popup("Point Mode", (int)CameraSequence.CameraPoints[i].PointMode, pointModeOptions);
            if (CameraSequence.CameraPoints[i].PointMode == PointMode.SNAP)
            {
                
                CameraSequence.CameraPoints[i].SnapDelay = EditorGUILayout.FloatField("Delay", CameraSequence.CameraPoints[i].SnapDelay);

            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("New"))
        {
            CameraPoint newPoint = new CameraPoint();
            newPoint.Location = SceneView.lastActiveSceneView.camera.transform.position;
            newPoint.RotationEuler = SceneView.lastActiveSceneView.camera.transform.eulerAngles;
            newPoint.Index = CameraSequence.CameraPoints.Count + 1;
            CameraSequence.CameraPoints.Add(newPoint);
        }

        if (GUILayout.Button("Start camera track"))
        {
            CameraSequence.StartCameraTrack();
        }
    }
}
#endif
