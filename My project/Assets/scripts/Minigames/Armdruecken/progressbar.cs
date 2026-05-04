using UnityEngine;
using UnityEngine.UI;

public class ProgressBarClass : MonoBehaviour
{
    private const float DefaultMaxPower = 800f;

    public Image fillImage;
    public float totalPower = DefaultMaxPower;
    public float progress = 0.5f;

    private float _currentPower = DefaultMaxPower * 0.5f;

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
            var sprite = LoadSprite("images/Minigames/Armdruecken/red_bar", "red_bar");
            if (sprite != null) fillImage.sprite = sprite;
        }

        if (totalPower <= 0f)
        {
            totalPower = DefaultMaxPower;
        }

        ConfigureFillImage();
        SetPower(_currentPower);
    }

    public void SetPower(float value)
    {
        _currentPower = Mathf.Clamp(value, 0f, totalPower);
        progress = totalPower <= 0f ? 0f : _currentPower / totalPower;

        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }
    }

    public void AddFill(float value)
    {
        SetPower(_currentPower + value);
    }

    public float GetPower()
    {
        return _currentPower;
    }

    public float GetFill()
    {
        return progress;
    }

    public void SetFillColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color;
        }
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
        fillImage.color = Color.yellow;
    }
}
