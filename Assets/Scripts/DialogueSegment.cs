using System.Collections.Generic;
using UnityEngine;

public class Actor
{
    public int id;
    public string actorName;
    public string description;
}

[System.Serializable]
public class DialogueSegment
{
    public string segmentName;
    public List<Actor> actors = new();
    public List<DialogueSegmentData> data = new();
}

[System.Serializable]
public class DialogueSegmentData
{
    public int dialogueId;
    public List<DialogueLine> lines = new();
}

public enum CharacterLocation
{
    Left,
    Right,
    Center
}

[System.Serializable]
public class DialogueLine
{
    public int dialogueId;
    public int dialogueLevel;
    public Actor actor;
    public string tag;
    public string content;
    public CharacterLocation location;
    public string description;
}
