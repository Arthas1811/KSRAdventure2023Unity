using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string name;
    public string description;
    public string image;
    public int quantity;
    public Sprite sprite;
}

public static class InventoryDatabase
{
    [Serializable]
    private class GameInventoryItem
    {
        public string id;
        public string name;
        public string description;
        public string image;
        public int quantity;
    }

    [Serializable]
    private class InventoryContainer
    {
        public List<GameInventoryItem> items;
    }

    [Serializable]
    private class GameData
    {
        public InventoryContainer inventory;
    }

    private const string GameDataResourcePath = "gameData";
    private static Dictionary<string, ItemData> _items;
    private static Dictionary<string, int> _defaultQuantities;
    private static Dictionary<string, Sprite> _spriteLookup;
    private static readonly Dictionary<string, string[]> SpriteAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "coffeemug", new[] { "Kaffeetasse", "coffee", "mug" } },
        { "goldkey", new[] { "key", "key1", "gold_key" } },
        { "key2", new[] { "key2" } },
        { "keys", new[] { "keys" } },
        { "surfacecharger", new[] { "SurfaceCharger", "surface_charger" } },
        { "nitro_beaker", new[] { "nitro_beaker" } },
        { "nitrogly_beaker", new[] { "nitrogly_beaker" } },
        { "nitro_pipette", new[] { "nitro_pipette", "pipette" } },
        { "nitric_acid", new[] { "nitric_acid" } },
        { "sulfuric_acid", new[] { "sulfuric_acid", "sulfuric_acid2" } },
        { "glycerin", new[] { "glycerin" } },
        { "pipette", new[] { "pipette" } },
        { "frog", new[] { "frog" } },
        { "redbull", new[] { "redbull", "red-bull" } }
    };

    public static void Load()
    {
        EnsureSpriteLookup();

        if (_items != null)
        {
            RebindMissingSprites();
            return;
        }

        _defaultQuantities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        TextAsset jsonFile = Resources.Load<TextAsset>(GameDataResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not find {GameDataResourcePath}.json in Resources!");
            _items = new Dictionary<string, ItemData>();
            return;
        }

        GameData dataRoot = JsonUtility.FromJson<GameData>(jsonFile.text);
        if (dataRoot?.inventory?.items == null)
        {
            Debug.LogError($"Inventory data missing or malformed in {GameDataResourcePath}.json.");
            _items = new Dictionary<string, ItemData>();
            return;
        }

        _items = new Dictionary<string, ItemData>();
        foreach (var entry in dataRoot.inventory.items)
        {
            if (string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            var itemData = new ItemData
            {
                name = string.IsNullOrWhiteSpace(entry.name) ? entry.id : entry.name,
                description = entry.description,
                image = entry.image,
                quantity = entry.quantity,
                sprite = ResolveSprite(entry.image, entry.id, entry.name)
            };

            if (itemData.sprite == null && !string.IsNullOrEmpty(itemData.image))
            {
                Debug.LogWarning($"No sprite found at Resources/{itemData.image} for item '{entry.id}'.");
            }

            _items[entry.id] = itemData;
            _defaultQuantities[entry.id] = Mathf.Max(0, entry.quantity);
        }

        Debug.Log($"Loaded {_items.Count} items from JSON.");
    }

    public static ItemData GetItem(string id)
    {
        if (_items == null) Load();
        _items.TryGetValue(id, out ItemData data);
        return data;
    }

    public static IEnumerable<KeyValuePair<string, ItemData>> GetAllItems()
    {
        if (_items == null) Load();
        return _items;
    }

    public static int GetDefaultQuantity(string id)
    {
        if (_items == null) Load();
        if (string.IsNullOrWhiteSpace(id) || _defaultQuantities == null)
        {
            return 0;
        }

        return _defaultQuantities.TryGetValue(id, out var value) ? Mathf.Max(0, value) : 0;
    }

    private static void RebindMissingSprites()
    {
        if (_items == null) return;

        foreach (var kvp in _items)
        {
            var data = kvp.Value;
            if (data == null || data.sprite != null) continue;

            data.sprite = ResolveSprite(data.image, kvp.Key, data.name);
        }
    }

    private static void EnsureSpriteLookup()
    {
        if (_spriteLookup != null) return;

        _spriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        AddSprites(Resources.LoadAll<Sprite>("images"));
        AddSprites(Resources.LoadAll<Sprite>("images/UI_Images"));
        AddSprites(Resources.LoadAll<Sprite>("images/inventory_items"));
    }

    private static void AddSprites(IEnumerable<Sprite> sprites)
    {
        if (sprites == null) return;
        foreach (var sprite in sprites)
        {
            if (sprite == null) continue;
            if (!_spriteLookup.ContainsKey(sprite.name))
            {
                _spriteLookup[sprite.name] = sprite;
            }
        }
    }

    private static Sprite ResolveSprite(string imagePath, string id, string name)
    {
        // 1) Explicit path
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            var sprite = Resources.Load<Sprite>(imagePath);
            if (sprite != null) return sprite;

            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            if (!string.IsNullOrWhiteSpace(fileName) && _spriteLookup.TryGetValue(fileName, out var found))
            {
                return found;
            }
        }

        // 2) Match by id or display name against loaded sprites
        if (!string.IsNullOrWhiteSpace(id) && _spriteLookup.TryGetValue(id, out var byId)) return byId;
        if (!string.IsNullOrWhiteSpace(name) && _spriteLookup.TryGetValue(name, out var byName)) return byName;

        // 3) Aliases for mismatched filenames
        if (!string.IsNullOrWhiteSpace(id) && TryAlias(id, out var aliasSprite)) return aliasSprite;
        if (!string.IsNullOrWhiteSpace(name) && TryAlias(name, out var aliasByName)) return aliasByName;

        // 4) Fuzzy match on normalized names (removing separators)
        var normalizedId = NormalizeKey(id);
        var normalizedName = NormalizeKey(name);
        if (TryFuzzy(normalizedId, out var fuzzy)) return fuzzy;
        if (TryFuzzy(normalizedName, out var fuzzyByName)) return fuzzyByName;

        return null;
    }

    private static bool TryAlias(string key, out Sprite sprite)
    {
        sprite = null;
        if (!SpriteAliases.TryGetValue(key, out var aliases) || aliases == null) return false;

        foreach (var alias in aliases)
        {
            if (_spriteLookup.TryGetValue(alias, out var found))
            {
                sprite = found;
                return true;
            }
        }

        return false;
    }

    private static bool TryFuzzy(string key, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(key)) return false;

        foreach (var kvp in _spriteLookup)
        {
            if (NormalizeKey(kvp.Key) == key)
            {
                sprite = kvp.Value;
                return true;
            }
        }

        return false;
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Replace("_", "").Replace("-", "").Trim().ToLowerInvariant();
    }
}
