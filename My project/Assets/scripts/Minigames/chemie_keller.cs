using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Framework.Minigames;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class chemie_keller : MonoBehaviour
{
    private enum ChemiePart
    {
        Part1,
        Part2
    }

    private enum Part1Mode
    {
        Buret,
        Pipette
    }

    private enum Part1BuretStage
    {
        Sulfuric,
        Nitric
    }

    private sealed class Droplet
    {
        public GameObject GameObject;
        public SpriteRenderer Renderer;
        public float SourceX;
        public float SourceY;
    }

    private sealed class Lump
    {
        public GameObject GameObject;
        public SpriteRenderer Renderer;
        public GameObject OutlineGameObject;
        public SpriteRenderer OutlineRenderer;
        public float SourceX;
        public float SourceY;
        public float RadiusX;
        public float RadiusY;
        public float RotationDeg;
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI cText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Scene References")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer buretteRenderer;

    [Header("Result Flags")]
    [SerializeField] private bool gameWon;
    [SerializeField] private bool gameEnded;

    [Header("Layout")]
    [SerializeField] private float sourceWidth = 1620f;
    [SerializeField] private float sourceHeight = 1080f;
    [SerializeField] [Range(0.6f, 1f)] private float contentScale = 1f;
    [SerializeField] private Vector2 contentOffsetSource = new Vector2(0f, 40f);

    [Header("Part 1")]
    [SerializeField] private float buretSpeedPerSecond = 240f;
    [SerializeField] private float dropletSpeedPerSecond = 840f;

    [Header("Part 2")]
    [SerializeField] private int lumpsToSpawn = 5;

    private const string ResourceBase = "images/Minigames/chemie_keller/";
    private const string Minigame2Key = "ChemieKeller_Minigame2";
    private const string PatternDefaultPath = ResourceBase + "pattern";
    private const string PatternBluePath = ResourceBase + "pattern_blue";
    private const string PatternYellowPath = ResourceBase + "pattern_yellow";
    private const string PatternGreenPath = ResourceBase + "pattern_green";
    private const int LumpOutlineSpriteSize = 64;
    private static Sprite _lumpOutlineSprite;
    private static readonly Color Part1MixTintStart = Color.white;
    private static readonly Color Part1MixTintEnd = new Color(0.995f, 0.884f, 1f, 1f);
    private const float Part1MixStepPerDrop = 0.2f;

    private static readonly int[][] MaxPos1 =
    {
        new[] { 1052, 1279 },
        new[] { 723, 895 }
    };

    private static readonly int[][] MaxPos2 =
    {
        new[] { 1079, 1249 },
        new[] { 896, 923 }
    };

    private static readonly int[][][] LumpSpawnRanges =
    {
        MaxPos1, MaxPos1, MaxPos1, MaxPos1, MaxPos2
    };

    private static readonly Vector2[] Part1PipettePolygon =
    {
        new Vector2(1158, 466),
        new Vector2(1165, 460),
        new Vector2(1062, 304),
        new Vector2(946, 144),
        new Vector2(935, 135),
        new Vector2(935, 117),
        new Vector2(883, 45),
        new Vector2(863, 32),
        new Vector2(848, 36),
        new Vector2(838, 56),
        new Vector2(899, 145),
        new Vector2(916, 151),
        new Vector2(1045, 330)
    };

    private static readonly Vector2[] Part2PipettePolygon =
    {
        new Vector2(283, 470),
        new Vector2(290, 449),
        new Vector2(306, 441),
        new Vector2(327, 453),
        new Vector2(380, 523),
        new Vector2(384, 546),
        new Vector2(513, 718),
        new Vector2(610, 867),
        new Vector2(602, 874),
        new Vector2(489, 738),
        new Vector2(360, 562),
        new Vector2(343, 555)
    };

    private static readonly Vector2[] RecipePolygon =
    {
        new Vector2(839, 641),
        new Vector2(885, 631),
        new Vector2(949, 658),
        new Vector2(965, 800),
        new Vector2(906, 826),
        new Vector2(855, 807),
        new Vector2(732, 797),
        new Vector2(745, 635)
    };

    private static readonly Vector2[] BuretClipPolygon =
    {
        new Vector2(470, 682),
        new Vector2(490, 685),
        new Vector2(519, 687),
        new Vector2(544, 684),
        new Vector2(543, 21),
        new Vector2(472, 21)
    };

    private static readonly Vector2[] BeakerClipPolygon =
    {
        new Vector2(1308, 903),
        new Vector2(1304, 914),
        new Vector2(1288, 927),
        new Vector2(1264, 939),
        new Vector2(1235, 946),
        new Vector2(1196, 950),
        new Vector2(1138, 950),
        new Vector2(1105, 946),
        new Vector2(1066, 939),
        new Vector2(1046, 930),
        new Vector2(1031, 917),
        new Vector2(1025, 900),
        new Vector2(1025, 585),
        new Vector2(1308, 582)
    };

    private static Sprite _whiteSprite;
    private static readonly Dictionary<string, Sprite> FullRectSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    private Camera _camera;
    private System.Random _random;

    private ChemiePart _activePart;
    private Part1Mode _part1Mode;
    private Part1BuretStage _part1Stage;
    private bool _part2PipetteMode;

    private bool _returnScheduled;
    private bool _recipeOverlayVisible;

    private float _buretLiquidY;
    private int _buretDirection = -1;
    private bool _buretWarmup = true;

    private float _beakerLiquidY;
    private int _dropletCounter;
    private float _part1MixProgress;

    private SpriteRenderer _buretLiquidRenderer;
    private SpriteRenderer _beakerRenderer;
    private SpriteRenderer _beakerLiquidRenderer;
    private SpriteRenderer _beakerLiquid2Renderer;
    private SpriteRenderer _nitricAcidRenderer;
    private SpriteRenderer _sulfuricAcidRenderer;
    private SpriteRenderer _pipetteRenderer;
    private SpriteRenderer _pipetteShadowRenderer;
    private SpriteRenderer _overlayRenderer;
    private SpriteRenderer _recipeRenderer;
    private SpriteRenderer _stopButtonRenderer;
    private SpriteRenderer _confirmButtonRenderer;
    private TextMeshProUGUI _stopButtonLabel;
    private TextMeshProUGUI _confirmButtonLabel;
    private SpriteMask _buretLiquidMask;
    private SpriteMask _beakerLiquidMask;

    private readonly List<Droplet> _droplets = new List<Droplet>();
    private readonly List<Lump> _lumps = new List<Lump>();

    private const float BuretStartY = 685f;
    private const float BuretTopY = 650f;
    private const float BuretBottomY = 20f;

    private const float SulfurBottomY = 305f;
    private const float SulfurTopY = 375f;

    private const float NitricBottomY = 80f;
    private const float NitricTopY = 150f;

    private const float BeakerStartY = 950f;
    private const float BeakerSulfuricY = 850f;
    private const float BeakerNitricY = 700f;

    private static readonly Rect StopButtonRect = new Rect(131f, 327f, 200f, 100f);
    private static readonly Rect ConfirmButtonRect = new Rect(1300f, 327f, 300f, 100f);

    private static readonly Rect BuretRect = new Rect(467f, -57f, 129f, 867f);
    private static readonly Rect BeakerRect = new Rect(948f, 449f, 881f, 533f);
    private static readonly Rect NitricRect = new Rect(288f, 572f, 715f, 714f);
    private static readonly Rect SulfuricRect = new Rect(291f, 574f, 705f, 705f);
    private static readonly Rect Part1PipetteRect = new Rect(623f, -59f, 775f, 615f);
    private static readonly Rect Part2PipetteRect = new Rect(284f, 442f, 331f, 436f);
    private static readonly Rect OverlayRect = new Rect(0f, 0f, 1620f, 1080f);
    private static readonly Rect RecipeRect = new Rect(451f, -131f, 712f, 949f);
    private static readonly Rect BuretLiquidRectTemplate = new Rect(473f, BuretStartY, 71f, 700f);
    private static readonly Rect BeakerLiquidRectTemplate = new Rect(1026f, BeakerStartY, 300f, 318f);

    private void Awake()
    {
        _camera = Camera.main;
        _random = new System.Random();

        if (backgroundRenderer == null)
        {
            var backgroundGo = GameObject.Find("background");
            backgroundRenderer = backgroundGo != null ? backgroundGo.GetComponent<SpriteRenderer>() : null;
        }

        if (buretteRenderer == null)
        {
            var buretteGo = GameObject.Find("burette");
            buretteRenderer = buretteGo != null ? buretteGo.GetComponent<SpriteRenderer>() : null;
        }

        // Hide legacy scene sprite that belonged to the old prototype implementation.
        var legacyRenderer = GetComponent<SpriteRenderer>();
        if (legacyRenderer != null)
        {
            legacyRenderer.enabled = false;
        }

        ResolveUiReferences();
        SetTextVisible(cText, false);
        SetTextVisible(winText, false);
    }

    private void Start()
    {
        BuildVisuals();

        var key = MinigameReturnState.CurrentMinigameKey;
        _activePart = string.Equals(key, Minigame2Key, StringComparison.OrdinalIgnoreCase)
            ? ChemiePart.Part2
            : ChemiePart.Part1;

        if (_activePart == ChemiePart.Part2)
        {
            SetupPart2();
        }
        else
        {
            SetupPart1();
        }
    }

    private void Update()
    {
        if (_returnScheduled)
        {
            return;
        }

        if (_activePart == ChemiePart.Part1)
        {
            UpdatePart1(Time.deltaTime);
        }
        else
        {
            UpdatePart2();
        }

        if (IsPrimaryClickDown())
        {
            HandlePrimaryClick();
        }
    }

    private void SetupPart1()
    {
        _part1Mode = Part1Mode.Buret;
        _part1Stage = Part1BuretStage.Sulfuric;
        _buretLiquidY = BuretStartY;
        _buretDirection = -1;
        _buretWarmup = true;

        _dropletCounter = 0;
        _part1MixProgress = 0f;
        ClearDroplets();

        _beakerLiquidY = BeakerStartY;
        SetBeakerLiquid2Alpha(0f);
        ApplyPart1ResetPatterns();
        ApplyPart1BeakerMixTint();

        SetPart1VisualState();
        ShowInstruction("Teil 1: Erst Schwefelsäure, dann Salpetersäure. Beim richtigen Stand auf Stopp klicken.");
        SetWinText(string.Empty, false);
        SetTextVisible(cText, false);

        ApplyLiquidRect(_buretLiquidRenderer, new Rect(BuretLiquidRectTemplate.x, _buretLiquidY, BuretLiquidRectTemplate.width, BuretLiquidRectTemplate.height));
        ApplyLiquidRect(_beakerLiquidRenderer, new Rect(BeakerLiquidRectTemplate.x, _beakerLiquidY, BeakerLiquidRectTemplate.width, BeakerLiquidRectTemplate.height));
    }

    private void SetupPart2()
    {
        _part2PipetteMode = false;
        _recipeOverlayVisible = false;

        ClearDroplets();
        ClearLumps();

        _beakerLiquidY = BeakerNitricY;
        SetPart2VisualState();
        SpawnPart2Lumps();
        _part1MixProgress = 0f;
        ApplyPart1BeakerMixTint();

        ShowInstruction("Teil 2: Pipette aktivieren und alle Klumpen entfernen.");
        SetWinText(string.Empty, false);
        SetTextVisible(cText, false);

        ApplyLiquidRect(_beakerLiquidRenderer, new Rect(BeakerLiquidRectTemplate.x, _beakerLiquidY, BeakerLiquidRectTemplate.width, BeakerLiquidRectTemplate.height));
        SetBeakerLiquid2Alpha(0f);
    }

    private void UpdatePart1(float deltaTime)
    {
        if (_part1Mode == Part1Mode.Buret)
        {
            _buretLiquidY += _buretDirection * buretSpeedPerSecond * deltaTime;

            if (_buretWarmup)
            {
                if (_buretLiquidY <= BuretTopY)
                {
                    _buretWarmup = false;
                }
                else
                {
                    ApplyLiquidRect(_buretLiquidRenderer, new Rect(BuretLiquidRectTemplate.x, _buretLiquidY, BuretLiquidRectTemplate.width, BuretLiquidRectTemplate.height));
                    return;
                }
            }

            if (_buretLiquidY <= BuretBottomY || _buretLiquidY >= BuretTopY)
            {
                _buretDirection *= -1;
            }
            else if (UnityEngine.Random.value < (1f / 60f) * deltaTime * 24f)
            {
                _buretDirection *= -1;
            }

            ApplyLiquidRect(_buretLiquidRenderer, new Rect(BuretLiquidRectTemplate.x, _buretLiquidY, BuretLiquidRectTemplate.width, BuretLiquidRectTemplate.height));
            return;
        }

        var remove = new List<Droplet>();
        for (var i = 0; i < _droplets.Count; i++)
        {
            var droplet = _droplets[i];
            droplet.SourceY += dropletSpeedPerSecond * deltaTime;

            if (droplet.SourceY >= BeakerNitricY)
            {
                remove.Add(droplet);
                _part1MixProgress = Mathf.Clamp01(_part1MixProgress + Part1MixStepPerDrop);
                ApplyPart1BeakerMixTint();
                continue;
            }

            droplet.Renderer.transform.position = SourceToWorld(new Vector2(droplet.SourceX, droplet.SourceY));
        }

        if (remove.Count > 0)
        {
            for (var i = 0; i < remove.Count; i++)
            {
                _droplets.Remove(remove[i]);
                if (remove[i].GameObject != null)
                {
                    Destroy(remove[i].GameObject);
                }
            }
        }
    }

    private void UpdatePart2()
    {
        if (!_part2PipetteMode || _pipetteRenderer == null)
        {
            return;
        }

        if (!TryGetPointerSourcePosition(out var sourcePos, out _))
        {
            return;
        }

        SetSpriteFromSourceRect(_pipetteRenderer, new Rect(sourcePos.x - Part2PipetteRect.width, sourcePos.y - Part2PipetteRect.height, Part2PipetteRect.width, Part2PipetteRect.height));
    }

    private void HandlePrimaryClick()
    {
        if (!TryGetPointerSourcePosition(out var sourcePos, out _))
        {
            return;
        }

        if (_recipeOverlayVisible)
        {
            SetRecipeOverlay(false);
            return;
        }

        if (IsPointInPolygon(sourcePos, RecipePolygon))
        {
            SetRecipeOverlay(true);
            return;
        }

        if (_activePart == ChemiePart.Part1)
        {
            HandlePart1Click(sourcePos);
        }
        else
        {
            HandlePart2Click(sourcePos);
        }
    }

    private void HandlePart1Click(Vector2 sourcePos)
    {
        if (_part1Mode == Part1Mode.Buret)
        {
            if (StopButtonRect.Contains(sourcePos))
            {
                HandlePart1Stop();
            }
            return;
        }

        if (ConfirmButtonRect.Contains(sourcePos))
        {
            HandlePart1Confirm();
            return;
        }

        if (Part1PipetteRect.Contains(sourcePos) || IsPointInPolygon(sourcePos, Part1PipettePolygon))
        {
            SpawnDroplet();
        }
    }

    private void HandlePart2Click(Vector2 sourcePos)
    {
        if (_part2PipetteMode)
        {
            for (var i = _lumps.Count - 1; i >= 0; i--)
            {
                if (!IsPointInEllipse(sourcePos, _lumps[i]))
                {
                    continue;
                }

                var lump = _lumps[i];
                _lumps.RemoveAt(i);
                if (lump.GameObject != null)
                {
                    Destroy(lump.GameObject);
                }
                if (lump.OutlineGameObject != null)
                {
                    Destroy(lump.OutlineGameObject);
                }

                if (_lumps.Count == 0)
                {
                    HandlePart2Success();
                }

                return;
            }
        }

        if (Part2PipetteRect.Contains(sourcePos) || IsPointInPolygon(sourcePos, Part2PipettePolygon))
        {
            TogglePart2Pipette(sourcePos);
        }
    }

    private void HandlePart1Stop()
    {
        if (_part1Stage == Part1BuretStage.Sulfuric)
        {
            if (IsWithinTargetWindow(_buretLiquidY, SulfurBottomY, SulfurTopY))
            {
                _part1Stage = Part1BuretStage.Nitric;
                _buretLiquidY = BuretStartY;
                _buretDirection = -1;
                _buretWarmup = true;

                _beakerLiquidY = BeakerSulfuricY;
                ApplyLiquidRect(_beakerLiquidRenderer, new Rect(BeakerLiquidRectTemplate.x, _beakerLiquidY, BeakerLiquidRectTemplate.width, BeakerLiquidRectTemplate.height));
                SetRendererSprite(_buretLiquidRenderer, PatternBluePath);

                if (_sulfuricAcidRenderer != null)
                {
                    _sulfuricAcidRenderer.gameObject.SetActive(false);
                }

                ShowInstruction("Gut. Jetzt im Bereich der Salpetersäure stoppen.");
                SetPart1VisualState();
                return;
            }

            SetupPart1();
            ShowInstruction("Schwefelsäure-Bereich verfehlt. Neustart.");
            return;
        }

        if (IsWithinTargetWindow(_buretLiquidY, NitricBottomY, NitricTopY))
        {
            _part1Mode = Part1Mode.Pipette;
            _beakerLiquidY = BeakerNitricY;
            ApplyLiquidRect(_beakerLiquidRenderer, new Rect(BeakerLiquidRectTemplate.x, _beakerLiquidY, BeakerLiquidRectTemplate.width, BeakerLiquidRectTemplate.height));
            SetRendererSprite(_beakerLiquidRenderer, PatternGreenPath);
            _part1MixProgress = 0f;
            ApplyPart1BeakerMixTint();

            ShowInstruction("5 Tropfen mit der Pipette abgeben und dann bestätigen.");
            SetPart1VisualState();
            return;
        }

        SetupPart1();
        ShowInstruction("Salpetersäure-Bereich verfehlt. Neustart.");
    }

    private static bool IsWithinTargetWindow(float value, float min, float max)
    {
        return value >= min && value <= max;
    }

    private void HandlePart1Confirm()
    {
        if (_dropletCounter != 5)
        {
            SetupPart1();
            ShowInstruction("Es werden genau 5 Tropfen benötigt. Neustart.");
            return;
        }

        var inventory = InventoryState.Instance;
        inventory?.RemoveItem("sulfuric_acid");
        inventory?.RemoveItem("nitric_acid");
        inventory?.RemoveItem("glycerin");

        EndMinigame(success: true, message: "Teil 1 abgeschlossen");
    }

    private void HandlePart2Success()
    {
        var inventory = InventoryState.Instance;
        inventory?.RemoveItem("nitrogly_beaker");
        inventory?.ReceiveItem("nitro_pipette");

        EndMinigame(success: true, message: "Teil 2 abgeschlossen");
    }

    private void EndMinigame(bool success, string message)
    {
        if (_returnScheduled)
        {
            return;
        }

        gameWon = success;
        gameEnded = true;

        SetWinText(message, true);
        ShowInstruction(string.Empty);

        _returnScheduled = true;
        Invoke(nameof(CommitMinigameResult), 1.0f);
    }

    private void CommitMinigameResult()
    {
        MinigameReturnState.SetResult(gameEnded, gameWon);
    }

    private void SpawnDroplet()
    {
        var go = new GameObject($"Droplet_{_dropletCounter + 1}");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        renderer.sortingOrder = 4;

        const float radius = 7f;
        SetRendererEllipse(renderer, 1162f, 463f, radius, radius);

        var droplet = new Droplet
        {
            GameObject = go,
            Renderer = renderer,
            SourceX = 1162f,
            SourceY = 463f
        };

        _droplets.Add(droplet);
        _dropletCounter++;
    }

    private void SpawnPart2Lumps()
    {
        var count = Mathf.Max(1, lumpsToSpawn);
        for (var i = 0; i < count; i++)
        {
            var range = LumpSpawnRanges[Mathf.Min(i, LumpSpawnRanges.Length - 1)];
            var sourceX = _random.Next(range[0][0], range[0][1]);
            var sourceY = _random.Next(range[1][0], range[1][1]);
            var rx = _random.Next(13, 20);
            var ry = _random.Next(8, 15);
            var rotationDeg = _random.Next(0, 360);

            var go = new GameObject($"Lump_{i + 1}");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateLumpSprite(rx, ry);
            renderer.color = Color.white;
            renderer.sortingOrder = 2;
            SetRendererEllipse(renderer, sourceX, sourceY, rx, ry);
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotationDeg);

            var outlineGo = new GameObject($"LumpOutline_{i + 1}");
            var outlineRenderer = outlineGo.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = GetLumpOutlineSprite();
            outlineRenderer.color = Color.black;
            outlineRenderer.sortingOrder = 5;
            SetRendererEllipse(outlineRenderer, sourceX, sourceY, rx + 1f, ry + 1f);
            outlineRenderer.transform.rotation = Quaternion.Euler(0f, 0f, rotationDeg);

            _lumps.Add(new Lump
            {
                GameObject = go,
                Renderer = renderer,
                OutlineGameObject = outlineGo,
                OutlineRenderer = outlineRenderer,
                SourceX = sourceX,
                SourceY = sourceY,
                RadiusX = rx,
                RadiusY = ry,
                RotationDeg = rotationDeg
            });
        }
    }

    private void TogglePart2Pipette(Vector2 sourcePos)
    {
        _part2PipetteMode = !_part2PipetteMode;

        if (_pipetteRenderer == null || _pipetteShadowRenderer == null)
        {
            return;
        }

        if (_part2PipetteMode)
        {
            SetSpriteFromSourceRect(_pipetteRenderer, new Rect(sourcePos.x - Part2PipetteRect.width, sourcePos.y - Part2PipetteRect.height, Part2PipetteRect.width, Part2PipetteRect.height));
            ShowInstruction("Pipette aktiv: Klumpen anklicken, um sie zu entfernen.");
        }
        else
        {
            SetSpriteFromSourceRect(_pipetteRenderer, Part2PipetteRect);
            ShowInstruction("Pipette inaktiv: Pipette anklicken, um sie erneut zu aktivieren.");
        }
    }

    private void SetPart1VisualState()
    {
        var inBuretMode = _part1Mode == Part1Mode.Buret;

        if (buretteRenderer != null) buretteRenderer.gameObject.SetActive(inBuretMode);
        if (_buretLiquidRenderer != null) _buretLiquidRenderer.gameObject.SetActive(inBuretMode);
        if (_nitricAcidRenderer != null)
        {
            _nitricAcidRenderer.gameObject.SetActive(inBuretMode && _part1Stage == Part1BuretStage.Nitric);
        }

        if (_sulfuricAcidRenderer != null)
        {
            _sulfuricAcidRenderer.gameObject.SetActive(inBuretMode && _part1Stage == Part1BuretStage.Sulfuric);
        }

        if (_beakerRenderer != null) _beakerRenderer.gameObject.SetActive(true);
        if (_beakerLiquidRenderer != null) _beakerLiquidRenderer.gameObject.SetActive(true);
        if (_beakerLiquid2Renderer != null) _beakerLiquid2Renderer.gameObject.SetActive(false);

        if (_pipetteShadowRenderer != null) _pipetteShadowRenderer.gameObject.SetActive(false);
        if (_pipetteRenderer != null)
        {
            SetRendererSprite(_pipetteRenderer, ResourceBase + "pipette");
            _pipetteRenderer.gameObject.SetActive(_part1Mode == Part1Mode.Pipette);
            SetSpriteFromSourceRect(_pipetteRenderer, Part1PipetteRect);
        }

        if (_stopButtonRenderer != null)
        {
            _stopButtonRenderer.gameObject.SetActive(_part1Mode == Part1Mode.Buret);
        }
        if (_stopButtonLabel != null)
        {
            _stopButtonLabel.gameObject.SetActive(_part1Mode == Part1Mode.Buret);
        }

        if (_confirmButtonRenderer != null)
        {
            _confirmButtonRenderer.gameObject.SetActive(_part1Mode == Part1Mode.Pipette);
        }
        if (_confirmButtonLabel != null)
        {
            _confirmButtonLabel.gameObject.SetActive(_part1Mode == Part1Mode.Pipette);
        }
    }

    private void SetPart2VisualState()
    {
        if (buretteRenderer != null) buretteRenderer.gameObject.SetActive(false);
        if (_buretLiquidRenderer != null) _buretLiquidRenderer.gameObject.SetActive(false);
        if (_nitricAcidRenderer != null) _nitricAcidRenderer.gameObject.SetActive(false);
        if (_sulfuricAcidRenderer != null) _sulfuricAcidRenderer.gameObject.SetActive(false);

        if (_beakerRenderer != null) _beakerRenderer.gameObject.SetActive(true);
        if (_beakerLiquidRenderer != null) _beakerLiquidRenderer.gameObject.SetActive(true);
        if (_beakerLiquid2Renderer != null) _beakerLiquid2Renderer.gameObject.SetActive(false);

        if (_pipetteShadowRenderer != null)
        {
            _pipetteShadowRenderer.gameObject.SetActive(true);
            SetSpriteFromSourceRect(_pipetteShadowRenderer, Part2PipetteRect);
            _pipetteShadowRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }

        if (_pipetteRenderer != null)
        {
            SetRendererSprite(_pipetteRenderer, ResourceBase + "pipette_crop");
            _pipetteRenderer.gameObject.SetActive(true);
            SetSpriteFromSourceRect(_pipetteRenderer, Part2PipetteRect);
        }

        if (_stopButtonRenderer != null)
        {
            _stopButtonRenderer.gameObject.SetActive(false);
        }

        if (_confirmButtonRenderer != null)
        {
            _confirmButtonRenderer.gameObject.SetActive(false);
        }
        if (_stopButtonLabel != null)
        {
            _stopButtonLabel.gameObject.SetActive(false);
        }
        if (_confirmButtonLabel != null)
        {
            _confirmButtonLabel.gameObject.SetActive(false);
        }

        SetRecipeOverlay(false);
    }

    private void BuildVisuals()
    {
        _buretLiquidMask = CreatePolygonMask("BuretLiquidMask", BuretClipPolygon, -50, 20);
        _beakerLiquidMask = CreatePolygonMask("BeakerLiquidMask", BeakerClipPolygon, -50, 20);

        _buretLiquidRenderer = CreatePatternLiquidRenderer("BuretLiquid", PatternYellowPath, 1, _buretLiquidMask);
        _beakerLiquidRenderer = CreatePatternLiquidRenderer("BeakerLiquid", PatternYellowPath, 1, _beakerLiquidMask);
        _beakerLiquid2Renderer = CreatePatternLiquidRenderer("BeakerLiquid2", PatternDefaultPath, 2, _beakerLiquidMask);

        _beakerRenderer = CreateSpriteRenderer("Beaker", ResourceBase + "beaker", 3);
        _nitricAcidRenderer = CreateSpriteRenderer("NitricAcid", ResourceBase + "nitric_acid", 3);
        _sulfuricAcidRenderer = CreateSpriteRenderer("SulfuricAcid", ResourceBase + "sulfuric_acid", 3);
        _pipetteRenderer = CreateSpriteRenderer("Pipette", ResourceBase + "pipette", 4);
        _pipetteShadowRenderer = CreateSpriteRenderer("PipetteShadow", ResourceBase + "pipette_crop", 2);
        _overlayRenderer = CreateSpriteRenderer("Overlay", ResourceBase + "overlay", 50);
        _recipeRenderer = CreateSpriteRenderer("Recipe", ResourceBase + "recipe", 51);
        _stopButtonRenderer = CreateRectRenderer("StopButton", new Color(0.8f, 0.2f, 0.2f, 0.92f), 10);
        _confirmButtonRenderer = CreateRectRenderer("ConfirmButton", new Color(0.2f, 0.65f, 0.2f, 0.92f), 10);
        _stopButtonLabel = CreateButtonLabel("StopButtonLabel", "Stopp");
        _confirmButtonLabel = CreateButtonLabel("ConfirmButtonLabel", "Bestätigen");

        if (buretteRenderer != null)
        {
            SetRendererSprite(buretteRenderer, ResourceBase + "buret");
            buretteRenderer.sortingOrder = 3;
            SetSpriteFromSourceRect(buretteRenderer, BuretRect);
        }

        if (_beakerRenderer != null)
        {
            SetSpriteFromSourceRect(_beakerRenderer, BeakerRect);
        }

        if (_nitricAcidRenderer != null)
        {
            SetSpriteFromSourceRect(_nitricAcidRenderer, NitricRect);
        }

        if (_sulfuricAcidRenderer != null)
        {
            SetSpriteFromSourceRect(_sulfuricAcidRenderer, SulfuricRect);
        }

        if (_pipetteRenderer != null)
        {
            SetSpriteFromSourceRect(_pipetteRenderer, Part1PipetteRect);
        }

        if (_stopButtonRenderer != null)
        {
            SetSpriteFromSourceRect(_stopButtonRenderer, StopButtonRect);
        }

        if (_confirmButtonRenderer != null)
        {
            SetSpriteFromSourceRect(_confirmButtonRenderer, ConfirmButtonRect);
        }
        SetButtonLabelFromSourceRect(_stopButtonLabel, StopButtonRect);
        SetButtonLabelFromSourceRect(_confirmButtonLabel, ConfirmButtonRect);

        if (_overlayRenderer != null)
        {
            SetSpriteFromSourceRect(_overlayRenderer, OverlayRect);
            _overlayRenderer.gameObject.SetActive(false);
        }

        if (_recipeRenderer != null)
        {
            SetSpriteFromSourceRect(_recipeRenderer, RecipeRect);
            _recipeRenderer.gameObject.SetActive(false);
        }

        ApplyPart1ResetPatterns();
    }

    private TextMeshProUGUI CreateButtonLabel(string objectName, string text)
    {
        RectTransform canvasRect = null;
        if (instructionText != null)
        {
            canvasRect = instructionText.transform.parent as RectTransform;
        }

        if (canvasRect == null)
        {
#if UNITY_2023_1_OR_NEWER
            var canvas = FindFirstObjectByType<Canvas>();
#else
            var canvas = FindObjectOfType<Canvas>();
#endif
            canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        }

        if (canvasRect == null)
        {
            return null;
        }

        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        if (instructionText != null && instructionText.font != null)
        {
            label.font = instructionText.font;
        }
        label.text = text;
        label.color = Color.white;
        label.outlineColor = Color.black;
        label.outlineWidth = 0.25f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 28f;
        label.fontSize = 22f;
        label.raycastTarget = false;
        go.transform.SetAsLastSibling();

        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        return label;
    }

    private void SetButtonLabelFromSourceRect(TextMeshProUGUI label, Rect sourceRect)
    {
        if (label == null)
        {
            return;
        }

        var canvas = label.canvas;
        var canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
        {
            return;
        }

        var centerSource = new Vector2(sourceRect.x + sourceRect.width * 0.5f, sourceRect.y + sourceRect.height * 0.5f);
        var worldCenter = SourceToWorld(centerSource);
        var screenCenter = _camera != null
            ? (Vector2)_camera.WorldToScreenPoint(worldCenter)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Camera uiCamera = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera != null ? canvas.worldCamera : _camera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenCenter, uiCamera, out var localCenter))
        {
            label.rectTransform.anchoredPosition = localCenter;
        }

        var topLeftSource = new Vector2(sourceRect.x, sourceRect.y);
        var bottomRightSource = new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height);
        var topLeftWorld = SourceToWorld(topLeftSource);
        var bottomRightWorld = SourceToWorld(bottomRightSource);
        var topLeftScreen = _camera != null ? _camera.WorldToScreenPoint(topLeftWorld) : new Vector3(0f, 0f, 0f);
        var bottomRightScreen = _camera != null ? _camera.WorldToScreenPoint(bottomRightWorld) : new Vector3(Screen.width, Screen.height, 0f);

        var widthPixels = Mathf.Abs(bottomRightScreen.x - topLeftScreen.x);
        var heightPixels = Mathf.Abs(bottomRightScreen.y - topLeftScreen.y);
        label.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(28f, widthPixels * 0.78f),
            Mathf.Max(16f, heightPixels * 0.66f)
        );
    }

    private void ApplyPart1ResetPatterns()
    {
        SetRendererSprite(_buretLiquidRenderer, PatternYellowPath);
        SetRendererSprite(_beakerLiquidRenderer, PatternYellowPath);
        SetRendererSprite(_beakerLiquid2Renderer, PatternDefaultPath);
    }

    private void SetRecipeOverlay(bool visible)
    {
        _recipeOverlayVisible = visible;
        if (_overlayRenderer != null)
        {
            _overlayRenderer.gameObject.SetActive(visible);
        }

        if (_recipeRenderer != null)
        {
            _recipeRenderer.gameObject.SetActive(visible);
        }
    }

    private SpriteRenderer CreateSpriteRenderer(string objectName, string resourcePath, int sortingOrder)
    {
        var sprite = LoadSprite(resourcePath);
        if (sprite == null)
        {
            return null;
        }

        var go = new GameObject(objectName);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private SpriteRenderer CreateRectRenderer(string objectName, Color color, int sortingOrder)
    {
        var go = new GameObject(objectName);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private SpriteRenderer CreatePatternLiquidRenderer(string objectName, string patternResourcePath, int sortingOrder, SpriteMask mask)
    {
        var renderer = CreateSpriteRenderer(objectName, patternResourcePath, sortingOrder);
        if (renderer == null)
        {
            renderer = CreateRectRenderer(objectName, Color.white, sortingOrder);
        }

        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        if (mask != null)
        {
            renderer.sortingOrder = sortingOrder;
        }

        return renderer;
    }

    private SpriteMask CreatePolygonMask(string objectName, IReadOnlyList<Vector2> polygon, int backSortingOrder, int frontSortingOrder)
    {
        if (polygon == null || polygon.Count < 3)
        {
            return null;
        }

        var sourceRect = GetPolygonBoundsRect(polygon);
        var sprite = CreatePolygonMaskSprite(polygon, sourceRect);
        if (sprite == null)
        {
            return null;
        }

        var go = new GameObject(objectName);
        var mask = go.AddComponent<SpriteMask>();
        mask.sprite = sprite;
        mask.alphaCutoff = 0.1f;
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = frontSortingOrder;
        mask.backSortingOrder = backSortingOrder;

        SetMaskFromSourceRect(mask, sourceRect);
        return mask;
    }

    private Sprite CreatePolygonMaskSprite(IReadOnlyList<Vector2> polygon, Rect sourceRect)
    {
        var width = Mathf.Max(2, Mathf.CeilToInt(sourceRect.width));
        var height = Mathf.Max(2, Mathf.CeilToInt(sourceRect.height));
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        var clear = new Color32(0, 0, 0, 0);
        var filled = new Color32(255, 255, 255, 255);
        var pixels = new Color32[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourcePoint = new Vector2(sourceRect.x + x + 0.5f, sourceRect.y + y + 0.5f);
                if (!IsPointInPolygon(sourcePoint, polygon))
                {
                    continue;
                }

                // Texture pixel rows are bottom-up; source coordinates are top-down.
                pixels[(height - 1 - y) * width + x] = filled;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Rect GetPolygonBoundsRect(IReadOnlyList<Vector2> polygon)
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        return new Rect(minX, minY, Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (TryLoadFullRectSprite(resourcePath, out var fullRectSprite))
        {
            return fullRectSprite;
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        var sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }

        if (resourcePath.Contains("images/"))
        {
            var alias = resourcePath.Replace("images/", "Images/");
            if (TryLoadFullRectSprite(alias, out fullRectSprite))
            {
                return fullRectSprite;
            }

            sprite = Resources.Load<Sprite>(alias);
            if (sprite != null)
            {
                return sprite;
            }

            sprites = Resources.LoadAll<Sprite>(alias);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }
        }

        if (resourcePath.Contains("Images/"))
        {
            var alias = resourcePath.Replace("Images/", "images/");
            if (TryLoadFullRectSprite(alias, out fullRectSprite))
            {
                return fullRectSprite;
            }

            sprite = Resources.Load<Sprite>(alias);
            if (sprite != null)
            {
                return sprite;
            }

            sprites = Resources.LoadAll<Sprite>(alias);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }
        }

        return null;
    }

    private static bool TryLoadFullRectSprite(string resourcePath, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return false;
        }

        if (FullRectSpriteCache.TryGetValue(resourcePath, out sprite) && sprite != null)
        {
            return true;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return false;
        }

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        if (sprite == null)
        {
            return false;
        }

        sprite.name = $"{texture.name}_fullrect";
        FullRectSpriteCache[resourcePath] = sprite;
        return true;
    }

    private static void SetRendererSprite(SpriteRenderer renderer, string resourcePath)
    {
        if (renderer == null)
        {
            return;
        }

        var sprite = LoadSprite(resourcePath);
        if (sprite != null)
        {
            renderer.sprite = sprite;
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
        {
            return _whiteSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }

    private static Sprite CreateLumpSprite(float radiusX, float radiusY)
    {
        var width = Mathf.Max(2, Mathf.CeilToInt(radiusX * 2f));
        var height = Mathf.Max(2, Mathf.CeilToInt(radiusY * 2f));
        var patternTexture = LoadPatternTexture(PatternDefaultPath);

        if (patternTexture == null)
        {
            return GetWhiteSprite();
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        var patternWidth = patternTexture.width;
        var patternHeight = patternTexture.height;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var nx = (x + 0.5f - width * 0.5f) / radiusX;
                var ny = (y + 0.5f - height * 0.5f) / radiusY;
                if (nx * nx + ny * ny <= 1f)
                {
                    var sampleX = x % patternWidth;
                    var sampleY = y % patternHeight;
                    texture.SetPixel(x, y, patternTexture.GetPixel(sampleX, sampleY));
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private static Texture2D LoadPatternTexture(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            return texture;
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite.texture;
        }

        var sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0].texture;
        }

        var textures = Resources.LoadAll<Texture2D>(resourcePath);
        if (textures != null && textures.Length > 0)
        {
            return textures[0];
        }

        if (resourcePath.Contains("images/"))
        {
            var alias = resourcePath.Replace("images/", "Images/");
            texture = Resources.Load<Texture2D>(alias);
            if (texture != null)
            {
                return texture;
            }

            sprite = Resources.Load<Sprite>(alias);
            if (sprite != null)
            {
                return sprite.texture;
            }

            sprites = Resources.LoadAll<Sprite>(alias);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0].texture;
            }

            textures = Resources.LoadAll<Texture2D>(alias);
            if (textures != null && textures.Length > 0)
            {
                return textures[0];
            }
        }

        if (resourcePath.Contains("Images/"))
        {
            var alias = resourcePath.Replace("Images/", "images/");
            texture = Resources.Load<Texture2D>(alias);
            if (texture != null)
            {
                return texture;
            }

            sprite = Resources.Load<Sprite>(alias);
            if (sprite != null)
            {
                return sprite.texture;
            }

            sprites = Resources.LoadAll<Sprite>(alias);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0].texture;
            }

            textures = Resources.LoadAll<Texture2D>(alias);
            if (textures != null && textures.Length > 0)
            {
                return textures[0];
            }
        }

        return null;
    }

    private static Sprite GetLumpEllipseMaskSprite()
    {
        var size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        var white = new Color32(255, 255, 255, 255);
        var radius = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - radius;
                var dy = y - radius;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, dist <= radius ? white : clear);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite GetLumpOutlineSprite()
    {
        if (_lumpOutlineSprite != null)
        {
            return _lumpOutlineSprite;
        }

        var size = LumpOutlineSpriteSize;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        var black = new Color32(0, 0, 0, 255);
        var radius = size * 0.5f - 1f;
        var thickness = Mathf.Max(1f, size * 0.08f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - (size - 1) * 0.5f;
                var dy = y - (size - 1) * 0.5f;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist >= radius - thickness && dist <= radius)
                {
                    texture.SetPixel(x, y, black);
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        _lumpOutlineSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return _lumpOutlineSprite;
    }

    private void ApplyLiquidRect(SpriteRenderer renderer, Rect sourceRect)
    {
        if (renderer == null)
        {
            return;
        }

        SetSpriteFromSourceRect(renderer, sourceRect);
    }

    private void SetRendererEllipse(SpriteRenderer renderer, float sourceX, float sourceY, float radiusX, float radiusY)
    {
        if (renderer == null)
        {
            return;
        }

        var center = SourceToWorld(new Vector2(sourceX, sourceY));
        var worldRadiusX = SourceLengthToWorldX(radiusX);
        var worldRadiusY = SourceLengthToWorldY(radiusY);

        renderer.transform.position = center;
        renderer.transform.localScale = new Vector3(worldRadiusX * 2f, worldRadiusY * 2f, 1f);
    }

    private void SetSpriteFromSourceRect(SpriteRenderer renderer, Rect sourceRect)
    {
        if (renderer == null || renderer.sprite == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        SetTransformFromSourceRect(renderer.transform, renderer.sprite.bounds.size, sourceRect);
    }

    private void SetMaskFromSourceRect(SpriteMask mask, Rect sourceRect)
    {
        if (mask == null || mask.sprite == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        SetTransformFromSourceRect(mask.transform, mask.sprite.bounds.size, sourceRect);
    }

    private void SetTransformFromSourceRect(Transform targetTransform, Vector2 spriteBoundsSize, Rect sourceRect)
    {
        if (targetTransform == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return;
        }

        var contentBounds = GetContentBounds();
        var left = contentBounds.min.x + (sourceRect.x / sourceWidth) * contentBounds.size.x;
        var right = contentBounds.min.x + ((sourceRect.x + sourceRect.width) / sourceWidth) * contentBounds.size.x;
        var top = contentBounds.max.y - (sourceRect.y / sourceHeight) * contentBounds.size.y;
        var bottom = contentBounds.max.y - ((sourceRect.y + sourceRect.height) / sourceHeight) * contentBounds.size.y;

        var center = new Vector3((left + right) * 0.5f, (top + bottom) * 0.5f, targetTransform.position.z);
        targetTransform.position = center;

        if (spriteBoundsSize.x <= 0f || spriteBoundsSize.y <= 0f)
        {
            return;
        }

        var targetWidth = Mathf.Abs(right - left);
        var targetHeight = Mathf.Abs(top - bottom);
        targetTransform.localScale = new Vector3(targetWidth / spriteBoundsSize.x, targetHeight / spriteBoundsSize.y, 1f);
    }

    private Vector2 SourceToWorld(Vector2 source)
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return Vector2.zero;
        }

        var bounds = GetContentBounds();
        var worldX = bounds.min.x + (source.x / sourceWidth) * bounds.size.x;
        var worldY = bounds.max.y - (source.y / sourceHeight) * bounds.size.y;
        return new Vector2(worldX, worldY);
    }

    private bool TryGetPointerSourcePosition(out Vector2 sourcePos, out Vector2 worldPos)
    {
        sourcePos = Vector2.zero;
        worldPos = Vector2.zero;

        if (_camera == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return false;
        }

        if (!TryGetPointerScreenPosition(out var screenPos))
        {
            return false;
        }

        var world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_camera.transform.position.z)));
        worldPos = new Vector2(world.x, world.y);

        var bounds = GetContentBounds();
        if (!bounds.Contains(new Vector3(worldPos.x, worldPos.y, bounds.center.z)))
        {
            return false;
        }

        var normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPos.x);
        var normalizedY = Mathf.InverseLerp(bounds.max.y, bounds.min.y, worldPos.y);

        sourcePos = new Vector2(normalizedX * sourceWidth, normalizedY * sourceHeight);
        return true;
    }

    private float SourceLengthToWorldX(float sourceLength)
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return 0f;
        }

        return (sourceLength / sourceWidth) * GetContentBounds().size.x;
    }

    private float SourceLengthToWorldY(float sourceLength)
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return 0f;
        }

        return (sourceLength / sourceHeight) * GetContentBounds().size.y;
    }

    private Bounds GetContentBounds()
    {
        var bgBounds = backgroundRenderer.bounds;
        var scale = Mathf.Clamp(contentScale, 0.6f, 1f);
        var offsetWorldX = (contentOffsetSource.x / sourceWidth) * bgBounds.size.x;
        var offsetWorldY = -(contentOffsetSource.y / sourceHeight) * bgBounds.size.y;

        var center = new Vector3(bgBounds.center.x + offsetWorldX, bgBounds.center.y + offsetWorldY, bgBounds.center.z);
        var size = new Vector3(bgBounds.size.x * scale, bgBounds.size.y * scale, bgBounds.size.z);
        return new Bounds(center, size);
    }

    private static bool IsPointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var j = i == 0 ? polygon.Count - 1 : i - 1;
            var pi = polygon[i];
            var pj = polygon[j];

            var intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                             (point.x < (pj.x - pi.x) * (point.y - pi.y) / ((pj.y - pi.y) + 0.00001f) + pi.x);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsPointInEllipse(Vector2 point, Lump lump)
    {
        var dx = point.x - lump.SourceX;
        var dy = point.y - lump.SourceY;
        if (Mathf.Abs(lump.RotationDeg) > float.Epsilon)
        {
            var radians = -lump.RotationDeg * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            var rotatedX = dx * cos - dy * sin;
            var rotatedY = dx * sin + dy * cos;
            dx = rotatedX;
            dy = rotatedY;
        }

        var nx = dx / lump.RadiusX;
        var ny = dy / lump.RadiusY;
        return (nx * nx + ny * ny) <= 1f;
    }

    private void SetBeakerLiquid2Alpha(float alpha)
    {
        if (_beakerLiquid2Renderer == null)
        {
            return;
        }

        var color = _beakerLiquid2Renderer.color;
        color.a = Mathf.Clamp01(alpha);
        _beakerLiquid2Renderer.color = color;
    }

    private void ApplyPart1BeakerMixTint()
    {
        if (_beakerLiquidRenderer == null)
        {
            return;
        }

        var progress = Mathf.Clamp01(_part1MixProgress);
        var tinted = Color.Lerp(Part1MixTintStart, Part1MixTintEnd, progress);
        _beakerLiquidRenderer.color = new Color(tinted.r, tinted.g, tinted.b, 1f);
    }

    private void ClearDroplets()
    {
        for (var i = 0; i < _droplets.Count; i++)
        {
            if (_droplets[i].GameObject != null)
            {
                Destroy(_droplets[i].GameObject);
            }
        }

        _droplets.Clear();
    }

    private void ClearLumps()
    {
        for (var i = 0; i < _lumps.Count; i++)
        {
            if (_lumps[i].GameObject != null)
            {
                Destroy(_lumps[i].GameObject);
            }
            if (_lumps[i].OutlineGameObject != null)
            {
                Destroy(_lumps[i].OutlineGameObject);
            }
        }

        _lumps.Clear();
    }

    private void ResolveUiReferences()
    {
        if (cText == null)
        {
            cText = FindTextByName("cText");
        }

        if (winText == null)
        {
            winText = FindTextByName("winText");
        }

        if (instructionText == null)
        {
            instructionText = FindTextByName("instructionText");
        }
    }

    private static TextMeshProUGUI FindTextByName(string objectName)
    {
        var target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private void ShowInstruction(string message)
    {
        if (instructionText == null)
        {
            return;
        }

        instructionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        instructionText.text = message;
    }

    private void SetWinText(string message, bool visible)
    {
        if (winText == null)
        {
            return;
        }

        winText.gameObject.SetActive(visible);
        winText.text = message;
    }

    private static void SetTextVisible(TextMeshProUGUI text, bool visible)
    {
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(visible);
    }

    private static bool IsPrimaryClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
#endif

        screenPos = Input.mousePosition;
        return true;
    }
}
