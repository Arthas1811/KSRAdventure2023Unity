using System.Collections.Generic;

// represents a single speech text entry
[System.Serializable]
public class DialogueLine
{
    public int order;      // sorting index 
    public string speaker; // character ID or Name
    public string content; // text displayed to player
    public string audio; // audio file 
}

// wrapper for a sequence of dialogue lines
[System.Serializable]
public class DialoguePhase
{
    public List<DialogueLine> Dialogue;
}

// holds the specific dialogue tracks for different game states
[System.Serializable]
public class DialoguePhases
{
    public DialoguePhase Start;
    public DialoguePhase Retry;
    public DialoguePhase Win;
    public DialoguePhase Loose; 
}

// main entry point for a single minigame's dialogue data
[System.Serializable]
public class DialogueRoot
{
    public int id;             // minigame ID
    public string background;
    public DialoguePhases phases;
}

// top-level container matching the JSON structure
[System.Serializable]
public class DialogueContainer
{
    public DialogueRoot Armdrugge;
    public DialogueRoot Ph;
    public DialogueRoot Piano;
    public DialogueRoot Bio;
    public DialogueRoot Geo;
    public DialogueRoot Dimitri;
    public DialogueRoot BgDialogue;
    public DialogueRoot Jacket;

    public DialogueRoot GetMinigameByID(string id)
    {
        switch (id)
        {
            case "Armdrugge": return Armdrugge;
            case "Ph": return Ph;
            case "Piano": return Piano;
            case "Bio": return Bio;
            case "Geo": return Geo;
            case "Dimitri": return Dimitri;
            case "BgDialogue": return BgDialogue;
            case "Jacket": return Jacket;
            default: return null;
        }
    }
}