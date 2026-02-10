using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LetterButtonGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 6;
    [SerializeField] private float buttonSize = 0.1f;
    [SerializeField] private float spacing = 0.02f;

    [Header("Button Style")]
    [SerializeField] private Color buttonColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private int fontSize = 36;

    private void Start()
    {
        CreateLetterButtons();
    }

    private void CreateLetterButtons()
    {
        const int totalButtons = 26;
        int rows = Mathf.CeilToInt(totalButtons / (float)columns);

        for (int i = 0; i < totalButtons; i++)
        {
            char letter = (char)('A' + i);
            int row = i / columns;
            int col = i % columns;

            // Calculate position in grid
            float xPos = col * (buttonSize + spacing);
            float yPos = -row * (buttonSize + spacing);
            Vector3 position = new Vector3(xPos, yPos, 0);

            // Create the button
            CreateButton(letter.ToString(), position);
        }

        // Center the grid
        CenterGrid(rows);
    }

    private void CreateButton(string letter, Vector3 localPosition)
    {
        // Create cube GameObject
        GameObject buttonObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonObj.name = $"Button_{letter}";
        buttonObj.transform.SetParent(transform);
        buttonObj.transform.localPosition = localPosition;
        buttonObj.transform.localRotation = Quaternion.identity;
        buttonObj.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);

        // Set cube color
        Renderer renderer = buttonObj.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = buttonColor;
        renderer.material = material;
        renderer.transform.localScale = new Vector3(1, 1, 0.01f);

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(buttonObj.transform);
        canvasObj.transform.localPosition = new Vector3(0, 0, -buttonSize / 2 - 0.001f); // Slightly in front of cube
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // Set canvas size to match cube face
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(buttonSize * 100, buttonSize * 100);

        // Create Text (using TextMeshPro)
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = new Vector3(0,0,-5.5f);
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = letter;
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Optional: Add BoxCollider for interaction
        BoxCollider collider = buttonObj.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        collider.isTrigger = true;

        // Optional: Add a component to handle button clicks
        ButtonInteraction interaction = buttonObj.AddComponent<ButtonInteraction>();
        interaction.letter = letter;
    }

    private void CenterGrid(int rows)
    {
        // Calculate the center offset
        float gridWidth = (columns - 1) * (buttonSize + spacing);
        float gridHeight = (rows - 1) * (buttonSize + spacing);

        // Offset all children to center the grid
        Vector3 offset = new Vector3(-gridWidth / 2, gridHeight / 2, 0);

        foreach (Transform child in transform)
        {
            child.localPosition += offset;
        }
    }
}

public class ButtonInteraction : MonoBehaviour
{
    public string letter;
    public float cooldownDuration = 2.0f; // Adjusted from hardcoded to a variable for flexibility

    private float lastPressTime;
    private Color originalColor;
    private Renderer buttonRenderer;

    private void Awake()
    {
        buttonRenderer = GetComponent<Renderer>();
        originalColor = buttonRenderer.material.color;

        // Initialize with a negative value so the button works immediately on start
        lastPressTime = -cooldownDuration;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Calculate the difference between now and the last press
        float timeSinceLastPress = Time.time - lastPressTime;

        if (timeSinceLastPress >= cooldownDuration)
        {
            lastPressTime = Time.time; // Update the timestamp
            ExecuteButtonAction();
        }
        else
        {
            Debug.Log($"Button {letter} is on cooldown. Wait {cooldownDuration - timeSinceLastPress:F1}s.");
        }
    }

    private void ExecuteButtonAction()
    {
        Debug.Log($"Pressed Button {letter} successfully!");
        // Add your visual feedback or sound effects here
    }
}