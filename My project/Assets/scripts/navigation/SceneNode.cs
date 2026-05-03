using System;
using System.Collections.Generic;

namespace Assets.logic
{
    internal sealed class SceneNode
    {
        internal SceneNode(
            string key,
            SceneNodeType nodeType,
            string imagePath,
            string fallbackSlide,
            string minigameClassName,
            Dictionary<string, ButtonDefinition> buttons,
            IReadOnlyList<ActionDefinition> onEnter,
            string sourceFile)
        {
            Key = key;
            NodeType = nodeType;
            ImagePath = imagePath;
            FallbackSlide = fallbackSlide;
            MinigameClassName = minigameClassName;
            Buttons = buttons ?? new Dictionary<string, ButtonDefinition>(StringComparer.OrdinalIgnoreCase);
            OnEnter = onEnter ?? Array.Empty<ActionDefinition>();
            SourceFile = sourceFile;
        }

        internal string Key { get; }
        internal SceneNodeType NodeType { get; }
        internal string ImagePath { get; }
        internal string FallbackSlide { get; }
        internal string MinigameClassName { get; }
        internal IReadOnlyDictionary<string, ButtonDefinition> Buttons { get; }
        internal IReadOnlyList<ActionDefinition> OnEnter { get; }
        internal string SourceFile { get; }
    }

    internal enum SceneNodeType
    {
        Slide,
        Minigame
    }
}
