using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public TMP_Text LivesText;
    public TMP_Text WinScreen;

    private GameData gameData;
    private int currentRound;
    private int currentNoteIndex;
    private int lives;

    private bool canPlayerInput = false;
    private bool _dialogueTriggered = false;

    private Dictionary<string, AudioClip> audioMap = new Dictionary<string, AudioClip>();
    private AudioSource audioSource;

    private const string AudioSequencesResource = "AudioSequences";
    private const string AudioSequencesFallback = "jsons/PianoGame/AudioSequences";

    void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (LivesText == null || WinScreen == null)
        {
            Debug.LogError("GameManager: Assign LivesText and WinScreen in inspector.");
            return;
        }

        LoadAudio();
        if (!LoadGameData())
        {
            return;
        }
        WinScreen.text = "";
        lives = gameData.lives;
        UpdateLivesUI();
        StartCoroutine(PlayCurrentRound());
    }

    void LoadAudio()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");

        foreach (AudioClip clip in clips)
        {
            audioMap.Add(clip.name, clip);
        }
    }

    bool LoadGameData()
    {
        TextAsset json = Resources.Load<TextAsset>(AudioSequencesResource);
        if (json == null)
        {
            json = Resources.Load<TextAsset>(AudioSequencesFallback);
        }

        if (json == null)
        {
            Debug.LogError($"GameManager: Could not find {AudioSequencesResource}.json. Tried: {AudioSequencesResource}, {AudioSequencesFallback}");
            return false;
        }

        gameData = JsonUtility.FromJson<GameData>(json.text);
        if (gameData == null || gameData.rounds == null || gameData.rounds.Length == 0)
        {
            Debug.LogError("GameManager: AudioSequences JSON is empty or invalid.");
            return false;
        }

        return true;
    }

    IEnumerator PlayCurrentRound()
    {
        canPlayerInput = false;
        currentNoteIndex = 0;

        yield return new WaitForSeconds(1f);

        foreach (string note in gameData.rounds[currentRound].notes)
        {
            audioSource.PlayOneShot(audioMap[note]);
            yield return new WaitForSeconds(0.7f);
        }

        canPlayerInput = true;
    }

    public void RegisterPlayerInput(string note)
    {
        if (!canPlayerInput) return;

        string expected = gameData.rounds[currentRound].notes[currentNoteIndex];

        if (note == expected)
        {
            currentNoteIndex++;

            if (currentNoteIndex >= gameData.rounds[currentRound].notes.Length)
            {
                currentRound++;

                if (currentRound >= gameData.rounds.Length)
                {
                    Debug.Log("YOU WIN");
                    WinScreen.text = "YOU WIN";
                    
                    if (!_dialogueTriggered)
                    {
                        _dialogueTriggered = true;
                        Debug.Log("[PianoGame] Player won - triggering dialogue");
                        MinigameDialogueHelper.TriggerDialogue("Piano", won: true, delaySeconds: 2f);
                    }
                }
                else
                {
                    StartCoroutine(PlayCurrentRound());
                }
            }
        }
        else
        {
            LoseLife();
        }
    }

    void LoseLife()
    {
        lives--;
        UpdateLivesUI();
        Debug.Log("Wrong! Lives left: " + lives);

        if (lives == 0)
        {
            Debug.Log("GAME OVER");
            
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                Debug.Log("[PianoGame] Player lost - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("Piano", won: false, delaySeconds: 2f);
            }
        }
        else
        {
            StartCoroutine(PlayCurrentRound());
        }
    }

    void UpdateLivesUI()
    {
        LivesText.text = "Lives: " + lives;
    }
}
