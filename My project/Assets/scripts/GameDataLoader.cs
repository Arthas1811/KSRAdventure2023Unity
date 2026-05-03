using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameDataLoader
{
    [Serializable]
    private class PositionData
    {
        public float x;
        public float y;
    }

    [Serializable]
    private class LocationData
    {
        public string room;
        public PositionData pos;
    }

    [Serializable]
    private class MinigameProgress
    {
        public string game;
        public bool completed;
        public int tries;
    }

    [Serializable]
    private class ProgressData
    {
        public MinigameProgress[] minigames;
    }

    [Serializable]
    private class GameData
    {
        public LocationData location;
        public ProgressData progress;
    }

    private const string ResourcePath = "gameData";
    private static GameData _cachedGameData;
    private static List<MinigameProgressSnapshot> _defaultProgress;
    public static event System.Action ProgressChanged;

    public static bool TryGetStartRoom(out string roomKey)
    {
        roomKey = null;

        if (!LoadGameDataIfNeeded())
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_cachedGameData?.location?.room))
        {
            roomKey = _cachedGameData.location.room;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves the progress data for a specific minigame.
    /// </summary>
    /// <param name="minigameID">The minigame identifier (e.g., "armdrugge", "physics", "biology")</param>
    /// <param name="completed">Output: Whether the minigame has been completed</param>
    /// <param name="tries">Output: The number of times the minigame has been attempted</param>
    /// <returns>True if progress data was found, false otherwise</returns>
    public static bool TryGetMinigameProgress(string minigameID, out bool completed, out int tries)
    {
        completed = false;
        tries = 0;

        if (!LoadGameDataIfNeeded())
        {
            return false;
        }

        if (_cachedGameData?.progress?.minigames == null)
        {
            return false;
        }

        // Normalize the minigame ID for comparison (case-insensitive, handle common variations)
        string normalizedID = NormalizeMinigameID(minigameID);

        foreach (var minigame in _cachedGameData.progress.minigames)
        {
            if (string.Equals(NormalizeMinigameID(minigame.game), normalizedID, StringComparison.OrdinalIgnoreCase))
            {
                completed = minigame.completed;
                tries = minigame.tries;
                return true;
            }
        }

        // Minigame not found in progress data
        Debug.LogWarning($"GameDataLoader: Minigame '{minigameID}' not found in progress data.");
        return false;
    }

    /// <summary>
    /// Updates the progress data for a specific minigame.
    /// Note: This updates the in-memory cache only. To persist changes, SaveGameData() must be called.
    /// </summary>
    /// <param name="minigameID">The minigame identifier</param>
    /// <param name="completed">Whether the minigame has been completed</param>
    /// <param name="incrementTries">If true, increments the tries counter</param>
    public static void UpdateMinigameProgress(string minigameID, bool completed, bool incrementTries = false)
    {
        if (!LoadGameDataIfNeeded())
        {
            Debug.LogWarning("GameDataLoader: Cannot update minigame progress, game data not loaded.");
            return;
        }

        if (_cachedGameData?.progress?.minigames == null)
        {
            Debug.LogWarning("GameDataLoader: No progress data structure found.");
            return;
        }

        string normalizedID = NormalizeMinigameID(minigameID);

        foreach (var minigame in _cachedGameData.progress.minigames)
        {
            if (string.Equals(NormalizeMinigameID(minigame.game), normalizedID, StringComparison.OrdinalIgnoreCase))
            {
                minigame.completed = completed;
                if (incrementTries)
                {
                    minigame.tries++;
                }
                Debug.Log($"GameDataLoader: Updated progress for '{minigameID}' - Completed: {completed}, Tries: {minigame.tries}");
                ProgressChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning($"GameDataLoader: Minigame '{minigameID}' not found in progress data, cannot update.");
    }

    public static IEnumerable<MinigameProgressSnapshot> GetProgressSnapshot()
    {
        if (!LoadGameDataIfNeeded() || _cachedGameData?.progress?.minigames == null)
        {
            yield break;
        }

        foreach (var m in _cachedGameData.progress.minigames)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.game)) continue;
            yield return new MinigameProgressSnapshot
            {
                id = m.game,
                completed = m.completed,
                tries = m.tries
            };
        }
    }

    public static void ApplyProgressSnapshot(IEnumerable<MinigameProgressSnapshot> items)
    {
        if (!LoadGameDataIfNeeded() || _cachedGameData?.progress?.minigames == null || items == null)
        {
            return;
        }

        var map = new Dictionary<string, MinigameProgressSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
            map[item.id.Trim()] = item;
        }

        var changed = false;
        foreach (var m in _cachedGameData.progress.minigames)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.game)) continue;
            if (map.TryGetValue(m.game, out var snap))
            {
                m.completed = snap.completed;
                m.tries = snap.tries;
                changed = true;
            }
        }

        if (changed)
        {
            ProgressChanged?.Invoke();
        }
    }

    public class MinigameProgressSnapshot
    {
        public string id;
        public bool completed;
        public int tries;
    }

    public static void ResetProgressToDefaults()
    {
        if (!LoadGameDataIfNeeded() || _cachedGameData?.progress?.minigames == null)
        {
            return;
        }

        EnsureDefaultProgressCached();
        ApplyProgressSnapshot(_defaultProgress);
    }

    private static void EnsureDefaultProgressCached()
    {
        if (_defaultProgress != null && _defaultProgress.Count > 0)
        {
            return;
        }

        if (_cachedGameData?.progress?.minigames == null)
        {
            return;
        }

        _defaultProgress = new List<MinigameProgressSnapshot>();
        foreach (var m in _cachedGameData.progress.minigames)
        {
            if (m == null || string.IsNullOrWhiteSpace(m.game)) continue;
            _defaultProgress.Add(new MinigameProgressSnapshot
            {
                id = m.game,
                completed = m.completed,
                tries = m.tries
            });
        }
    }

    /// <summary>
    /// Normalizes minigame IDs to handle common variations.
    /// Examples: "Bio" -> "biology", "Ph" -> "physics", "Armdrugge" -> "armdrugge"
    /// </summary>
    private static string NormalizeMinigameID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return string.Empty;
        }

        string normalized = id.Trim().ToLowerInvariant();

        // Map common abbreviations to their full names as they appear in gameData.json
        switch (normalized)
        {
            case "ph": return "physics";
            case "bio": return "biology";
            case "geo": return "geography";
            case "bg": return "bg";
            case "bgdialogue": return "bg";
            case "dimitri": return "bossfight";
            case "armdrugge": return "armdrugge";
            case "jacket": return "jacket";
            case "piano": return "piano";
            default: return normalized;
        }
    }

    /// <summary>
    /// Loads the game data from Resources if not already cached.
    /// </summary>
    private static bool LoadGameDataIfNeeded()
    {
        if (_cachedGameData != null)
        {
            return true;
        }

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"GameDataLoader: Failed to load resource '{ResourcePath}'.");
            return false;
        }

        try
        {
            _cachedGameData = JsonUtility.FromJson<GameData>(asset.text);
            if (_cachedGameData != null)
            {
                EnsureDefaultProgressCached();
            }
            return _cachedGameData != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"GameDataLoader: Failed to parse game data from {ResourcePath}.json. {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clears the cached game data, forcing a reload on next access.
    /// </summary>
    public static void ClearCache()
    {
        _cachedGameData = null;
    }
}
