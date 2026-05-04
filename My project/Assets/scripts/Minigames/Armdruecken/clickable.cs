using UnityEngine;
using UnityEngine.InputSystem;

public class clickable : MonoBehaviour
{
    private Camera _mainCamera;
    private armdruecken _game;

    private void Awake()
    {
        _game = FindGame();
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_mainCamera == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = _mainCamera.ScreenToWorldPoint(mousePos);
        var hit = Physics2D.OverlapPoint(worldPos);

        if (hit != null && hit.gameObject == gameObject)
        {
            if (_game == null)
            {
                _game = FindGame();
            }

            _game?.HandleClickTargetClicked(gameObject);
        }
    }

    private static armdruecken FindGame()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<armdruecken>();
#else
        return FindObjectOfType<armdruecken>();
#endif
    }
}
