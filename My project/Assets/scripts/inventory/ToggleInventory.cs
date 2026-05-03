using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class InventoryAnimatedToggle : MonoBehaviour
{
    [Header("Assign Inventory Canvas Root")]
    public GameObject inventoryCanvas;
    [SerializeField] private Key toggleKey = Key.E;
    [SerializeField] private bool useInternalInput = true;

    private CanvasGroup canvasGroup;
    private bool isOpen = false;
    private float animationDuration = 0.25f; // seconds

    void Start()
    {
        inventoryCanvas = inventoryCanvas ?? InventoryOverlayBootstrap.GetOverlayInstance() ?? GameObject.Find("InventoryCanvas");
        if (inventoryCanvas == null)
        {
            Debug.LogError("Inventory overlay could not be found or loaded.");
            return;
        }

        canvasGroup = inventoryCanvas.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = inventoryCanvas.AddComponent<CanvasGroup>();

        if (!inventoryCanvas.activeSelf)
            inventoryCanvas.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private Key ActiveToggleKey => toggleKey == Key.None ? Key.I : toggleKey;

    void Update()
    {
        if (!useInternalInput)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        var primaryKey = ActiveToggleKey;
        var pressed = false;

        if (primaryKey != Key.None)
        {
            var keyControl = keyboard[primaryKey];
            pressed = keyControl != null && keyControl.wasPressedThisFrame;
        }

        if (!pressed)
        {
            var fallbackKey = primaryKey == Key.E ? Key.I : Key.E;
            var fallbackControl = keyboard[fallbackKey];
            pressed = fallbackControl != null && fallbackControl.wasPressedThisFrame;
        }

        if (pressed) ToggleInventory();
    }

    public void ToggleInventory()
    {
        SetInventoryVisible(!isOpen);
    }

    public void OpenInventory()
    {
        SetInventoryVisible(true);
    }

    public void CloseInventory()
    {
        SetInventoryVisible(false);
    }

    private void SetInventoryVisible(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;
        StopAllCoroutines();
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);
        if (inventoryCanvas != null && !inventoryCanvas.activeSelf)
            inventoryCanvas.SetActive(true);
        StartCoroutine(AnimateInventory(isOpen));
    }

    private IEnumerator AnimateInventory(bool open)
    {
        if (open && inventoryCanvas != null && !inventoryCanvas.activeSelf)
            inventoryCanvas.SetActive(true);

        float startAlpha = canvasGroup.alpha;
        float endAlpha = open ? 1f : 0f;

        Vector3 startScale = inventoryCanvas.transform.localScale;
        Vector3 endScale = open ? Vector3.one : new Vector3(0.9f, 0.9f, 0.9f);

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = Mathf.SmoothStep(0, 1, t); // smooth curve

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            inventoryCanvas.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        inventoryCanvas.transform.localScale = endScale;

        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;

        if (!open)
        {
            // Keep object active to allow future coroutines; visibility is controlled via CanvasGroup.
            inventoryCanvas.SetActive(true);
        }
    }

    public void DisableInternalInput()
    {
        useInternalInput = false;
    }
}
