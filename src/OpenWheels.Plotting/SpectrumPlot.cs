using System.Collections.Generic;
using System.Numerics;
using OpenWheels.Rendering;

namespace OpenWheels.Plotting
{
    public class SpectrumPlot
    {
        public RectangleF Rectangle { get; set; }
        public float BarWidth { get; set; }
        public float BarSpacing { get; set; }
        public Color? BackgroundColor { get; set; }
        public Color Color { get; set; }

        public void Draw(Batcher batcher, IEnumerable<float> values)
        {
            if (BackgroundColor.HasValue)
                batcher.FillRect(Rectangle, BackgroundColor.Value);
            Draw(batcher, Rectangle, BarWidth, BarSpacing, values, Color);
        }

        public static void Draw(Batcher batcher, RectangleF rect, float barWidth, float barSpacing, IEnumerable<float> values, Color color)
        {
            var x = rect.Left + barWidth / 2;
            foreach (var v in values)
            {
                if (x + barWidth / 2 > rect.Right)
                    break;
                if (v < 0)
                    continue;

                var clamped = v > 1 ? 1 : v;
                var height = rect.Height * clamped;
                batcher.DrawBarUp(x, rect.Bottom, height, color, barWidth);

                x += barWidth + barSpacing;
            }
        }
    }
}
