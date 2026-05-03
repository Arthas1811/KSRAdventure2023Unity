using System.Diagnostics;
using UnityEngine;
using TMPro;

public class FindErrorsMinigame : MonoBehaviour
{
    private int count = 0;
    private int click = 0;
    private bool _dialogueTriggered = false;

    public GameObject winText;
    public TextMeshProUGUI clickText;
    public GameObject restartButton;

    public void Count()
    {
        count++;
    }

    public void Click()
    {
        click++;
        clickText.text = "Clicks: " + click.ToString();
    }
    // Update is called once per frame
    void Update()
    {
        if (count >= 7)
        {
            winText.SetActive(true);
            restartButton.SetActive(true);
            
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                UnityEngine.Debug.Log("[FindErrorsMinigame] Player won - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("BgDialogue", won: true, delaySeconds: 2f);
            }
        }
    }
}
