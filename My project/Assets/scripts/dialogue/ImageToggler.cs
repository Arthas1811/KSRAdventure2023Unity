using UnityEngine;
using UnityEngine.UI;

public class ImageToggler : MonoBehaviour
{
    [Header("UI References")]
    public Image buttonImage;    // The Image component we want to swap
    public Sprite firstSprite;   // Starting sprite
    public Sprite secondSprite;  // Alternative sprite

    private bool isFirstImage = true;

    // This runs as soon as the game starts
    private void Start()
    {
        // Make sure we have a sprite assigned and set the initial state
        if (buttonImage != null && firstSprite != null)
        {
            buttonImage.sprite = firstSprite;
        }
    }

    public void ToggleImage()
    {
        // Safety check to avoid errors if references are missing
        if (buttonImage == null || firstSprite == null || secondSprite == null)
            return;

        if (isFirstImage)
        {
            buttonImage.sprite = secondSprite;
        }
        else
        {
            buttonImage.sprite = firstSprite;
        }

        // Flip the state
        isFirstImage = !isFirstImage;
    }
}