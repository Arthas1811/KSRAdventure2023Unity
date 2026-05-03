using UnityEngine;
using UnityEngine.UI;

public class ProgressBarClass : MonoBehaviour
{
    public Image fillImage;
    public float totalTime = 5f;
    private float currentTime;
    public float progress = 0f;
    private Image _emptyImage;

    private void Awake()
    {
        if (fillImage == null)
        {
            fillImage = FindImageByName("Fill", "FillImage", "Fill Image", "Image", "fill");
            if (fillImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                if (images.Length > 0) fillImage = images[0];
            }
        }
        if (fillImage != null && fillImage.sprite == null)
        {
            var sprite = LoadSprite("images/Minigames/Armdruecken/health_red_0", "health_red_0");
            if (sprite == null)
            {
                sprite = LoadSprite("images/Minigames/Armdruecken/red_bar", "red_bar");
            }
            if (sprite != null) fillImage.sprite = sprite;
        }

        if (totalTime <= 0f || Mathf.Approximately(totalTime, 5f))
        {
            totalTime = 10f;
        }

        if (progress < 0f)
        {
            progress = 0f;
        }

        ConfigureFillImage();
        EnsureEmptyBarVisible();
    }

    void Start()
    {
        currentTime = totalTime/2;
        UpdateFill();
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateFill();
        }
    }

    private void UpdateFill()
    {
        progress = currentTime / totalTime;
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }
    }

    // PUBLIC METHOD TO CHANGE THE BAR
    public void AddFill(float value)
    {
        currentTime = Mathf.Clamp(currentTime + value, 0, totalTime);
        UpdateFill();
    }
    public float GetFill()
    {
        return currentTime / totalTime;
    }

    private Image FindImageByName(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var child = transform.Find(name);
            if (child == null) continue;
            var image = child.GetComponent<Image>();
            if (image != null) return image;
        }
        return null;
    }

    private static Sprite LoadSprite(string primaryPath, string fallbackPath)
    {
        var sprite = Resources.Load<Sprite>(primaryPath);
        return sprite != null ? sprite : Resources.Load<Sprite>(fallbackPath);
    }

    private void ConfigureFillImage()
    {
        if (fillImage == null) return;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
        var color = fillImage.color;
        if (color.a < 1f)
        {
            color.a = 1f;
            fillImage.color = color;
        }
    }

    private void EnsureEmptyBarVisible()
    {
        if (fillImage == null) return;
        var parent = fillImage.transform.parent as RectTransform;
        if (parent == null) return;
        var existing = parent.Find("EmptyBar");
        if (existing != null)
        {
            _emptyImage = existing.GetComponent<Image>();
        }
        if (_emptyImage == null)
        {
            var emptyGo = new GameObject("EmptyBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            emptyGo.transform.SetParent(parent, false);
            _emptyImage = emptyGo.GetComponent<Image>();
            CopyRectTransform(fillImage.rectTransform, _emptyImage.rectTransform);
        }
        if (_emptyImage == null) return;

        _emptyImage.sprite = fillImage.sprite;
        _emptyImage.type = Image.Type.Simple;
        _emptyImage.raycastTarget = false;
        _emptyImage.color = new Color(1f, 1f, 1f, 0.25f);

        var fillIndex = fillImage.transform.GetSiblingIndex();
        _emptyImage.transform.SetSiblingIndex(fillIndex);
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null) return;
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }
}
