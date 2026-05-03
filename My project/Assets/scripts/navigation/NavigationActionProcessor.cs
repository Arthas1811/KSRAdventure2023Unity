using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace Assets.logic
{
    internal sealed class NavigationActionProcessor
    {
        private readonly Action<string> _routeCallback;
        private readonly NavigationGameState _gameState;
        private readonly bool _verbose;

        private bool actionBlocker = false;

        internal NavigationActionProcessor(Action<string> routeCallback, NavigationGameState gameState, bool verboseLogging)
        {
            _routeCallback = routeCallback ?? (_ => { });
            _gameState = gameState;
            _verbose = verboseLogging;
        }

        internal ActionExecutionResult Handle(ButtonDefinition definition)
        {
            if (definition?.Actions == null || definition.Actions.Count == 0)
            {
                return ActionExecutionResult.Continue;
            }

            ActionExecutionResult lastResult = ActionExecutionResult.Continue;
            foreach (var action in definition.Actions)
            {
                lastResult = Execute(action);
                if (lastResult != ActionExecutionResult.Continue)
                {
                    break;
                }
            }

            return lastResult;
        }

        internal ActionExecutionResult Execute(ActionDefinition action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Command))
            {
                return ActionExecutionResult.Continue;
            }

            switch (action.Command.ToLowerInvariant())
            {
                case "route":
                case "minigame":
                    if (string.Equals(action.Command, "minigame", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMinigame(action);
                    }
                    else
                    {
                        RouteTo(action);
                    }
                    return ActionExecutionResult.StopChain;
                case "additem":
                    HandleAddItem(action);
                    return ActionExecutionResult.Continue;
                case "removeitem":
                    HandleRemoveItem(action);
                    return ActionExecutionResult.Continue;
                case "requireitem":
                    return HandleRequireItem(action) ? ActionExecutionResult.Continue : ActionExecutionResult.ConditionFailed;
                case "setgamestate":
                    HandleSetGameState(action);
                    return ActionExecutionResult.Continue;
                case "changeredbull":
                    HandleChangeRedBull(action);
                    return ActionExecutionResult.Continue;
                case "playmusic":
                    HandlePlayMusic(action);
                    return ActionExecutionResult.Continue;
                case "stopmusic":
                    HandleStopMusic();
                    return ActionExecutionResult.Continue;
                case "playsound":
                    HandlePlaySound(action);
                    return ActionExecutionResult.Continue;
                case "playvideo":
                    HandlePlayVideo(action);
                    return ActionExecutionResult.Continue;
                default:
                    if (_verbose)
                    {
                        Debug.LogWarning($"NavigationActionProcessor: Unknown action '{action.Command}'.");
                    }

                    return ActionExecutionResult.Continue;
            }
        }

        private void RouteTo(ActionDefinition action)
        {
            if (action?.Parameters == null || action.Parameters.Count == 0)
            {
                if (_verbose)
                {
                    Debug.LogWarning("NavigationActionProcessor: Route action missing target.");
                }

                return;
            }

            var target = action.Parameters[0];
            if (string.IsNullOrWhiteSpace(target))
            {
                if (_verbose)
                {
                    Debug.LogWarning("NavigationActionProcessor: Route action target is empty.");
                }

                return;
            }

            _routeCallback(target);
        }

        private void HandleMinigame(ActionDefinition action)
        {
            if (!TryGetParameter(action, out var sceneName))
            {
                if (_verbose)
                {
                    Debug.LogWarning("NavigationActionProcessor: Minigame action missing target scene.");
                }

                return;
            }

            var targetScenePath = $"Assets/Scenes/Minigames/{sceneName}";
            if (!targetScenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                targetScenePath += ".unity";
            }

            if (_verbose)
            {
                Debug.Log($"NavigationActionProcessor: Loading minigame scene '{targetScenePath}'.");
            }

            SceneManager.LoadScene(targetScenePath, LoadSceneMode.Single);
        }

        private void HandleAddItem(ActionDefinition action)
        {
            if (!TryGetParameter(action, out var itemId))
            {
                return;
            }

            var state = InventoryState.Instance;
            if (state == null)
            {
                if (_verbose)
                {
                    Debug.LogWarning($"NavigationActionProcessor: InventoryState missing while adding '{itemId}'.");
                }

                return;
            }

            state.ReceiveItem(itemId);
        }

        private void HandleRemoveItem(ActionDefinition action)
        {
            if (!TryGetParameter(action, out var itemId))
            {
                return;
            }

            var state = InventoryState.Instance;
            if (state == null)
            {
                if (_verbose)
                {
                    Debug.LogWarning($"NavigationActionProcessor: InventoryState missing while removing '{itemId}'.");
                }

                return;
            }

            state.RemoveItem(itemId);
        }

        private bool HandleRequireItem(ActionDefinition action)
        {
            if (!TryGetParameter(action, out var itemId))
            {
                return true;
            }

            var state = InventoryState.Instance;
            if (state == null)
            {
                if (_verbose)
                {
                    Debug.LogWarning($"NavigationActionProcessor: InventoryState missing while requiring '{itemId}'.");
                }

                return false;
            }

            var hasItem = state.HasItem(itemId);
            if (!hasItem && _verbose)
            {
                Debug.LogWarning($"NavigationActionProcessor: Requirement '{itemId}' not met.");
            }

            return hasItem;
        }

        private void HandleSetGameState(ActionDefinition action)
        {
            if (_gameState == null || action?.Parameters == null || action.Parameters.Count < 2)
            {
                if (_verbose)
                {
                    Debug.LogWarning("NavigationActionProcessor: SetGameState missing parameters.");
                }

                return;
            }

            var key = action.Parameters[0];
            var valueRaw = action.Parameters[1];

            if (string.IsNullOrWhiteSpace(key))
            {
                if (_verbose)
                {
                    Debug.LogWarning("NavigationActionProcessor: SetGameState missing key.");
                }

                return;
            }

            var value = ParseBool(valueRaw);
            _gameState.SetFlag(key, value);
        }

        private void HandleChangeRedBull(ActionDefinition action)
        {
            if (_gameState == null || !TryGetParameter(action, out var deltaRaw))
            {
                return;
            }

            var delta = ParseInt(deltaRaw, 0);
            _gameState.ChangeRedBull(delta);
        }

        private void HandlePlayMusic(ActionDefinition action)
        {
            if (_verbose)
            {
                var clip = TryGetParameter(action, out var path) ? path : "<missing>";
                Debug.Log($"NavigationActionProcessor: PlayMusic placeholder for '{clip}'.");
            }

            string fileName = action.Parameters[0];
            int index = fileName.LastIndexOf('.');
            fileName = (index > 0) ? fileName.Substring(0, index) : fileName;
            var audioMusic = Resources.Load<AudioClip>(fileName);
            if (audioMusic == null)
            {
                return;
            }
            var music = NavigationAudio.Instance.musicSource;
            music.clip = audioMusic;
            music.loop = true;

            music.Play();
        }

        private void HandleStopMusic()
        {
            if (_verbose)
            {
                Debug.Log("NavigationActionProcessor: StopMusic placeholder.");
            }

            var music = NavigationAudio.Instance?.musicSource;
            if (music != null)
            {
                music.Stop();
            }
        }

        private void HandlePlaySound(ActionDefinition action)
        {
            if (_verbose)
            {
                var clip = TryGetParameter(action, out var path) ? path : "<missing>";
                Debug.Log($"NavigationActionProcessor: PlaySound placeholder for '{clip}'.");
            }

            string fileName = action.Parameters[0];
            int index = fileName.LastIndexOf('.');
            fileName = (index > 0) ? fileName.Substring(0, index) : fileName;
            var audioSound = Resources.Load<AudioClip>(fileName);
            if (audioSound == null)
            {
                return;
            }
            NavigationAudio.Instance.soundSource.PlayOneShot(audioSound);
        }

        private void HandlePlayVideo(ActionDefinition action)
        {
            var target = action.Parameters[0];
            if (_verbose)
            {
                target = action?.Parameters != null && action.Parameters.Count > 1 ? action.Parameters[1] : "<missing>";
                Debug.Log($"NavigationActionProcessor: PlayVideo placeholder for '{target}'.");
            }

            string fileName = action.Parameters[1];
            int index = fileName.LastIndexOf('.');
            fileName = (index > 0) ? fileName.Substring(0, index) : fileName;
            NavigationVideo.Instance.StartCoroutine(NavigationVideo.Instance.PlayVideo(fileName));
        }

        internal static bool TryGetParameter(ActionDefinition action, out string value)
        {
            value = null;
            if (action?.Parameters == null || action.Parameters.Count == 0)
            {
                return false;
            }

            value = action.Parameters[0];
            return !string.IsNullOrWhiteSpace(value);
        }

        internal static bool ParseBool(string raw)
        {
            if (bool.TryParse(raw, out var result))
            {
                return result;
            }

            return string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
        }

        internal static int ParseInt(string raw, int fallback)
        {
            return int.TryParse(raw, out var parsed) ? parsed : fallback;
        }
    }

    internal enum ActionExecutionResult
    {
        Continue,
        StopChain,
        ConditionFailed
    }
}
