using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class NavigationStatePersistence
{
    private const string GameStateFilePathSaveKey = "Navigation.GameStateFilePath";
    private const string DefaultGameStateFileName = "gamestates.json";

    [Serializable]
    private class GameStateFileData
    {
        public int redBull;
        public string startSlideKey;
        public List<GameStateFileFlag> flags = new List<GameStateFileFlag>();
        public List<GameStateFileItem> inventory = new List<GameStateFileItem>();
        public List<GameStateFileProgress> progress = new List<GameStateFileProgress>();
    }

    [Serializable]
    private class GameStateFileFlag
    {
        public string key;
        public bool value;
    }

    [Serializable]
    private class GameStateFileItem
    {
        public string id;
        public int quantity;
    }

    [Serializable]
    private class GameStateFileProgress
    {
        public string id;
        public bool completed;
        public int tries;
    }

    public static bool PersistInventoryAndProgress()
    {
        try
        {
            var path = ResolveConfiguredGameStatePath();
            if (string.IsNullOrWhiteSpace(path))
            {
                path = BuildDefaultGameStatePath();
            }

            var data = LoadExistingGameStateFile(path);
            if (data == null)
            {
                data = new GameStateFileData();
            }

            UpdateInventory(data);
            UpdateProgress(data);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"NavigationStatePersistence: Failed to persist inventory/progress to game state file. {ex.Message}");
            return false;
        }
    }

    private static GameStateFileData LoadExistingGameStateFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<GameStateFileData>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void UpdateInventory(GameStateFileData data)
    {
        if (data == null) return;

        data.inventory.Clear();
        var inventoryState = InventoryState.Instance;
        if (inventoryState == null) return;

        foreach (var entry in inventoryState.GetSnapshot().OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Value <= 0) continue;
            data.inventory.Add(new GameStateFileItem { id = entry.Key, quantity = entry.Value });
        }
    }

    private static void UpdateProgress(GameStateFileData data)
    {
        if (data == null) return;

        data.progress.Clear();
        foreach (var progress in GameDataLoader.GetProgressSnapshot())
        {
            data.progress.Add(new GameStateFileProgress
            {
                id = progress.id,
                completed = progress.completed,
                tries = progress.tries
            });
        }
    }

    private static string ResolveConfiguredGameStatePath()
    {
        var configured = PlayerPrefs.GetString(GameStateFilePathSaveKey, string.Empty);
        return EnsureJsonPath(configured);
    }

    private static string BuildDefaultGameStatePath()
    {
        return EnsureJsonPath(DefaultGameStateFileName);
    }

    private static string EnsureJsonPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim();
        if (!Path.IsPathRooted(normalized))
        {
            normalized = Path.Combine(Application.persistentDataPath, normalized);
        }

        if (!normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            normalized += ".json";
        }

        return normalized;
    }
}
