using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueActor
{
    //public Sprite dialoguePic;
    public int actorID;
    public string actorName;
    public string objName;
    public Color actorColor;
    public string actorDescription;
}
public class DialogueEntry
{
    public string blockName = "";
    public List<List<string>> rows;
}
public class DialogueProps
{
    public int BlockID;
    public List<DialogueEntry> dialogueEntries;
}
public class Keywords
{
    public static List<string> block =new List<string> { "¶Ô»°" };
    public static List<string> actor =new List<string> { "½ÇÉ«","Actor" };
    public static List<List<string>> keywords = new List<List<string>>
    { 
        block, actor,
    };
}