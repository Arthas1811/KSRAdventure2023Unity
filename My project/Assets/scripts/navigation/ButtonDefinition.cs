using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.logic
{
    internal sealed class ButtonDefinition
    {
        internal ButtonDefinition(
            string key,
            ButtonType kind,
            bool visible,
            IReadOnlyList<ActionDefinition> actions,
            string presetKey,
            string imageKey,
            string explicitImagePath,
            IReadOnlyList<Vector2> polygonPoints,
            Rect rectangle)
        {
            Key = key;
            Kind = kind;
            Visible = visible;
            Actions = actions ?? Array.Empty<ActionDefinition>();
            PresetKey = presetKey;
            ImageKey = imageKey;
            ExplicitImagePath = explicitImagePath;
            PolygonPoints = polygonPoints ?? Array.Empty<Vector2>();
            Rectangle = rectangle;
        }

        internal string Key { get; }
        internal ButtonType Kind { get; }
        internal bool Visible { get; }
        internal IReadOnlyList<ActionDefinition> Actions { get; }
        internal string PresetKey { get; }
        internal string ImageKey { get; }
        internal string ExplicitImagePath { get; }
        internal IReadOnlyList<Vector2> PolygonPoints { get; }
        internal Rect Rectangle { get; }
    }

    internal enum ButtonType
    {
        Preset,
        Polygon,
        Image
    }
}
