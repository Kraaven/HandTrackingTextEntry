using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using PDollarGestureRecognizer;
using Newtonsoft.Json;

public class PalmInputHandler : MonoBehaviour
{
    public static PalmInputHandler Instance { get; private set; }

    [Header("Sampling")]
    [SerializeField] private float sampleThreshold = 0.01f;

    [Header("Line Rendering")]
    [SerializeField] private LineRenderer projectedLine;
    [SerializeField] private LineRenderer rawLine;
    [SerializeField] private float lineWidth = 0.005f;

    [Header("Gesture Recognition")]
    [SerializeField] private bool recogniseGesture = true;
    [SerializeField] private bool logGestureDebug = true;


    [Header("Debug")]
    [SerializeField] private bool showDebugPlane = true;

    private readonly List<Vector3> projectedSamples = new();
    private readonly List<Vector3> rawSamples = new();

    private FingerTipCollider activeFinger;
    private bool isRecording;

    private Plane drawingPlane;
    private Vector3 planeNormal;
    private Vector3 planeOrigin;

    private Vector3 lastProjectedSample;
    private Vector3 lastRawSample;

    private Gesture[] gestureSet;
    private Gesture currentGesture;
    private Point[] currentPointSet;

    private string GesturePath => Path.Combine(Application.streamingAssetsPath, "Gestures");

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupLineRenderer(projectedLine);
        SetupLineRenderer(rawLine);

#if UNITY_EDITOR
        GenerateGestureIndex();
#endif

        if (recogniseGesture)
            StartCoroutine(LoadGestures());
    }

#if UNITY_EDITOR
    private void GenerateGestureIndex()
    {
        if (!Directory.Exists(GesturePath))
            Directory.CreateDirectory(GesturePath);

        string indexPath = Path.Combine(GesturePath, "index.txt");

        var files = Directory
            .GetFiles(GesturePath, "*.json")
            .Select(Path.GetFileName)
            .ToArray();

        File.WriteAllLines(indexPath, files);

        Debug.Log($"[Gesture Index] Generated index.txt with {files.Length} entries");
    }
#endif


    #endregion

    #region Gesture Loading (ANDROID SAFE)

    private IEnumerator LoadGestures()
    {
        var gestures = new List<Gesture>();

        if (!Directory.Exists(GesturePath) && !Application.isEditor)
        {
            Debug.LogWarning($"Gesture directory not found: {GesturePath}");
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    // REAL Android device path (APK)
    string indexPath = Path.Combine(GesturePath, "index.txt");

    using UnityWebRequest indexRequest = UnityWebRequest.Get(indexPath);
    yield return indexRequest.SendWebRequest();

    if (indexRequest.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError($"Failed to load gesture index: {indexRequest.error}");
        yield break;
    }

    string[] files = indexRequest.downloadHandler.text.Split('\n');

    foreach (string file in files)
    {
        if (!file.EndsWith(".json")) continue;

        string fullPath = Path.Combine(GesturePath, file.Trim());

        using UnityWebRequest gestureRequest = UnityWebRequest.Get(fullPath);
        yield return gestureRequest.SendWebRequest();

        if (gestureRequest.result != UnityWebRequest.Result.Success)
            continue;

        Gesture g = JsonConvert.DeserializeObject<Gesture>(gestureRequest.downloadHandler.text);
        if (g != null) gestures.Add(g);
    }
#else
        // Editor + PC + Quest (filesystem-safe)
        foreach (string file in Directory.GetFiles(GesturePath, "*.json"))
        {
            Gesture g = JsonConvert.DeserializeObject<Gesture>(File.ReadAllText(file));
            if (g != null) gestures.Add(g);
        }
#endif

        gestureSet = gestures.ToArray();
        Debug.Log($"Loaded {gestureSet.Length} gestures");

        yield return new WaitForSeconds(0.5f);
        ClearData();

    }

    #endregion

    #region Trigger Handling

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out FingerTipCollider finger)) return;
        if (finger.FingerType != FingerType.Index) return;

        BeginGesture(finger);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isRecording || !other.TryGetComponent(out FingerTipCollider finger)) return;
        if (finger != activeFinger) return;

        SampleFinger();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out FingerTipCollider finger)) return;
        if (finger != activeFinger) return;

        EndGesture();
    }

    #endregion

    #region Gesture Recording

    private void BeginGesture(FingerTipCollider finger)
    {
        activeFinger = finger;
        isRecording = true;

        planeNormal = transform.forward;
        planeOrigin = transform.position;
        drawingPlane = new Plane(planeNormal, planeOrigin);

        ClearData();

        Vector3 raw = finger.transform.position;
        Vector3 projected = ProjectToPlane(raw);

        lastRawSample = raw;
        lastProjectedSample = projected;

        rawSamples.Add(raw);
        projectedSamples.Add(projected);

        AddLinePoint(rawLine, raw);
        AddLinePoint(projectedLine, projected);
    }

    private void SampleFinger()
    {
        Vector3 raw = activeFinger.transform.position;
        Vector3 projected = ProjectToPlane(raw);

        if (Vector3.Distance(projected, lastProjectedSample) < sampleThreshold)
            return;

        rawSamples.Add(raw);
        projectedSamples.Add(projected);

        lastRawSample = raw;
        lastProjectedSample = projected;

        AddLinePoint(rawLine, raw);
        AddLinePoint(projectedLine, projected);
    }

    private void EndGesture()
    {
        isRecording = false;
        activeFinger = null;

        int sampleCount = projectedSamples.Count;

        if (logGestureDebug)
        {
            Debug.Log(
                $"Gesture finished | Raw samples: {rawSamples.Count}, " +
                $"Projected samples: {sampleCount}"
            );
        }

        // Skip recognition if not enough samples
        if (sampleCount < 20)
        {
            if (logGestureDebug)
            {
                Debug.LogWarning(
                    $"Gesture skipped — insufficient samples ({sampleCount} < 30)"
                );
            }

            ClearData();
            currentGesture = null;
            currentPointSet = null;
            return;
        }

        // Convert projected 3D samples to 2D plane space
        List<Vector2> points2D = ProjectSamplesTo2D();

        if (logGestureDebug && points2D.Count > 0)
        {
            float minX = points2D.Min(p => p.x);
            float maxX = points2D.Max(p => p.x);
            float minY = points2D.Min(p => p.y);
            float maxY = points2D.Max(p => p.y);

            Debug.Log(
                $"2D Sample Range | X[{minX:F3}, {maxX:F3}] " +
                $"Y[{minY:F3}, {maxY:F3}]"
            );
        }

        // Build PDollar point set
        currentPointSet = points2D
            .Select(p => new Point(p.x, p.y, 0))
            .ToArray();

        if (recogniseGesture && gestureSet?.Length > 0)
        {
            currentGesture = new Gesture(currentPointSet);

            string result = QDollarGestureRecognizer.QPointCloudRecognizer
                .Classify(currentGesture, gestureSet);

            if (logGestureDebug)
                Debug.Log($"Recognised Gesture: {result}");

            GameManager.Instance.InsertCharacter(result.ToLower()[0]);
        }
    }


    #endregion

    #region Tap Gestures

    [SerializeField] private float indexTapCooldown = 0.5f;
    [SerializeField] private float middleTapCooldown = 0.5f;

    private float lastIndexTapTime = -Mathf.Infinity;
    private float lastMiddleTapTime = -Mathf.Infinity;

    public void OnIndexTapped()
    {
        if (Time.time - lastIndexTapTime < indexTapCooldown)
            return;

        lastIndexTapTime = Time.time;
        GameManager.Instance.DeleteCharacter();
    }

    public void OnMiddleTapped()
    {
        if (Time.time - lastMiddleTapTime < middleTapCooldown)
            return;

        lastMiddleTapTime = Time.time;
        GameManager.Instance.InsertCharacter(' ');
    }

    #endregion


    #region Math & Helpers

    private Vector3 ProjectToPlane(Vector3 worldPoint)
    {
        float distance = drawingPlane.GetDistanceToPoint(worldPoint);
        return worldPoint - planeNormal * distance;
    }

    private List<Vector2> ProjectSamplesTo2D()
    {
        Vector3 right = Vector3.Cross(Vector3.up, planeNormal);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.forward, planeNormal);

        right.Normalize();
        Vector3 up = Vector3.Cross(planeNormal, right);

        return projectedSamples
            .Select(p =>
            {
                Vector3 offset = p - planeOrigin;
                return new Vector2(
                    Vector3.Dot(offset, right),
                    Vector3.Dot(offset, up)
                );
            })
            .ToList();
    }

    private void SetupLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = false;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 0;
    }

    private void AddLinePoint(LineRenderer lr, Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        lr.positionCount++;
        lr.SetPosition(lr.positionCount - 1, local);
    }

    private void ClearData()
    {
        projectedSamples.Clear();
        rawSamples.Clear();
        projectedLine.positionCount = 0;
        rawLine.positionCount = 0;
    }

    #endregion

    #region Saving

    public void SaveGesture(string name)
    {
        if (currentPointSet == null || currentPointSet.Length == 0) return;

        Gesture g = new Gesture(currentPointSet) { Name = name };

        Directory.CreateDirectory(GesturePath);

        int i = 1;
        string path;
        do
        {
            path = Path.Combine(GesturePath, $"{name}_{i}.json");
            i++;
        } while (File.Exists(path));

        File.WriteAllText(path, JsonConvert.SerializeObject(g));

        ClearData();
        currentGesture = null;
        currentPointSet = null;

        Debug.Log($"Saved gesture: {path}");
    }

    #endregion
}
