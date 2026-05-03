using System;
using System.Collections.Generic;

namespace Assets.logic
{
    internal sealed class NavigationDatabase
    {
        internal Dictionary<string, SceneNode> Nodes { get; } = new Dictionary<string, SceneNode>(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, ButtonPreset> Presets { get; } = new Dictionary<string, ButtonPreset>(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, string> ButtonSpriteLookup { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
