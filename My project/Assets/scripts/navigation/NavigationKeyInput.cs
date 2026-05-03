using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public sealed class NavigationKeyInput : MonoBehaviour
{
    private readonly Dictionary<KeyCode, Action> _bindings = new Dictionary<KeyCode, Action>();
    private readonly List<KeyCode> _bindingKeys = new List<KeyCode>();
    private readonly HashSet<KeyCode> _registeredKeys = new HashSet<KeyCode>();

    public void Register(KeyCode key, Action action)
    {
        if (action == null) return;
        _bindings[key] = action;
        if (_registeredKeys.Add(key)) _bindingKeys.Add(key);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (_bindingKeys.Count == 0) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (var i = 0; i < _bindingKeys.Count; i++)
        {
            var keyCode = _bindingKeys[i];
            if (!TryConvertKey(keyCode, out var inputKey)) continue;

            var keyControl = keyboard[inputKey];
            if (keyControl == null || !keyControl.wasPressedThisFrame) continue;

            if (_bindings.TryGetValue(keyCode, out var handler) && handler != null)
            {
                handler.Invoke();
            }

            return;
        }
#else
        if (_bindingKeys.Count == 0 || !Input.anyKeyDown) return;

        for (var i = 0; i < _bindingKeys.Count; i++)
        {
            var key = _bindingKeys[i];
            if (!Input.GetKeyDown(key)) continue;

            if (_bindings.TryGetValue(key, out var handler) && handler != null)
            {
                handler.Invoke();
            }

            return;
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private static bool TryConvertKey(KeyCode keyCode, out Key key)
    {
        switch (keyCode)
        {
            case KeyCode.LeftArrow:
                key = Key.LeftArrow;
                return true;
            case KeyCode.RightArrow:
                key = Key.RightArrow;
                return true;
            case KeyCode.UpArrow:
                key = Key.UpArrow;
                return true;
            case KeyCode.DownArrow:
                key = Key.DownArrow;
                return true;
            case KeyCode.E:
                key = Key.E;
                return true;
            case KeyCode.I:
                key = Key.I;
                return true;
            case KeyCode.Space:
                key = Key.Space;
                return true;
            case KeyCode.Return:
                key = Key.Enter;
                return true;
            case KeyCode.Escape:
                key = Key.Escape;
                return true;
            default:
                key = Key.None;
                return false;
        }
    }
#endif
}
