using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TextPanelHandler : MonoBehaviour
{
    private UIDocument document;
    private VisualElement root;

    private Label PhraseNumberText;
    private Label InputTypeText;
    private Label TargetPhraseText;
    private Label TypedPhraseText;

    private StringBuilder userTypingText;
    private Stack<string> formattedStringStack;
    private string targetPhrase;

    public string SessionInstructions;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;

        PhraseNumberText = root.Q<Label>(className: "phase-number");
        InputTypeText = root.Q<Label>(className: "session-input-type");
        TargetPhraseText = root.Q<Label>(className: "phrase-text");
        TypedPhraseText = root.Q<Label>(className: "answer-text");

        userTypingText = new StringBuilder();
        formattedStringStack = new Stack<string>();

        PhraseNumberText.text = "Phrase : 0";
        InputTypeText.text = "Undecided";
        TargetPhraseText.text = SessionInstructions;
    }

    // ================================
    // PHRASE CONTROL
    // ================================

    public void StartNewPhrase(string phrase, int phraseNumber, string inputType)
    {
        targetPhrase = phrase;
        userTypingText.Clear();
        formattedStringStack.Clear();

        PhraseNumberText.text = $"Phrase: {phraseNumber}";
        InputTypeText.text = inputType;
        TargetPhraseText.text = phrase;
        TypedPhraseText.text = "";
    }

    // ================================
    // TEXT INPUT
    // ================================

    public void InsertCharacter(string character)
    {
        userTypingText.Append(character);
        InsertCharacterInStack(character);
        UpdateTypedText();
    }

    public void RemoveCharacter()
    {
        if (userTypingText.Length == 0) return;

        userTypingText.Length--;
        formattedStringStack.Pop();
        UpdateTypedText();
    }

    private void InsertCharacterInStack(string character)
    {
        int index = userTypingText.Length - 1;

        if (index >= targetPhrase.Length ||
            targetPhrase[index] != character[0])
        {
            formattedStringStack.Push(
                $"<color=#E06C75>{character}</color>");
        }
        else
        {
            formattedStringStack.Push(character);
        }
    }

    private void UpdateTypedText()
    {
        var array = formattedStringStack.ToArray();
        System.Array.Reverse(array);
        TypedPhraseText.text = string.Join("", array);
    }

    // ================================
    // GETTERS
    // ================================

    public string GetTypedText()
    {
        return userTypingText.ToString();
    }

    public int GetCursorPosition()
    {
        return userTypingText.Length;
    }
}
