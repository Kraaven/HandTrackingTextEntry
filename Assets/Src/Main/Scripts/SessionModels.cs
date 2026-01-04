using System.Collections.Generic;

[System.Serializable]
public class InputEvent
{
    public float time;
    public InputEventType eventType;
    public char character;
    public int cursorBefore;
    public int cursorAfter;
}

public enum InputEventType
{
    InsertCharacter,
    DeleteCharacter
}

[System.Serializable]
public class PhraseTrial
{
    public int phraseIndex;
    public string targetPhrase;
    public string finalText;
    public float startTime;
    public float endTime;
    public List<InputEvent> events = new List<InputEvent>();
}

[System.Serializable]
public class TextEntrySession
{
    public string participantId;
    public string entryType;
    public float sessionStartTime;
    public float sessionEndTime;
    public List<PhraseTrial> trials = new List<PhraseTrial>();
}

public enum EntryType{ 
    Standard,
    PinchT9
}




