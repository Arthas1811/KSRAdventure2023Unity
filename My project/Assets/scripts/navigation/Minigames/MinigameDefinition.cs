using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.Minigames
{
    public readonly struct MinigameLaunchContext
    {
        public MinigameLaunchContext(string nodeKey, string entrySlideKey, string fallbackSlideKey, bool verbose)
        {
            NodeKey = nodeKey;
            EntrySlideKey = entrySlideKey;
            FallbackSlideKey = fallbackSlideKey;
            Verbose = verbose;
        }

        public string NodeKey { get; }
        public string EntrySlideKey { get; }
        public string FallbackSlideKey { get; }
        public string ReturnSlideKey => string.IsNullOrWhiteSpace(FallbackSlideKey) ? EntrySlideKey : FallbackSlideKey;
        public bool Verbose { get; }
    }

    public abstract class MinigameDefinition
    {
        protected abstract string SceneName { get; }
        protected virtual LoadSceneMode LoadMode => LoadSceneMode.Single;

        public virtual bool Launch(MinigameLaunchContext context)
        {
            if (string.IsNullOrWhiteSpace(SceneName))
            {
                if (context.Verbose) Debug.LogWarning($"MinigameDefinition '{GetType().Name}' missing scene name.");
                return false;
            }

            MinigameReturnState.SetReturnState(context.EntrySlideKey, context.FallbackSlideKey, context.NodeKey);

            if (!IsSceneInBuild(SceneName))
            {
                if (context.Verbose) Debug.LogWarning($"MinigameDefinition: Scene '{SceneName}' not in build settings.");
                return false;
            }

            SceneManager.LoadScene(SceneName, LoadMode);
            return true;
        }

        private static bool IsSceneInBuild(string sceneName)
        {
            var target = Path.GetFileNameWithoutExtension(sceneName);
            var count = SceneManager.sceneCountInBuildSettings;
            for (var i = 0; i < count; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrWhiteSpace(path)) continue;
                var file = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(file, target, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }

    public static class MinigameReturnState
    {
        private static string _entrySlideKey;
        private static string _fallbackSlideKey;
        private static string _minigameKey;
        private static bool? _gameEnded;
        private static bool? _gameWon;
        private static string _returnSceneName;
        private static bool _returnTriggered;

        public static void SetReturnSlide(string slideKey)
        {
            _entrySlideKey = slideKey;
        }

        public static string CurrentMinigameKey => _minigameKey;

        public static void SetReturnState(string entrySlideKey, string fallbackSlideKey, string minigameKey)
        {
            _entrySlideKey = entrySlideKey;
            _fallbackSlideKey = fallbackSlideKey;
            _minigameKey = minigameKey;
            _gameEnded = null;
            _gameWon = null;
            _returnSceneName = SceneManager.GetActiveScene().name;
            _returnTriggered = false;
        }

        public static void SetResult(bool? gameEnded, bool? gameWon)
        {
            _gameEnded = gameEnded;
            _gameWon = gameWon;

            if (!_returnTriggered && (_gameEnded == true || _gameWon == true) && !string.IsNullOrWhiteSpace(_returnSceneName))
            {
                _returnTriggered = true;
                NavigationStatePersistence.PersistInventoryAndProgress();
                SceneManager.LoadScene(_returnSceneName, LoadSceneMode.Single);
            }
        }

        public static bool TryConsume(out MinigameReturnData data)
        {
            data = new MinigameReturnData(_entrySlideKey, _fallbackSlideKey, _minigameKey, _gameEnded, _gameWon);
            var hasData = data.HasAny;
            _entrySlideKey = null;
            _fallbackSlideKey = null;
            _minigameKey = null;
            _gameEnded = null;
            _gameWon = null;
            _returnSceneName = null;
            _returnTriggered = false;
            return hasData;
        }

        public static bool TryConsume(out string slideKey)
        {
            if (TryConsume(out MinigameReturnData data) && data.HasReturnTarget)
            {
                slideKey = !string.IsNullOrWhiteSpace(data.EntrySlideKey) ? data.EntrySlideKey : data.FallbackSlideKey;
                return true;
            }

            slideKey = null;
            return false;
        }
    }

    public readonly struct MinigameReturnData
    {
        internal MinigameReturnData(string entrySlideKey, string fallbackSlideKey, string minigameKey, bool? gameEnded, bool? gameWon)
        {
            EntrySlideKey = entrySlideKey;
            FallbackSlideKey = fallbackSlideKey;
            MinigameKey = minigameKey;
            GameEnded = gameEnded;
            GameWon = gameWon;
        }

        public string EntrySlideKey { get; }
        public string FallbackSlideKey { get; }
        public string MinigameKey { get; }
        public bool? GameEnded { get; }
        public bool? GameWon { get; }
        public bool HasReturnTarget => !string.IsNullOrWhiteSpace(EntrySlideKey) || !string.IsNullOrWhiteSpace(FallbackSlideKey);
        public bool HasAny => HasReturnTarget || GameEnded.HasValue || GameWon.HasValue;
    }

    internal static class MinigameFactory
    {
        private static readonly Dictionary<string, MinigameDefinition> Cache = new Dictionary<string, MinigameDefinition>(StringComparer.OrdinalIgnoreCase);

        internal static bool TryCreate(string className, bool verbose, out MinigameDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(className))
            {
                if (verbose) Debug.LogWarning("MinigameFactory: Minigame class name was empty.");
                return false;
            }

            if (Cache.TryGetValue(className, out definition))
            {
                return definition != null;
            }

            var type = Type.GetType(className, false);
            if (type == null || !typeof(MinigameDefinition).IsAssignableFrom(type))
            {
                Cache[className] = null;
                if (verbose) Debug.LogWarning($"MinigameFactory: Unable to resolve '{className}'.");
                definition = null;
                return false;
            }

            try
            {
                definition = Activator.CreateInstance(type) as MinigameDefinition;
                Cache[className] = definition;
                if (definition == null && verbose)
                {
                    Debug.LogWarning($"MinigameFactory: Failed to instantiate '{className}'.");
                }

                return definition != null;
            }
            catch (Exception ex)
            {
                Cache[className] = null;
                if (verbose) Debug.LogWarning($"MinigameFactory: Exception while creating '{className}'. {ex.Message}");
                definition = null;
                return false;
            }
        }
    }
}
