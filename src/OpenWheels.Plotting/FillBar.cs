using System;
using OpenWheels.Rendering;

namespace OpenWheels.Plotting
{
    public class FillBar
    {
        public RectangleF Rectangle { get; set; }
        public Direction Direction { get; set; }
        public Color FillColor { get; set; }
        public Color NotFillColor { get; set; }
        public float Progress { get; set; }

        public FillBar()
        {
        }

        public void Draw(Batcher batcher)
        {
            Draw(batcher, Rectangle, Direction, FillColor, NotFillColor, Progress);
        }

        public static void Draw(Batcher batcher, RectangleF rect, Direction direction, Color fill, Color notFill, float progress)
        {
            switch (direction)
            {
                case Direction.Up:
                    batcher.DrawBarUp(rect.Center.X, rect.Bottom, rect.Height * progress, fill, rect.Width);
                    if (notFill.A != 0)
                        batcher.DrawBarDown(rect.Center.X, rect.Top, rect.Height * (1f - progress), notFill, rect.Width);
                    break;
                case Direction.Right:
                    batcher.DrawBarRight(rect.Center.Y, rect.Left, rect.Width * progress, fill, rect.Height);
                    if (notFill.A != 0)
                        batcher.DrawBarLeft(rect.Center.Y, rect.Right, rect.Width * (1f - progress), notFill, rect.Height);
                    break;
                case Direction.Down:
                    batcher.DrawBarDown(rect.Center.X, rect.Top, rect.Height * progress, fill, rect.Width);
                    if (notFill.A != 0)
                        batcher.DrawBarUp(rect.Center.X, rect.Bottom, rect.Height * (1f - progress), notFill, rect.Width);
                    break;
                case Direction.Left:
                    batcher.DrawBarLeft(rect.Center.Y, rect.Right, rect.Width * progress, fill, rect.Height);
                    if (notFill.A != 0)
                        batcher.DrawBarRight(rect.Center.Y, rect.Left, rect.Width * (1f - progress), notFill, rect.Height);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

    }
}
