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

    public float MoveSpeed = 1.0f; // Geschwindigkeit der Kamerabewegung
    public float RotationMultiplier = 5.0f; // Geschwindigkeit der Kamerabewegung

    public int currentIndex = 0; // Index des aktuellen Kamerapunkts
    public CameraState State;

    [HideInInspector] public Camera Cam;

    // Start is called before the first frame update
    void Start()
    {
        if (Cam == null) Cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Methode, um die Kamerabewegung zu starten
    public void StartCameraTrack()
    {
        if (CameraPoints.Count > 0)
        {
            currentIndex = 0; // Setze den Index auf den ersten Kamerapunkt
            MoveCameraToNextPoint(); // Bewege die Kamera zum ersten Punkt
        }
    }

    // Methode, um die Kamera zum nächsten Punkt zu bewegen
    private void MoveCameraToNextPoint()
    {
        if (currentIndex < CameraPoints.Count)
        {
            CameraPoint nextPoint = CameraPoints[currentIndex];
            Vector3 targetPosition = nextPoint.Location;
            Quaternion targetRotation = Quaternion.Euler(nextPoint.RotationEuler);

            // Bewege die Szeneansichtskamera zum nächsten Punkt
            //SceneView.lastActiveSceneView.LookAtDirect(targetPosition, targetRotation);
            Cam.transform.position = targetPosition;
            Cam.transform.rotation = targetRotation;

            // Starte die Coroutine, um zur nächsten Position zu wechseln
            Debug.LogWarning("START ");
            //StartCoroutine(MoveCameraCoroutine(targetPosition, targetRotation));
            //EditorCoroutineUtility.StartCoroutine(MoveCameraCoroutine(targetPosition, targetRotation), this);
            //EditorCoroutineUtility.StartCoroutine(CountEditorUpdates(targetPosition, targetRotation), this);

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
                        RotationMultiplier); // Wir multiplizieren mit 10f, um eine schnellere Rotation zu ermöglichen

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

    // Coroutine, um die Szeneansichtskamera allmählich zum Ziel zu bewegen
    private IEnumerator MoveCameraCoroutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        Debug.LogError("ER: " + Cam.transform.position + " :: " + targetPosition);
        while (Vector3.Distance(Cam.transform.position, targetPosition) > 0.01f || Quaternion.Angle(Cam.transform.rotation, targetRotation) > 0.01f)
        {
            Cam.transform.position = Vector3.MoveTowards(Cam.transform.position, targetPosition, MoveSpeed * Time.deltaTime);
            Cam.transform.rotation = Quaternion.RotateTowards(Cam.transform.rotation, targetRotation, MoveSpeed * Time.deltaTime * RotationMultiplier); // Wir multiplizieren mit 10f, um eine schnellere Rotation zu ermöglichen

            yield return new WaitForFixedUpdate();
        }

        // Wenn die Kamera ihr Ziel erreicht hat, bewege sie zum nächsten Punkt
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

    // Optionen für das Dropdown-Menü
    private string[] pointModeOptions = new string[] { "SNAP", "ANIMATE" };

    void OnEnable()
    {
        CameraSequence = (CameraSequence)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // Zeichne jeden CameraPoint in der Liste
        for (int i = 0; i < CameraSequence.CameraPoints.Count; i++)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("" + CameraSequence.CameraPoints[i].Index, GUILayout.Width(20));

            // Zeige das Location-Feld als Vector3-Feld an
            CameraSequence.CameraPoints[i].Location = EditorGUILayout.Vector3Field("Position: ", CameraSequence.CameraPoints[i].Location);

            // Wenn das Viereck-Icon angeklickt wird, setze die Location des CameraPoints auf die Position der Kamera im Editor
            if (GUILayout.Button("\u25A0", GUILayout.Width(20))) // \u25A0 ist das Unicode-Zeichen für ein Viereck
            {
                SceneView.lastActiveSceneView.LookAtDirect(CameraSequence.CameraPoints[i].Location, Quaternion.Euler(CameraSequence.CameraPoints[i].RotationEuler));
            }

            // Wenn der "X"-Button geklickt wird, entferne den CameraPoint aus der Liste
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                CameraSequence.CameraPoints.RemoveAt(i);
                i--; // Reduziere den Index, um den korrekten nächsten Index zu lesen
            }
            EditorGUILayout.EndHorizontal();

            // Zeige das Rotation-Feld als Vector3-Feld an
            CameraSequence.CameraPoints[i].RotationEuler = EditorGUILayout.Vector3Field("Rotation: ", CameraSequence.CameraPoints[i].RotationEuler);

            // Dropdown-Menü für den PointMode
            CameraSequence.CameraPoints[i].PointMode = (PointMode)EditorGUILayout.Popup("Point Mode", (int)CameraSequence.CameraPoints[i].PointMode, pointModeOptions);
            if (CameraSequence.CameraPoints[i].PointMode == PointMode.SNAP)
            {
                
                CameraSequence.CameraPoints[i].SnapDelay = EditorGUILayout.FloatField("Delay", CameraSequence.CameraPoints[i].SnapDelay);

            }

            EditorGUILayout.EndVertical();
        }

        // Erzeuge einen neuen CameraPoint mit der aktuellen Kameraposition im Editor und erhöhe den Index um 1
        if (GUILayout.Button("New"))
        {
            CameraPoint newPoint = new CameraPoint();
            newPoint.Location = SceneView.lastActiveSceneView.camera.transform.position;
            newPoint.RotationEuler = SceneView.lastActiveSceneView.camera.transform.eulerAngles;
            newPoint.Index = CameraSequence.CameraPoints.Count + 1;
            CameraSequence.CameraPoints.Add(newPoint);
        }

        // Button, um die Kamerabewegung zu starten
        if (GUILayout.Button("Start camera track"))
        {
            CameraSequence.StartCameraTrack();
        }
    }
}
#endif
