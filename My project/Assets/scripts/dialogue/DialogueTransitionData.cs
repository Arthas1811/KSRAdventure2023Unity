using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static class to store dialogue transition data between scenes.
/// This allows passing minigame ID, phase, and return information when loading the Dialogue scene.
/// </summary>
public static class DialogueTransitionData
{
    private static string _minigameID;
    private static string _phase;
    private static string _returnSceneName;
    private static string _returnSlideKey;
    private static string _nextMinigameScene;  // NEW: For chaining dialogue -> minigame
    private static bool _hasData;

    /// <summary>
    /// Sets the dialogue transition data before loading the Dialogue scene.
    /// </summary>
    /// <param name="minigameID">The minigame identifier (e.g., "Armdrugge", "Ph", "Bio", etc.)</param>
    /// <param name="phase">The dialogue phase (e.g., "Start", "Retry", "Win", "Loose")</param>
    /// <param name="returnSceneName">The scene to return to after dialogue completes (typically "SampleScene")</param>
    /// <param name="returnSlideKey">The specific slide/node to return to in the navigation system</param>
    /// <param name="nextMinigameScene">Optional: Scene to load after dialogue (for Start phase -> Minigame flow)</param>
    public static void SetTransitionData(string minigameID, string phase, string returnSceneName, string returnSlideKey, string nextMinigameScene = null)
    {
        _minigameID = minigameID;
        _phase = phase;
        _returnSceneName = returnSceneName;
        _returnSlideKey = returnSlideKey;
        _nextMinigameScene = nextMinigameScene;
        _hasData = true;
    }

    /// <summary>
    /// Attempts to retrieve the stored transition data WITHOUT consuming it.
    /// The data remains available until ReturnToGame() is called.
    /// </summary>
    /// <param name="minigameID">Output: The minigame identifier</param>
    /// <param name="phase">Output: The dialogue phase</param>
    /// <param name="returnSceneName">Output: The scene to return to</param>
    /// <param name="returnSlideKey">Output: The slide/node to return to</param>
    /// <returns>True if data was available, false otherwise</returns>
    public static bool TryConsumeData(out string minigameID, out string phase, out string returnSceneName, out string returnSlideKey)
    {
        minigameID = _minigameID;
        phase = _phase;
        returnSceneName = _returnSceneName;
        returnSlideKey = _returnSlideKey;

        // Do NOT clear the data here - we need it for ReturnToGame()
        return _hasData;
    }

    /// <summary>
    /// Returns to the stored scene and slide key without consuming all data.
    /// Use this when dialogue is complete and you want to return to the game.
    /// </summary>
    public static void ReturnToGame()
    {
        Debug.Log("[DialogueTransitionData] Rephase: '{_phase}'");
        Debug.Log($"[DialogueTransitionData] _nextMinigameScene: '{_nextMinigameScene}'");
        Debug.Log($"[DialogueTransitionData] _returnSceneName: '{_returnSceneName}'");
        Debug.Log($"[DialogueTransitionData] _returnSlideKey: '{_returnSlideKey}'");
        
        if (!_hasData)
        {
            Debug.LogWarning("DialogueTransitionData: No return data stored, cannot return to game.");
            return;
        }
        
        // Check if this is a Start-phase dialogue that should chain to a minigame
        if (_phase == "Start" && !string.IsNullOrWhiteSpace(_nextMinigameScene))
        {
            Debug.Log($"[DialogueTransitionData] Start phase detected - loading minigame scene: {_nextMinigameScene}");
            
            // Store the scene name before clearing
            string sceneToLoad = _nextMinigameScene;
            
            // Clear data before loading the minigame scene
            _minigameID = null;
            _phase = null;
            _returnSceneName = null;
            _returnSlideKey = null;
            _nextMinigameScene = null;
            _hasData = false;
            
            // Load the minigame scene
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            return;
        }
        
        // Otherwise, return to the navigation scene
        if (string.IsNullOrWhiteSpace(_returnSceneName))
        {
            Debug.LogWarning("DialogueTransitionData: No return scene stored, cannot return to game.");
            return;
        }

        // Store the return slide key for the navigation system to pick up
        if (!string.IsNullOrWhiteSpace(_returnSlideKey))
        {
            Debug.Log($"[DialogueTransitionData] Setting NavigationReturnState to: {_returnSlideKey}");
            NavigationReturnState.SetReturnSlide(_returnSlideKey);
        }

        // Store the scene name before clearing (we need it for LoadScene)
        string returnScene = _returnSceneName;

        // Clear data before loading the scene
        _minigameID = null;
        _phase = null;
        _returnSceneName = null;
        _returnSlideKey = null;
        _nextMinigameScene = null;
        _hasData = false;

        // Load the return scene
        Debug.Log($"[DialogueTransitionData] Loading scene: {returnScene}");
        SceneManager.LoadScene(returnScene, LoadSceneMode.Single);
    }

    /// <summary>
    /// Checks if transition data is currently stored.
    /// </summary>
    public static bool HasData => _hasData;

    /// <summary>
    /// Gets the current minigame ID without consuming the data.
    /// </summary>
    public static string CurrentMinigameID => _minigameID;

    /// <summary>
    /// Gets the current phase without consuming the data.
    /// </summary>
    public static string CurrentPhase => _phase;
}

/// <summary>
/// Helper class to store navigation return state after dialogue.
/// This works alongside MinigameReturnState but specifically for dialogue scenes.
/// </summary>
public static class NavigationReturnState
{
    private static string _returnSlideKey;

    public static void SetReturnSlide(string slideKey)
    {
        _returnSlideKey = slideKey;
    }

    public static bool TryConsumeReturnSlide(out string slideKey)
    {
        slideKey = _returnSlideKey;
        bool hasData = !string.IsNullOrWhiteSpace(_returnSlideKey);
        _returnSlideKey = null;
        return hasData;
    }

    public static bool HasReturnSlide => !string.IsNullOrWhiteSpace(_returnSlideKey);
}
