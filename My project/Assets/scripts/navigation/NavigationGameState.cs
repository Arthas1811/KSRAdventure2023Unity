using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.logic
{
    internal sealed class NavigationGameState
    {
        [Serializable]
        private sealed class StateFlag
        {
            public string key;
            public bool value;
        }

        [Serializable]
        private sealed class GameStateSaveData
        {
            public List<StateFlag> flags = new List<StateFlag>();
            public int redBull;
        }

        private const string SaveKey = "Navigation.GameState";
        private readonly Dictionary<string, bool> _flags;
        private readonly Dictionary<string, bool> _defaultFlags;
        private readonly int _defaultRedBull;
        private readonly bool _verbose;
        private bool _hasPersistedState;

        internal int RedBullCount { get; private set; }
        internal bool HasPersistedState => _hasPersistedState;
        internal event Action StateChanged;

        private NavigationGameState(Dictionary<string, bool> defaults, int defaultRedBull, bool verboseLogging)
        {
            _verbose = verboseLogging;
            _defaultFlags = new Dictionary<string, bool>(defaults ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            _flags = new Dictionary<string, bool>(_defaultFlags, StringComparer.OrdinalIgnoreCase);
            _defaultRedBull = Mathf.Max(0, defaultRedBull);
            RedBullCount = _defaultRedBull;
            LoadFromPreferences();
        }

        internal static NavigationGameState CreateFromGameData(bool verboseLogging)
        {
            var defaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var redBull = 0;
            var asset = Resources.Load<TextAsset>("gameData");
            if (asset != null)
            {
                try
                {
                    if (MiniJson.Deserialize(asset.text) is Dictionary<string, object> root &&
                        root.TryGetValue("gameState", out var stateObj) &&
                        stateObj is Dictionary<string, object> stateDict)
                    {
                        if (stateDict.TryGetValue("flags", out var flagsObj) && flagsObj is Dictionary<string, object> flagDict)
                        {
                            foreach (var flag in flagDict)
                            {
                                defaults[flag.Key] = ConvertToBool(flag.Value);
                            }
                        }

                        if (stateDict.TryGetValue("redBull", out var redBullObj))
                        {
                            redBull = ConvertToInt(redBullObj, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning($"NavigationGameState: Failed to parse defaults from gameData.json. {ex.Message}");
                    }
                }
            }

            return new NavigationGameState(defaults, redBull, verboseLogging);
        }

        internal bool GetFlag(string key, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            return _flags.TryGetValue(key, out var value) ? value : fallback;
        }

        internal void SetFlag(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _flags[key] = value;
            SaveState();
        }

        internal void ChangeRedBull(int delta)
        {
            RedBullCount = Mathf.Max(0, RedBullCount + delta);
            SaveState();
        }

        private void LoadFromPreferences()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return;
            }

            _hasPersistedState = true;
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<GameStateSaveData>(json);
                if (data != null)
                {
                    _flags.Clear();
                    if (data.flags != null)
                    {
                        foreach (var flag in data.flags)
                        {
                            if (!string.IsNullOrWhiteSpace(flag?.key))
                            {
                                _flags[flag.key] = flag.value;
                            }
                        }
                    }

                    RedBullCount = Mathf.Max(0, data.redBull);
                }
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    Debug.LogWarning($"NavigationGameState: Failed to load saved state. {ex.Message}");
                }
            }
        }

        internal bool TryGetFlag(string key, out bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = false;
                return false;
            }

            return _flags.TryGetValue(key, out value);
        }

        internal void SetInitialValue(string key, bool value, bool overrideExisting)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _defaultFlags[key] = value;

            if (_flags.ContainsKey(key))
            {
                if (!overrideExisting) return;
                _flags[key] = value;
                return;
            }

            _flags[key] = value;
        }

        internal Dictionary<string, bool> GetFlagSnapshot()
        {
            return new Dictionary<string, bool>(_flags, StringComparer.OrdinalIgnoreCase);
        }

        internal void LoadSnapshot(Dictionary<string, bool> flags, int redBull, bool persistToPlayerPrefs = true)
        {
            _flags.Clear();
            if (flags != null)
            {
                foreach (var entry in flags)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    _flags[entry.Key] = entry.Value;
                }
            }

            RedBullCount = Mathf.Max(0, redBull);
            if (persistToPlayerPrefs)
            {
                SaveState();
            }
        }

        internal void ResetToDefaults(bool persistToPlayerPrefs = true)
        {
            _flags.Clear();
            foreach (var entry in _defaultFlags)
            {
                _flags[entry.Key] = entry.Value;
            }

            RedBullCount = _defaultRedBull;
            if (persistToPlayerPrefs)
            {
                SaveState();
            }
        }

        private void SaveState()
        {
            try
            {
                _hasPersistedState = true;
                var data = new GameStateSaveData
                {
                    flags = _flags.Select(pair => new StateFlag { key = pair.Key, value = pair.Value }).ToList(),
                    redBull = RedBullCount
                };

                var json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    Debug.LogWarning($"NavigationGameState: Failed to save state. {ex.Message}");
                }
            }
        }

        private static bool ConvertToBool(object value, bool fallback = false)
        {
            switch (value)
            {
                case null:
                    return fallback;
                case bool b:
                    return b;
                case string s when bool.TryParse(s, out var parsed):
                    return parsed;
                case double d:
                    return Math.Abs(d) > float.Epsilon;
                case int i:
                    return i != 0;
                default:
                    return fallback;
            }
        }

        private static int ConvertToInt(object value, int fallback)
        {
            switch (value)
            {
                case null:
                    return fallback;
                case int i:
                    return i;
                case long l:
                    return (int)l;
                case double d:
                    return (int)d;
                case string s when int.TryParse(s, out var parsed):
                    return parsed;
                default:
                    return fallback;
            }
        }
    }
}
