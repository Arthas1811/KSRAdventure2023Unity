using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.logic
{
    internal static class NavigationDataLoader
    {
        // Navigation imagery and hotspot coordinates are authored at 1620x1080.
        // Keeping the reference width in sync with the asset pipeline prevents
        // double-scaling and keeps overlays aligned with the underlying images.
        public const float ReferenceWidth = 1620f;
        public const float ReferenceHeight = 1080f;

        private const string JsonRootRelative = "resources/jsons";
        private const string ImagesRootRelative = "resources/images";

        private static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg" };

        internal static NavigationDatabase Load()
        {
            var database = new NavigationDatabase();

            var assetsRoot = Application.dataPath;
            var resourcesRoot = Path.Combine(assetsRoot, "resources");
            var jsonRoot = Path.Combine(assetsRoot, JsonRootRelative.Replace('/', Path.DirectorySeparatorChar));
            var imagesRoot = Path.Combine(assetsRoot, ImagesRootRelative.Replace('/', Path.DirectorySeparatorChar));

            var loadedAny = TryLoadFromJsonDirectory(jsonRoot, database);
            if (!loadedAny)
            {
                loadedAny = TryLoadFromResources(database);
                if (!loadedAny)
                {
                    Debug.LogWarning($"NavigationDataLoader: JSON data not found at {jsonRoot} or Resources/jsons.");
                }
            }

            BuildSpriteLookup(resourcesRoot, imagesRoot, database);
            if (database.ButtonSpriteLookup.Count == 0)
            {
                BuildSpriteLookupFromResources(database);
            }

            return database;
        }

        private static bool TryLoadFromJsonDirectory(string jsonRoot, NavigationDatabase database)
        {
            if (!Directory.Exists(jsonRoot))
            {
                return false;
            }

            var loadedAny = false;
            foreach (var jsonFile in Directory.GetFiles(jsonRoot, "*.json", SearchOption.AllDirectories))
            {
                loadedAny = true;
                var fileName = Path.GetFileName(jsonFile);
                try
                {
                    if (string.Equals(fileName, "presets.json", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadPresets(jsonFile, database);
                        continue;
                    }

                    ProcessNavigationJson(File.ReadAllText(jsonFile), fileName, database);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NavigationDataLoader: Failed to parse {fileName}. {ex.Message}");
                }
            }

            return loadedAny;
        }

        private static bool TryLoadFromResources(NavigationDatabase database)
        {
            var jsonAssets = Resources.LoadAll<TextAsset>("jsons");
            if (jsonAssets == null || jsonAssets.Length == 0)
            {
                return false;
            }

            var loadedAny = false;
            foreach (var asset in jsonAssets)
            {
                if (asset == null)
                {
                    continue;
                }

                loadedAny = true;
                var fileName = string.IsNullOrWhiteSpace(asset.name) ? "<unnamed>.json" : $"{asset.name}.json";
                try
                {
                    if (string.Equals(asset.name, "presets", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadPresetsFromContent(asset.text, database);
                        continue;
                    }

                    ProcessNavigationJson(asset.text, fileName, database);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NavigationDataLoader: Failed to parse {fileName} from Resources. {ex.Message}");
                }
            }

            return loadedAny;
        }

        private static void ProcessNavigationJson(string jsonContent, string sourceFile, NavigationDatabase database)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return;
            }

            if (MiniJson.Deserialize(jsonContent) is not Dictionary<string, object> rootObject)
            {
                Debug.LogWarning($"NavigationDataLoader: Root of {sourceFile} is not a JSON object.");
                return;
            }

            foreach (var entry in rootObject)
            {
                if (entry.Value is Dictionary<string, object> nodeObject)
                {
                    var node = ParseSceneNode(entry.Key, nodeObject, sourceFile);
                    if (node != null)
                    {
                        database.Nodes[entry.Key] = node;
                    }
                }
            }
        }

        private static SceneNode ParseSceneNode(string nodeKey, Dictionary<string, object> element, string sourceFile)
        {
            var type = ReadString(element, "Type");
            var onEnter = ParseActions(element, nodeKey, "<OnEnter>", sourceFile, "OnEnter");
            if (string.Equals(type, "Minigame", StringComparison.OrdinalIgnoreCase))
            {
                return new SceneNode(
                    nodeKey,
                    SceneNodeType.Minigame,
                    null,
                    ReadString(element, "FallbackSlide"),
                    ReadString(element, "MinigameDefClassName"),
                    new Dictionary<string, ButtonDefinition>(),
                    onEnter,
                    sourceFile);
            }

            var imagePath = ReadString(element, "Image");
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                Debug.LogWarning($"NavigationDataLoader: Node '{nodeKey}' in {sourceFile} has no Image path.");
            }

            var buttons = new Dictionary<string, ButtonDefinition>(StringComparer.OrdinalIgnoreCase);
            if (element.TryGetValue("Buttons", out var buttonsObj) && buttonsObj is Dictionary<string, object> buttonDict)
            {
                foreach (var buttonEntry in buttonDict)
                {
                    if (buttonEntry.Value is Dictionary<string, object> buttonObject)
                    {
                        var buttonDef = ParseButton(nodeKey, buttonEntry.Key, buttonObject, sourceFile);
                        if (buttonDef != null)
                        {
                            buttons[buttonEntry.Key] = buttonDef;
                        }
                    }
                }
            }

            return new SceneNode(
                nodeKey,
                SceneNodeType.Slide,
                imagePath,
                null,
                null,
                buttons,
                onEnter,
                sourceFile);
        }

        private static ButtonDefinition ParseButton(string nodeKey, string buttonKey, Dictionary<string, object> element, string sourceFile)
        {
            var type = ReadString(element, "Type");
            var visible = !element.TryGetValue("Visible", out var visibleObj) || !IsFalsy(visibleObj);
            var actions = ParseActions(element, nodeKey, buttonKey, sourceFile);
            var buttonKind = ButtonType.Preset;
            var rectangle = Rect.zero;

            if (string.Equals(type, "polygon", StringComparison.OrdinalIgnoreCase))
            {
                var rawPoints = ReadString(element, "Points");
                var polygonPoints = ParsePolygonPoints(rawPoints, nodeKey, buttonKey, sourceFile);
                return new ButtonDefinition(
                    buttonKey,
                    ButtonType.Polygon,
                    visible,
                    actions,
                    null,
                    null,
                    null,
                    polygonPoints,
                    Rect.zero);
            }

            if (string.Equals(type, "rect", StringComparison.OrdinalIgnoreCase))
            {
                rectangle = ParseRectangle(ReadString(element, "Points"), nodeKey, buttonKey, sourceFile, "Rect");
                return new ButtonDefinition(
                    buttonKey,
                    ButtonType.Polygon,
                    visible,
                    actions,
                    null,
                    null,
                    null,
                    RectangleToPolygonPoints(rectangle),
                    Rect.zero);
            }

            if (string.Equals(type, "circle", StringComparison.OrdinalIgnoreCase))
            {
                var polygonPoints = ParseCirclePoints(ReadString(element, "Points"), nodeKey, buttonKey, sourceFile);
                return new ButtonDefinition(
                    buttonKey,
                    ButtonType.Polygon,
                    visible,
                    actions,
                    null,
                    null,
                    null,
                    polygonPoints,
                    Rect.zero);
            }

            if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase))
            {
                buttonKind = ButtonType.Image;
                rectangle = ParseRectangle(ReadString(element, "Points"), nodeKey, buttonKey, sourceFile, "Image");
            }

            var presetKey = ReadString(element, "Preset");
            if (string.IsNullOrWhiteSpace(presetKey))
            {
                presetKey = buttonKey;
            }

            string imageKey = null;
            string explicitPath = null;
            var imageValue = ReadString(element, "Image");
            if (!string.IsNullOrWhiteSpace(imageValue))
            {
                if (imageValue.Contains("/", StringComparison.Ordinal) || imageValue.Contains("\\", StringComparison.Ordinal) || imageValue.Contains(".", StringComparison.Ordinal))
                {
                    explicitPath = imageValue;
                }
                else
                {
                    imageKey = imageValue;
                    if (buttonKind == ButtonType.Preset)
                    {
                        presetKey = imageValue;
                    }
                }
            }

            return new ButtonDefinition(
                buttonKey,
                buttonKind,
                visible,
                actions,
                presetKey,
                imageKey,
                explicitPath,
                Array.Empty<Vector2>(),
                rectangle);
        }

        private static IReadOnlyList<ActionDefinition> ParseActions(
            Dictionary<string, object> element,
            string nodeKey,
            string buttonKey,
            string sourceFile,
            string propertyName = "Actions")
        {
            if (!element.TryGetValue(propertyName, out var actionsObj) || actionsObj is not IList actionList)
            {
                return Array.Empty<ActionDefinition>();
            }

            var actions = new List<ActionDefinition>();
            foreach (var entry in actionList)
            {
                if (entry is not IList parts || parts.Count == 0)
                {
                    continue;
                }

                var command = ConvertToString(parts[0]);
                if (string.IsNullOrWhiteSpace(command))
                {
                    Debug.LogWarning($"NavigationDataLoader: Action missing command. Node '{nodeKey}', button '{buttonKey}' in {sourceFile}.");
                    continue;
                }

                var parameters = new List<string>();
                for (var i = 1; i < parts.Count; i++)
                {
                    var parameter = ConvertToString(parts[i]);
                    if (!string.IsNullOrWhiteSpace(parameter))
                    {
                        parameters.Add(parameter);
                    }
                }

                actions.Add(new ActionDefinition(command, parameters));
            }

            return actions;
        }

        private static IReadOnlyList<Vector2> ParsePolygonPoints(string rawPoints, string nodeKey, string buttonKey, string sourceFile)
        {
            var points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(rawPoints))
            {
                Debug.LogWarning($"NavigationDataLoader: Polygon button '{buttonKey}' in '{nodeKey}' has no points defined ({sourceFile}).");
                return points;
            }

            var separators = new[] { ',', '\n', '\r', '\t', ' ' };
            var tokens = rawPoints.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length % 2 != 0)
            {
                Debug.LogWarning($"NavigationDataLoader: Polygon button '{buttonKey}' in '{nodeKey}' has uneven point data ({sourceFile}).");
                return points;
            }

            for (var i = 0; i < tokens.Length; i += 2)
            {
                if (!TryParseFloat(tokens[i], out var x) || !TryParseFloat(tokens[i + 1], out var y))
                {
                    Debug.LogWarning($"NavigationDataLoader: Failed to parse polygon point at index {i} for button '{buttonKey}' in '{nodeKey}' ({sourceFile}).");
                    points.Clear();
                    break;
                }

                points.Add(new Vector2(x, y));
            }

            return points;
        }

        private static Rect ParseRectangle(string rawPoints, string nodeKey, string buttonKey, string sourceFile, string shapeName)
        {
            if (string.IsNullOrWhiteSpace(rawPoints))
            {
                Debug.LogWarning($"NavigationDataLoader: {shapeName} button '{buttonKey}' in '{nodeKey}' has no rectangle defined ({sourceFile}).");
                return Rect.zero;
            }

            var separators = new[] { ',', '\n', '\r', '\t', ' ' };
            var tokens = rawPoints.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 4)
            {
                Debug.LogWarning($"NavigationDataLoader: {shapeName} button '{buttonKey}' in '{nodeKey}' expected 4 rectangle values ({sourceFile}).");
                return Rect.zero;
            }

            if (!TryParseFloat(tokens[0], out var x) ||
                !TryParseFloat(tokens[1], out var y) ||
                !TryParseFloat(tokens[2], out var width) ||
                !TryParseFloat(tokens[3], out var height))
            {
                Debug.LogWarning($"NavigationDataLoader: {shapeName} button '{buttonKey}' in '{nodeKey}' has invalid rectangle values ({sourceFile}).");
                return Rect.zero;
            }

            return new Rect(x, y, width, height);
        }

        private static IReadOnlyList<Vector2> RectangleToPolygonPoints(Rect rectangle)
        {
            if (rectangle.width <= 0f || rectangle.height <= 0f)
            {
                return Array.Empty<Vector2>();
            }

            return new[]
            {
                new Vector2(rectangle.xMin, rectangle.yMin),
                new Vector2(rectangle.xMax, rectangle.yMin),
                new Vector2(rectangle.xMax, rectangle.yMax),
                new Vector2(rectangle.xMin, rectangle.yMax)
            };
        }

        private static IReadOnlyList<Vector2> ParseCirclePoints(string rawPoints, string nodeKey, string buttonKey, string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(rawPoints))
            {
                Debug.LogWarning($"NavigationDataLoader: Circle button '{buttonKey}' in '{nodeKey}' has no circle defined ({sourceFile}).");
                return Array.Empty<Vector2>();
            }

            var separators = new[] { ',', '\n', '\r', '\t', ' ' };
            var tokens = rawPoints.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 3)
            {
                Debug.LogWarning($"NavigationDataLoader: Circle button '{buttonKey}' in '{nodeKey}' expected 3 circle values ({sourceFile}).");
                return Array.Empty<Vector2>();
            }

            if (!TryParseFloat(tokens[0], out var centerX) ||
                !TryParseFloat(tokens[1], out var centerY) ||
                !TryParseFloat(tokens[2], out var radius) ||
                radius <= 0f)
            {
                Debug.LogWarning($"NavigationDataLoader: Circle button '{buttonKey}' in '{nodeKey}' has invalid circle values ({sourceFile}).");
                return Array.Empty<Vector2>();
            }

            const int SegmentCount = 32;
            var points = new List<Vector2>(SegmentCount);
            for (var i = 0; i < SegmentCount; i++)
            {
                var angle = Mathf.PI * 2f * i / SegmentCount;
                points.Add(new Vector2(
                    centerX + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius));
            }

            return points;
        }

        private static void LoadPresets(string presetFile, NavigationDatabase database)
        {
            var content = File.ReadAllText(presetFile);
            LoadPresetsFromContent(content, database);
        }

        private static void LoadPresetsFromContent(string content, NavigationDatabase database)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var sanitized = SanitizePresetJson(content);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                Debug.LogWarning("NavigationDataLoader: Presets JSON could not be sanitized.");
                return;
            }

            try
            {
                if (MiniJson.Deserialize(sanitized) is not IList entries)
                {
                    Debug.LogWarning("NavigationDataLoader: Presets JSON expected an array.");
                    return;
                }

                foreach (var entry in entries)
                {
                    if (entry is not Dictionary<string, object> presetContainer)
                    {
                        continue;
                    }

                    foreach (var pair in presetContainer)
                    {
                        if (pair.Value is not Dictionary<string, object> presetObject)
                        {
                            continue;
                        }

                        var presetName = pair.Key;
                        var id = ReadString(presetObject, "id");
                        var x = ReadFloat(presetObject, "x");
                        var y = ReadFloat(presetObject, "y");
                        var width = ReadFloat(presetObject, "width");
                        var height = ReadFloat(presetObject, "height");

                        database.Presets[presetName] = new ButtonPreset(presetName, id, x, y, width, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"NavigationDataLoader: Failed to parse presets.json. {ex.Message}");
            }
        }

        private static string SanitizePresetJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var normalized = raw.Replace("\r\n", "\n").Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            normalized = Regex.Replace(normalized, ",\\s*([}\\]])", "$1");
            normalized = normalized.Replace("}\n\t{", "},{", StringComparison.Ordinal);
            normalized = normalized.Replace("}\n{", "},{", StringComparison.Ordinal);
            normalized = normalized.Replace("}\n\n{", "},{", StringComparison.Ordinal);
            normalized = normalized.Replace("}\t{", "},{", StringComparison.Ordinal);

            if (!normalized.StartsWith("[", StringComparison.Ordinal))
            {
                normalized = "[" + normalized;
            }

            if (!normalized.EndsWith("]", StringComparison.Ordinal))
            {
                normalized += "]";
            }

            return normalized;
        }

        private static void BuildSpriteLookup(string resourcesRoot, string imagesRoot, NavigationDatabase database)
        {
            if (!Directory.Exists(imagesRoot))
            {
                return;
            }

            var uiRoot = Path.Combine(imagesRoot, "UI_Images");
            if (!Directory.Exists(uiRoot))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(uiRoot, "*.*", SearchOption.AllDirectories))
            {
                if (!SpriteExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var key = Path.GetFileNameWithoutExtension(file);
                var relative = Path.GetRelativePath(resourcesRoot, file).Replace("\\", "/");

                if (!database.ButtonSpriteLookup.ContainsKey(key))
                {
                    database.ButtonSpriteLookup[key] = relative;
                }
            }
        }

        private static void BuildSpriteLookupFromResources(NavigationDatabase database)
        {
            AddSpritesFromResources("images/UI_Images", database);
            AddSpritesFromResources("images/UI_Images/arrows", database);
        }

        private static void AddSpritesFromResources(string resourceFolder, NavigationDatabase database)
        {
            var sprites = Resources.LoadAll<Sprite>(resourceFolder);
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

            foreach (var sprite in sprites)
            {
                if (sprite == null || string.IsNullOrWhiteSpace(sprite.name))
                {
                    continue;
                }

                if (!database.ButtonSpriteLookup.ContainsKey(sprite.name))
                {
                    database.ButtonSpriteLookup[sprite.name] = $"{resourceFolder}/{sprite.name}.png";
                }
            }
        }

        private static string ReadString(Dictionary<string, object> element, string propertyName)
        {
            if (element != null && element.TryGetValue(propertyName, out var value))
            {
                return ConvertToString(value);
            }

            return null;
        }

        private static float ReadFloat(Dictionary<string, object> element, string propertyName)
        {
            if (element != null && element.TryGetValue(propertyName, out var value))
            {
                return ConvertToFloat(value);
            }

            return 0f;
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string ConvertToString(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    return s;
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case bool b:
                    return b ? bool.TrueString : bool.FalseString;
                default:
                    return value.ToString();
            }
        }

        private static float ConvertToFloat(object value)
        {
            switch (value)
            {
                case null:
                    return 0f;
                case double d:
                    return (float)d;
                case float f:
                    return f;
                case int i:
                    return i;
                case long l:
                    return l;
                case string s when TryParseFloat(s, out var parsed):
                    return parsed;
                default:
                    return 0f;
            }
        }

        private static bool IsFalsy(object value)
        {
            if (value is bool b)
            {
                return !b;
            }

            if (value is string s)
            {
                return string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(s, "0", StringComparison.OrdinalIgnoreCase);
            }

            if (value is double d)
            {
                return Math.Abs(d) < float.Epsilon;
            }

            if (value is int i)
            {
                return i == 0;
            }

            return false;
        }
    }
}
