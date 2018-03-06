namespace OpenWheels
{
    public struct ColorPoint
    {
        public float Position { get; }
        public HsvColor Color { get; }

        public ColorPoint(float position, HsvColor color)
        {
            Position = position;
            Color = color;
        }
    }
}
