namespace OpenWheels.Rendering
{
    internal static class Conversions
    {
        public static RectangleF ToOwRect(this SixLabors.Primitives.RectangleF rect)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}