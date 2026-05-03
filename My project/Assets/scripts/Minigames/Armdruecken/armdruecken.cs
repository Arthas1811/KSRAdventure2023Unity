using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class armdruecken : MonoBehaviour
{
    public GameObject ButtonLeft;
    public GameObject ButtonRight;
    public GameObject ButtonMiddle;
    public GameObject ProgressBarObject;
    public GameObject Text;
    public GameObject Background;
    private ProgressBarClass _progressBar;
    private TextMeshProUGUI _statusText;
    private SpriteRenderer _backgroundRenderer;
    private Sprite _startSprite;
    private Sprite _loseSprite;
    private Sprite _winSprite;
    private static Sprite _fallbackButtonSprite;
    
    // Track if dialogue has been triggered to avoid multiple triggers
    private bool _dialogueTriggered = false;

    private void Awake()
    {
        ButtonLeft = ButtonLeft ?? FindByNames("Button left", "ButtonLeft", "button left", "button_left", "LeftButton");
        ButtonRight = ButtonRight ?? FindByNames("Button right", "ButtonRight", "button right", "button_right", "RightButton");
        ButtonMiddle = ButtonMiddle ?? FindByNames("Button middle", "ButtonMiddle", "button middle", "button_middle", "MiddleButton");
        ProgressBarObject = ProgressBarObject ?? FindByNames("Progressbar controller", "ProgressBar", "Progressbar", "progressbar");
        Text = Text ?? FindByNames("Text", "StatusText", "ResultText");
        Background = Background ?? FindByNames("Background", "BackgroundSprite", "BackgroundImage");

        _progressBar = ProgressBarObject != null ? ProgressBarObject.GetComponent<ProgressBarClass>() : null;
        _statusText = Text != null ? Text.GetComponent<TextMeshProUGUI>() : null;
        _backgroundRenderer = Background != null ? Background.GetComponent<SpriteRenderer>() : null;

        _startSprite = LoadSprite("images/Minigames/Armdruecken/Arm_1", "Arm_1");
        _loseSprite = LoadSprite("images/Minigames/Armdruecken/Arm_3", "Arm_3");
        _winSprite = LoadSprite("images/Minigames/Armdruecken/Arm_5", "Arm_5");

        EnsureButtonVisible(ButtonLeft, 5);
        EnsureButtonVisible(ButtonMiddle, 5);
        EnsureButtonVisible(ButtonRight, 5);

        if (_backgroundRenderer != null && _startSprite != null)
        {
            _backgroundRenderer.sprite = _startSprite;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (ButtonMiddle != null) ButtonMiddle.SetActive(true);
        if (ButtonLeft != null) ButtonLeft.SetActive(false);
        if (ButtonRight != null) ButtonRight.SetActive(false);
        if (Text != null) Text.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {       
        if (_progressBar == null && ProgressBarObject != null)
        {
            _progressBar = ProgressBarObject.GetComponent<ProgressBarClass>();
        }
        if (_progressBar == null) return;

        float progress = _progressBar.progress;
        if (ButtonRight != null) ButtonRight.SetActive(progress > 0f && progress < 0.4f);
        if (ButtonMiddle != null) ButtonMiddle.SetActive(progress > 0.3f && progress < 0.7f);
        if (ButtonLeft != null) ButtonLeft.SetActive(progress > 0.6f && progress < 1f);
        
        // Player lost
        if (progress <= 0f)
        {
            if (Text != null) Text.SetActive(true);
            if (_backgroundRenderer != null && _loseSprite != null)
            {
                _backgroundRenderer.sprite = _loseSprite;
            }
            
            // Trigger dialogue for losing
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                Debug.Log("[Armdruecken] Player lost - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("Armdrugge", won: false, delaySeconds: 2f);
            }
        }
        
        // Player won
        if (progress >= 0.995f)
        {
            if (_statusText == null && Text != null)
            {
                _statusText = Text.GetComponent<TextMeshProUGUI>();
            }
            if (_statusText != null)
            {
                _statusText.text = "Won";
            }
            if (Text != null) Text.SetActive(true);
            if (_backgroundRenderer != null && _winSprite != null)
            {
                _backgroundRenderer.sprite = _winSprite;
            }
            if (ProgressBarObject != null) ProgressBarObject.SetActive(false);
            if (ButtonLeft != null) ButtonLeft.SetActive(false);
            if (ButtonRight != null) ButtonRight.SetActive(false);
            if (ButtonMiddle != null) ButtonMiddle.SetActive(false);
            
            // Trigger dialogue for winning
            if (!_dialogueTriggered)
            {
                _dialogueTriggered = true;
                Debug.Log("[Armdruecken] Player won - triggering dialogue");
                MinigameDialogueHelper.TriggerDialogue("Armdrugge", won: true, delaySeconds: 2f);
            }
        }
    }

    private static GameObject FindByNames(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var found = GameObject.Find(name);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite LoadSprite(string primaryPath, string fallbackPath)
    {
        var sprite = Resources.Load<Sprite>(primaryPath);
        return sprite != null ? sprite : Resources.Load<Sprite>(fallbackPath);
    }

    private static void EnsureButtonVisible(GameObject button, int sortingOrder)
    {
        if (button == null) return;
        var renderer = button.GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        renderer.enabled = true;
        var color = renderer.color;
        if (color.a < 1f)
        {
            color.a = 1f;
            renderer.color = color;
        }
        if (renderer.sortingOrder < sortingOrder)
        {
            renderer.sortingOrder = sortingOrder;
        }
        if (renderer.sprite == null)
        {
            renderer.sprite = GetFallbackButtonSprite();
        }
    }

    private static Sprite GetFallbackButtonSprite()
    {
        if (_fallbackButtonSprite != null) return _fallbackButtonSprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        var pixels = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        texture.SetPixels(pixels);
        texture.Apply();
        _fallbackButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        _fallbackButtonSprite.hideFlags = HideFlags.HideAndDontSave;
        _fallbackButtonSprite.name = "ArmButtonFallback";
        return _fallbackButtonSprite;
    }
}
