using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel; // The main container for the settings menu
    public Slider volumeSlider;      // Controls the dialogue loudness
    public Button automateButton;    // UI button to trigger auto-advance
    public Button audioButton;       // UI button to toggle dialogue audio

    [Header("System References")]
    public DialogueManager dialogueManager; // Reference to our main dialogue logic
    public AudioSource dialogueAudioSource; // Where the voice lines actually play from

    private bool isSettingsOpen = false;

    void Start()
    {
        // Make sure the settings menu is hidden when the game starts
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Setup the volume slider to match the current audio level and listen for changes
        if (volumeSlider != null && dialogueAudioSource != null)
        {
            volumeSlider.value = dialogueAudioSource.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Hide these buttons by default; we only want them visible when settings are open
        if (automateButton != null) automateButton.gameObject.SetActive(false);
        if (audioButton != null) audioButton.gameObject.SetActive(false);
    }

    // Call this from your main UI "Settings" button to open/close the menu
    public void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(isSettingsOpen);
        }

        // Update the visibility of the sub-buttons based on the menu state
        if (automateButton != null)
        {
            automateButton.gameObject.SetActive(isSettingsOpen);
        }
        if (audioButton != null)
        {
            audioButton.gameObject.SetActive(isSettingsOpen);
        }
    }

    // Triggered whenever the user moves the volume slider
    private void OnVolumeChanged(float value)
    {
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.volume = value;
        }
    }

    // UI Hook for the Automate button
    public void OnAutomateButtonClicked()
    {
        if (dialogueManager != null)
        {
            dialogueManager.ToggleAutoPlay();
        }
    }

    // UI Hook for the Audio toggle button
    public void OnAudioButtonClicked()
    {
        if (dialogueManager != null)
        {
            dialogueManager.ToggleAudio();
        }
    }
}