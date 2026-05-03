using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper class to trigger dialogue after a minigame completes.
/// Minigames should call TriggerDialogue() when they finish.
/// </summary>
public static class MinigameDialogueHelper
{
    /// <summary>
    /// Triggers the dialogue scene for the specified minigame with the given outcome.
    /// </summary>
    /// <param name="minigameID">The minigame identifier (e.g., "Armdrugge", "Bio", "Geo", etc.)</param>
    /// <param name="won">True if the player won, false if lost, null for other outcomes</param>
    /// <param name="delaySeconds">Optional delay before loading dialogue (default: 2 seconds)</param>
    public static void TriggerDialogue(string minigameID, bool? won = null, float delaySeconds = 2f)
    {
        Debug.Log($"[MinigameDialogueHelper] TriggerDialogue called for {minigameID}, won: {won}");
        
        // Determine the phase based on the outcome and game progress
        string phase = DeterminePhase(minigameID, won);
        
        Debug.Log($"[MinigameDialogueHelper] Phase determined: {phase}");
        
        // IMPORTANT: We need to return to SampleScene (the navigation scene), NOT the minigame scene
        // This prevents an endless loop where minigame -> dialogue -> minigame -> dialogue...
        string returnSceneName = "SampleScene";
        
        Debug.Log($"[MinigameDialogueHelper] Return scene will be: {returnSceneName}");
        
        // Store transition data
        DialogueTransitionData.SetTransitionData(
            minigameID: minigameID,
            phase: phase,
            returnSceneName: returnSceneName,
            returnSlideKey: null  // Minigames typically don't have a specific slide to return to
        );
        
        // Schedule the dialogue scene load
        if (delaySeconds > 0f)
        {
            // We need a MonoBehaviour to run a coroutine, so we'll use a simple delay technique
            // If there's a GameObject in the scene, we can use it
            var helper = new GameObject("MinigameDialogueLoader");
            var loader = helper.AddComponent<MinigameDialogueLoader>();
            loader.LoadDialogueAfterDelay(delaySeconds);
        }
        else
        {
            LoadDialogueScene();
        }
    }
    
    /// <summary>
    /// Determines the dialogue phase based on the minigame outcome and progress.
    /// </summary>
    private static string DeterminePhase(string minigameID, bool? won)
    {
        // If the outcome is explicitly provided, use it
        if (won.HasValue)
        {
            if (won.Value)
            {
                // Player won - update progress and show win dialogue
                GameDataLoader.UpdateMinigameProgress(minigameID, completed: true, incrementTries: true);
                return "Win";
            }
            else
            {
                // Player lost - increment tries and show retry/loose dialogue
                GameDataLoader.UpdateMinigameProgress(minigameID, completed: false, incrementTries: true);
                
                // Check if this was the first try
                if (GameDataLoader.TryGetMinigameProgress(minigameID, out bool completed, out int tries))
                {
                    return tries > 1 ? "Retry" : "Loose";
                }
                return "Loose";
            }
        }
        
        // No explicit outcome - check game progress
        if (GameDataLoader.TryGetMinigameProgress(minigameID, out bool isCompleted, out int numTries))
        {
            if (isCompleted)
            {
                return "Win";
            }
            else if (numTries > 0)
            {
                return "Retry";
            }
        }
        
        // Default to Start for first-time encounters
        return "Start";
    }
    
    /// <summary>
    /// Loads the Dialogue scene immediately.
    /// </summary>
    public static void LoadDialogueScene()
    {
        Debug.Log("[MinigameDialogueHelper] Loading Dialogue scene");
        SceneManager.LoadScene("Dialogue", LoadSceneMode.Single);
    }
    
    /// <summary>
    /// Helper MonoBehaviour to delay the dialogue scene load.
    /// </summary>
    private class MinigameDialogueLoader : MonoBehaviour
    {
        public void LoadDialogueAfterDelay(float delay)
        {
            StartCoroutine(LoadAfterDelay(delay));
        }
        
        private System.Collections.IEnumerator LoadAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadDialogueScene();
            Destroy(gameObject);
        }
    }
}
