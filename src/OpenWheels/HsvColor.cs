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
    }
}
