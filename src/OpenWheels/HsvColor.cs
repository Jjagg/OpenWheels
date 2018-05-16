using System;

namespace OpenWheels
{
    /// <summary>
    /// Hue, Saturation, Value color.
    /// </summary>
    public struct HsvColor
    {
        /// <summary>
        /// Hue of the color in the range [0 360[.
        /// </summary>
        public readonly float H;
        /// <summary>
        /// Saturation of the color in the range [0 1].
        /// </summary>
        public readonly float S;
        /// <summary>
        /// Value of the color in the range [0 1].
        /// </summary>
        public readonly float V;

        /// <summary>
        /// Create a new HSV color.
        /// </summary>
        /// <param name="h">Hue of the color. Normalized to [0 360[.</param>
        /// <param name="s">Saturation of the color. Clamped to [0 1].</param>
        /// <param name="v">Value of the color. Clamped to [0 1].</param>
        public HsvColor(float h, float s, float v)
        {
            H = NormalizeHue(h);
            S = s < 0 ? 0 : (s > 1 ? 1 : s);
            V = v < 0 ? 0 : (v > 1 ? 1 : v);
        }

        private static float NormalizeHue(float h)
        {
            return (h % 360 + 360) % 360;
        }

        public override string ToString()
        {
            return $"H: {H}°; S: {(int) (S * 100)}%, V: {(int) (V * 100)}%";
        }

        /// <summary>
        /// Linearly interpolate between two colors.
        /// Takes the closest path from one hue to the other.
        /// </summary>
        /// <param name="c1">First color.</param>
        /// <param name="c2">Second color.</param>
        /// <param name="t">Interpolation factor in the range [0, 1].</param>
        /// <returns>Interpolated color.</returns>
        public static HsvColor Lerp(HsvColor c1, HsvColor c2, float t)
        {
            var d = c2.H - c1.H;
            var delta = d + (Math.Abs(d) > 180 ? (d < 0 ? 360 : -360) : 0);

            return new HsvColor(
                c1.H + t * delta,
                c1.S + t * (c2.S - c1.S),
                c1.V + t * (c2.V - c1.V));
        }

        /// <summary>
        /// Convert an HSV color to an RGB color.
        /// </summary>
        /// <param name="hsv">HSV color to convert.</param>
        /// <returns>Corresponding color in RGB color space.</returns>
        public static implicit operator Color(HsvColor hsv)
        {
            return HsvToRgb(hsv);
        }

        /// <summary>
        /// Convert an RGB color to an HSV color.
        /// </summary>
        /// <param name="rgb">RGB color to convert.</param>
        /// <returns>Corresponding color in HSV color space.</returns>
        public static implicit operator HsvColor(Color rgb)
        {
            return RgbToHsv(rgb);
        }

        private static HsvColor RgbToHsv(Color color)
        {
            var r = color.R / 255f;
            var b = color.G / 255f;
            var g = color.B / 255f;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var d = max - min;
            float h;
            if (d == 0)
                h = 0;
            else if (max == r)
                h = ((g - b) / d + 6) % 6;
            else if (max == g)
                h = (b - r) / d + 2;
            else
                h = (r - g) / d + 4;
            var s = max == 0 ? 0 : d / max;
            
            return new HsvColor(60 * h, s, max);
       }

        private static Color HsvToRgb(HsvColor hsv)
        {
            var hi = hsv.H / 60f;
            var c = hsv.V * hsv.S;
            var x = c * (1 - Math.Abs(hi % 2 - 1));
            var m = hsv.V - c;
            if (hi < 1)
                return new Color(c + m, x + m, m);
            if (hi < 2)
                return new Color(x + m, c + m, m);
            if (hi < 3)
                return new Color(m, c + m, x + m);
            if (hi < 4)
                return new Color(m, x + m, c + m);
            if (hi < 5)
                return new Color(x + m, m, c + m);
            return new Color(c + m, m, x + m);
       }
    }
}
