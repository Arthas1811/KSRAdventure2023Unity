using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public GameObject dialogueBox; // Changed from dialogueBackground for clarity
    public Image backgroundImageDisplay;

    [Header("Navigation Buttons")]
    public Button previousButton; // Button to go back one dialogue line
    public Button skipButton; // Button to skip forward one dialogue line
    public string currentMinigameID = "Armdrugge"; 
    public string currentPhase = "Start";
    public float autoAdvanceDelay = 1.5f; // Time to wait before moving to the next line

    [Header("Audio")]
    public AudioSource dialogueAudioSource;

    // Internal tracking
    private DialogueContainer dialogueData;
    private List<DialogueLine> linesToPlay = new List<DialogueLine>();
    private int currentIndex = 0;
    private int displayedIndex = 0; // The index that is ACTUALLY being displayed (for Previous button)

    // State toggles
    private bool isAutoPlaying = true;
    private bool isAudioEnabled = true;
    private DialogueLine currentLine;
    private Coroutine typewriterCoroutine;
    private bool isTypewriterRunning = false;
    private bool skipAutoAdvance = false; // Flag to prevent auto-advance after manual navigation
    private int coroutineId = 0; // Unique ID for each coroutine to prevent stale coroutines from advancing

    void Start()
    {
        // Configure Canvas Scaler for responsive UI across different screen sizes
        ConfigureCanvasScaler();
        
        // Configure UI element anchors for proper scaling
        ConfigureUIAnchors();
        
        // Initialize navigation buttons
        InitializeNavigationButtons();
        
        // Try to load transition data from the previous scene
        if (DialogueTransitionData.TryConsumeData(out string minigameID, out string phase, out string returnScene, out string returnSlide))
        {
            currentMinigameID = minigameID;
            currentPhase = phase;
            Debug.Log($"DialogueManager: Loaded transition data - Minigame: {minigameID}, Phase: {phase}");
        }
        else
        {
            Debug.LogWarning($"DialogueManager: No transition data found, using default values - Minigame: {currentMinigameID}, Phase: {currentPhase}");
        }

        // First, we get the data from the JSON file
        LoadDialogueData();
        
        // Then we filter and sort the lines based on the current minigame/phase
        SetupDialogueLines();
        
        // Kick off the first line of dialogue
        DisplayCurrentLine();
    }

    void ConfigureCanvasScaler()
    {
        // Find the Canvas component in the scene hierarchy
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            // Configure like in Navigation system
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // Standard HD resolution
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height (0.5 = balanced scaling)
            
            Debug.Log("DialogueManager: Canvas Scaler configured for responsive UI");
        }
        else
        {
            Debug.LogWarning("DialogueManager: Could not find Canvas to configure scaler");
        }
    }
    
    void ConfigureUIAnchors()
    {
        // Only configure background image - leave other UI elements as they are in the Unity Editor
        if (backgroundImageDisplay != null)
        {
            RectTransform bgRect = backgroundImageDisplay.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                // Anchor to all corners (stretch to fill)
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                Debug.Log($"DialogueManager: Background '{backgroundImageDisplay.name}' anchors configured (stretch to fill)");
            }
        }
    }

    void Update()
    {
        // Manual skip/advance using the Space key
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("DialogueManager: Space key pressed - advancing dialogue");
            AdvanceDialogue();
        }
    }

    void LoadDialogueData()
    {
        // Construct the path to your JSON. 
        string filePath = Path.Combine(Application.dataPath, "scripts/dialogue/Content_Dialog_Minigames.json");

        if (File.Exists(filePath))
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                dialogueData = JsonUtility.FromJson<DialogueContainer>(jsonText);
                Debug.Log("Dialogue system: JSON data loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Dialogue system: Failed to parse JSON. Error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"Dialogue system: JSON file missing at path: {filePath}");
        }
    }

    void SetupDialogueLines()
    {
        linesToPlay.Clear();
        DialoguePhase selectedPhase = null;

        if (dialogueData == null)
        {
            Debug.LogError("Dialogue system: Cannot setup lines because dialogueData is null.");
            return;
        }

        // Fetch the specific minigame data using the ID helper
        DialogueRoot currentMinigame = dialogueData.GetMinigameByID(currentMinigameID);

        if (currentMinigame == null)
        {
            Debug.LogError($"Dialogue system: Minigame ID '{currentMinigameID}' wasn't found in the JSON.");
            return;
        }

        // Background handling: Strip extensions so Resources.Load can find the file
        string rawPath = currentMinigame.background;
        string newfolder = "images/";
        if (!string.IsNullOrEmpty(rawPath))
        {
            string cleanPath = rawPath.Replace(".png", "").Replace(".jpg", "").Replace(".JPG", "");
            string finalPath = newfolder + cleanPath;
            Sprite bgSprite = Resources.Load<Sprite>(finalPath);
            
            if (bgSprite != null && backgroundImageDisplay != null)
            {
                backgroundImageDisplay.sprite = bgSprite;
                
                // Add or update AspectRatioFitter to maintain aspect ratio
                AspectRatioFitter fitter = backgroundImageDisplay.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                {
                    fitter = backgroundImageDisplay.gameObject.AddComponent<AspectRatioFitter>();
                }
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
            }
        }

        // Map the current string phase to the actual data object
        switch (currentPhase)
        {
            case "Retry": selectedPhase = currentMinigame.phases.Retry; break;
            case "Win":   selectedPhase = currentMinigame.phases.Win; break;
            case "Loose": selectedPhase = currentMinigame.phases.Loose; break;
            default:      selectedPhase = currentMinigame.phases.Start; break;
        }

        // If we found a valid phase, sort the lines by their 'order' property
        if (selectedPhase != null && selectedPhase.Dialogue != null)
        {
            linesToPlay = new List<DialogueLine>(selectedPhase.Dialogue);
            linesToPlay.Sort((a, b) => a.order.CompareTo(b.order));
        }
        else
        {
            Debug.LogWarning($"Dialogue system: No lines found for Phase '{currentPhase}' in '{currentMinigameID}'.");
        }
    }

    void DisplayCurrentLine()
    {
        Debug.Log($"[DialogueManager] DisplayCurrentLine called. Index: {currentIndex}/{linesToPlay.Count}, skipAutoAdvance: {skipAutoAdvance}");
        
        // Store the current skipAutoAdvance value for THIS line
        bool skipAutoForThisLine = skipAutoAdvance;
        
        // Reset the flag immediately - it only applies to the line we're about to show
        skipAutoAdvance = false;
        
        Debug.Log($"[DialogueManager] skipAutoForThisLine: {skipAutoForThisLine}, skipAutoAdvance reset to: {skipAutoAdvance}");
        
        // Toggle the UI box visibility
        if (dialogueBox != null && !dialogueBox.activeSelf)
            dialogueBox.SetActive(true);

        // Check if we've reached the end of the conversation
        if (linesToPlay.Count == 0 || currentIndex >= linesToPlay.Count)
        {
            Debug.Log($"[DialogueManager] Dialogue complete! CurrentIndex: {currentIndex}, Total lines: {linesToPlay.Count}");
            if (dialogueBox != null) dialogueBox.SetActive(false);
            
            // Dialogue is complete, return to the game
            OnDialogueComplete();
            return;
        }

        currentLine = linesToPlay[currentIndex];
        Debug.Log($"[DialogueManager] About to display line at index {currentIndex}, current displayedIndex: {displayedIndex}");

        // Stop the previous typewriter effect before starting a new one
        if (typewriterCoroutine != null)
        {
            Debug.Log("DialogueManager: Stopping previous typewriter coroutine in DisplayCurrentLine");
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // Increment coroutine ID to invalidate any old running coroutines
        coroutineId++;
        int currentCoroutineId = coroutineId;
        Debug.Log($"DialogueManager: Starting new typewriter coroutine with ID {currentCoroutineId}");
        
        // Start new typewriter effect with unique ID and the skip flag for THIS line
        typewriterCoroutine = StartCoroutine(TypewriterEffectRoutine(currentCoroutineId, skipAutoForThisLine));
        
        // Update button states
        UpdateNavigationButtons();
    }

    /// <summary>
    /// Called when all dialogue lines have been displayed.
    /// Returns to the navigation scene.
    /// </summary>
    private void OnDialogueComplete()
    {
        Debug.Log("[DialogueManager] OnDialogueComplete called!");
        Debug.Log($"[DialogueManager] DialogueTransitionData.HasData: {DialogueTransitionData.HasData}");
        
        // Return to the game scene using the stored return data
        DialogueTransitionData.ReturnToGame();
        
        Debug.Log("[DialogueManager] ReturnToGame() called");
    }

    private IEnumerator TypewriterEffectRoutine(int myCoroutineId, bool skipAutoForThisLine)
    {
        Debug.Log($"DialogueManager: TypewriterEffectRoutine started with ID {myCoroutineId}, skipAutoForThisLine: {skipAutoForThisLine}");
        isTypewriterRunning = true;
        
        // Add character name centered and in bold using TMP rich text tags
        if (dialogueText != null) dialogueText.text = $"<align=\"center\"><b>{currentLine.speaker}</b></align>\n";
        string messageBody = currentLine.content;

        string audioBasePath = "Audio/dialogue/";

        // Try to load the audio clip associated with this line
        AudioClip voiceClip = null;
        if (!string.IsNullOrEmpty(currentLine.audio))
        {
            string fullAudioPath = audioBasePath + currentLine.audio;
            voiceClip = Resources.Load<AudioClip>(fullAudioPath);
        }

        // Calculate timing: if audio exists, sync text speed to audio length; otherwise use default
        float charDelay = 0.05f;
        if (voiceClip != null && messageBody.Length > 0)
        {
            charDelay = voiceClip.length / messageBody.Length;
        }

        // Play the voice line if enabled
        if (isAudioEnabled && voiceClip != null && dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.PlayOneShot(voiceClip);
        }

        // The actual typewriter animation loop
        if (isAutoPlaying)
        {
            foreach (char c in messageBody)
            {
                if (dialogueText != null) dialogueText.text += c;
                yield return new WaitForSeconds(charDelay);
            }
        }
        else
        {
            // If autoplay is off, just show the whole text immediately
            if (dialogueText != null) dialogueText.text = $"<align=\"center\"><b>{currentLine.speaker}</b></align>\n{messageBody}";
        }

        // Small safety: ensure the full text is displayed at the end
        if (dialogueText != null) dialogueText.text = $"<align=\"center\"><b>{currentLine.speaker}</b></align>\n{messageBody}";

        isTypewriterRunning = false;
        
        // NOW update displayedIndex - the line is actually visible to the user
        displayedIndex = currentIndex;
        Debug.Log($"DialogueManager: Coroutine {myCoroutineId} - Text displayed. displayedIndex updated to {displayedIndex}, coroutineId: {coroutineId}, isAutoPlaying: {isAutoPlaying}, skipAutoForThisLine: {skipAutoForThisLine}");
        
        // Check if this coroutine is still the active one
        if (myCoroutineId != coroutineId)
        {
            Debug.Log($"DialogueManager: Coroutine {myCoroutineId} is outdated (current is {coroutineId}). Stopping.");
            yield break;
        }
        
        // If we are in "Automatic" mode AND not manually navigated to this line, wait and advance
        if (isAutoPlaying && !skipAutoForThisLine)
        {
            Debug.Log($"DialogueManager: Coroutine {myCoroutineId} - Waiting {autoAdvanceDelay} seconds before auto-advance");
            yield return new WaitForSeconds(autoAdvanceDelay);
            
            // Check again if this coroutine is still the active one after the wait
            if (myCoroutineId != coroutineId)
            {
                Debug.Log($"DialogueManager: Coroutine {myCoroutineId} is outdated after wait (current is {coroutineId}). Stopping.");
                yield break;
            }
            
            Debug.Log($"DialogueManager: Coroutine {myCoroutineId} - Auto-advancing to next line");
            AdvanceDialogue();
        }
        else
        {
            Debug.Log($"DialogueManager: Coroutine {myCoroutineId} - No auto-advance (isAutoPlaying: {isAutoPlaying}, skipAutoForThisLine: {skipAutoForThisLine})");
        }
    }

    public void AdvanceDialogue()
    {
        Debug.Log($"DialogueManager: AdvanceDialogue called. Current index: {currentIndex}");
        
        currentIndex++;
        Debug.Log($"DialogueManager: Advanced to index {currentIndex}");
        
        DisplayCurrentLine();
    }

    public void ToggleAutoPlay()
    {
        isAutoPlaying = !isAutoPlaying;
    }

    public void ToggleAudio()
    {
        isAudioEnabled = !isAudioEnabled;

        // Stop audio immediately if user mutes mid-dialogue
        if (!isAudioEnabled && dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }
    }

    // === NAVIGATION BUTTONS ===
    
    private void InitializeNavigationButtons()
    {
        // Setup previous button
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            previousButton.onClick.AddListener(RestartEntireDialogue);
            Debug.Log("DialogueManager: Previous button configured");
        }
        else
        {
            Debug.LogWarning("DialogueManager: Previous button not assigned!");
        }
        
        // Setup skip button
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            skipButton.onClick.AddListener(SkipEntireDialogue);
            Debug.Log("DialogueManager: Skip button configured");
        }
        else
        {
            Debug.LogWarning("DialogueManager: Skip button not assigned!");
        }
        
        UpdateNavigationButtons();
        Debug.Log("DialogueManager: Navigation buttons initialized");
    }
    
    private void UpdateNavigationButtons()
    {
        // Restart button is always available
        if (previousButton != null)
        {
            previousButton.interactable = true;
        }
        
        // Skip button is always available to skip the entire dialogue
        if (skipButton != null)
        {
            skipButton.interactable = true;
        }
    }
    
    public void GoToPreviousLine()
    {
        Debug.Log($"DialogueManager: GoToPreviousLine called. DisplayedIndex: {displayedIndex}, CurrentIndex: {currentIndex}, isTypewriterRunning: {isTypewriterRunning}");
        
        // Stop any running coroutines FIRST to prevent race conditions
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // Stop audio
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }
        
        // Invalidate any running coroutines
        coroutineId++;
        
        // Use displayedIndex (what the user actually sees) instead of currentIndex
        // If we're at index 0, we can't go back - just replay this line
        if (displayedIndex == 0)
        {
            Debug.Log("DialogueManager: At first line, replaying it");
            currentIndex = 0;
            skipAutoAdvance = true;
            DisplayCurrentLine();
            return;
        }
        
        // Go back to the previous line based on what was DISPLAYED, not currentIndex
        currentIndex = displayedIndex - 1;
        Debug.Log($"DialogueManager: Going back from displayed index {displayedIndex} to index {currentIndex}");
        
        // Set flag to prevent auto-advance after manual navigation
        skipAutoAdvance = true;
        
        // Display the previous line
        DisplayCurrentLine();
    }
    
    public void SkipEntireDialogue()
    {
        Debug.Log("DialogueManager: SkipEntireDialogue called - skipping entire dialogue part...");
        
        // Stop any running typewriter effect
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // Stop any playing audio
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }
        
        // Hide dialogue box
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
        
        // Complete the dialogue and return to game
        Debug.Log("DialogueManager: Calling OnDialogueComplete...");
        OnDialogueComplete();
    }
    
    public void RestartEntireDialogue()
    {
        Debug.Log("DialogueManager: RestartEntireDialogue called - restarting from beginning...");
        
        // Stop any running typewriter effect
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        // Stop any playing audio
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }
        
        // Invalidate any running coroutines
        coroutineId++;
        
        // Reset to the first line
        currentIndex = 0;
        
        Debug.Log("DialogueManager: Restarting from index 0");
        
        // Display the first line
        DisplayCurrentLine();
    }
    
    // Keep old name for backwards compatibility if assigned in Unity
    public void SkipToNextLine()
    {
        SkipEntireDialogue();
    }
}