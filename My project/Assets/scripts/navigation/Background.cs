using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.logic;
using Framework.Minigames;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class Background : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string startingSlideKey = "Start";
    [SerializeField] private Vector2 referenceResolution = new Vector2(NavigationDataLoader.ReferenceWidth, NavigationDataLoader.ReferenceHeight);
    [SerializeField] private bool verboseLogging;
    [SerializeField] private bool showHotspotOverlay = true;
    [SerializeField, Range(0f, 1f)] private float hotspotAlpha = 0.18f;
    [SerializeField] private Color hotspotTint = new Color(0.5f, 0.5f, 0.5f, 0.18f);
    [Header("Inventory Overlay")]
    [SerializeField] private bool enableInventoryWindow = true;
    [SerializeField] private KeyCode inventoryToggleKey = KeyCode.I;
    [SerializeField] private KeyCode inventorySecondaryToggleKey = KeyCode.E;
    [Header("Interaction Controls")]
    [SerializeField] private bool lockInteractionWhenInventoryOpen = false;
    [SerializeField] private bool manualInteractionLock;
    [SerializeField] private bool manualInventoryOpen;
    [Header("Settings and Game State JSON")]
    [SerializeField] private bool enableSettingsMenu = true;
    [SerializeField] private bool autoLoadSelectedGameStateOnStart = true;
    [SerializeField] private string gameStateFileName = "gamestates.json";
    [Header("Top Bar Icons")]
    [SerializeField] private string menuIconPath = "images/Icons/Menu.png";
    [SerializeField] private string inventoryIconPath = "images/Icons/Inventory.png";
    [SerializeField] private Vector2 topBarButtonSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 topBarMargin = new Vector2(20f, 20f);
    [SerializeField] private float topBarSpacing = 12f;

    private NavigationDatabase _database;
    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private RectTransform _buttonContainer;
    private Image _backgroundImage;
    private string _currentSlideKey;
    private string _resourcesRoot;
    private Sprite _fallbackButtonSprite;
    private NavigationKeyInput _keyInput;

    private readonly List<GameObject> _activeButtons = new List<GameObject>();
    private readonly Dictionary<string, SpriteRecord> _spriteCache = new Dictionary<string, SpriteRecord>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string[]> SpriteKeyAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "top", new[] { "up" } },
        { "up", new[] { "top" } },
        { "bottom", new[] { "down", "back" } },
        { "down", new[] { "bottom", "back" } },
        { "back", new[] { "down", "bottom" } },
        { "left", Array.Empty<string>() },
        { "right", Array.Empty<string>() }
    };

    private static readonly HashSet<string> CursorArrowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "left",
        "right",
        "up",
        "top",
        "down",
        "bottom",
        "back"
    };
    private static readonly Dictionary<KeyCode, string[]> NavigationKeyLookup = new Dictionary<KeyCode, string[]>
    {
        { KeyCode.LeftArrow, new[] { "left" } },
        { KeyCode.RightArrow, new[] { "right" } },
        { KeyCode.UpArrow, new[] { "top", "up" } },
        { KeyCode.DownArrow, new[] { "bottom", "down" } }
    };
    private static readonly KeyValuePair<string, bool>[] DefaultGameStateFlags =
    {
        new KeyValuePair<string, bool>("button1", true),
        new KeyValuePair<string, bool>("LeButton", false),
        new KeyValuePair<string, bool>("CodeTerminal", true),
        new KeyValuePair<string, bool>("Door", true),
        new KeyValuePair<string, bool>("False", false),
        new KeyValuePair<string, bool>("KeyTaken", false),
        new KeyValuePair<string, bool>("Armdrugge.Tried", false),
        new KeyValuePair<string, bool>("BG-Forum.Visited", false),
        new KeyValuePair<string, bool>("BG-Game.Complete", false),
        new KeyValuePair<string, bool>("Bio-Forum.Visited", false),
        new KeyValuePair<string, bool>("Bio-Game.Complete", false),
        new KeyValuePair<string, bool>("Geo-Forum.Visited", false),
        new KeyValuePair<string, bool>("Geo-Game.Complete", false),
        new KeyValuePair<string, bool>("Ph-Forum.Visited", false),
        new KeyValuePair<string, bool>("Ph-Game.Complete", false),
        new KeyValuePair<string, bool>("Music-1.Visited", false),
        new KeyValuePair<string, bool>("Music-Red.Complete", false)
    };

    private Vector2 _defaultReferenceSize;
    private Vector2 _currentReferenceSize;
    private Rect _backgroundDisplayRect;
    private Texture2D _fallbackTexture;
    private bool _ownsFallbackSprite;
    private NavigationGameState _gameState;
    private NavigationActionProcessor _actionProcessor;
    private Coroutine _onEnterRoutine;
    private Vector2 _lastScreenSize;
    private Vector2 _lastContainerSize;
    private InventoryAnimatedToggle _inventoryToggle;
    private CanvasScaler _inventoryCanvasScaler;
    private Canvas _inventoryCanvas;
    private CanvasGroup _inventoryCanvasGroup;
    private bool _slideshowInteractionEnabled = true;
    private bool _inventoryPersistenceSubscribed;
    private bool _lastManualInteractionLock;
    private bool _lastManualInventoryOpen;
    private Button _inventoryButton;
    private Button _settingsButton;
    private Button _debugAddItemButton;
    private int _inventoryBaseFontSize = 26;
    private int _settingsButtonBaseFontSize = 26;
    private int _debugAddItemBaseFontSize = 28;
    private const float HotspotRaycastAlphaFloor = 0.0011f;
    private readonly List<string> _inventoryItemIds = new List<string>();
    private RectTransform _settingsPanel;
    private Text _settingsStatusLabel;
    private Text _settingsTitleLabel;
    private Text _settingsSubtitleLabel;
    private readonly List<Button> _settingsActionButtons = new List<Button>();
    private string _selectedGameStatePath;
    private string _persistedStartSlideKey;
    private const string GameStateFilePathSaveKey = "Navigation.GameStateFilePath";
    private const string DefaultGameStateFileName = "gamestates.json";
    private const string GameStateExportFolder = "gamestate_exports";
    private const int SettingsTitleBaseFontSize = 24;
    private const int SettingsSubtitleBaseFontSize = 15;
    private const int SettingsActionBaseFontSize = 22;
    private const int SettingsStatusBaseFontSize = 16;
    private static readonly Dictionary<string, string> MinigameProgressIdLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "ArmDrugge", "armdrugge" },
        { "BiologyMinigame", "biology" },
        { "BossFightMinigame", "bossfight" },
        { "ChemieMinigame1", "chemie" },
        { "ChemieMinigame2", "chemie" },
        { "FindErrorsMinigame", "bg" },
        { "Geography", "geography" },
        { "LockpickingMinigame", "lockpicking" },
        { "PhMinigame", "physics" },
        { "PianoMinigame", "piano" }
    };
    private static readonly HashSet<string> DialogueManagedProgressClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ArmDrugge",
        "BiologyMinigame",
        "FindErrorsMinigame",
        "Geography",
        "PhMinigame",
        "PianoMinigame"
    };

    private float ScaleX => _currentReferenceSize.x / NavigationDataLoader.ReferenceWidth;
    private float ScaleY => _currentReferenceSize.y / NavigationDataLoader.ReferenceHeight;

    private void Awake()
    {
        _resourcesRoot = Path.Combine(Application.dataPath, "resources");
        _database = NavigationDataLoader.Load();
        _gameState = NavigationGameState.CreateFromGameData(verboseLogging);
        GameDataLoader.ProgressChanged += HandleProgressChanged;
        ApplyDefaultGameStateFlags();
        InitializeGameStatePersistence();
        _actionProcessor = new NavigationActionProcessor(SwitchToNode, _gameState, verboseLogging);
        referenceResolution = new Vector2(NavigationDataLoader.ReferenceWidth, NavigationDataLoader.ReferenceHeight);
        _defaultReferenceSize = referenceResolution;
        _currentReferenceSize = referenceResolution;
        _fallbackButtonSprite = CreateFallbackSprite();
        _keyInput = GetComponent<NavigationKeyInput>() ?? gameObject.AddComponent<NavigationKeyInput>();
        EnsureEventSystem();
        BuildUiScaffold();
        if (_buttonContainer != null) _buttonContainer.gameObject.SetActive(true);
        if (_keyInput != null) _keyInput.enabled = true;
        ApplyReferenceToCanvas(_currentReferenceSize);
        SetupInventoryOverlay();
        SetupInventoryButton();
        SetupSettingsMenu();
        AttachInventoryPersistenceHooks();
        // SetupDebugAddItemButton();
        SetupDefaultKeyBindings();
        _lastManualInteractionLock = !manualInteractionLock;
        _lastManualInventoryOpen = !manualInventoryOpen;
    }

    private void Start()
    {
        AttachInventoryPersistenceHooks();
        ApplyManualOverrides();
        if (_database.Nodes.Count == 0)
        {
            Debug.LogWarning("Background: No navigation data loaded. Check JSON files under Assets/resources/jsons.");
            return;
        }

        var initialKey = ResolveStartingSlide();
        if (string.IsNullOrWhiteSpace(initialKey))
        {
            Debug.LogWarning("Background: Unable to determine starting slide.");
            return;
        }

        SwitchToNode(initialKey);
    }

    public void RegisterKeyBinding(KeyCode key, Action action)
    {
        if (action == null) return;
        if (_keyInput == null) _keyInput = GetComponent<NavigationKeyInput>() ?? gameObject.AddComponent<NavigationKeyInput>();
        _keyInput.Register(key, action);
    }

    private void SetupDefaultKeyBindings()
    {
        RegisterKeyBinding(KeyCode.LeftArrow, () => HandleNavigationKey(KeyCode.LeftArrow));
        RegisterKeyBinding(KeyCode.RightArrow, () => HandleNavigationKey(KeyCode.RightArrow));
        RegisterKeyBinding(KeyCode.UpArrow, () => HandleNavigationKey(KeyCode.UpArrow));
        RegisterKeyBinding(KeyCode.DownArrow, () => HandleNavigationKey(KeyCode.DownArrow));
        RegisterInventoryKeyBindings();
        RegisterSettingsKeyBindings();
    }

    private void ApplyDefaultGameStateFlags()
    {
        if (_gameState == null) return;

        for (var i = 0; i < DefaultGameStateFlags.Length; i++)
        {
            var entry = DefaultGameStateFlags[i];
            _gameState.SetInitialValue(entry.Key, entry.Value, overrideExisting: false);
        }
    }

    private void InitializeGameStatePersistence()
    {
        if (_gameState == null) return;

        _selectedGameStatePath = ResolveConfiguredGameStatePath();
        var loadedFromFile = false;
        if (autoLoadSelectedGameStateOnStart && File.Exists(_selectedGameStatePath))
        {
            loadedFromFile = TryLoadGameStateFromFile(_selectedGameStatePath);
        }

        if (!loadedFromFile)
        {
            PersistGameStateToSelectedFile();
        }

        _gameState.StateChanged -= HandleGameStateChanged;
        _gameState.StateChanged += HandleGameStateChanged;
    }

    private void HandleGameStateChanged()
    {
        PersistGameStateToSelectedFile();
    }

    private void HandleProgressChanged()
    {
        PersistGameStateToSelectedFile();
    }

    private void AttachInventoryPersistenceHooks()
    {
        if (_inventoryPersistenceSubscribed) return;

        var inventoryState = InventoryState.Instance;
        if (inventoryState == null) return;

        inventoryState.InventoryChanged -= HandleInventoryChanged;
        inventoryState.InventoryChanged += HandleInventoryChanged;
        _inventoryPersistenceSubscribed = true;
    }

    private void DetachInventoryPersistenceHooks()
    {
        if (!_inventoryPersistenceSubscribed) return;
        var inventoryState = InventoryState.Instance;
        if (inventoryState != null)
        {
            inventoryState.InventoryChanged -= HandleInventoryChanged;
        }

        _inventoryPersistenceSubscribed = false;
    }

    private void HandleInventoryChanged()
    {
        PersistGameStateToSelectedFile();
    }

    private string ResolveConfiguredGameStatePath()
    {
        var saved = PlayerPrefs.GetString(GameStateFilePathSaveKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            return EnsureJsonPath(saved);
        }

        return BuildDefaultGameStatePath();
    }

    private string BuildDefaultGameStatePath()
    {
        var fileName = string.IsNullOrWhiteSpace(gameStateFileName) ? DefaultGameStateFileName : gameStateFileName.Trim();
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }

        return EnsureJsonPath(Path.Combine(Application.persistentDataPath, fileName));
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

    private void SaveSelectedGameStatePath()
    {
        if (string.IsNullOrWhiteSpace(_selectedGameStatePath))
        {
            return;
        }

        PlayerPrefs.SetString(GameStateFilePathSaveKey, _selectedGameStatePath);
        PlayerPrefs.Save();
    }

    private bool PersistGameStateToSelectedFile()
    {
        if (_gameState == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_selectedGameStatePath))
        {
            _selectedGameStatePath = BuildDefaultGameStatePath();
        }

        try
        {
            var directory = Path.GetDirectoryName(_selectedGameStatePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var data = BuildGameStateFileData();
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(_selectedGameStatePath, json);
            SaveSelectedGameStatePath();
            return true;
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"Background: Failed to persist game state JSON '{_selectedGameStatePath}'. {ex.Message}");
            }

            SetSettingsStatus("Failed to save gamestate file.");
            return false;
        }
    }

    private GameStateFileData BuildGameStateFileData()
    {
        var data = new GameStateFileData
        {
            redBull = _gameState != null ? _gameState.RedBullCount : 0,
            startSlideKey = ResolveStartSlideKeyForPersistence()
        };
        if (_gameState != null)
        {
            var snapshot = _gameState.GetFlagSnapshot();
            foreach (var entry in snapshot.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                data.flags.Add(new GameStateFileFlag { key = entry.Key, value = entry.Value });
            }
        }

        var inventoryState = InventoryState.Instance;
        if (inventoryState != null)
        {
            var inventorySnapshot = inventoryState.GetSnapshot();
            foreach (var entry in inventorySnapshot.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (entry.Value <= 0) continue;
                data.inventory.Add(new GameStateFileItem { id = entry.Key, quantity = entry.Value });
            }
        }

        foreach (var progress in GameDataLoader.GetProgressSnapshot())
        {
            data.progress.Add(new GameStateFileProgress
            {
                id = progress.id,
                completed = progress.completed,
                tries = progress.tries
            });
        }

        return data;
    }

    private bool IsKnownSlideKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && _database != null && _database.Nodes.ContainsKey(key);
    }

    private string ResolveStartSlideKeyForPersistence()
    {
        if (IsKnownSlideKey(_currentSlideKey))
        {
            return _currentSlideKey;
        }

        if (IsKnownSlideKey(_persistedStartSlideKey))
        {
            return _persistedStartSlideKey;
        }

        if (IsKnownSlideKey(startingSlideKey))
        {
            return startingSlideKey;
        }

        return _database?.Nodes.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }

    private void ApplyStartSlideFromSaveData(GameStateFileData data)
    {
        var candidate = data?.startSlideKey;
        if (IsKnownSlideKey(candidate))
        {
            _persistedStartSlideKey = candidate;
            return;
        }

        _persistedStartSlideKey = null;
    }

    private bool TryLoadGameStateFromFile(string path)
    {
        if (_gameState == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            if (json.IndexOf("\"flags\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var data = JsonUtility.FromJson<GameStateFileData>(json);
            if (data == null)
            {
                return false;
            }

            ApplyStartSlideFromSaveData(data);

            var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (data.flags != null)
            {
                foreach (var entry in data.flags)
                {
                    if (string.IsNullOrWhiteSpace(entry?.key))
                    {
                        continue;
                    }

                    flags[entry.key] = entry.value;
                }
            }

            _gameState.LoadSnapshot(flags, Mathf.Max(0, data.redBull));
            ApplyDefaultGameStateFlags();
            ApplyInventoryFromSaveData(data);
            ApplyProgressFromSaveData(data);
            _selectedGameStatePath = EnsureJsonPath(path);
            SaveSelectedGameStatePath();
            PersistGameStateToSelectedFile();
            RefreshCurrentSlideButtons();
            return true;
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"Background: Failed to load game state JSON '{path}'. {ex.Message}");
            }

            return false;
        }
    }

    private void ApplyInventoryFromSaveData(GameStateFileData data)
    {
        var inventoryState = InventoryState.Instance;
        if (inventoryState == null) return;

        var inventorySnapshot = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (data?.inventory != null)
        {
            for (var i = 0; i < data.inventory.Count; i++)
            {
                var entry = data.inventory[i];
                if (string.IsNullOrWhiteSpace(entry?.id) || entry.quantity <= 0)
                {
                    continue;
                }

                inventorySnapshot[entry.id] = entry.quantity;
            }
        }

        if (inventorySnapshot.Count > 0)
        {
            inventoryState.LoadSnapshot(inventorySnapshot);
            return;
        }

        inventoryState.ResetToDefaults();
    }

    private void ApplyProgressFromSaveData(GameStateFileData data)
    {
        if (data?.progress == null || data.progress.Count == 0) return;

        GameDataLoader.ApplyProgressSnapshot(data.progress.Select(p => new GameDataLoader.MinigameProgressSnapshot
        {
            id = p.id,
            completed = p.completed,
            tries = p.tries
        }));
    }

    private void SetSelectedGameStatePath(string path, bool loadExistingFile)
    {
        var normalized = EnsureJsonPath(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            SetSettingsStatus("Invalid gamestate file path.");
            return;
        }

        var fileName = Path.GetFileName(normalized);
        if (string.Equals(fileName, "gameData.json", StringComparison.OrdinalIgnoreCase))
        {
            SetSettingsStatus("Select a dedicated gamestates.json file.");
            return;
        }

        _selectedGameStatePath = normalized;
        SaveSelectedGameStatePath();

        if (loadExistingFile && File.Exists(_selectedGameStatePath) && TryLoadGameStateFromFile(_selectedGameStatePath))
        {
            SetSettingsStatus($"Loaded {Path.GetFileName(_selectedGameStatePath)}");
            return;
        }

        PersistGameStateToSelectedFile();
        SetSettingsStatus($"Using {Path.GetFileName(_selectedGameStatePath)}");
    }

    private void ResetGameStateToBase()
    {
        if (_gameState == null)
        {
            return;
        }

        // Clear transient return targets so a reset always boots into the true start slide
        NavigationReturnState.TryConsumeReturnSlide(out _);
        MinigameReturnState.TryConsume(out MinigameReturnData _);

        _gameState.ResetToDefaults();
        ApplyDefaultGameStateFlags();
        var inventoryState = InventoryState.Instance;
        if (inventoryState != null)
        {
            inventoryState.ResetToDefaults();
        }
        GameDataLoader.ClearCache();
        GameDataLoader.ResetProgressToDefaults();

        // Force the next launch to start from the canonical starting slide
        _persistedStartSlideKey = ResolveResetStartSlideKey();
        _currentSlideKey = _persistedStartSlideKey;

        PersistGameStateToSelectedFile();
        // Clear persisted selection so next launch uses the fresh default file
        PlayerPrefs.DeleteKey(GameStateFilePathSaveKey);
        PlayerPrefs.Save();

        SetSettingsStatus("Resetting game...");
        // Reload the active scene to restart everything (videos, audio, UI, state)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private string ResolveResetStartSlideKey()
    {
        if (!string.IsNullOrWhiteSpace(startingSlideKey) && IsKnownSlideKey(startingSlideKey))
        {
            return startingSlideKey;
        }

        if (_database != null && _database.Nodes.Count > 0)
        {
            return _database.Nodes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }

        return null;
    }

    private void ExportSelectedGameState()
    {
        if (!PersistGameStateToSelectedFile() || string.IsNullOrWhiteSpace(_selectedGameStatePath))
        {
            return;
        }

        try
        {
            var exportDirectory = Path.Combine(Application.persistentDataPath, GameStateExportFolder);
            Directory.CreateDirectory(exportDirectory);
            var exportPath = Path.Combine(exportDirectory, $"gamestates_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(_selectedGameStatePath, exportPath, true);
            SetSettingsStatus($"Exported {Path.GetFileName(exportPath)}");
        }
        catch (Exception ex)
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"Background: Failed to export game state JSON. {ex.Message}");
            }

            SetSettingsStatus("Export failed.");
        }
    }

    private void SelectGameStateFile()
    {
#if UNITY_EDITOR
        var startDirectory = Application.persistentDataPath;
        if (!string.IsNullOrWhiteSpace(_selectedGameStatePath))
        {
            var configuredDirectory = Path.GetDirectoryName(_selectedGameStatePath);
            if (!string.IsNullOrWhiteSpace(configuredDirectory) && Directory.Exists(configuredDirectory))
            {
                startDirectory = configuredDirectory;
            }
        }

        var selected = EditorUtility.OpenFilePanel("Select gamestates.json", startDirectory, "json");
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        SetSelectedGameStatePath(selected, loadExistingFile: true);
#else
        CycleGameStateFileCandidate();
#endif
    }

    private void CycleGameStateFileCandidate()
    {
        var candidates = new List<string>();
        AddUniqueGameStatePath(candidates, _selectedGameStatePath);
        AddUniqueGameStatePath(candidates, BuildDefaultGameStatePath());

        var selectedDirectory = Path.GetDirectoryName(_selectedGameStatePath);
        if (!string.IsNullOrWhiteSpace(selectedDirectory) && Directory.Exists(selectedDirectory))
        {
            foreach (var file in Directory.GetFiles(selectedDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                AddUniqueGameStatePath(candidates, file);
            }
        }

        if (candidates.Count == 0)
        {
            SetSettingsStatus("No gamestate JSON files found.");
            return;
        }

        var currentIndex = candidates.FindIndex(path => string.Equals(path, _selectedGameStatePath, StringComparison.OrdinalIgnoreCase));
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % candidates.Count;
        SetSelectedGameStatePath(candidates[nextIndex], loadExistingFile: true);
    }

    private void AddUniqueGameStatePath(List<string> candidates, string path)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = EnsureJsonPath(path);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i], normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        candidates.Add(normalized);
    }

    private void SetSettingsStatus(string message)
    {
        if (_settingsStatusLabel != null)
        {
            _settingsStatusLabel.text = message ?? string.Empty;
        }
    }

    private string ResolveStartingSlide()
    {
        // First, check if we're returning from a dialogue scene
        if (NavigationReturnState.TryConsumeReturnSlide(out string dialogueReturnSlide))
        {
            if (!string.IsNullOrWhiteSpace(dialogueReturnSlide) && _database.Nodes.ContainsKey(dialogueReturnSlide))
            {
                if (verboseLogging) Debug.Log($"Background: Returning from dialogue to slide '{dialogueReturnSlide}'.");
                return dialogueReturnSlide;
            }
            else if (verboseLogging)
            {
                Debug.LogWarning($"Background: Dialogue return slide '{dialogueReturnSlide}' not found. Falling back to defaults.");
            }
        }

        // Next, check if we're returning from a minigame
        if (MinigameReturnState.TryConsume(out MinigameReturnData returnData))
        {
            var resolved = ResolveMinigameReturnTarget(returnData);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                if (_database.Nodes.ContainsKey(resolved)) return resolved;
                if (verboseLogging) Debug.LogWarning($"Background: Return slide '{resolved}' from minigame not found. Falling back to defaults.");
            }
        }

        if (IsKnownSlideKey(_persistedStartSlideKey)) return _persistedStartSlideKey;

        if (!string.IsNullOrWhiteSpace(startingSlideKey) && _database.Nodes.ContainsKey(startingSlideKey)) return startingSlideKey;

        if (GameDataLoader.TryGetStartRoom(out var savedRoom) && _database.Nodes.ContainsKey(savedRoom))
        {
            if (verboseLogging && !string.IsNullOrWhiteSpace(startingSlideKey))
            {
                Debug.LogWarning($"Background: Starting slide '{startingSlideKey}' not found. Using saved room '{savedRoom}'.");
            }

            return savedRoom;
        }

        var first = _database.Nodes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (verboseLogging && !string.IsNullOrWhiteSpace(startingSlideKey)) Debug.LogWarning($"Background: Starting slide '{startingSlideKey}' not found. Falling back to '{first}'.");
        return first;
    }

    private string ResolveMinigameReturnTarget(MinigameReturnData returnData)
    {
        if (!returnData.HasAny) return null;

        var result = ReadMinigameResult(returnData);
        ApplyMinigameProgressFromReturn(returnData, result);
        if (result.GameWon == true && !string.IsNullOrWhiteSpace(returnData.FallbackSlideKey)) return returnData.FallbackSlideKey;
        if (result.GameEnded == true && !string.IsNullOrWhiteSpace(returnData.EntrySlideKey)) return returnData.EntrySlideKey;

        if (returnData.HasReturnTarget)
        {
            return !string.IsNullOrWhiteSpace(returnData.EntrySlideKey) ? returnData.EntrySlideKey : returnData.FallbackSlideKey;
        }

        return null;
    }

    private MinigameResult ReadMinigameResult(MinigameReturnData returnData)
    {
        var result = new MinigameResult(returnData.GameEnded, returnData.GameWon);
        ApplyParameterIfMissing("gameEnded", ref result.GameEnded);
        ApplyParameterIfMissing("gameWon", ref result.GameWon);
        return result;
    }

    private void ApplyMinigameProgressFromReturn(MinigameReturnData returnData, MinigameResult result)
    {
        if (_database == null || string.IsNullOrWhiteSpace(returnData.MinigameKey))
        {
            return;
        }

        if (!_database.Nodes.TryGetValue(returnData.MinigameKey, out var node) || node == null)
        {
            return;
        }

        var className = node.MinigameClassName;
        if (IsDialogueManagedProgressClass(className))
        {
            return;
        }

        var progressId = ResolveMinigameProgressId(node);
        if (string.IsNullOrWhiteSpace(progressId))
        {
            return;
        }

        if (result.GameWon == true)
        {
            var isChemie = string.Equals(progressId, "chemie", StringComparison.OrdinalIgnoreCase);
            var isChemiePart1 = isChemie &&
                                string.Equals(returnData.MinigameKey, "ChemieKeller_Minigame1", StringComparison.OrdinalIgnoreCase);

            // Chemie has two consecutive minigame parts. Part 1 must not mark the whole
            // minigame as completed, otherwise Part 2 gets blocked by completion gating.
            if (isChemiePart1)
            {
                InventoryState.Instance?.ReceiveItem("nitro_beaker");
                _gameState?.SetFlag("ChemieKeller_s.FridgePolygon", true);
                return;
            }

            if (GameDataLoader.TryGetMinigameProgress(progressId, out var completed, out _) && completed)
            {
                return;
            }

            GameDataLoader.UpdateMinigameProgress(progressId, completed: true, incrementTries: true);
            return;
        }

        if (result.GameEnded == true)
        {
            GameDataLoader.UpdateMinigameProgress(progressId, completed: false, incrementTries: true);
        }
    }

    private static bool IsDialogueManagedProgressClass(string minigameClassName)
    {
        if (string.IsNullOrWhiteSpace(minigameClassName))
        {
            return false;
        }

        foreach (var className in DialogueManagedProgressClasses)
        {
            if (minigameClassName.EndsWith(className, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveMinigameProgressId(SceneNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.MinigameClassName))
        {
            return null;
        }

        var className = node.MinigameClassName;
        foreach (var mapping in MinigameProgressIdLookup)
        {
            if (className.EndsWith(mapping.Key, StringComparison.OrdinalIgnoreCase))
            {
                return mapping.Value;
            }
        }

        return null;
    }

    private static bool IsCompletedMinigame(SceneNode node)
    {
        var progressId = ResolveMinigameProgressId(node);
        if (string.IsNullOrWhiteSpace(progressId))
        {
            return false;
        }

        return GameDataLoader.TryGetMinigameProgress(progressId, out var completed, out _) && completed;
    }

    private void ApplyParameterIfMissing(string key, ref bool? target)
    {
        if (target.HasValue) return;
        if (TryGetBoolParameter(key, out var parsed)) target = parsed;
    }

    private bool TryGetBoolParameter(string key, out bool value)
    {
        value = false;
        if (TryGetBoolFromArgs(Environment.GetCommandLineArgs(), key, out value)) return true;
        if (TryGetBoolFromUrl(Application.absoluteURL, key, out value)) return true;
        return false;
    }

    private static bool TryGetBoolFromArgs(string[] args, string key, out bool value)
    {
        value = false;
        if (args == null || string.IsNullOrWhiteSpace(key)) return false;

        foreach (var arg in args)
        {
            if (TryParseBool(arg, key, out value)) return true;
        }

        return false;
    }

    private static bool TryGetBoolFromUrl(string url, string key, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key)) return false;

        var queryIndex = url.IndexOf('?');
        if (queryIndex < 0 || queryIndex >= url.Length - 1) return false;

        var query = url.Substring(queryIndex + 1);
        var parts = query.Split('&');
        foreach (var part in parts)
        {
            if (TryParseBool(part, key, out value)) return true;
        }

        return false;
    }

    private static bool TryParseBool(string raw, string expectedKey, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expectedKey)) return false;

        var trimmed = raw.Trim().TrimStart('-', '/');
        var tokens = trimmed.Split(new[] { '=' }, 2);
        if (tokens.Length != 2) return false;
        if (!string.Equals(tokens[0], expectedKey, StringComparison.OrdinalIgnoreCase)) return false;

        var rawValue = tokens[1];
        if (bool.TryParse(rawValue, out var parsedBool))
        {
            value = parsedBool;
            return true;
        }

        if (int.TryParse(rawValue, out var parsedInt))
        {
            value = parsedInt != 0;
            return true;
        }

        return false;
    }

    private struct MinigameResult
    {
        internal MinigameResult(bool? ended, bool? won)
        {
            GameEnded = ended;
            GameWon = won;
        }

        internal bool? GameEnded;
        internal bool? GameWon;
    }

    private void SwitchToNode(string nodeKey)
    {
        if (string.IsNullOrWhiteSpace(nodeKey)) return;

        if (!_database.Nodes.TryGetValue(nodeKey, out var node))
        {
            if (verboseLogging) Debug.LogWarning($"Background: Node '{nodeKey}' not found in navigation data.");
            return;
        }

        // Avoid re-entering the same minigame node repeatedly (prevents recursion/stack overflows).
        if (node.NodeType == SceneNodeType.Minigame && string.Equals(nodeKey, _currentSlideKey, StringComparison.OrdinalIgnoreCase))
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"Background: Ignoring re-entry to completed minigame node '{nodeKey}'.");
            }
            return;
        }

        if (node.NodeType == SceneNodeType.Minigame)
        {
            HandleMinigameNode(node);
            return;
        }

        _currentSlideKey = node.Key;
        _persistedStartSlideKey = _currentSlideKey;
        PersistGameStateToSelectedFile();
        ApplyBackground(node);
        ProcessOnEnter(node);
        RebuildButtons(node);
    }

    private void ProcessOnEnter(SceneNode node)
    {
        if (_onEnterRoutine != null) StopCoroutine(_onEnterRoutine);
        if (node?.OnEnter == null || node.OnEnter.Count == 0) return;

        _onEnterRoutine = StartCoroutine(RunOnEnterActions(node));
    }

    private IEnumerator RunOnEnterActions(SceneNode node)
    {
        if (_actionProcessor == null) yield break;

        var blockStack = new Stack<string>();
        string skipLabel = null;
        var nestedSkips = 0;

        foreach (var action in node.OnEnter)
        {
            if (_currentSlideKey != node.Key) yield break;

            var command = action?.Command;
            if (string.IsNullOrWhiteSpace(command)) continue;

            var lower = command.ToLowerInvariant();
            if (lower == "startblock")
            {
                var label = GetActionLabel(action);
                if (!string.IsNullOrWhiteSpace(label))
                {
                    var pushed = false;
                    if (blockStack.Count == 0 || !string.Equals(blockStack.Peek(), label, StringComparison.OrdinalIgnoreCase))
                    {
                        blockStack.Push(label);
                        pushed = true;
                    }

                    if (pushed && skipLabel != null && string.Equals(skipLabel, label, StringComparison.OrdinalIgnoreCase)) nestedSkips++;
                }

                continue;
            }

            if (lower == "endblock")
            {
                if (blockStack.Count > 0)
                {
                    var label = blockStack.Pop();
                    if (skipLabel != null && string.Equals(label, skipLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        if (nestedSkips == 0)
                        {
                            skipLabel = null;
                        }
                        else
                        {
                            nestedSkips--;
                            if (nestedSkips == 0) skipLabel = null;
                        }
                    }
                }

                continue;
            }

            if (skipLabel != null) continue;

            if (lower == "sleep")
            {
                var waitSeconds = ParseSleep(action);
                if (waitSeconds > 0f) yield return new WaitForSeconds(waitSeconds);
                continue;
            }

            var result = _actionProcessor.Execute(action);
            if (result == ActionExecutionResult.StopChain)
            {
                yield break;
            }

            if (result == ActionExecutionResult.ConditionFailed)
            {
                if (blockStack.Count > 0)
                {
                    skipLabel = blockStack.Peek();
                    nestedSkips = 0;
                }
                else
                {
                    yield break;
                }

                continue;
            }
        }

        if (_currentSlideKey == node.Key)
        {
            RefreshCurrentSlideButtons();
        }
    }

    private static string GetActionLabel(ActionDefinition action)
    {
        return NavigationActionProcessor.TryGetParameter(action, out var label) ? label : null;
    }

    private static float ParseSleep(ActionDefinition action)
    {
        if (!NavigationActionProcessor.TryGetParameter(action, out var raw)) return 0f;
        var milliseconds = NavigationActionProcessor.ParseInt(raw, 0);
        return Mathf.Max(0f, milliseconds / 1000f);
    }

    [ContextMenu("Log Current Position")]
    public void LogCurrentPosition()
    {
        if (string.IsNullOrWhiteSpace(_currentSlideKey))
        {
            Debug.Log("Background: No slide loaded yet.");
            return;
        }

        Debug.Log($"Background: Current slide is '{_currentSlideKey}'.");
    }

    private void HandleMinigameNode(SceneNode node)
    {
        Debug.Log($"[Background] HandleMinigameNode called for: {node.Key}");
        Debug.Log($"[Background] MinigameClassName: {node.MinigameClassName}");

        if (IsCompletedMinigame(node))
        {
            var blockedTarget = !string.IsNullOrWhiteSpace(node.FallbackSlide) ? node.FallbackSlide : _currentSlideKey;
            if (verboseLogging)
            {
                Debug.Log($"Background: Minigame '{node.Key}' is already completed and cannot be started again.");
            }

            if (!string.IsNullOrWhiteSpace(blockedTarget) &&
                !string.Equals(blockedTarget, node.Key, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(blockedTarget, _currentSlideKey, StringComparison.OrdinalIgnoreCase))
            {
                SwitchToNode(blockedTarget);
            }

            return;
        }
        
        var entrySlide = _currentSlideKey;
        var fallbackSlide = !string.IsNullOrWhiteSpace(node.FallbackSlide) ? node.FallbackSlide : null;

        if (!PersistGameStateToSelectedFile() && verboseLogging)
        {
            Debug.LogWarning("Background: Failed to persist game state before launching minigame.");
        }

        MinigameReturnState.SetReturnState(entrySlide, fallbackSlide, node?.Key);
        var defaultReturn = fallbackSlide ?? entrySlide;

        if (MinigameFactory.TryCreate(node.MinigameClassName, verboseLogging, out var minigame))
        {
            Debug.Log($"[Background] Minigame factory successfully created: {node.MinigameClassName}");
            var context = new MinigameLaunchContext(node.Key, entrySlide, fallbackSlide, verboseLogging);
            if (minigame.Launch(context))
            {
                Debug.Log($"[Background] Minigame launched successfully");
                return;
            }

            Debug.LogWarning($"[Background] Minigame Launch() returned false");
            
            if (!string.IsNullOrWhiteSpace(defaultReturn))
            {
                if (verboseLogging) Debug.LogWarning($"Background: Launch failed for minigame '{node.MinigameClassName}', routing to '{defaultReturn}'.");
                SwitchToNode(defaultReturn);
                return;
            }
        }
        else
        {
            Debug.LogError($"[Background] MinigameFactory.TryCreate FAILED for: {node.MinigameClassName}");
        }

        if (!string.IsNullOrWhiteSpace(fallbackSlide))
        {
            if (verboseLogging) Debug.Log($"Background: Node '{node.Key}' is a minigame. Redirecting to fallback '{fallbackSlide}'.");
            SwitchToNode(fallbackSlide);
            return;
        }

        Debug.LogWarning($"Background: Node '{node.Key}' is a minigame. No handler configured for '{node.MinigameClassName}'.");
    }

    private void ApplyBackground(SceneNode node)
    {
        if (_backgroundImage == null) return;

        var sprite = LoadSprite(node.ImagePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Background: Failed to load background sprite '{node.ImagePath}' for node '{node.Key}'.");
            _backgroundImage.sprite = null;
            ApplyReferenceToCanvas(_defaultReferenceSize);
            _backgroundDisplayRect = new Rect(0f, 0f, _defaultReferenceSize.x, _defaultReferenceSize.y);
            return;
        }

        // Keep reference space fixed to the authored coordinate system (1620x1080)
        ApplyReferenceToCanvas(_defaultReferenceSize);
        _backgroundImage.sprite = sprite;
        _backgroundImage.preserveAspect = true;
        _backgroundDisplayRect = CalculateBackgroundDisplayRect(sprite);
        ApplyCoordinateAlignedLayout();
    }

    private void RebuildButtons(SceneNode node)
    {
        ClearButtons();

        foreach (var definition in node.Buttons.Values)
        {
            if (!ShouldRenderButton(node, definition)) continue;

            switch (definition.Kind)
            {
                case ButtonType.Preset:
                    CreatePresetButton(node, definition);
                    break;
                case ButtonType.Polygon:
                    CreatePolygonButton(node, definition);
                    break;
                case ButtonType.Image:
                    CreateImageButton(node, definition);
                    break;
            }
        }
    }

    private bool ShouldRenderButton(SceneNode node, ButtonDefinition definition)
    {
        if (_gameState == null) return definition.Visible;

        var stateKey = $"{node.Key}.{definition.Key}";
        _gameState.SetInitialValue(stateKey, definition.Visible, !_gameState.HasPersistedState);

        if (ShouldDisableButtonForCompletedMinigame(definition))
        {
            return false;
        }

        if (_gameState.TryGetFlag(stateKey, out var state)) return state;

        return definition.Visible;
    }

    private bool ShouldDisableButtonForCompletedMinigame(ButtonDefinition definition)
    {
        if (_database == null || definition?.Actions == null || definition.Actions.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < definition.Actions.Count; i++)
        {
            var action = definition.Actions[i];
            if (action == null || string.IsNullOrWhiteSpace(action.Command) || action.Parameters == null || action.Parameters.Count == 0)
            {
                continue;
            }

            if (!string.Equals(action.Command, "route", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var target = action.Parameters[0];
            if (string.IsNullOrWhiteSpace(target))
            {
                continue;
            }

            if (!_database.Nodes.TryGetValue(target, out var targetNode))
            {
                continue;
            }

            if (targetNode.NodeType != SceneNodeType.Minigame)
            {
                continue;
            }

            if (IsCompletedMinigame(targetNode))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshCurrentSlideButtons()
    {
        if (string.IsNullOrWhiteSpace(_currentSlideKey) || _database == null) return;
        if (_database.Nodes.TryGetValue(_currentSlideKey, out var node))
        {
            RebuildButtons(node);
        }
    }

    private void ClearButtons()
    {
        foreach (var button in _activeButtons)
        {
            if (button != null) Destroy(button);
        }

        _activeButtons.Clear();

        // If buttons disappear (e.g., when switching slides), the cursor may stay stuck
        // on the last hotspot because OnPointerExit won't fire. Force a reset here.
        PolygonHotspot.ResetSharedCursor();
    }

    private void CreatePresetButton(SceneNode node, ButtonDefinition definition)
    {
        var preset = ResolvePreset(definition);
        if (preset == null) return;

        var buttonGo = new GameObject($"Preset_{node.Key}_{definition.Key}", typeof(RectTransform), typeof(CanvasRenderer), typeof(PolygonHotspot));
        buttonGo.transform.SetParent(_buttonContainer, false);

        var cursorSprite = ResolveCursorSprite(definition);
        var rect = buttonGo.GetComponent<RectTransform>();
        var size = ApplyPresetRect(rect, preset, cursorSprite);

        var polygon = BuildRectanglePoints(size.x, size.y);
        var hotspot = buttonGo.GetComponent<PolygonHotspot>();
        hotspot.Initialize(rect, polygon, () => HandleButtonActions(definition), cursorSprite);

        var polygonGraphic = buttonGo.AddComponent<PolygonGraphic>();
        polygonGraphic.raycastTarget = true;
        polygonGraphic.SetPolygon(polygon, GetHotspotColor(true));

        _activeButtons.Add(buttonGo);
    }

    private void CreatePolygonButton(SceneNode node, ButtonDefinition definition)
    {
        var points = definition.PolygonPoints;
        if (points == null || points.Count < 3)
        {
            Debug.LogWarning($"Background: Polygon button '{definition.Key}' in '{node.Key}' requires at least 3 points.");
            return;
        }

        var displayRect = GetActiveDisplayRect();
        float refWidth = NavigationDataLoader.ReferenceWidth;
        float refHeight = NavigationDataLoader.ReferenceHeight;

        // Hall1 uses a background whose native width differs from the standard 1620px;
        // adjust the horizontal reference so its polygons line up with that source image.
        if (_backgroundImage != null &&
            _backgroundImage.sprite != null &&
            string.Equals(node.Key, "Hall1", StringComparison.OrdinalIgnoreCase))
        {
            refWidth = Mathf.Max(1f, _backgroundImage.sprite.rect.width);
            // Keep Y scaling on the standard reference to avoid shifting vertical alignment.
        }

        var scaleX = displayRect.width / Mathf.Max(1f, refWidth);
        var scaleY = displayRect.height / Mathf.Max(1f, refHeight);
        var displayYMax = displayRect.yMin + displayRect.height;

        var scaledPoints = new List<Vector2>(points.Count);
        foreach (var point in points)
        {
            var x = displayRect.xMin + point.x * scaleX;
            var y = displayYMax - point.y * scaleY; // convert top-left origin to bottom-left UI space
            x = Mathf.Clamp(x, displayRect.xMin, displayRect.xMax);
            y = Mathf.Clamp(y, displayRect.yMin, displayRect.yMax);
            scaledPoints.Add(new Vector2(x, y));
        }

        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var point in scaledPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        var width = maxX - minX;
        var height = maxY - minY;
        if (width <= 0f || height <= 0f)
        {
            Debug.LogWarning($"Background: Polygon button '{definition.Key}' in '{node.Key}' computed invalid size.");
            return;
        }

        var polygonGo = new GameObject($"Polygon_{node.Key}_{definition.Key}", typeof(RectTransform), typeof(CanvasRenderer), typeof(PolygonHotspot));
        polygonGo.transform.SetParent(_buttonContainer, false);

        var rect = polygonGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(minX, minY);

        var localPoints = new List<Vector2>(scaledPoints.Count);
        foreach (var point in scaledPoints) localPoints.Add(new Vector2(point.x - minX, point.y - minY));

        var polygonGraphic = polygonGo.AddComponent<PolygonGraphic>();
        polygonGraphic.raycastTarget = true;
        polygonGraphic.SetPolygon(localPoints, GetHotspotColor(false));

        var hotspot = polygonGo.GetComponent<PolygonHotspot>();
        hotspot.Initialize(rect, localPoints, () => HandleButtonActions(definition), ResolveCursorSprite(definition));

        _activeButtons.Add(polygonGo);
    }

    private void CreateImageButton(SceneNode node, ButtonDefinition definition)
    {
        if (_buttonContainer == null)
        {
            if (verboseLogging) Debug.LogWarning("Background: Button container missing; cannot create image button.");
            return;
        }

        var rectData = definition.Rectangle;
        if (rectData.width <= 0f || rectData.height <= 0f)
        {
            if (verboseLogging) Debug.LogWarning($"Background: Image button '{definition.Key}' in '{node.Key}' has invalid size.");
            return;
        }

        var displayRect = GetActiveDisplayRect();
        GetReferenceScale(displayRect, out var scaleX, out var scaleY);

        var width = Mathf.Max(0f, rectData.width * scaleX);
        var height = Mathf.Max(0f, rectData.height * scaleY);
        var posX = displayRect.xMin + rectData.x * scaleX;
        var posY = displayRect.yMin + displayRect.height - (rectData.y + rectData.height) * scaleY;

        var hasActions = definition.Actions != null && definition.Actions.Count > 0;
        var buttonGo = hasActions
            ? new GameObject($"Image_{node.Key}_{definition.Key}", typeof(RectTransform), typeof(CanvasRenderer), typeof(PolygonHotspot))
            : new GameObject($"Image_{node.Key}_{definition.Key}", typeof(RectTransform), typeof(CanvasRenderer));
        buttonGo.transform.SetParent(_buttonContainer, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(posX, posY);

        var buttonSprite = ResolveButtonSprite(definition) ?? (_fallbackButtonSprite ?? TryLoadFallbackSprite());
        if (buttonSprite == null)
        {
            if (verboseLogging) Debug.LogWarning($"Background: No sprite available for image button '{definition.Key}' in '{node.Key}'.");
        }
        var polygon = BuildRectanglePoints(width, height);
        Graphic raycastGraphic = buttonGo.GetComponent<Graphic>();

        if (hasActions)
        {
            var hotspot = buttonGo.GetComponent<PolygonHotspot>() ?? buttonGo.AddComponent<PolygonHotspot>();
            var cursorSprite = ResolveCursorSprite(definition, buttonSprite);
            hotspot.Initialize(rect, polygon, () => HandleButtonActions(definition), cursorSprite);

            if (showHotspotOverlay && buttonSprite == null)
            {
                var polygonGraphic = buttonGo.AddComponent<PolygonGraphic>();
                polygonGraphic.raycastTarget = true;
                polygonGraphic.SetPolygon(polygon, GetHotspotColor(false));
                raycastGraphic = polygonGraphic;
            }
        }

        if (buttonSprite != null && buttonGo.GetComponent<Graphic>() == null)
        {
            var image = buttonGo.AddComponent<Image>();
            image.sprite = buttonSprite;
            image.preserveAspect = true;
            raycastGraphic = image;
        }

        // Ensure interactive image buttons always have a raycast target so hover/click events and cursors stay accurate.
        if (hasActions && raycastGraphic == null)
        {
            var polygonGraphic = buttonGo.AddComponent<PolygonGraphic>();
            polygonGraphic.SetPolygon(polygon, showHotspotOverlay ? GetHotspotColor(false) : new Color(0f, 0f, 0f, HotspotRaycastAlphaFloor));
            raycastGraphic = polygonGraphic;
        }

        if (raycastGraphic != null)
        {
            raycastGraphic.raycastTarget = hasActions;
        }

        _activeButtons.Add(buttonGo);
    }

    private void HandleButtonActions(ButtonDefinition definition)
    {
        if (_actionProcessor == null) return;

        var initialKey = _currentSlideKey;
        var result = _actionProcessor.Handle(definition);

        if (initialKey == _currentSlideKey && result != ActionExecutionResult.StopChain)
        {
            RefreshCurrentSlideButtons();
        }
    }

    private void HandleNavigationKey(KeyCode key)
    {
        if (_database == null || string.IsNullOrWhiteSpace(_currentSlideKey)) return;
        if (!_database.Nodes.TryGetValue(_currentSlideKey, out var node)) return;

        var navigationButton = FindNavigationButton(node, key);
        if (navigationButton == null) return;

        HandleButtonActions(navigationButton);
    }

    private ButtonDefinition FindNavigationButton(SceneNode node, KeyCode key)
    {
        if (node?.Buttons == null || !NavigationKeyLookup.TryGetValue(key, out var candidates)) return null;

        ButtonDefinition fallback = null;

        foreach (var candidate in candidates)
        {
            if (!node.Buttons.TryGetValue(candidate, out var direct) || !ShouldRenderButton(node, direct)) continue;
            if (direct.Kind == ButtonType.Preset) return direct;
            fallback ??= direct;
        }

        foreach (var definition in node.Buttons.Values)
        {
            if (definition == null || !ShouldRenderButton(node, definition)) continue;
            if (!MatchesNavigationKey(definition, candidates)) continue;

            if (definition.Kind == ButtonType.Preset) return definition;
            fallback ??= definition;
        }

        return fallback;
    }

    private static bool MatchesNavigationKey(ButtonDefinition definition, IReadOnlyList<string> keys)
    {
        foreach (var candidate in keys)
        {
            if (string.Equals(definition.Key, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrWhiteSpace(definition.PresetKey) && string.Equals(definition.PresetKey, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrWhiteSpace(definition.ImageKey) && string.Equals(definition.ImageKey, candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private Vector2 ApplyPresetRect(RectTransform rect, ButtonPreset preset, Sprite sprite)
    {
        var displayRect = GetActiveDisplayRect();

        var targetWidth = Mathf.Max(1f, displayRect.width);
        var targetHeight = Mathf.Max(1f, displayRect.height);

        preset.GetScaledRect(targetWidth, targetHeight, out var left, out var top, out var width, out var height);
        width = Mathf.Clamp(width, 0f, targetWidth);
        height = Mathf.Clamp(height, 0f, targetHeight);

        float rightShift = 175f * (targetWidth / NavigationDataLoader.ReferenceWidth);
        // shift right arrow button
        if (string.Equals(preset.Key, "right", StringComparison.OrdinalIgnoreCase))
        {
            left += rightShift;
        }

        // Keep preset fully inside the visible background area
        left = Mathf.Clamp(left, 0f, Mathf.Max(0f, targetWidth - width));
        top = Mathf.Clamp(top, 0f, Mathf.Max(0f, targetHeight - height));

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(
            displayRect.xMin + left,
            displayRect.yMin + (targetHeight - (top + height))
        );

        return new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
    }

    private List<Vector2> BuildRectanglePoints(float width, float height)
    {
        return new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(width, 0f),
            new Vector2(width, height),
            new Vector2(0f, height)
        };
    }

    private Color GetHotspotColor(bool isNavigationHotspot)
    {
        if (!showHotspotOverlay) return new Color(0f, 0f, 0f, HotspotRaycastAlphaFloor);

        if (!isNavigationHotspot)
        {
            var tintAlpha = hotspotTint.a > 0f ? hotspotTint.a : hotspotAlpha;
            return new Color(hotspotTint.r, hotspotTint.g, hotspotTint.b, Mathf.Max(HotspotRaycastAlphaFloor, Mathf.Clamp01(tintAlpha)));
        }

        return new Color(0f, 0f, 0f, Mathf.Max(HotspotRaycastAlphaFloor, Mathf.Clamp01(hotspotAlpha)));
    }

    private ButtonPreset ResolvePreset(ButtonDefinition definition)
    {
        var presetKey = !string.IsNullOrWhiteSpace(definition.PresetKey) ? definition.PresetKey : definition.Key;
        if (string.IsNullOrWhiteSpace(presetKey))
        {
            Debug.LogWarning($"Background: Button '{definition.Key}' has no preset identifier.");
            return null;
        }

        if (_database.Presets.TryGetValue(presetKey, out var preset)) return preset;

        preset = CreateDefaultPreset(presetKey);
        if (preset != null)
        {
            _database.Presets[presetKey] = preset;
            if (verboseLogging)
            {
                Debug.LogWarning($"Background: Preset '{presetKey}' not found. Generated fallback preset.");
            }
            return preset;
        }

        Debug.LogWarning($"Background: Preset '{presetKey}' not found for button '{definition.Key}'.");
        return null;
    }

    private ButtonPreset CreateDefaultPreset(string key)
    {
        float x;
        float y;
        float width;
        float height;

        switch (key.ToLowerInvariant())
        {
            case "left":
                x = 0f;
                y = 340f;
                width = 150f;
                height = 400f;
                break;
            case "right":
                x = 1470f;
                y = 340f;
                width = 150f;
                height = 400f;
                break;
            case "top":
            case "up":
                x = 610f;
                y = 0f;
                width = 400f;
                height = 150f;
                break;
            case "bottom":
            case "down":
            case "back":
                x = 610f;
                y = 930f;
                width = 400f;
                height = 150f;
                break;
            default:
                width = 200f;
                height = 200f;
                x = (NavigationDataLoader.ReferenceWidth - width) * 0.5f;
                y = (NavigationDataLoader.ReferenceHeight - height) * 0.5f;
                break;
        }

        return new ButtonPreset(key, $"auto-{key}", x, y, width, height);
    }

    private Sprite ResolveButtonSprite(ButtonDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.ExplicitImagePath)) return LoadSprite(definition.ExplicitImagePath, logOnMissing: false);

        var keySprite = TryResolveSpriteByKey(definition.ImageKey);
        if (keySprite != null) return keySprite;

        var presetSprite = TryResolveSpriteByKey(definition.PresetKey);
        return presetSprite;
    }

    private Sprite ResolveCursorSprite(ButtonDefinition definition, Sprite buttonSprite = null)
    {
        var arrowKey = ResolveArrowKey(definition);
        if (!string.IsNullOrWhiteSpace(arrowKey))
        {
            var arrowSprite = TryResolveSpriteByKey(arrowKey);
            if (arrowSprite != null) return arrowSprite;
        }

        return buttonSprite ?? ResolveButtonSprite(definition);
    }

    private static string ResolveArrowKey(ButtonDefinition definition)
    {
        if (definition == null) return null;

        var candidates = new[] { definition.PresetKey, definition.ImageKey, definition.Key };
        for (var i = 0; i < candidates.Length; i++)
        {
            if (TryNormalizeArrowKey(candidates[i], out var normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static bool TryNormalizeArrowKey(string key, out string normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(key)) return false;

        var trimmed = key.Trim();
        if (!CursorArrowKeys.Contains(trimmed)) return false;

        if (trimmed.Equals("top", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "up";
            return true;
        }

        if (trimmed.Equals("bottom", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("back", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "down";
            return true;
        }

        normalized = trimmed.ToLowerInvariant();
        return true;
    }

    private Sprite TryResolveSpriteByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (_database.ButtonSpriteLookup.TryGetValue(key, out var directPath))
        {
            var directSprite = LoadSprite(directPath, logOnMissing: false);
            if (directSprite != null) return directSprite;
        }

        if (SpriteKeyAliases.TryGetValue(key, out var aliases))
        {
            foreach (var alias in aliases)
            {
                if (_database.ButtonSpriteLookup.TryGetValue(alias, out var aliasPath))
                {
                    var aliasSprite = LoadSprite(aliasPath, logOnMissing: false);
                    if (aliasSprite != null) return aliasSprite;
                }
            }
        }

        var fallbackSprite = LoadSprite($"images/UI_Images/{key}.png", logOnMissing: false) ?? LoadSprite($"images/UI_Images/arrows/{key}.png", logOnMissing: false);
        if (fallbackSprite != null) return fallbackSprite;

        if (SpriteKeyAliases.TryGetValue(key, out var aliasCandidates))
        {
            foreach (var alias in aliasCandidates)
            {
                var sprite = LoadSprite($"images/UI_Images/{alias}.png", logOnMissing: false) ?? LoadSprite($"images/UI_Images/arrows/{alias}.png", logOnMissing: false);
                if (sprite != null) return sprite;
            }
        }

        return null;
    }

    private Sprite LoadSprite(string relativePath, bool logOnMissing = true)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var resourcePath = ToResourcePath(relativePath);
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            var resourceSprite = Resources.Load<Sprite>(resourcePath);
            if (resourceSprite != null) return resourceSprite;
        }

        var absolutePath = ResolveAbsolutePath(relativePath, logOnMissing);
        if (absolutePath == null) return null;

        if (_spriteCache.TryGetValue(absolutePath, out var cached)) return cached.Sprite;

        if (!File.Exists(absolutePath))
        {
            if (logOnMissing) Debug.LogWarning($"Background: Sprite file not found at '{absolutePath}' (requested '{relativePath}').");
            return null;
        }

        try
        {
            var binary = File.ReadAllBytes(absolutePath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, binary))
            {
                Destroy(texture);
                Debug.LogWarning($"Background: Failed to decode image '{absolutePath}'.");
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            _spriteCache[absolutePath] = new SpriteRecord(sprite, texture);
            return sprite;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Background: Exception while loading sprite '{absolutePath}'. {ex.Message}");
            return null;
        }
    }

    private static string ToResourcePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return null;

        var sanitized = rawPath.Replace("\\", "/").Trim().TrimStart('/');
        if (sanitized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = sanitized.Substring("Assets/".Length);
        }

        var resourcesIndex = sanitized.IndexOf("resources/", StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
        {
            sanitized = sanitized.Substring(resourcesIndex + "resources/".Length);
        }

        var extension = Path.GetExtension(sanitized);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            sanitized = sanitized.Substring(0, sanitized.Length - extension.Length);
        }

        return sanitized;
    }

    private string ResolveAbsolutePath(string rawPath, bool logOnMissing)
    {
        if (string.IsNullOrWhiteSpace(rawPath)) return null;

        var sanitized = rawPath.Replace("\\", "/").TrimStart('/');
        if (sanitized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) sanitized = sanitized.Substring("Assets/".Length);
        if (sanitized.StartsWith("resources/", StringComparison.OrdinalIgnoreCase)) sanitized = sanitized.Substring("resources/".Length);

        if (_pathCache.TryGetValue(sanitized, out var cached))
        {
            if (cached == null && logOnMissing && verboseLogging) Debug.LogWarning($"Background: Unable to resolve path for '{rawPath}'.");
            return cached;
        }

        var candidatePaths = new[]
        {
            Path.Combine(_resourcesRoot, sanitized.Replace('/', Path.DirectorySeparatorChar)),
            Path.Combine(_resourcesRoot, "images", sanitized.Replace('/', Path.DirectorySeparatorChar))
        };

        foreach (var candidate in candidatePaths)
        {
            if (!File.Exists(candidate)) continue;
            _pathCache[sanitized] = candidate;
            return candidate;
        }

        var fileName = Path.GetFileName(sanitized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            try
            {
                var matches = Directory.GetFiles(_resourcesRoot, fileName, SearchOption.AllDirectories);
                if (matches.Length > 0)
                {
                    _pathCache[sanitized] = matches[0];
                    return matches[0];
                }
            }
            catch (Exception ex)
            {
                if (verboseLogging) Debug.LogWarning($"Background: Failed during fallback search for '{rawPath}'. {ex.Message}");
            }
        }

        if (logOnMissing && verboseLogging) Debug.LogWarning($"Background: Unable to resolve path for '{rawPath}'.");
        _pathCache[sanitized] = null;
        return null;
    }

    private void ApplyReferenceToCanvas(Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f) size = _defaultReferenceSize;

        _currentReferenceSize = size;
        referenceResolution = size;

        if (_canvasScaler != null) _canvasScaler.referenceResolution = size;

        var inventoryReference = _defaultReferenceSize;
        if (inventoryReference.x <= 0f || inventoryReference.y <= 0f)
        {
            inventoryReference = new Vector2(NavigationDataLoader.ReferenceWidth, NavigationDataLoader.ReferenceHeight);
        }

        if (_inventoryCanvasScaler != null) _inventoryCanvasScaler.referenceResolution = inventoryReference;

        if (_backgroundImage != null)
        {
            var bgRect = _backgroundImage.rectTransform;
            // Keep the background anchored to the full canvas; actual drawn area
            // is handled separately via _backgroundDisplayRect.
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }

        ApplyCoordinateAlignedLayout();
    }

    private Vector2 ScaleToCurrentReference(Vector2 coordinateValue)
    {
        var width = Mathf.Max(1f, NavigationDataLoader.ReferenceWidth);
        var height = Mathf.Max(1f, NavigationDataLoader.ReferenceHeight);
        return new Vector2(coordinateValue.x * _currentReferenceSize.x / width, coordinateValue.y * _currentReferenceSize.y / height);
    }

    private int ScaleFontSize(int baseSize)
    {
        var scaled = Mathf.RoundToInt(baseSize * ScaleY);
        return scaled < 1 ? 1 : scaled;
    }

    private Rect CalculateBackgroundDisplayRect(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Rect(0f, 0f, _currentReferenceSize.x, _currentReferenceSize.y);
        }

        var containerSize = GetContainerSize();
        var spriteSize = sprite.rect.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f || containerSize.x <= 0f || containerSize.y <= 0f)
        {
            return new Rect(0f, 0f, _currentReferenceSize.x, _currentReferenceSize.y);
        }

        var scale = Mathf.Min(containerSize.x / spriteSize.x, containerSize.y / spriteSize.y);
        var displaySize = spriteSize * scale;
        var offset = (containerSize - displaySize) * 0.5f;

        return new Rect(offset.x, offset.y, displaySize.x, displaySize.y);
    }

    private Rect GetActiveDisplayRect()
    {
        return _backgroundDisplayRect.width > 0.01f
            ? _backgroundDisplayRect
            : new Rect(0f, 0f, _currentReferenceSize.x, _currentReferenceSize.y);
    }

    private static void GetReferenceScale(Rect displayRect, out float scaleX, out float scaleY)
    {
        scaleX = displayRect.width / Mathf.Max(1f, NavigationDataLoader.ReferenceWidth);
        scaleY = displayRect.height / Mathf.Max(1f, NavigationDataLoader.ReferenceHeight);
    }

    private Vector2 GetContainerSize()
    {
        if (_buttonContainer != null)
        {
            return _buttonContainer.rect.size;
        }

        if (_canvas != null)
        {
            var scaleFactor = Mathf.Approximately(_canvas.scaleFactor, 0f) ? 1f : _canvas.scaleFactor;
            var pixelRect = _canvas.pixelRect;
            return new Vector2(pixelRect.width / scaleFactor, pixelRect.height / scaleFactor);
        }

        return _currentReferenceSize;
    }

    private void ApplyCoordinateAlignedLayout()
    {
        var displayRect = GetActiveDisplayRect();
        var scaledSize = ScaleToCurrentReference(topBarButtonSize);
        var scaledMargin = ScaleToCurrentReference(topBarMargin);
        var scaledSpacing = ScaleToCurrentReference(new Vector2(topBarSpacing, topBarSpacing));

        var menuX = displayRect.xMin + Mathf.Max(0f, scaledMargin.x);
        var topY = displayRect.yMin + displayRect.height - scaledSize.y - Mathf.Max(0f, scaledMargin.y);
        var inventoryX = menuX + scaledSize.x + scaledSpacing.x;

        if (_inventoryButton != null)
        {
            var rect = _inventoryButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = scaledSize;
                rect.anchoredPosition = new Vector2(inventoryX, topY);
            }

            var label = _inventoryButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = ScaleFontSize(_inventoryBaseFontSize);
                label.enabled = false;
            }
        }

        if (_settingsButton != null)
        {
            var rect = _settingsButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = scaledSize;
                rect.anchoredPosition = new Vector2(menuX, topY);
            }

            var label = _settingsButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = ScaleFontSize(_settingsButtonBaseFontSize);
                label.enabled = false;
            }
        }

        if (_settingsPanel != null)
        {
            var size = ScaleToCurrentReference(new Vector2(560f, 420f));
            _settingsPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _settingsPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _settingsPanel.pivot = new Vector2(0.5f, 0.5f);
            _settingsPanel.sizeDelta = size;
            _settingsPanel.anchoredPosition = Vector2.zero;

            if (_settingsTitleLabel != null)
            {
                _settingsTitleLabel.fontSize = ScaleFontSize(SettingsTitleBaseFontSize);
            }

            if (_settingsSubtitleLabel != null)
            {
                _settingsSubtitleLabel.fontSize = ScaleFontSize(SettingsSubtitleBaseFontSize);
            }

            for (var i = 0; i < _settingsActionButtons.Count; i++)
            {
                var button = _settingsActionButtons[i];
                if (button == null) continue;
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.fontSize = ScaleFontSize(SettingsActionBaseFontSize);
            }

            if (_settingsStatusLabel != null)
            {
                _settingsStatusLabel.fontSize = ScaleFontSize(SettingsStatusBaseFontSize);
            }
        }

        if (_debugAddItemButton != null)
        {
            var rect = _debugAddItemButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                var size = ScaleToCurrentReference(new Vector2(220f, 64f));
                var offset = ScaleToCurrentReference(new Vector2(20f, 20f));
                rect.sizeDelta = size;
                rect.anchoredPosition = new Vector2(offset.x, -offset.y);
            }

            var label = _debugAddItemButton.GetComponentInChildren<Text>();
            if (label != null) label.fontSize = ScaleFontSize(_debugAddItemBaseFontSize);
        }
    }

    private Sprite TryLoadFallbackSprite()
    {
        return _fallbackButtonSprite;
    }

    private Sprite CreateFallbackSprite()
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var solid = new Color(1f, 1f, 1f, 1f);
        texture.SetPixels(new[] { solid, solid, solid, solid });
        texture.Apply();

        _fallbackTexture = texture;
        _ownsFallbackSprite = true;

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void BuildUiScaffold()
    {
        var existingCanvas = GetComponentInChildren<Canvas>();
        if (existingCanvas != null) _canvas = existingCanvas;
        else
        {
            var canvasGo = new GameObject("NavigationCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = true;
        }

        _canvasScaler = _canvas.GetComponent<CanvasScaler>() ?? _canvas.gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = referenceResolution;
        _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvasScaler.matchWidthOrHeight = 1f;

        if (_canvas.GetComponent<GraphicRaycaster>() == null) _canvas.gameObject.AddComponent<GraphicRaycaster>();

        var bgRect = _canvas.transform.Find("BackgroundImage") as RectTransform;
        if (bgRect == null)
        {
            var bgGo = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_canvas.transform, false);
            bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGo.transform.SetSiblingIndex(0);
        }

        _backgroundImage = bgRect.GetComponent<Image>();
        _backgroundImage.raycastTarget = false;

        var container = _canvas.transform.Find("ButtonContainer") as RectTransform;
        if (container == null)
        {
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(_canvas.transform, false);
            container = containerGo.GetComponent<RectTransform>();
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.SetAsLastSibling();
        }

        _buttonContainer = container;
    }

    private void SetupInventoryOverlay()
    {
        if (!enableInventoryWindow) return;

        var overlay = InventoryOverlayBootstrap.GetOverlayInstance();
        if (overlay == null)
        {
            if (verboseLogging) Debug.LogWarning("Background: Inventory overlay could not be initialized.");
            return;
        }

        _inventoryToggle = overlay.GetComponent<InventoryAnimatedToggle>() ?? overlay.AddComponent<InventoryAnimatedToggle>();
        if (_inventoryToggle.inventoryCanvas == null) _inventoryToggle.inventoryCanvas = overlay;
        _inventoryToggle.DisableInternalInput();

        _inventoryCanvas = overlay.GetComponent<Canvas>() ?? overlay.AddComponent<Canvas>();
        _inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _inventoryCanvas.overrideSorting = true;
        _inventoryCanvas.sortingOrder = (_canvas != null ? _canvas.sortingOrder + 10 : 100);
        _inventoryCanvasGroup = overlay.GetComponent<CanvasGroup>() ?? overlay.AddComponent<CanvasGroup>();

        _inventoryCanvasScaler = overlay.GetComponent<CanvasScaler>() ?? overlay.AddComponent<CanvasScaler>();
        _inventoryCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _inventoryCanvasScaler.referenceResolution = referenceResolution;
        _inventoryCanvasScaler.screenMatchMode = _canvasScaler != null ? _canvasScaler.screenMatchMode : CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _inventoryCanvasScaler.matchWidthOrHeight = _canvasScaler != null ? _canvasScaler.matchWidthOrHeight : 1f;

        if (overlay.GetComponent<GraphicRaycaster>() == null) overlay.AddComponent<GraphicRaycaster>();
    }

    private void SetupInventoryButton()
    {
        if (!enableInventoryWindow || _canvas == null) return;

        var existing = _canvas.transform.Find("InventoryToggleButton");
        if (existing != null)
        {
            _inventoryButton = existing.GetComponent<Button>();
            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.RemoveAllListeners();
                _inventoryButton.onClick.AddListener(ToggleInventoryWindow);
                var existingInventoryImage = _inventoryButton.GetComponent<Image>();
                if (existingInventoryImage != null)
                {
                    var existingInventoryIcon = LoadSprite(inventoryIconPath, logOnMissing: false);
                    if (existingInventoryIcon != null)
                    {
                        existingInventoryImage.sprite = existingInventoryIcon;
                        existingInventoryImage.type = Image.Type.Simple;
                        existingInventoryImage.preserveAspect = true;
                        existingInventoryImage.color = Color.white;
                    }
                }

                var inventoryLabel = _inventoryButton.GetComponentInChildren<Text>();
                if (inventoryLabel != null)
                {
                    if (inventoryLabel.fontSize > 0) _inventoryBaseFontSize = inventoryLabel.fontSize;
                    inventoryLabel.enabled = false;
                }
            }

            ApplyCoordinateAlignedLayout();
            return;
        }

        var buttonGo = new GameObject("InventoryToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(_canvas.transform, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = topBarButtonSize;
        rect.anchoredPosition = Vector2.zero;

        var newInventoryImage = buttonGo.GetComponent<Image>();
        var inventoryIconSprite = LoadSprite(inventoryIconPath, logOnMissing: false);
        newInventoryImage.sprite = inventoryIconSprite;
        newInventoryImage.type = Image.Type.Simple;
        newInventoryImage.preserveAspect = true;
        newInventoryImage.color = Color.white;

        _inventoryButton = buttonGo.GetComponent<Button>();
        _inventoryButton.onClick.AddListener(ToggleInventoryWindow);

        // Optional existing label becomes hidden (icons only)
        var labelGo = buttonGo.transform.Find("Label");
        if (labelGo != null)
        {
            var label = labelGo.GetComponent<Text>();
            if (label != null) label.enabled = false;
        }

        ApplyCoordinateAlignedLayout();
    }

    private void SetupSettingsMenu()
    {
        if (!enableSettingsMenu || _canvas == null)
        {
            return;
        }

        SetupSettingsButton();
        SetupSettingsPanel();
        SetSettingsStatus($"Active file: {Path.GetFileName(_selectedGameStatePath)}");
        ApplyCoordinateAlignedLayout();
    }

    private void SetupSettingsButton()
    {
        var existing = _canvas.transform.Find("SettingsToggleButton");
        if (existing != null)
        {
            _settingsButton = existing.GetComponent<Button>();
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(ToggleSettingsMenu);
                var existingLabel = _settingsButton.GetComponentInChildren<Text>();
                if (existingLabel != null && existingLabel.fontSize > 0)
                {
                    _settingsButtonBaseFontSize = existingLabel.fontSize;
                    existingLabel.enabled = false;
                }

                var existingImage = _settingsButton.GetComponent<Image>();
                if (existingImage != null)
                {
                    var existingMenuIcon = LoadSprite(menuIconPath, logOnMissing: false);
                    if (existingMenuIcon != null)
                    {
                        existingImage.sprite = existingMenuIcon;
                        existingImage.type = Image.Type.Simple;
                        existingImage.preserveAspect = true;
                        existingImage.color = Color.white;
                    }
                }

                StyleSettingsToggleButton(_settingsButton);
            }

            return;
        }

        var buttonGo = new GameObject("SettingsToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(_canvas.transform, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = topBarButtonSize;
        rect.anchoredPosition = Vector2.zero;

        var settingsImage = buttonGo.GetComponent<Image>();
        var menuIconSprite = LoadSprite(menuIconPath, logOnMissing: false);
        settingsImage.sprite = menuIconSprite;
        settingsImage.type = Image.Type.Simple;
        settingsImage.preserveAspect = true;
        settingsImage.color = Color.white;

        _settingsButton = buttonGo.GetComponent<Button>();
        _settingsButton.onClick.AddListener(ToggleSettingsMenu);
        StyleSettingsToggleButton(_settingsButton);

        // Remove text label for icon-only button
        var labelGo = buttonGo.transform.Find("Label");
        if (labelGo != null)
        {
            Destroy(labelGo.gameObject);
        }
    }

    private void SetupSettingsPanel()
    {
        var existing = _canvas.transform.Find("SettingsPanel");
        if (existing != null)
        {
            _settingsPanel = existing as RectTransform;
            _settingsStatusLabel = existing.Find("StatusCard/StatusLabel")?.GetComponent<Text>() ?? existing.Find("StatusLabel")?.GetComponent<Text>();
            _settingsTitleLabel = existing.Find("Header/TitleLabel")?.GetComponent<Text>() ?? existing.Find("TitleLabel")?.GetComponent<Text>();
            _settingsSubtitleLabel = existing.Find("Header/SubtitleLabel")?.GetComponent<Text>();
            _settingsActionButtons.Clear();
            foreach (var button in existing.GetComponentsInChildren<Button>(includeInactive: true))
            {
                _settingsActionButtons.Add(button);
                switch (button.name)
                {
                    case "SelectGameStateButton":
                        StyleSettingsActionButton(button, new Color(0.20f, 0.36f, 0.57f, 0.96f));
                        break;
                    case "ExportGameStateButton":
                        StyleSettingsActionButton(button, new Color(0.21f, 0.46f, 0.36f, 0.96f));
                        break;
                    case "ResetGameStateButton":
                        StyleSettingsActionButton(button, new Color(0.58f, 0.25f, 0.26f, 0.96f));
                        break;
                }
            }

            NormalizeSettingsStatusCardLayout();

            if (_settingsPanel != null)
            {
                _settingsPanel.gameObject.SetActive(false);
            }

            return;
        }

        var panelGo = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
        panelGo.transform.SetParent(_canvas.transform, false);

        _settingsPanel = panelGo.GetComponent<RectTransform>();
        _settingsPanel.anchorMin = new Vector2(1f, 0f);
        _settingsPanel.anchorMax = new Vector2(1f, 0f);
        _settingsPanel.pivot = new Vector2(1f, 0f);
        _settingsPanel.sizeDelta = new Vector2(560f, 420f);
        _settingsPanel.anchoredPosition = new Vector2(-30f, 110f);

        var panelImage = panelGo.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.12f, 0.19f, 0.94f);

        var panelOutline = panelGo.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.34f, 0.49f, 0.71f, 0.92f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        var panelShadow = panelGo.GetComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        panelShadow.effectDistance = new Vector2(0f, -3f);

        var headerGo = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerGo.transform.SetParent(panelGo.transform, false);
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 104f);
        headerRect.anchoredPosition = Vector2.zero;
        var headerImage = headerGo.GetComponent<Image>();
        headerImage.color = new Color(0.14f, 0.24f, 0.36f, 0.96f);

        var titleGo = new GameObject("TitleLabel", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(headerGo.transform, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(500f, 46f);
        titleRect.anchoredPosition = new Vector2(0f, -16f);

        _settingsTitleLabel = titleGo.GetComponent<Text>();
        _settingsTitleLabel.text = "Game Save Settings";
        _settingsTitleLabel.alignment = TextAnchor.MiddleCenter;
        _settingsTitleLabel.color = new Color(0.95f, 0.98f, 1f, 1f);
        _settingsTitleLabel.fontSize = SettingsTitleBaseFontSize;
        _settingsTitleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var subtitleGo = new GameObject("SubtitleLabel", typeof(RectTransform), typeof(Text));
        subtitleGo.transform.SetParent(headerGo.transform, false);
        var subtitleRect = subtitleGo.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.sizeDelta = new Vector2(500f, 32f);
        subtitleRect.anchoredPosition = new Vector2(0f, -58f);

        _settingsSubtitleLabel = subtitleGo.GetComponent<Text>();
        _settingsSubtitleLabel.text = "Select, export, or reset game + inventory state";
        _settingsSubtitleLabel.alignment = TextAnchor.MiddleCenter;
        _settingsSubtitleLabel.color = new Color(0.80f, 0.90f, 1f, 0.92f);
        _settingsSubtitleLabel.fontSize = SettingsSubtitleBaseFontSize;
        _settingsSubtitleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _settingsActionButtons.Clear();
        _settingsActionButtons.Add(CreateSettingsActionButton(panelGo.transform, "SelectGameStateButton", "Select gamestates.json", new Vector2(0f, -130f), SelectGameStateFile, new Color(0.20f, 0.36f, 0.57f, 0.96f)));
        _settingsActionButtons.Add(CreateSettingsActionButton(panelGo.transform, "ExportGameStateButton", "Export gamestates.json", new Vector2(0f, -198f), ExportSelectedGameState, new Color(0.21f, 0.46f, 0.36f, 0.96f)));
        _settingsActionButtons.Add(CreateSettingsActionButton(panelGo.transform, "ResetGameStateButton", "Reset game + inventory", new Vector2(0f, -266f), ResetGameStateToBase, new Color(0.58f, 0.25f, 0.26f, 0.96f)));

        var statusCardGo = new GameObject("StatusCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statusCardGo.transform.SetParent(panelGo.transform, false);
        var statusCardRect = statusCardGo.GetComponent<RectTransform>();
        statusCardRect.anchorMin = new Vector2(0.5f, 0f);
        statusCardRect.anchorMax = new Vector2(0.5f, 0f);
        statusCardRect.pivot = new Vector2(0.5f, 0f);
        statusCardRect.sizeDelta = new Vector2(500f, 124f);
        statusCardRect.anchoredPosition = new Vector2(0f, 20f);
        var statusCardImage = statusCardGo.GetComponent<Image>();
        statusCardImage.color = new Color(0.06f, 0.10f, 0.16f, 0.92f);

        var statusGo = new GameObject("StatusLabel", typeof(RectTransform), typeof(Text));
        statusGo.transform.SetParent(statusCardGo.transform, false);
        var statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(14f, 12f);
        statusRect.offsetMax = new Vector2(-14f, -12f);

        _settingsStatusLabel = statusGo.GetComponent<Text>();
        _settingsStatusLabel.text = string.Empty;
        _settingsStatusLabel.alignment = TextAnchor.UpperLeft;
        _settingsStatusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        _settingsStatusLabel.verticalOverflow = VerticalWrapMode.Overflow;
        _settingsStatusLabel.color = new Color(0.89f, 0.93f, 0.98f, 1f);
        _settingsStatusLabel.fontSize = SettingsStatusBaseFontSize;
        _settingsStatusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        NormalizeSettingsStatusCardLayout();
        panelGo.SetActive(false);
        panelGo.transform.SetAsLastSibling();
    }

    private void NormalizeSettingsStatusCardLayout()
    {
        if (_settingsPanel == null) return;

        var statusCardRect = _settingsPanel.Find("StatusCard") as RectTransform;
        if (statusCardRect == null && _settingsStatusLabel != null)
        {
            statusCardRect = _settingsStatusLabel.transform.parent as RectTransform;
        }

        if (statusCardRect != null)
        {
            statusCardRect.anchorMin = new Vector2(0.5f, 0f);
            statusCardRect.anchorMax = new Vector2(0.5f, 0f);
            statusCardRect.pivot = new Vector2(0.5f, 0f);
            statusCardRect.sizeDelta = new Vector2(500f, 78f);
            statusCardRect.anchoredPosition = new Vector2(0f, 12f);
        }

        if (_settingsStatusLabel == null) return;

        var statusRect = _settingsStatusLabel.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = new Vector2(14f, 10f);
        statusRect.offsetMax = new Vector2(-14f, -10f);
    }

    private Button CreateSettingsActionButton(Transform parent, string name, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick, Color baseColor)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(500f, 54f);
        rect.anchoredPosition = anchoredPosition;

        var button = buttonGo.GetComponent<Button>();
        StyleSettingsActionButton(button, baseColor);
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(buttonGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.96f, 0.98f, 1f, 1f);
        label.fontSize = SettingsActionBaseFontSize;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return button;
    }

    private static void StyleSettingsToggleButton(Button button)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            if (image.sprite != null)
            {
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.13f, 0.22f, 0.32f, 0.9f);
            }
        }

        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        if (image != null && image.sprite != null)
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
        }
        else
        {
            colors.normalColor = new Color(0.13f, 0.22f, 0.32f, 0.95f);
            colors.highlightedColor = new Color(0.18f, 0.30f, 0.44f, 0.98f);
            colors.pressedColor = new Color(0.10f, 0.17f, 0.27f, 1f);
            colors.selectedColor = colors.highlightedColor;
        }
        colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void StyleSettingsActionButton(Button button, Color baseColor)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = baseColor;
        }

        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.15f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void ToggleSettingsMenu()
    {
        if (_settingsPanel == null)
        {
            return;
        }

        var isOpen = !_settingsPanel.gameObject.activeSelf;
        _settingsPanel.gameObject.SetActive(isOpen);
        if (isOpen)
        {
            SetSettingsStatus($"Active file: {_selectedGameStatePath}");
        }
    }

    private void SetupDebugAddItemButton()
    {
        if (_canvas == null) return;

        var existing = _canvas.transform.Find("DebugAddItemButton");
        if (existing != null)
        {
            _debugAddItemButton = existing.GetComponent<Button>();
            if (_debugAddItemButton != null)
            {
                _debugAddItemButton.onClick.RemoveAllListeners();
                _debugAddItemButton.onClick.AddListener(AddRandomInventoryItem);
                var debugLabel = _debugAddItemButton.GetComponentInChildren<Text>();
                if (debugLabel != null && debugLabel.fontSize > 0) _debugAddItemBaseFontSize = debugLabel.fontSize;
            }

            ApplyCoordinateAlignedLayout();
            return;
        }

        var buttonGo = new GameObject("DebugAddItemButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(_canvas.transform, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(220f, 64f);
        rect.anchoredPosition = new Vector2(20f, -20f);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = TryLoadFallbackSprite() ?? _fallbackButtonSprite;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0f, 0f, 0f, 0.72f);

        _debugAddItemButton = buttonGo.GetComponent<Button>();
        _debugAddItemButton.onClick.AddListener(AddRandomInventoryItem);
        buttonGo.transform.SetAsLastSibling();

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(buttonGo.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<Text>();
        label.text = "Add Random Item";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 28;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _debugAddItemBaseFontSize = label.fontSize;

        ApplyCoordinateAlignedLayout();
    }

    private void AddRandomInventoryItem()
    {
        EnsureInventoryItemIds();
        if (_inventoryItemIds.Count == 0) return;

        var index = UnityEngine.Random.Range(0, _inventoryItemIds.Count);
        var itemId = _inventoryItemIds[index];

        var state = InventoryState.Instance;
        if (state == null)
        {
            if (verboseLogging) Debug.LogWarning("Background: InventoryState unavailable for debug add item.");
            return;
        }

        var added = state.ReceiveItem(itemId);
        if (verboseLogging)
        {
            var status = added ? "added" : "already present";
            Debug.Log($"Background: Debug add item '{itemId}' {status}.");
        }
    }

    private void EnsureInventoryItemIds()
    {
        if (_inventoryItemIds.Count > 0) return;
        InventoryDatabase.Load();
        foreach (var kvp in InventoryDatabase.GetAllItems())
        {
            if (kvp.Value != null && !string.IsNullOrWhiteSpace(kvp.Key))
            {
                _inventoryItemIds.Add(kvp.Key);
            }
        }
    }

    private void RegisterInventoryKeyBindings()
    {
        if (_inventoryToggle == null) return;
        RegisterKeyBinding(KeyCode.E, ToggleInventoryWindow);

        if (inventoryToggleKey != KeyCode.None && inventoryToggleKey != KeyCode.E)
        {
            RegisterKeyBinding(inventoryToggleKey, ToggleInventoryWindow);
        }

        if (inventorySecondaryToggleKey != KeyCode.None && inventorySecondaryToggleKey != inventoryToggleKey && inventorySecondaryToggleKey != KeyCode.E)
        {
            RegisterKeyBinding(inventorySecondaryToggleKey, ToggleInventoryWindow);
        }
    }

    private void RegisterSettingsKeyBindings()
    {
        if (!enableSettingsMenu)
        {
            return;
        }

        RegisterKeyBinding(KeyCode.Escape, ToggleSettingsMenu);
    }

    private void ToggleInventoryWindow()
    {
        if (_inventoryToggle == null) return;
        _inventoryToggle.ToggleInventory();
    }

    private void Update()
    {
        if (!_inventoryPersistenceSubscribed) AttachInventoryPersistenceHooks();
        CheckScreenSizeChange();
        ApplyManualOverrides();
        UpdateSlideshowInteractionState();
    }

    private void CheckScreenSizeChange()
    {
        var currentScreenSize = new Vector2(Screen.width, Screen.height);
        var currentContainerSize = GetContainerSize();

        var screenChanged = !Mathf.Approximately(_lastScreenSize.x, currentScreenSize.x) ||
                            !Mathf.Approximately(_lastScreenSize.y, currentScreenSize.y);
        var containerChanged = !Mathf.Approximately(_lastContainerSize.x, currentContainerSize.x) ||
                               !Mathf.Approximately(_lastContainerSize.y, currentContainerSize.y);
        
        // Only rebuild if screen or UI container size actually changed
        if (screenChanged || containerChanged)
        {
            _lastScreenSize = currentScreenSize;
            _lastContainerSize = currentContainerSize;

            if (_backgroundImage != null && _backgroundImage.sprite != null)
            {
                _backgroundDisplayRect = CalculateBackgroundDisplayRect(_backgroundImage.sprite);
            }

            ApplyCoordinateAlignedLayout();

            // Rebuild buttons with new screen dimensions
            if (!string.IsNullOrEmpty(_currentSlideKey))
            {
                if (verboseLogging) Debug.Log($"Background: Screen size changed to {currentScreenSize}, rebuilding buttons...");
                RebuildCurrentSlideButtons();
            }
        }
    }

    private void RebuildCurrentSlideButtons()
    {
        if (string.IsNullOrEmpty(_currentSlideKey) || !_database.Nodes.ContainsKey(_currentSlideKey)) return;
        
        // Recreate buttons with current screen size
        var node = _database.Nodes[_currentSlideKey];
        RebuildButtons(node);
    }

    private void ApplyManualOverrides()
    {
        if (_inventoryToggle == null) return;

        if (manualInventoryOpen != _lastManualInventoryOpen)
        {
            _lastManualInventoryOpen = manualInventoryOpen;
            if (manualInventoryOpen)
            {
                _inventoryToggle.OpenInventory();
            }
            else
            {
                _inventoryToggle.CloseInventory();
            }
        }

        if (manualInteractionLock != _lastManualInteractionLock)
        {
            _lastManualInteractionLock = manualInteractionLock;
            SetSlideshowInteractionEnabled(!manualInteractionLock);
        }
    }

    private void UpdateSlideshowInteractionState()
    {
        if (!lockInteractionWhenInventoryOpen && !manualInteractionLock)
        {
            SetSlideshowInteractionEnabled(true);
            return;
        }

        var inventoryOpen = manualInventoryOpen || IsInventoryOpen();
        var locked = manualInteractionLock || (lockInteractionWhenInventoryOpen && inventoryOpen);
        SetSlideshowInteractionEnabled(!locked);
    }

    private void SetSlideshowInteractionEnabled(bool enabled)
    {
        if (_slideshowInteractionEnabled == enabled) return;

        _slideshowInteractionEnabled = enabled;

        if (_buttonContainer != null) _buttonContainer.gameObject.SetActive(enabled);
        if (_keyInput != null) _keyInput.enabled = enabled;
    }

    private bool IsInventoryOpen()
    {
        if (_inventoryToggle == null) return false;

        var overlay = _inventoryToggle.inventoryCanvas;
        if (overlay == null || !overlay.activeInHierarchy) return false;

        if (_inventoryCanvasGroup == null) _inventoryCanvasGroup = overlay.GetComponent<CanvasGroup>();
        if (_inventoryCanvasGroup == null) return false;

        return _inventoryCanvasGroup.blocksRaycasts || _inventoryCanvasGroup.interactable || _inventoryCanvasGroup.alpha > 0.001f;
    }

    private static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        var existing = EventSystem.current ?? FindFirstObjectByType<EventSystem>();
#else
        var existing = EventSystem.current ?? FindObjectOfType<EventSystem>();
#endif
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var legacy = existing.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);

            if (existing.GetComponent<InputSystemUIInputModule>() == null) existing.gameObject.AddComponent<InputSystemUIInputModule>();
#endif
            return;
        }

        var eventSystemGo = new GameObject("EventSystem") { hideFlags = HideFlags.None };
        eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
    }

    private void OnDestroy()
    {
        if (_onEnterRoutine != null) StopCoroutine(_onEnterRoutine);
        DetachInventoryPersistenceHooks();
        if (_gameState != null) _gameState.StateChanged -= HandleGameStateChanged;
        GameDataLoader.ProgressChanged -= HandleProgressChanged;

        foreach (var record in _spriteCache.Values)
        {
            if (record.Sprite != null) Destroy(record.Sprite);
            if (record.Texture != null) Destroy(record.Texture);
        }

        _spriteCache.Clear();

        if (!_ownsFallbackSprite) return;

        if (_fallbackButtonSprite != null) Destroy(_fallbackButtonSprite);
        if (_fallbackTexture != null) Destroy(_fallbackTexture);
    }

    [Serializable]
    private sealed class GameStateFileData
    {
        public List<GameStateFileFlag> flags = new List<GameStateFileFlag>();
        public List<GameStateFileItem> inventory = new List<GameStateFileItem>();
        public List<GameStateFileProgress> progress = new List<GameStateFileProgress>();
        public int redBull;
        public string startSlideKey;
    }

    [Serializable]
    private sealed class GameStateFileFlag
    {
        public string key;
        public bool value;
    }

    [Serializable]
    private sealed class GameStateFileItem
    {
        public string id;
        public int quantity;
    }

    [Serializable]
    private sealed class GameStateFileProgress
    {
        public string id;
        public bool completed;
        public int tries;
    }

    private sealed class SpriteRecord
    {
        internal SpriteRecord(Sprite sprite, Texture2D texture)
        {
            Sprite = sprite;
            Texture = texture;
        }

        internal Sprite Sprite { get; }
        internal Texture2D Texture { get; }
    }

    private sealed class PolygonHotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, ICanvasRaycastFilter
    {
        private RectTransform _rectTransform;
        private List<Vector2> _points = new List<Vector2>();
        private Action _onClick;
        private Sprite _cursorSprite;

        internal void Initialize(RectTransform rectTransform, List<Vector2> points, Action onClick, Sprite cursorSprite)
        {
            _rectTransform = rectTransform;
            _points = points ?? new List<Vector2>();
            _onClick = onClick;
            _cursorSprite = cursorSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_rectTransform == null || _points == null || _points.Count < 3 || _onClick == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint)) return;
            if (IsInsidePolygon(localPoint)) _onClick.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _activeHotspot = this;
            SetCursor(_cursorSprite);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_activeHotspot != this) return;

            _activeHotspot = null;
            ResetCursor();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_activeHotspot != this)
            {
                _activeHotspot = this;
            }

            SetCursor(_cursorSprite);
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (_rectTransform == null || _points == null || _points.Count < 3) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, sp, eventCamera, out var localPoint) && IsInsidePolygon(localPoint);
        }

        private bool IsInsidePolygon(Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = _points.Count - 1; i < _points.Count; j = i++)
            {
                var pi = _points[i];
                var pj = _points[j];

                var intersects = (pi.y > point.y) != (pj.y > point.y);
                if (!intersects) continue;

                var slope = (pj.x - pi.x) / (pj.y - pi.y + Mathf.Epsilon);
                var intersectX = slope * (point.y - pi.y) + pi.x;
                if (point.x < intersectX) inside = !inside;
            }

            return inside;
        }

        private static readonly Dictionary<int, Texture2D> CursorTextureCache = new Dictionary<int, Texture2D>();
        private static PolygonHotspot _activeHotspot;
        private static int _activeCursorId;
        private static bool _cursorApplied;

        private static void SetCursor(Sprite sprite)
        {
            if (sprite == null)
            {
                ResetCursor();
                return;
            }

            var tex = sprite.texture;
            if (tex == null)
            {
                ResetCursor();
                return;
            }

            var rect = sprite.rect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            if (width <= 0 || height <= 0)
            {
                ResetCursor();
                return;
            }

            var cursorId = sprite.GetInstanceID();
            if (_cursorApplied && _activeCursorId == cursorId)
            {
                return;
            }

            if (!CursorTextureCache.TryGetValue(cursorId, out var cursorTexture) || cursorTexture == null)
            {
                cursorTexture = BuildCursorTexture(sprite, width, height);
                if (cursorTexture == null)
                {
                    ResetCursor();
                    return;
                }

                CursorTextureCache[cursorId] = cursorTexture;
            }

            var hotspot = new Vector2(width * 0.5f, height * 0.5f);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
            _activeCursorId = cursorId;
            _cursorApplied = true;
        }

        private static Texture2D BuildCursorTexture(Sprite sprite, int width, int height)
        {
            var tex = sprite.texture;
            if (tex == null) return null;

            var cursorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                alphaIsTransparency = true
            };

            var rect = sprite.rect;
            var srcX = Mathf.RoundToInt(rect.x);
            var srcY = Mathf.RoundToInt(rect.y);

            if (tex.isReadable)
            {
                cursorTexture.SetPixels(tex.GetPixels(srcX, srcY, width, height));
            }
            else
            {
                var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(tex, rt);

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                cursorTexture.ReadPixels(new Rect(srcX, srcY, width, height), 0, 0);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }

            cursorTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return cursorTexture;
        }

        private static void ResetCursor()
        {
            _activeHotspot = null;
            _cursorApplied = false;
            _activeCursorId = 0;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        internal static void ResetSharedCursor()
        {
            ResetCursor();
        }
    }
}
