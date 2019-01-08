using System;

namespace OpenWheels
{
    public static class ColorConversions
    {
        /// <summary>
        /// Convert an RGB color to an HSV color. Note that HsvColor does not
        /// contain an alpha channel, so the alpha value of the RGB color is lost.
        /// </summary>
        /// <param name="rgb">RGB color to convert.</param>
        /// <returns>Corresponding color in HSV color space.</returns>
        public static HsvColor ToHsv(this Color color)
        {
            var r = color.R / 255f;
            var g = color.G / 255f;
            var b = color.B / 255f;
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
        /// Convert an HSV color to an RGB color. The alpha channel is set to 255 (fully opaque).
        /// </summary>
        /// <remarks>
        /// Note that HSV color is stored with 3 32-bit floating point numbers and color
        /// with 1 byte for the R, G and B channels (and 1 alpha channel byte), so precision
        /// is lost in this conversion.
        /// </remarks>
        /// <param name="hsv">HSV color to convert.</param>
        /// <returns>Corresponding color in RGB color space.</returns>
        public static Color ToRgb(this HsvColor hsv)
        {
            var c = hsv.V * hsv.S;
            var hp = hsv.H / 60f;
            var x = c * (1 - Math.Abs(hp % 2 - 1));
            float r = 0f, g = 0f, b = 0f;
            if (hp <= 1)
            {
                r = c;
                g = x;
            }
            else if (hp <= 2)
            {
                r = x;
                g = c;
            }
            else if (hp <= 3)
            {
                g = c;
                b = x;
            }
            else if (hp <= 4)
            {
                g = x;
                b = c;
            }
            else if (hp <= 5)
            {
                r = x;
                b = c;
            }
            else
            {
                r = c;
                b = x;
            }

            var m = (hsv.V - c);
            return new Color(r + m, g + m, b + m);
        }
    }
}

