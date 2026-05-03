namespace Assets.logic
{
    internal sealed class ButtonPreset
    {
        internal ButtonPreset(string key, string id, float x, float y, float width, float height)
        {
            Key = key;
            Id = id;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal string Key { get; }
        internal string Id { get; }
        internal float X { get; }
        internal float Y { get; }
        internal float Width { get; }
        internal float Height { get; }

        internal void GetScaledRect(float targetWidth, float targetHeight, out float x, out float y, out float width, out float height)
        {
            var scaleX = targetWidth / NavigationDataLoader.ReferenceWidth;
            var scaleY = targetHeight / NavigationDataLoader.ReferenceHeight;

            x = X * scaleX;
            y = Y * scaleY;
            width = Width * scaleX;
            height = Height * scaleY;
        }
    }
}
