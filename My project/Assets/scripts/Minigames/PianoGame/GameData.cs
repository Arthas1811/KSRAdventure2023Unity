using System;

[Serializable]
public class GameData
{
    public int lives;
    public RoundData[] rounds;
}

[Serializable]
public class RoundData
{
    public string[] notes;
}
