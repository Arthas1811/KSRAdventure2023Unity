using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Framework.Minigames;

public class LockpickingGame : MonoBehaviour
{
    private const int StartingTries = 10;
    private const int DefaultMaxClicks = 80;
    private const float SourceImageWidth = 6000f;
    private const float SourceImageHeight = 4000f;
    private const float DialCenterX = 3578f;
    private const float DialSideNumberOffset = 165f;
    private const float DialHitWidth = 440f;
    private const float DialHitHeight = 150f;
    private const float MainDigitHeight = 188f;
    private const float SideDigitHeight = 132f;
    private const float DialTextYOffset = -27.5f;
    private static readonly int[] CorrectCode = { 1, 9, 6, 8 };

    private int clicks = 0;
    public int MaxClicks = DefaultMaxClicks;
    public TextMeshProUGUI counter;
    private int tries = StartingTries;
    public TextMeshProUGUI triedcounter;

    public NumberSelector dial1;
    public NumberSelector dial2;
    public NumberSelector dial3;
    public NumberSelector dial4;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button addCounterButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float finishDelay = 0.65f;

    private bool lockBreakingEnabled;
    private bool finished;
    private bool showingBack;
    private bool returnPending;
    private Sprite closedSprite;
    private Sprite backSprite;
    private Sprite openSprite;
    private AudioSource audioSource;
    private AudioClip punchSound;
    private AudioClip rageMusic;
    private Coroutine shakeRoutine;
    private RectTransform activeShakeTarget;
    private Vector2 activeShakeOrigin;
    private bool layoutReady;

    private void Awake()
    {
        dial1 = dial1 ?? FindDial("dial_1", "dial1", "Dial_1", "Dial1");
        dial2 = dial2 ?? FindDial("dial_2", "dial2", "Dial_2", "Dial2");
        dial3 = dial3 ?? FindDial("dial_3", "dial3", "Dial_3", "Dial3");
        dial4 = dial4 ?? FindDial("dial_4", "dial4", "Dial_4", "Dial4");

        counter = counter ?? FindText("countertext", "counter", "CounterText", "Counter");
        triedcounter = triedcounter ?? FindText("Tried", "TriedCounter", "TriedText", "tries");

        confirmButton = confirmButton ?? FindButton("ConfrmButton", "confrmButton", "confirmButton", "ConfirmButton", "confirm");
        addCounterButton = addCounterButton ?? FindButton("button", "Button", "addCounterButton", "AddCounterButton");

        backgroundImage = backgroundImage ?? FindImage("Background", "BackgroundImage", "Background image", "background");
        statusText = statusText ?? FindText("StatusText", "LockpickingStatus", "Status");
        if (MaxClicks <= 0)
        {
            MaxClicks = DefaultMaxClicks;
        }

        LoadAssets();
        SetupAudio();
        ConfigureBackground();
        DisableLegacyNumberOverlay();
        EnsureStatusText();

        WireDialButton(dial1);
        WireDialButton(dial2);
        WireDialButton(dial3);
        WireDialButton(dial4);

        WireButton(confirmButton, confirm);
        WireButton(addCounterButton, addCounter);

        SetButtonVisible(addCounterButton, false);
        SetButtonInteractable(addCounterButton, false);
        layoutReady = true;
        ApplyResponsiveLayout();
        UpdateAttemptsText();
        UpdatePunchCounter();
        ShowFrontView();
        SetStatus("Locked");
    }

    private void Start()
    {
        ApplyResponsiveLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (layoutReady)
        {
            ApplyResponsiveLayout();
        }
    }

    public void confirm()
    {
        if (finished) return;
        if (showingBack)
        {
            ShowFrontView();
            return;
        }

        if (dial1 == null || dial2 == null || dial3 == null || dial4 == null)
        {
            Debug.LogWarning("LockpickingGame: Dial references missing.");
            return;
        }

        if (IsCorrectCode())
        {
            finish(true);
            return;
        }

        tries = Mathf.Max(tries - 1, 0);
        UpdateAttemptsText();

        if (tries == 0)
        {
            EnableLockBreaking();
            return;
        }

        SetStatus("Wrong code");
        Shake(confirmButton != null ? confirmButton.transform as RectTransform : null, 0.16f, 7f);
    }

    public void addCounter()
    {
        if (finished || !lockBreakingEnabled) return;
        clicks += 1;
        UpdatePunchCounter();
        PlayPunchSound();
        Shake(backgroundImage != null ? backgroundImage.rectTransform : null, 0.08f, 5f);

        if (clicks >= MaxClicks)
        {
            finish(false);
        }
    }

    void finish(bool openedByCode)
    {
        if (finished) return;
        finished = true;
        StopRageMusic();
        if (backgroundImage != null && openSprite != null)
        {
            backgroundImage.sprite = openSprite;
        }
        SetStatus(openedByCode ? "Unlocked" : "Opened");
        SetButtonInteractable(confirmButton, false);
        SetButtonInteractable(addCounterButton, false);
        SetButtonVisible(addCounterButton, false);
        SetCodeControlsVisible(false);
        var gameWon = openedByCode || lockBreakingEnabled;
        if (!returnPending)
        {
            returnPending = true;
            StartCoroutine(ReturnAfterReveal(gameWon));
        }
    }

    private void EnableLockBreaking()
    {
        if (lockBreakingEnabled) return;
        lockBreakingEnabled = true;
        showingBack = false;
        clicks = 0;
        if (backgroundImage != null && closedSprite != null) backgroundImage.sprite = closedSprite;
        SetStatus("Rage mode");
        UpdatePunchCounter();
        SetCodeControlsVisible(false);
        SetButtonVisible(addCounterButton, true);
        SetButtonInteractable(addCounterButton, true);
        if (addCounterButton != null)
        {
            addCounterButton.transform.SetAsLastSibling();
        }
        if (counter != null) counter.gameObject.SetActive(true);
        LayoutRageControls();
        PlayRageMusic();
        Shake(backgroundImage != null ? backgroundImage.rectTransform : null, 0.28f, 8f);
    }

    private void LoadAssets()
    {
        closedSprite = LoadSprite(
            "images/Minigames/Lockpicking/LockerVonVorne",
            "Images/Minigames/Lockpicking/LockerVonVorne");
        backSprite = LoadSprite(
            "images/Minigames/Lockpicking/LockerVonHinten",
            "Images/Minigames/Lockpicking/LockerVonHinten");
        openSprite = LoadSprite(
            "images/Minigames/Lockpicking/GeoeffneterLocker",
            "Images/Minigames/Lockpicking/GeoeffneterLocker");
        punchSound = Resources.Load<AudioClip>("Audio/Minigames/Lockpicking/punch")
            ?? Resources.Load<AudioClip>("Audio/Minigames/BossFight/punch");
        rageMusic = Resources.Load<AudioClip>("Audio/Minigames/Lockpicking/lock_smasher");
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void ConfigureBackground()
    {
        if (backgroundImage == null) return;

        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = true;
        backgroundImage.raycastTarget = true;
        if (closedSprite != null) backgroundImage.sprite = closedSprite;

        var trigger = backgroundImage.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = backgroundImage.gameObject.AddComponent<EventTrigger>();
        }

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(OnBackgroundClicked);
        trigger.triggers.Add(entry);
    }

    private void ApplyResponsiveLayout()
    {
        ResizeBackgroundToCoverScreen();
        LayoutAttempts();
        LayoutStatus();
        LayoutConfirmButton();
        LayoutRageControls();
        DisableLegacyNumberOverlay();

        if (!ShouldShowCodeControls())
        {
            HideCodeControlSurfaces();
            return;
        }

        LayoutCodeDials();
    }

    private void LayoutCodeDials()
    {
        LayoutDial(dial1, DialCenterX / SourceImageWidth, (2220f + DialTextYOffset) / SourceImageHeight);
        LayoutDial(dial2, DialCenterX / SourceImageWidth, (2520f + DialTextYOffset) / SourceImageHeight);
        LayoutDial(dial3, DialCenterX / SourceImageWidth, (2820f + DialTextYOffset) / SourceImageHeight);
        LayoutDial(dial4, DialCenterX / SourceImageWidth, (3125f + DialTextYOffset) / SourceImageHeight);
    }

    private void ResizeBackgroundToCoverScreen()
    {
        if (backgroundImage == null) return;

        var canvas = backgroundImage.canvas;
        var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        var targetSize = canvasRect != null ? canvasRect.rect.size : new Vector2(Screen.width, Screen.height);
        if (targetSize.x <= 0f || targetSize.y <= 0f) return;

        var sprite = backgroundImage.sprite;
        var spriteAspect = sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : targetSize.x / targetSize.y;
        var targetAspect = targetSize.x / targetSize.y;

        var size = targetAspect > spriteAspect
            ? new Vector2(targetSize.x, targetSize.x / spriteAspect)
            : new Vector2(targetSize.y * spriteAspect, targetSize.y);

        var rect = backgroundImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        backgroundImage.preserveAspect = true;
    }

    private void LayoutAttempts()
    {
        if (triedcounter == null) return;

        var textRect = triedcounter.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        textRect.localScale = Vector3.one;

        triedcounter.alignment = TextAlignmentOptions.Center;
        triedcounter.fontSize = 32f;
        triedcounter.fontSizeMax = 32f;
        triedcounter.fontSizeMin = 22f;
        triedcounter.enableAutoSizing = true;
        triedcounter.color = Color.white;
        triedcounter.raycastTarget = false;

        var panel = triedcounter.transform.parent;
        if (panel == null) return;

        var panelRect = panel as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(18f, -18f);
            panelRect.sizeDelta = new Vector2(224f, 58f);
            panelRect.localScale = Vector3.one;
        }

        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            StyleLikeConfirmPanel(panelImage, new Color(0.52f, 0.02f, 0.02f, 0.82f), false);
        }
    }

    private void LayoutStatus()
    {
        if (statusText == null) return;

        var rect = statusText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -22f);
        rect.sizeDelta = new Vector2(300f, 44f);
        rect.localScale = Vector3.one;
        statusText.fontSize = 30f;
        statusText.fontSizeMax = 30f;
        statusText.fontSizeMin = 18f;
        statusText.enableAutoSizing = true;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
        EnsureTextBackdrop(statusText, "StatusBackdrop", new Color(0.08f, 0.08f, 0.08f, 0.42f), new Vector2(22f, 8f));
    }

    private void LayoutConfirmButton()
    {
        if (confirmButton == null) return;

        var rect = confirmButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 22f);
            rect.sizeDelta = new Vector2(164f, 58f);
            rect.localScale = Vector3.one;
        }

        SetButtonText(confirmButton, "Confirm");
        var image = confirmButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.07f, 0.62f, 0f, 0.58f);
            StyleLikeConfirmPanel(image, image.color, false);
        }
    }

    private void LayoutRageControls()
    {
        if (counter != null)
        {
            var counterRect = counter.rectTransform;
            counterRect.anchorMin = new Vector2(0.5f, 1f);
            counterRect.anchorMax = new Vector2(0.5f, 1f);
            counterRect.pivot = new Vector2(0.5f, 1f);
            counterRect.anchoredPosition = new Vector2(0f, -66f);
            counterRect.sizeDelta = new Vector2(260f, 42f);
            counterRect.localScale = Vector3.one;
            counter.alignment = TextAlignmentOptions.Center;
            counter.fontSize = 28f;
            counter.fontSizeMax = 28f;
            counter.fontSizeMin = 16f;
            counter.enableAutoSizing = true;
            counter.color = Color.white;
            counter.raycastTarget = false;
            EnsureTextBackdrop(counter, "RageCounterBackdrop", new Color(0.08f, 0.08f, 0.08f, 0.42f), new Vector2(22f, 8f));
        }

        if (addCounterButton == null) return;

        var rect = addCounterButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.6f, 0.34f);
            rect.anchorMax = new Vector2(0.6f, 0.34f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 360f);
            rect.localScale = Vector3.one;
        }

        var image = addCounterButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }
    }

    private void LayoutDial(NumberSelector dial, float imageX, float imageYFromTop)
    {
        if (dial == null) return;

        var backgroundRect = backgroundImage != null ? backgroundImage.rectTransform : null;
        var backgroundSize = backgroundRect != null ? backgroundRect.rect.size : Vector2.zero;
        if (backgroundSize.x <= 0f || backgroundSize.y <= 0f)
        {
            backgroundSize = new Vector2(Screen.width, Screen.height);
        }

        var position = new Vector2(
            (imageX - 0.5f) * backgroundSize.x,
            (0.5f - imageYFromTop) * backgroundSize.y);

        var rowWidth = Mathf.Clamp(backgroundSize.x * (DialHitWidth / SourceImageWidth), 72f, 100f);
        var rowHeight = Mathf.Clamp(backgroundSize.y * (DialHitHeight / SourceImageHeight), 28f, 40f);
        var mainFontSize = Mathf.Clamp(backgroundSize.y * (MainDigitHeight / SourceImageHeight), 23f, 31f);
        var sideFontSize = Mathf.Clamp(backgroundSize.y * (SideDigitHeight / SourceImageHeight), 17f, 23f);
        var sideOffset = Mathf.Clamp(backgroundSize.x * (DialSideNumberOffset / SourceImageWidth), 18f, 28f);

        var rect = dial.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(rowWidth, rowHeight);
            rect.localScale = Vector3.one;
        }

        dial.ConfigureVisuals(
            sideOffset,
            mainFontSize,
            sideFontSize,
            new Color(0.08f, 0.08f, 0.08f, 0.92f),
            new Color(0.08f, 0.08f, 0.08f, 0.52f));
    }

    private void EnsureStatusText()
    {
        if (statusText != null) return;

        var canvas = backgroundImage != null ? backgroundImage.canvas : FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("LockpickingStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -36f);
        rect.sizeDelta = new Vector2(360f, 48f);

        statusText = go.GetComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 28f;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 16f;
        statusText.fontSizeMax = 28f;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
        if (triedcounter != null)
        {
            statusText.font = triedcounter.font;
            statusText.fontSharedMaterial = triedcounter.fontSharedMaterial;
        }
    }

    private void StyleLikeConfirmPanel(Image image, Color color, bool addShadow)
    {
        if (image == null) return;

        var confirmImage = confirmButton != null ? confirmButton.GetComponent<Image>() : null;
        if (confirmImage != null && confirmImage.sprite != null)
        {
            image.sprite = confirmImage.sprite;
            image.type = confirmImage.type;
            image.material = confirmImage.material;
        }
        else
        {
            image.type = Image.Type.Sliced;
        }

        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = false;

        var shadow = image.GetComponent<Shadow>();
        if (addShadow)
        {
            if (shadow == null)
            {
                shadow = image.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
        }
        else if (shadow != null)
        {
            shadow.enabled = false;
        }
    }

    private void EnsureTextBackdrop(TextMeshProUGUI text, string objectName, Color color, Vector2 padding)
    {
        if (text == null || text.transform.parent == null) return;

        var parent = text.transform.parent;
        var backdrop = parent.Find(objectName) as RectTransform;
        if (backdrop == null)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            backdrop = go.GetComponent<RectTransform>();
        }

        var textRect = text.rectTransform;
        backdrop.anchorMin = textRect.anchorMin;
        backdrop.anchorMax = textRect.anchorMax;
        backdrop.pivot = textRect.pivot;
        backdrop.anchoredPosition = textRect.anchoredPosition;
        backdrop.sizeDelta = textRect.sizeDelta + padding;
        backdrop.localScale = Vector3.one;
        backdrop.localRotation = Quaternion.identity;

        var image = backdrop.GetComponent<Image>();
        StyleLikeConfirmPanel(image, color, false);

        backdrop.gameObject.SetActive(text.gameObject.activeSelf && text.enabled);
        var textSiblingIndex = text.transform.GetSiblingIndex();
        backdrop.SetSiblingIndex(textSiblingIndex);
        text.transform.SetSiblingIndex(backdrop.GetSiblingIndex() + 1);
    }

    private void OnBackgroundClicked(BaseEventData eventData)
    {
        if (finished || lockBreakingEnabled || backSprite == null) return;
        if (eventData is PointerEventData pointerData && pointerData.dragging) return;

        showingBack = !showingBack;
        ApplyLockerView();
    }

    private void ShowFrontView()
    {
        showingBack = false;
        ApplyLockerView();
    }

    private void ApplyLockerView()
    {
        if (backgroundImage != null)
        {
            var sprite = showingBack && backSprite != null ? backSprite : closedSprite;
            if (sprite != null) backgroundImage.sprite = sprite;
        }

        var showCodeControls = !finished && !lockBreakingEnabled && !showingBack;
        SetCodeControlsVisible(showCodeControls);
        if (showCodeControls)
        {
            LayoutCodeDials();
        }
        else
        {
            HideCodeControlSurfaces();
        }

        if (showCodeControls)
        {
            SetStatus("Locked");
        }
        else if (showingBack)
        {
            SetStatus("Back side");
        }
    }

    private bool IsCorrectCode()
    {
        return dial1.currentNumber == CorrectCode[0]
            && dial2.currentNumber == CorrectCode[1]
            && dial3.currentNumber == CorrectCode[2]
            && dial4.currentNumber == CorrectCode[3];
    }

    private IEnumerator ReturnAfterReveal(bool gameWon)
    {
        if (finishDelay > 0f)
        {
            yield return new WaitForSeconds(finishDelay);
        }

        MinigameReturnState.SetResult(true, gameWon);
    }

    private void UpdateAttemptsText()
    {
        if (triedcounter != null)
        {
            triedcounter.text = "Tries: " + tries.ToString();
        }
    }

    private void UpdatePunchCounter()
    {
        if (counter != null)
        {
            counter.text = "Rage: " + clicks + "/" + MaxClicks;
        }
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void PlayPunchSound()
    {
        if (audioSource != null && punchSound != null)
        {
            audioSource.PlayOneShot(punchSound);
        }
    }

    private void PlayRageMusic()
    {
        if (audioSource == null || rageMusic == null) return;

        audioSource.Stop();
        audioSource.clip = rageMusic;
        audioSource.loop = true;
        audioSource.volume = 0.55f;
        audioSource.Play();
    }

    private void StopRageMusic()
    {
        if (audioSource != null && audioSource.clip == rageMusic)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    private void Shake(RectTransform target, float duration, float distance)
    {
        if (target == null) return;
        if (shakeRoutine != null)
        {
            if (activeShakeTarget != null)
            {
                activeShakeTarget.anchoredPosition = activeShakeOrigin;
            }

            StopCoroutine(shakeRoutine);
        }

        activeShakeTarget = target;
        activeShakeOrigin = target.anchoredPosition;
        shakeRoutine = StartCoroutine(ShakeRoutine(target, activeShakeOrigin, duration, distance));
    }

    private IEnumerator ShakeRoutine(RectTransform target, Vector2 original, float duration, float distance)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var damp = 1f - Mathf.Clamp01(elapsed / duration);
            var offset = Mathf.Sin(elapsed * 75f) * distance * damp;
            target.anchoredPosition = original + new Vector2(offset, 0f);
            yield return null;
        }

        target.anchoredPosition = original;
        shakeRoutine = null;
        activeShakeTarget = null;
    }

    private static NumberSelector FindDial(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var dial = go.GetComponent<NumberSelector>();
            if (dial != null) return dial;
        }
        return null;
    }

    private static TextMeshProUGUI FindText(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (text != null) return text;
            text = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) return text;
        }
        return null;
    }

    private static Button FindButton(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var button = go.GetComponent<Button>();
            if (button != null) return button;
        }
        return null;
    }

    private static Image FindImage(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var go = GameObject.Find(name);
            if (go == null) continue;
            var image = go.GetComponent<Image>();
            if (image != null) return image;
        }
        return null;
    }

    private static void WireDialButton(NumberSelector dial)
    {
        if (dial == null) return;
        var button = dial.GetComponent<Button>();
        if (button == null) return;
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(dial.clickNumber);
        }
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void SetButtonInteractable(Button button, bool enabled)
    {
        if (button == null) return;
        button.interactable = enabled;
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button == null) return;
        button.gameObject.SetActive(visible);
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = Mathf.Max(24f, label.fontSize);
        }
    }

    private void SetCodeControlsVisible(bool visible)
    {
        SetDialInteractable(dial1, visible);
        SetDialInteractable(dial2, visible);
        SetDialInteractable(dial3, visible);
        SetDialInteractable(dial4, visible);
        SetDialVisible(dial1, visible);
        SetDialVisible(dial2, visible);
        SetDialVisible(dial3, visible);
        SetDialVisible(dial4, visible);
        SetButtonVisible(confirmButton, visible);
    }

    private static void SetDialVisible(NumberSelector dial, bool visible)
    {
        if (dial == null) return;
        dial.SetOverlayVisible(visible);
        dial.gameObject.SetActive(visible);
    }

    private static void SetDialInteractable(NumberSelector dial, bool enabled)
    {
        if (dial == null) return;
        dial.SetInteractable(enabled);
    }

    private bool ShouldShowCodeControls()
    {
        return !finished && !lockBreakingEnabled && !showingBack;
    }

    private void HideCodeControlSurfaces()
    {
        DisableLegacyNumberOverlay();
        HideDialSurface(dial1);
        HideDialSurface(dial2);
        HideDialSurface(dial3);
        HideDialSurface(dial4);
    }

    private static void HideDialSurface(NumberSelector dial)
    {
        if (dial == null) return;
        dial.SetOverlayVisible(false);
    }

    private static void DisableLegacyNumberOverlay()
    {
        var overlay = GameObject.Find("Overlay");
        if (overlay == null) return;

        var graphic = overlay.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 0f);
            graphic.raycastTarget = false;
            graphic.enabled = false;
        }

        overlay.SetActive(false);
    }

    private static Sprite LoadSprite(params string[] paths)
    {
        foreach (var path in paths)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0) return sprites[0];

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) continue;

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
        }

        return null;
    }
}
