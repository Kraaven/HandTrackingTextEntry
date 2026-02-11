using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PDollarGestureRecognizer;
using Newtonsoft.Json;

public class PalmInputHandler : MonoBehaviour
{
    [Header("Sampling")]
    [SerializeField] private float threshold = 0.01f;

    [Header("Drawing Style References")]
    [SerializeField] private LineRenderer projectedLineRenderer;  // For projected points
    [SerializeField] private LineRenderer rawLineRenderer;        // For raw points
    [SerializeField] private float lineWidth = 0.005f;
    [SerializeField] private Material projectedLineMaterial;      // Green material
    [SerializeField] private Material rawLineMaterial;            // Red material

    [Header("Gesture Save Settings")]
    [SerializeField] private string gestureSavePath = "Assets/Gestures/";

    [Header("Debug")]
    [SerializeField] private bool showDebugPlane = true;

    private bool recordInput;
    private FingerTipCollider activeFinger;
    private Plane drawingPlane;

    // Store both raw and projected world space positions
    private readonly List<Vector3> samplesWorld3D = new();       // Projected points
    private readonly List<Vector3> rawSamplesWorld3D = new();    // Raw points
    private Vector3 lastSampleWorld;
    private Vector3 lastRawSampleWorld;

    // Store the plane transform at gesture start
    private Vector3 planeNormal;
    private Vector3 planePosition;

    private Gesture CurrentGesture;
    private Point[] CurrentPointSet;

    void Awake()
    {
        projectedLineRenderer.useWorldSpace = false;
        projectedLineRenderer.startWidth = lineWidth;
        projectedLineRenderer.endWidth = lineWidth;

        rawLineRenderer.useWorldSpace = false;
        rawLineRenderer.positionCount = 0;
        rawLineRenderer.startWidth = lineWidth;
        rawLineRenderer.endWidth = lineWidth;

        // Ensure gesture save directory exists
        if (!Directory.Exists(gestureSavePath))
        {
            Directory.CreateDirectory(gestureSavePath);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collision Detected: {other.gameObject.name}");

        if (!other.gameObject.TryGetComponent(out FingerTipCollider fingertip))
            return;

        if (fingertip.FingerType != FingerType.Index)
            return;

        // Capture the plane orientation at the START of the gesture
        planeNormal = transform.forward;
        planePosition = transform.position;
        drawingPlane = new Plane(planeNormal, planePosition);

        activeFinger = fingertip;
        recordInput = true;

        // Clear previous data
        samplesWorld3D.Clear();
        rawSamplesWorld3D.Clear();
        projectedLineRenderer.positionCount = 0;
        rawLineRenderer.positionCount = 0;

        // Get first points
        Vector3 rawWorldPos = activeFinger.transform.position;
        Vector3 projectedWorldPos = GetProjectedWorldPoint(rawWorldPos);

        lastRawSampleWorld = rawWorldPos;
        lastSampleWorld = projectedWorldPos;

        rawSamplesWorld3D.Add(rawWorldPos);
        samplesWorld3D.Add(projectedWorldPos);

        AddRawLinePoint(rawWorldPos);
        AddProjectedLinePoint(projectedWorldPos);

        Debug.Log($"Started recording - Raw: {rawWorldPos}, Projected: {projectedWorldPos}");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!recordInput || activeFinger == null)
            return;

        if (!other.gameObject.TryGetComponent(out FingerTipCollider fingertip))
            return;

        if (fingertip != activeFinger)
            return;

        SampleFingerPosition();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out FingerTipCollider fingertip))
            return;

        if (fingertip != activeFinger)
            return;

        recordInput = false;
        activeFinger = null;

        Debug.Log($"Gesture finished - Raw samples: {rawSamplesWorld3D.Count}, Projected samples: {samplesWorld3D.Count}");

        if (samplesWorld3D.Count > 1)
        {
            // Convert world samples to 2D on the plane for PDollar
            List<Vector2> samples2D = ConvertWorldSamplesToPlane2D();

            Debug.Log($"2D Samples range: X[{samples2D.Min(p => p.x):F3}, {samples2D.Max(p => p.x):F3}] Y[{samples2D.Min(p => p.y):F3}, {samples2D.Max(p => p.y):F3}]");

            // Convert all the Vector2 Points into PDollar Points
            CurrentPointSet = new Point[samples2D.Count];
            for (int i = 0; i < samples2D.Count; i++)
            {
                CurrentPointSet[i] = new Point(samples2D[i].x, samples2D[i].y, 0);
            }

            Debug.Log($"Converted {CurrentPointSet.Length} points for PDollar recognition");
        }
    }

    private void SampleFingerPosition()
    {
        if (!recordInput || activeFinger == null)
            return;

        Vector3 rawWorldPos = activeFinger.transform.position;
        Vector3 projectedWorldPos = GetProjectedWorldPoint(rawWorldPos);

        float rawDistance = Vector3.Distance(rawWorldPos, lastRawSampleWorld);
        float projectedDistance = Vector3.Distance(projectedWorldPos, lastSampleWorld);

        // Use projected distance for threshold (you could use raw distance instead if preferred)
        if (projectedDistance >= threshold)
        {
            rawSamplesWorld3D.Add(rawWorldPos);
            samplesWorld3D.Add(projectedWorldPos);

            lastRawSampleWorld = rawWorldPos;
            lastSampleWorld = projectedWorldPos;

            AddRawLinePoint(rawWorldPos);
            AddProjectedLinePoint(projectedWorldPos);

            if (showDebugPlane)
            {
                Debug.Log($"Sample #{samplesWorld3D.Count}: Raw={rawWorldPos}, Projected={projectedWorldPos}, distance={projectedDistance:F4}");
            }
        }
    }

    private Vector3 GetProjectedWorldPoint(Vector3 worldPoint)
    {
        // Project onto the FIXED plane (captured at gesture start)
        float distance = drawingPlane.GetDistanceToPoint(worldPoint);
        Vector3 projectedWorld = worldPoint - planeNormal * distance;

        return projectedWorld;
    }

    private void AddRawLinePoint(Vector3 worldPoint)
    {
        // Convert world point to local space relative to this transform
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        int index = rawLineRenderer.positionCount;
        rawLineRenderer.positionCount = index + 1;
        rawLineRenderer.SetPosition(index, localPoint);
    }

    private void AddProjectedLinePoint(Vector3 worldPoint)
    {
        // Convert world point to local space relative to this transform
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        int index = projectedLineRenderer.positionCount;
        projectedLineRenderer.positionCount = index + 1;
        projectedLineRenderer.SetPosition(index, localPoint);
    }

    private List<Vector2> ConvertWorldSamplesToPlane2D()
    {
        // Create a coordinate system on the plane
        Vector3 planeRight = Vector3.Cross(Vector3.up, planeNormal).normalized;
        if (planeRight.magnitude < 0.1f) // Handle case where normal is up/down
            planeRight = Vector3.Cross(Vector3.forward, planeNormal).normalized;

        Vector3 planeUp = Vector3.Cross(planeNormal, planeRight).normalized;

        List<Vector2> samples2D = new List<Vector2>();

        foreach (Vector3 worldSample in samplesWorld3D)
        {
            // Get vector from plane origin to sample
            Vector3 offset = worldSample - planePosition;

            // Project onto plane's 2D coordinate system
            float x = Vector3.Dot(offset, planeRight);
            float y = Vector3.Dot(offset, planeUp);

            samples2D.Add(new Vector2(x, y));
        }

        return samples2D;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPlane)
            return;

        // Draw the current palm orientation
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 normal = transform.forward;

        Vector3 right = transform.right * 0.05f;
        Vector3 up = transform.up * 0.05f;

        Vector3 p1 = center + right + up;
        Vector3 p2 = center - right + up;
        Vector3 p3 = center - right - up;
        Vector3 p4 = center + right - up;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        // Draw the FIXED plane (if recording)
        if (Application.isPlaying && recordInput)
        {
            Gizmos.color = Color.green;

            Vector3 fixedRight = Vector3.Cross(Vector3.up, planeNormal).normalized * 0.05f;
            if (fixedRight.magnitude < 0.01f)
                fixedRight = Vector3.Cross(Vector3.forward, planeNormal).normalized * 0.05f;
            Vector3 fixedUp = Vector3.Cross(planeNormal, fixedRight).normalized * 0.05f;

            p1 = planePosition + fixedRight + fixedUp;
            p2 = planePosition - fixedRight + fixedUp;
            p3 = planePosition - fixedRight - fixedUp;
            p4 = planePosition + fixedRight - fixedUp;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);

            // Draw normal
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(planePosition, planeNormal * 0.05f);
        }

        // Draw collected projected samples (green)
        if (Application.isPlaying && samplesWorld3D.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < samplesWorld3D.Count - 1; i++)
            {
                Gizmos.DrawLine(samplesWorld3D[i], samplesWorld3D[i + 1]);
            }

            // Draw sample points
            Gizmos.color = Color.cyan;
            foreach (Vector3 sample in samplesWorld3D)
            {
                Gizmos.DrawSphere(sample, 0.002f);
            }
        }

        // Draw collected raw samples (red)
        if (Application.isPlaying && rawSamplesWorld3D.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < rawSamplesWorld3D.Count - 1; i++)
            {
                Gizmos.DrawLine(rawSamplesWorld3D[i], rawSamplesWorld3D[i + 1]);
            }

            // Draw sample points
            Gizmos.color = Color.magenta;
            foreach (Vector3 sample in rawSamplesWorld3D)
            {
                Gizmos.DrawSphere(sample, 0.002f);
            }
        }
    }

    public void OnIndexTapped()
    {
        // Leave this code be for now
    }

    public void OnMiddletapped()
    {
        // Leave this code for now
    }

    public void SaveGesture(string letterName)
    {
        // Check if we have points to save
        if (CurrentPointSet == null || CurrentPointSet.Length == 0)
        {
            Debug.LogWarning("No gesture points to save!");
            return;
        }

        // Create gesture with the accumulated points
        CurrentGesture = new Gesture(CurrentPointSet);

        // Find the next available file number
        int fileNumber = 1;
        string fileName;
        string fullPath;

        do
        {
            fileName = $"{letterName.ToLower()}_{fileNumber}.json";
            fullPath = Path.Combine(gestureSavePath, fileName);
            fileNumber++;
        } while (File.Exists(fullPath));

        // Serialize and save the gesture
        string jsonData = JsonConvert.SerializeObject(CurrentGesture);
        File.WriteAllText(fullPath, jsonData);

        Debug.Log($"Gesture saved to: {fullPath}");

        // Clear all data and create new gesture reference
        CurrentGesture = null;
        CurrentPointSet = null;
        samplesWorld3D.Clear();
        rawSamplesWorld3D.Clear();

        // Clear line renderers
        projectedLineRenderer.positionCount = 0;
        rawLineRenderer.positionCount = 0;

        Debug.Log($"Gesture data cleared. Ready for new gesture.");
    }
}