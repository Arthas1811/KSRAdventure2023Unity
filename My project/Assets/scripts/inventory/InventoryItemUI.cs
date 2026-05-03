using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventoryItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemImage;
    public TMP_Text itemName;
    public TMP_Text itemQuantity;
    [TextArea] public string description;

    public void Setup(ItemData data)
    {
        if (data == null) return;

        var amount = Mathf.Max(1, data.quantity);
        var showQuantity = amount > 0;
        if (itemName != null)
        {
            itemName.text = amount > 1 ? $"{data.name} (x{amount})" : data.name;
            itemName.enableAutoSizing = false;
            itemName.fontSize = 26;
            itemName.gameObject.SetActive(true);
        }
        description = string.IsNullOrEmpty(data.description) ? string.Empty : data.description;
        itemImage.sprite = data.sprite;
        itemImage.preserveAspect = true;
        SetFixedImageSize(itemImage);
        EnsureQuantityReference();

        if (itemQuantity != null)
        {
            itemQuantity.enableAutoSizing = false;
            itemQuantity.fontSize = 36;
            itemQuantity.color = Color.red;
            itemQuantity.alignment = TextAlignmentOptions.BottomRight;
            itemQuantity.text = $"x{amount}";
            itemQuantity.enabled = showQuantity;
            itemQuantity.gameObject.SetActive(showQuantity);
            MoveQuantityOverImage();
        }
    }

    private static void SetFixedImageSize(Image image)
    {
        if (image == null) return;
        var rt = image.rectTransform;
        rt.sizeDelta = new Vector2(128f, 128f);
    }

    private void EnsureQuantityReference()
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;
            if (string.Equals(txt.gameObject.name, "ItemDescription", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(txt.gameObject.name, "ItemQuantity", System.StringComparison.OrdinalIgnoreCase))
            {
                if (itemQuantity == null) itemQuantity = txt;
                // Keep it enabled; it is our count label.
                txt.gameObject.SetActive(true);
                txt.enabled = true;
            }
        }
    }

    private void MoveQuantityOverImage()
    {
        if (itemQuantity == null || itemImage == null) return;

        var qtyRect = itemQuantity.rectTransform;
        if (qtyRect.parent != itemImage.transform)
        {
            qtyRect.SetParent(itemImage.transform, false);
        }

        qtyRect.anchorMin = new Vector2(1f, 0f);
        qtyRect.anchorMax = new Vector2(1f, 0f);
        qtyRect.pivot = new Vector2(1f, 0f);
        qtyRect.sizeDelta = new Vector2(120f, 48f);
        qtyRect.anchoredPosition = new Vector2(-8f, 8f);
        itemQuantity.alignment = TextAlignmentOptions.BottomRight;
    }

    // show hover window
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HoverWindow.Instance != null && !string.IsNullOrEmpty(description))
        {
            HoverWindow.Instance.Show(description);
        }
    }

    // hide hover window
    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoverWindow.Instance != null && !string.IsNullOrEmpty(description))
        {
            HoverWindow.Instance.Hide();
        }
    }
}
