using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NumberSelector : MonoBehaviour
{
    public int currentNumber = 0;
    public TextMeshProUGUI numberText;
    [SerializeField] private TextMeshProUGUI previousNumberText;
    [SerializeField] private TextMeshProUGUI nextNumberText;
    [SerializeField] private bool createSideNumbers = true;
    [SerializeField] private float sideNumberOffset = 42f;

    private Button button;
    private InvisibleRaycastGraphic hitAreaGraphic;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void clickNumber()
    {
        SetNumber(currentNumber + 1);
    }

    public void SetNumber(int number)
    {
        EnsureInitialized();
        currentNumber = Wrap(number);
        RefreshNumbers();
    }

    public void SetInteractable(bool enabled)
    {
        EnsureInitialized();
        if (button != null)
        {
            button.interactable = enabled;
        }

        if (hitAreaGraphic != null)
        {
            hitAreaGraphic.raycastTarget = enabled;
            hitAreaGraphic.enabled = enabled;
        }
    }

    public void SetOverlayVisible(bool visible)
    {
        EnsureInitialized();
        SetTextVisible(numberText, visible);
        SetTextVisible(previousNumberText, visible);
        SetTextVisible(nextNumberText, visible);

        if (hitAreaGraphic != null)
        {
            hitAreaGraphic.raycastTarget = visible && button != null && button.interactable;
            hitAreaGraphic.enabled = visible;
        }

        DisableBlockingGraphics();
    }

    public void ConfigureVisuals(float sideOffset, float mainFontSize, float sideFontSize, Color mainColor, Color sideColor)
    {
        EnsureInitialized();
        sideNumberOffset = sideOffset;

        ConfigureText(numberText, mainFontSize, mainColor, 0.8f);
        ConfigureText(previousNumberText, sideFontSize, sideColor, 0.75f);
        ConfigureText(nextNumberText, sideFontSize, sideColor, 0.75f);
        PositionSideNumber(previousNumberText, sideNumberOffset);
        PositionSideNumber(nextNumberText, -sideNumberOffset);
        ConfigureHitArea();

        RefreshNumbers();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        button = GetComponent<Button>();
        var sourceText = numberText;

        if (sourceText == null)
        {
            sourceText = GetComponent<TextMeshProUGUI>();
            if (sourceText == null)
            {
                sourceText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        numberText = FindSideText("CurrentNumber") ?? CreateNumberText("CurrentNumber", sourceText);
        if (sourceText != null && sourceText != numberText)
        {
            sourceText.enabled = false;
            sourceText.raycastTarget = false;
        }

        ConfigureMainNumber();
        EnsureSideNumbers();
        ConfigureHitArea();
        RefreshNumbers();
    }

    private void ConfigureMainNumber()
    {
        if (numberText == null) return;

        numberText.raycastTarget = false;
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.enableAutoSizing = true;
        numberText.fontSizeMin = Mathf.Max(12f, numberText.fontSize * 0.65f);
        numberText.fontSizeMax = Mathf.Max(36f, numberText.fontSize);
    }

    private void EnsureSideNumbers()
    {
        if (!createSideNumbers || numberText == null) return;

        previousNumberText = previousNumberText ?? FindSideText("PreviousNumber");
        nextNumberText = nextNumberText ?? FindSideText("NextNumber");

        if (previousNumberText == null)
        {
            previousNumberText = CreateNumberText("PreviousNumber", numberText);
        }

        if (nextNumberText == null)
        {
            nextNumberText = CreateNumberText("NextNumber", numberText);
        }
    }

    private TextMeshProUGUI FindSideText(string objectName)
    {
        var child = transform.Find(objectName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private TextMeshProUGUI CreateNumberText(string objectName, TextMeshProUGUI template)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(34f, 44f);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.fontSize = template.fontSize;
            text.color = template.color;
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(10f, text.fontSize * 0.65f);
        text.fontSizeMax = text.fontSize;
        return text;
    }

    private void ConfigureHitArea()
    {
        RemoveStaleHitArea();
        hitAreaGraphic = hitAreaGraphic ?? FindHitArea();
        if (hitAreaGraphic == null)
        {
            var go = new GameObject("DialHitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(InvisibleRaycastGraphic));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            hitAreaGraphic = go.GetComponent<InvisibleRaycastGraphic>();
        }

        var hitRect = hitAreaGraphic.rectTransform;
        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.anchoredPosition = Vector2.zero;
        hitRect.sizeDelta = Vector2.zero;
        hitAreaGraphic.color = Color.clear;
        hitAreaGraphic.raycastTarget = true;

        if (button != null)
        {
            button.targetGraphic = hitAreaGraphic;
            button.transition = Selectable.Transition.None;
        }
    }

    private InvisibleRaycastGraphic FindHitArea()
    {
        var child = transform.Find("DialHitArea");
        return child == null ? null : child.GetComponent<InvisibleRaycastGraphic>();
    }

    private void RemoveStaleHitArea()
    {
        for (var index = transform.childCount - 1; index >= 0; index--)
        {
            var child = transform.GetChild(index);
            if (child.name != "DialHitArea" || child.GetComponent<InvisibleRaycastGraphic>() != null) continue;

            DestroyChild(child.gameObject);
        }
    }

    private static void ConfigureText(TextMeshProUGUI text, float fontSize, Color color, float minScale)
    {
        if (text == null) return;

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Max(10f, fontSize * minScale);
        text.enableAutoSizing = true;
        text.color = color;
        text.enabled = color.a > 0.001f;
    }

    private static void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text == null) return;
        text.enabled = visible && text.color.a > 0.001f;
        text.raycastTarget = false;
    }

    private void DisableBlockingGraphics()
    {
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            if (graphic == null || graphic is TextMeshProUGUI || graphic is InvisibleRaycastGraphic) continue;

            graphic.raycastTarget = false;
            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 0f);
            graphic.enabled = false;
        }
    }

    private static void DestroyChild(GameObject child)
    {
        if (Application.isPlaying)
        {
            Destroy(child);
        }
        else
        {
            DestroyImmediate(child);
        }
    }

    private static void PositionSideNumber(TextMeshProUGUI text, float xOffset)
    {
        if (text == null) return;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xOffset, 0f);
    }

    private void RefreshNumbers()
    {
        EnsureInitialized();
        currentNumber = Wrap(currentNumber);

        if (numberText != null)
        {
            numberText.text = currentNumber.ToString();
        }

        if (previousNumberText != null)
        {
            previousNumberText.text = Wrap(currentNumber - 1).ToString();
        }

        if (nextNumberText != null)
        {
            nextNumberText.text = Wrap(currentNumber + 1).ToString();
        }
    }

    private static int Wrap(int number)
    {
        number %= 10;
        return number < 0 ? number + 10 : number;
    }
}

public sealed class InvisibleRaycastGraphic : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}
