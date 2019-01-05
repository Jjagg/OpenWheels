using System;

namespace OpenWheels
{
    public static class ColorConversions
    {
        /// <summary>
        /// Convert an RGB color to an HSV color.
        /// </summary>
        /// <param name="rgb">RGB color to convert.</param>
        /// <returns>Corresponding color in HSV color space.</returns>
        public static HsvColor ToHsv(this Color color)
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

        /// <summary>
        /// Convert an HSV color to an RGB color.
        /// </summary>
        /// <param name="hsv">HSV color to convert.</param>
        /// <returns>Corresponding color in RGB color space.</returns>
        public static Color ToRgb(this HsvColor hsv)
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

