using UnityEngine;
using UnityEngine.InputSystem;

public class clickable : MonoBehaviour
{
    private Camera mainCamera;

    // Drag the ProgressBar GameObject here in Inspector
    public ProgressBarClass progressBar; // renamed for clarity

    private void Awake()
    {
        if (progressBar == null)
        {
            progressBar = FindObjectOfType<ProgressBarClass>();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Check for left mouse click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

            // Check if we clicked this sprite
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.gameObject == gameObject)
            {
                Debug.Log("Sprite clicked!");

                // Add fill to the ProgressBar safely
                if (progressBar != null)
                {
                    progressBar.AddFill(0.4f);
                }
                else
                {
                    Debug.LogWarning("ProgressBar reference is missing!");
                }
            }
        }
    }
}
