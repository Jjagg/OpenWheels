namespace OpenWheels.GameTools.Rendering
{
    public class Sprite
    {
        public readonly int Texture;
        public readonly RectangleF Rect;

        public Sprite(int texture, RectangleF rect)
        {
            Texture = texture;
            Rect = rect;
        }

        public override string ToString()
        {
            var t = $"{nameof(Texture)}: {Texture}";
            if (Rect != RectangleF.Unit)
                t = t + $", {nameof(Rect)}: {Rect}";
            return t;
        }
    }
}