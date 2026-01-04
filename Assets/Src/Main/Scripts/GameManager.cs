using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Session Config")]
    public EntryType entryType;
    public int numberOfTrials = 3;
    public List<string> phrases;

    [Header("References")]
    public TextPanelHandler textHandler;

    private TextEntrySession currentSession;
    private PhraseTrial currentTrial;
    private int currentTrialIndex;

    private void Awake()
    {
        if (Instance == null){Instance = this; DontDestroyOnLoad(gameObject);}
        else if (Instance != this)Destroy(gameObject);
        
    }


    private void Start()
    {
        StartCoroutine(StreamingAssetsLoader.LoadTextFile("phrases.txt", (str) => {
            string[] totalPhrases = str.Split('\n');

            phrases = new List<string>();

            for (int i = 0; i < numberOfTrials; i++)
            {
                phrases.Add(totalPhrases[UnityEngine.Random.Range(0, totalPhrases.Length)]);
            }

            Debug.Log("Phrases Loaded");
        },
        (err) => { Debug.Log(err); }));
    }
    // ================================
    // SESSION
    // ================================

    public void StartSession()
    {
        currentSession = new TextEntrySession
        {
            participantId = Guid.NewGuid().ToString(),
            entryType = entryType.ToString(),
            sessionStartTime = Time.time,
            trials = new List<PhraseTrial>()
        };

        currentTrialIndex = 0;
        StartNextTrial();
    }

    private void StartNextTrial()
    {
        if (currentTrialIndex >= numberOfTrials)
        {
            EndSession();
            return;
        }

        string phrase = phrases[currentTrialIndex % phrases.Count];

        currentTrial = new PhraseTrial
        {
            phraseIndex = currentTrialIndex,
            targetPhrase = phrase,
            finalText = "",
            startTime = Time.time,
            events = new List<InputEvent>()
        };

        currentSession.trials.Add(currentTrial);

        textHandler.StartNewPhrase(
            phrase,
            currentTrialIndex + 1,
            entryType.ToString()
        );

        currentTrialIndex++;
    }

    private void EndTrial()
    {
        currentTrial.endTime = Time.time;
        currentTrial.finalText = textHandler.GetTypedText();

        StartNextTrial();
    }

    private void EndSession()
    {
        currentSession.sessionEndTime = Time.time;
        Debug.Log("Testing done");

        string json = JsonConvert.SerializeObject(currentSession, Formatting.Indented);
        File.WriteAllText(Path.Combine(Application.dataPath,"session.json"), json);
    }

    // ================================
    // INPUT
    // ================================

    public void InsertCharacter(char character)
    {
        int cursorBefore = textHandler.GetCursorPosition();

        textHandler.InsertCharacter(character.ToString());

        int cursorAfter = textHandler.GetCursorPosition();

        currentTrial.events.Add(new InputEvent
        {
            time = Time.time,
            eventType = InputEventType.InsertCharacter,
            character = character,
            cursorBefore = cursorBefore,
            cursorAfter = cursorAfter
        });

        CheckPhraseCompletion();
    }

    public void DeleteCharacter()
    {
        if (textHandler.GetCursorPosition() == 0) return;

        int cursorBefore = textHandler.GetCursorPosition();

        textHandler.RemoveCharacter();

        int cursorAfter = textHandler.GetCursorPosition();

        currentTrial.events.Add(new InputEvent
        {
            time = Time.time,
            eventType = InputEventType.DeleteCharacter,
            character = '\0',
            cursorBefore = cursorBefore,
            cursorAfter = cursorAfter
        });
    }

    private void CheckPhraseCompletion()
    {
        if (textHandler.GetTypedText().Length ==
            currentTrial.targetPhrase.Length)
        {
            EndTrial();
        }
    }
}
