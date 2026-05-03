using UnityEngine;
using TMPro;

public class FluidMovement : MonoBehaviour
{
    [Header("Stretch Settings")]
    public float minStretch = 0.5f;
    public float maxStretch = 1.5f;
    public float animationSpeed = 2f;

    [Header("Mapped Value")]
    public float c;

    [Header("UI")]
    public TextMeshProUGUI cText; // Drag dein Textfeld hier rein im Inspector
    public TextMeshProUGUI winText;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isFrozen = false;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        if (cText != null)
            cText.gameObject.SetActive(false); // Textfeld ausblenden

        winText.gameObject.SetActive(false); // Textfeld ausblenden
    }

    void Update()
    {
        if (!isFrozen)
        {
            float t = Mathf.Sin(Time.time * animationSpeed) * 0.5f + 0.5f;
            float dynamicStretch = Mathf.Lerp(minStretch, maxStretch, t);

            transform.localScale = new Vector3(originalScale.x, originalScale.y * dynamicStretch, originalScale.z);

            float heightOffset = (originalScale.y * (dynamicStretch - 1f)) / 2f;
            transform.position = originalPosition + new Vector3(0f, heightOffset, 0f);

            float normalized = Mathf.InverseLerp(-0.26f, 0f, heightOffset);
            c = Mathf.Lerp(5.7f, 20.5f, normalized);
        }

        // Linksklick: Animation stoppen und Text anzeigen
        if (Input.GetMouseButtonDown(0) && !isFrozen)
        {
            isFrozen = true;
            if (cText != null)
                {
                    cText.gameObject.SetActive(true);
                    cText.color = new Color(cText.color.r, cText.color.g, cText.color.b, 1f); // Alpha auf 1
                    c = Mathf.Round(c * 10f) / 10f; // Rundet auf 1 Nachkommastelle
                    cText.text = c.ToString("F2") + " ml";
                }
            if (winText != null && 11 <= c && c <= 13)
            {
                winText.gameObject.SetActive(true);
                winText.text = "WOW! You (almost) hit the perfect amount";
            }
            else
            {
                winText.gameObject.SetActive(true);
                winText.text = "Sorry you screwed up by too much. Unacceptable.";
            }
            
        }
    }
}