using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class armdruecken : MonoBehaviour
{
    private const float CanvasWidth = 1620f;
    private const float CanvasHeight = 1080f;
    private const float MinPower = 0f;
    private const float StartPower = 400f;
    private const float MaxPower = 800f;
    private const float PlayerClickPower = 12f;
    private const float FrogPowerMultiplier = 3f;
    private const float EnemyClickPower = 45f;
    private const float EnemyIntervalSeconds = 0.3f;
    private const float FinishDialogueDelaySeconds = 2f;

    private static readonly Vector2 StartCirclePosition = FromOriginalCanvas(800f, 640f);
    private static readonly Vector2 ScoreNeutralCirclePosition = FromOriginalCanvas(1200f, 500f);
    private static readonly Vector2 ScoreWinningCirclePosition = FromOriginalCanvas(970f, 720f);
    private static readonly Vector2 ScoreLosingCirclePosition = FromOriginalCanvas(1300f, 500f);

    public GameObject ButtonLeft;
    public GameObject ButtonRight;
    public GameObject ButtonMiddle;
    public GameObject ProgressBarObject;
    public GameObject Text;
    public GameObject Background;

    private ProgressBarClass _progressBar;
    private TextMeshProUGUI _statusText;
    private SpriteRenderer _backgroundRenderer;
    private AudioSource _audioSource;

    private Sprite _introSprite;
    private Sprite _neutralSprite;
    private Sprite _losingSprite;
    private Sprite _lostSprite;
    private Sprite _winningSprite;
    private Sprite _wonSprite;
    private readonly List<AudioClip> _armWrestlingSounds = new List<AudioClip>();

    private GameObject _activeClickTarget;
    private bool _gameStarted;
    private bool _gameOver;
    private bool _dialogueTriggered;
    private int _score;
    private int _soundTickCounter;
    private float _power = StartPower;
    private float _playerPowerMultiplier = 1f;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private static Sprite _fallbackButtonSprite;

    private void Awake()
    {
        ButtonLeft = ButtonLeft ?? FindByNames("Button left", "ButtonLeft", "button left", "button_left", "LeftButton");
        ButtonRight = ButtonRight ?? FindByNames("Button right", "ButtonRight", "button right", "button_right", "RightButton");
        ButtonMiddle = ButtonMiddle ?? FindByNames("Button middle", "ButtonMiddle", "button middle", "button_middle", "MiddleButton");
        ProgressBarObject = ProgressBarObject ?? FindByNames("Progressbar controller", "progressBarController", "ProgressBar", "Progressbar", "progressbar");
        Text = Text ?? FindByNames("Text", "Text (TMP)", "StatusText", "ResultText");
        Background = Background ?? FindByNames("Background", "background", "BackgroundSprite", "BackgroundImage");

        _progressBar = ProgressBarObject != null ? ProgressBarObject.GetComponent<ProgressBarClass>() : null;
        _statusText = Text != null ? Text.GetComponent<TextMeshProUGUI>() : null;
        _backgroundRenderer = Background != null ? Background.GetComponent<SpriteRenderer>() : null;

        _introSprite = LoadSprite("images/images/Routing/Haupthall/Arm_0", "images/Minigames/Armdruecken/Arm_1", "Arm_1");
        _neutralSprite = LoadSprite("images/Minigames/Armdruecken/Arm_1", "images/images/Routing/Haupthall/Arm_1", "Arm_1");
        _losingSprite = LoadSprite("images/Minigames/Armdruecken/Arm_2_", "images/images/Routing/Haupthall/Arm_2_", "Arm_2_");
        _lostSprite = LoadSprite("images/Minigames/Armdruecken/Arm_3", "images/images/Routing/Haupthall/Arm_3", "Arm_3");
        _winningSprite = LoadSprite("images/Minigames/Armdruecken/Arm_4", "images/images/Routing/Haupthall/Arm_4", "Arm_4");
        _wonSprite = LoadSprite("images/Minigames/Armdruecken/Arm_5", "images/images/Routing/Haupthall/Arm_5", "Arm_5");

        ConfigureCamera();
        ConfigureBackground();
        ConfigureProgressBarLayout();
        ConfigureClickTarget(ButtonLeft);
        ConfigureClickTarget(ButtonMiddle);
        ConfigureClickTarget(ButtonRight);
        LoadArmWrestlingSounds();
    }

    private void Start()
    {
        ResetToOriginalStartState();
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            ConfigureCamera();
        }
    }

    public void HandleClickTargetClicked(GameObject clickedTarget)
    {
        if (_gameOver || clickedTarget == null || clickedTarget != _activeClickTarget)
        {
            return;
        }

        if (!_gameStarted)
        {
            StartArmWrestling();
            return;
        }

        ApplyPlayerClick();
    }

    private void ResetToOriginalStartState()
    {
        StopAllCoroutines();

        _gameStarted = false;
        _gameOver = false;
        _dialogueTriggered = false;
        _score = 0;
        _soundTickCounter = 0;
        _power = StartPower;
        _playerPowerMultiplier = HasFrog() ? FrogPowerMultiplier : 1f;

        if (_statusText != null)
        {
            _statusText.text = string.Empty;
        }
        if (Text != null)
        {
            Text.SetActive(false);
        }
        SetProgressBarVisible(false);

        SetBackground(_introSprite);
        HideClickTargets();
        ShowClickTarget(ButtonMiddle ?? ButtonLeft ?? ButtonRight, StartCirclePosition, Color.yellow, 30f);
    }

    private void StartArmWrestling()
    {
        _gameStarted = true;
        _power = StartPower;

        SetBackground(_neutralSprite);

        SetProgressBarVisible(true);

        UpdateProgressBar(Color.yellow);
        ApplyScoreVisuals();
        StartCoroutine(EnemyClickLoop());
    }

    private IEnumerator EnemyClickLoop()
    {
        var wait = new WaitForSeconds(EnemyIntervalSeconds);

        while (!_gameOver)
        {
            yield return wait;

            _power -= EnemyClickPower;
            _soundTickCounter++;

            UpdateProgressBar(GetPowerColor());

            if (_soundTickCounter >= 8)
            {
                PlayArmWrestlingSound();
                _soundTickCounter = 0;
            }

            EvaluatePowerThresholds();
        }
    }

    private void ApplyPlayerClick()
    {
        if (_gameOver)
        {
            return;
        }

        _power += PlayerClickPower * _playerPowerMultiplier;
        UpdateProgressBar(GetPowerColor());
        EvaluatePowerThresholds();
    }

    private void EvaluatePowerThresholds()
    {
        if (_power >= MaxPower)
        {
            _score++;
            _power = StartPower;
            UpdateProgressBar(Color.yellow);
        }
        else if (_power <= MinPower)
        {
            _score--;
            _power = StartPower;
            UpdateProgressBar(Color.yellow);
        }

        ApplyScoreVisuals();
    }

    private void ApplyScoreVisuals()
    {
        switch (_score)
        {
            case 0:
                SetBackground(_neutralSprite);
                ShowClickTarget(ButtonMiddle ?? ButtonLeft ?? ButtonRight, ScoreNeutralCirclePosition, Color.red, 60f);
                break;
            case 1:
                SetBackground(_winningSprite);
                ShowClickTarget(ButtonMiddle ?? ButtonLeft ?? ButtonRight, ScoreWinningCirclePosition, Color.red, 60f);
                break;
            case -1:
                SetBackground(_losingSprite);
                ShowClickTarget(ButtonMiddle ?? ButtonLeft ?? ButtonRight, ScoreLosingCirclePosition, Color.red, 60f);
                break;
            case -2:
                FinishGame(false);
                break;
            case 2:
                FinishGame(true);
                break;
        }
    }

    private void FinishGame(bool won)
    {
        if (_gameOver)
        {
            return;
        }

        _gameOver = true;
        StopAllCoroutines();

        SetBackground(won ? _wonSprite : _lostSprite);
        HideClickTargets();

        SetProgressBarVisible(false);
        if (Text != null)
        {
            Text.SetActive(false);
        }

        if (!_dialogueTriggered)
        {
            _dialogueTriggered = true;
            MinigameDialogueHelper.TriggerDialogue("Armdrugge", won: won, delaySeconds: FinishDialogueDelaySeconds);
        }
    }

    private void UpdateProgressBar(Color color)
    {
        if (_progressBar == null && ProgressBarObject != null)
        {
            _progressBar = ProgressBarObject.GetComponent<ProgressBarClass>();
        }

        if (_progressBar == null)
        {
            return;
        }

        _progressBar.totalPower = MaxPower;
        _progressBar.SetPower(_power);
        _progressBar.SetFillColor(color);
    }

    private void SetProgressBarVisible(bool visible)
    {
        if (_progressBar == null && ProgressBarObject != null)
        {
            _progressBar = ProgressBarObject.GetComponent<ProgressBarClass>();
        }

        if (_progressBar != null && _progressBar.fillImage != null)
        {
            _progressBar.fillImage.gameObject.SetActive(visible);
            return;
        }

        if (ProgressBarObject != null)
        {
            ProgressBarObject.SetActive(visible);
        }
    }

    private Color GetPowerColor()
    {
        if (_power <= 300f)
        {
            return Color.red;
        }

        if (_power >= 500f)
        {
            return Color.green;
        }

        return Color.yellow;
    }

    private bool HasFrog()
    {
        return InventoryState.Instance != null && InventoryState.Instance.HasItem("frog");
    }

    private void ConfigureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        var aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : CanvasWidth / CanvasHeight;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(CanvasHeight * 0.5f, CanvasWidth / (2f * aspect));
        camera.transform.position = new Vector3(CanvasWidth * 0.5f, CanvasHeight * 0.5f, -10f);

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    private void ConfigureBackground()
    {
        if (_backgroundRenderer == null)
        {
            return;
        }

        _backgroundRenderer.sortingOrder = 0;
        _backgroundRenderer.transform.localScale = new Vector3(100f, 100f, 1f);
    }

    private void SetBackground(Sprite sprite)
    {
        if (_backgroundRenderer == null || sprite == null)
        {
            return;
        }

        _backgroundRenderer.sprite = sprite;
        _backgroundRenderer.transform.localScale = new Vector3(100f, 100f, 1f);
        _backgroundRenderer.transform.position = Vector3.zero;

        var bounds = _backgroundRenderer.bounds;
        var correction = new Vector3(-bounds.min.x, -bounds.min.y, 0f);
        _backgroundRenderer.transform.position += correction;
    }

    private void ConfigureProgressBarLayout()
    {
        if (ProgressBarObject == null)
        {
            return;
        }

        var canvas = ProgressBarObject.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        var rect = _progressBar != null && _progressBar.fillImage != null
            ? _progressBar.fillImage.rectTransform
            : ProgressBarObject.GetComponentInChildren<RectTransform>(true);

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(350f / CanvasWidth, 1f);
        rect.anchorMax = new Vector2((350f + MaxPower) / CanvasWidth, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 35f);
        rect.anchoredPosition = new Vector2(0f, -50f - 17.5f);
    }

    private void ConfigureClickTarget(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.sortingOrder = 5;
            if (renderer.sprite == null)
            {
                renderer.sprite = GetFallbackButtonSprite();
            }
        }

        var collider = target.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        target.SetActive(false);
    }

    private void ShowClickTarget(GameObject target, Vector2 position, Color color, float radius)
    {
        if (target == null || _gameOver)
        {
            return;
        }

        HideClickTargets();

        _activeClickTarget = target;
        target.SetActive(true);
        target.transform.position = new Vector3(position.x, position.y, -1f);

        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            if (renderer.sprite == null)
            {
                renderer.sprite = GetFallbackButtonSprite();
            }

            renderer.color = color;
            renderer.sortingOrder = 5;

            var spriteDiameter = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y, 0.01f);
            var scale = (radius * 2f) / spriteDiameter;
            target.transform.localScale = new Vector3(scale, scale, 1f);

            var collider = target.GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = radius / scale;
            }
        }
    }

    private void HideClickTargets()
    {
        if (ButtonLeft != null) ButtonLeft.SetActive(false);
        if (ButtonMiddle != null) ButtonMiddle.SetActive(false);
        if (ButtonRight != null) ButtonRight.SetActive(false);
        _activeClickTarget = null;
    }

    private void LoadArmWrestlingSounds()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;

        _armWrestlingSounds.Clear();
        AddSound("Audio/Minigames/Armdruecken/Armwrestling3");
        AddSound("Audio/Minigames/Armdruecken/Armwrestling2");
        AddSound("Audio/Minigames/Armdruecken/Armwrestling1");
    }

    private void AddSound(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            _armWrestlingSounds.Add(clip);
        }
    }

    private void PlayArmWrestlingSound()
    {
        if (_audioSource == null || _armWrestlingSounds.Count == 0)
        {
            return;
        }

        var index = Random.Range(0, _armWrestlingSounds.Count);
        _audioSource.PlayOneShot(_armWrestlingSounds[index]);
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

    private static Vector2 FromOriginalCanvas(float x, float y)
    {
        return new Vector2(x, CanvasHeight - y);
    }

    private static Sprite LoadSprite(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
        }
        return null;
    }

    private static Sprite GetFallbackButtonSprite()
    {
        if (_fallbackButtonSprite != null) return _fallbackButtonSprite;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();

        _fallbackButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        _fallbackButtonSprite.hideFlags = HideFlags.HideAndDontSave;
        _fallbackButtonSprite.name = "ArmButtonFallback";
        return _fallbackButtonSprite;
    }
}
