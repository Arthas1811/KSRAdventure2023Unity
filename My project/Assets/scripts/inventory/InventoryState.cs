using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryState : MonoBehaviour
{
    public static InventoryState Instance { get; private set; }

    public event Action InventoryChanged;

    private readonly Dictionary<string, int> _receivedItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
#if UNITY_2023_1_OR_NEWER
        var existing = UnityEngine.Object.FindFirstObjectByType<InventoryState>();
#else
        var existing = UnityEngine.Object.FindObjectOfType<InventoryState>();
#endif
        if (existing != null)
        {
            return;
        }

        var container = new GameObject("InventoryState");
        container.AddComponent<InventoryState>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDefaultsFromDatabase();
    }

    public bool ReceiveItem(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0)
        {
            return false;
        }

        var current = GetCount(id);
        var newCount = current + amount;
        _receivedItems[id] = newCount;
        ApplyQuantitiesToDatabase();
        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0)
        {
            return false;
        }

        if (!_receivedItems.TryGetValue(id, out var current) || current <= 0)
            return false;

        var newCount = Mathf.Max(0, current - amount);
        if (newCount == 0)
            _receivedItems.Remove(id);
        else
            _receivedItems[id] = newCount;

        ApplyQuantitiesToDatabase();
        InventoryChanged?.Invoke();

        return true;
    }

    public bool HasItem(string id)
    {
        return !string.IsNullOrEmpty(id) && _receivedItems.TryGetValue(id, out var count) && count > 0;
    }

    public IEnumerable<string> GetReceivedItems()
    {
        return _receivedItems.Keys;
    }

    public IEnumerable<KeyValuePair<string, int>> GetReceivedItemCounts()
    {
        return _receivedItems;
    }

    public Dictionary<string, int> GetSnapshot()
    {
        return new Dictionary<string, int>(_receivedItems, StringComparer.OrdinalIgnoreCase);
    }

    public void LoadSnapshot(Dictionary<string, int> itemCounts, bool notify = true)
    {
        _receivedItems.Clear();
        if (itemCounts != null)
        {
            foreach (var entry in itemCounts)
            {
                if (string.IsNullOrEmpty(entry.Key) || entry.Value <= 0)
                {
                    continue;
                }

                _receivedItems[entry.Key] = entry.Value;
            }
        }

        ApplyQuantitiesToDatabase();
        if (notify)
        {
            InventoryChanged?.Invoke();
        }
    }

    public void ResetToDefaults(bool notify = true)
    {
        var defaults = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        InventoryDatabase.Load();
        foreach (var item in InventoryDatabase.GetAllItems())
        {
            if (item.Value == null || string.IsNullOrEmpty(item.Key))
            {
                continue;
            }

            var defaultQuantity = InventoryDatabase.GetDefaultQuantity(item.Key);
            if (defaultQuantity > 0)
            {
                defaults[item.Key] = defaultQuantity;
            }
        }

        LoadSnapshot(defaults, notify);
    }

    private void LoadDefaultsFromDatabase()
    {
        _receivedItems.Clear();
        InventoryDatabase.Load();
        foreach (var item in InventoryDatabase.GetAllItems())
        {
            if (item.Value != null && !string.IsNullOrEmpty(item.Key))
            {
                var defaultQuantity = InventoryDatabase.GetDefaultQuantity(item.Key);
                if (defaultQuantity > 0)
                {
                    _receivedItems[item.Key] = defaultQuantity;
                }
            }
        }

        ApplyQuantitiesToDatabase();
    }

    private int GetCount(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _receivedItems.TryGetValue(id, out var value) ? value : 0;
    }

    private void ApplyQuantitiesToDatabase()
    {
        InventoryDatabase.Load();
        foreach (var item in InventoryDatabase.GetAllItems())
        {
            if (item.Value == null)
            {
                continue;
            }

            item.Value.quantity = 0;
        }

        foreach (var entry in _receivedItems)
        {
            var data = InventoryDatabase.GetItem(entry.Key);
            if (data != null)
            {
                data.quantity = Mathf.Max(0, entry.Value);
            }
        }
    }
}
