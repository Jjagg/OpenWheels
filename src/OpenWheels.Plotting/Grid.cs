using System.Numerics;
using OpenWheels.Rendering;

namespace OpenWheels.Plotting
{
    public class Grid
    {
        public RectangleF Rectangle { get; set; }
        public float SpaceX { get; set; }
        public float SpaceY { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float Thickness { get; set; }
        public Color? BackgroundColor { get; set; }
        public Color Color { get; set; }

        public Grid()
        {
        }

        public void Draw(Batcher batcher)
        {
            if (BackgroundColor.HasValue)
                batcher.FillRect(Rectangle, BackgroundColor.Value);
            Draw(batcher, Rectangle, SpaceX, SpaceY, OffsetX, OffsetY, Thickness, Color);
        }

        public static void Draw(Batcher batcher, RectangleF rect, float spaceX, float spaceY, float offsetX, float offsetY, float thickness, Color color)
        {
            for (var x = rect.Left + offsetX + spaceX; x < rect.Right; x += spaceX)
                batcher.DrawBarDown(x, rect.Top, rect.Height, color, thickness);
            for (var y = rect.Top + offsetY; y < rect.Bottom; y += spaceY)
                batcher.DrawBarRight(y, rect.Left, rect.Width, color, thickness);
        }
    }
}
