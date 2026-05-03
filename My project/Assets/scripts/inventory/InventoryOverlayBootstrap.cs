using UnityEngine;

public static class InventoryOverlayBootstrap
{
    private const string InventoryPrefabPath = "ui/InventoryCanvas";
    private static GameObject _overlayInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        EnsureOverlay();
    }

    public static GameObject GetOverlayInstance()
    {
        return EnsureOverlay();
    }

    private static GameObject EnsureOverlay()
    {
        if (_overlayInstance != null)
            return _overlayInstance;

        GameObject prefab = Resources.Load<GameObject>(InventoryPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Inventory overlay prefab not found at Resources/{InventoryPrefabPath}.");
            return null;
        }

        _overlayInstance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(_overlayInstance);
        var toggle = _overlayInstance.GetComponent<InventoryAnimatedToggle>() ?? _overlayInstance.AddComponent<InventoryAnimatedToggle>();
        if (toggle.inventoryCanvas == null)
        {
            toggle.inventoryCanvas = _overlayInstance;
        }
        return _overlayInstance;
    }
}
