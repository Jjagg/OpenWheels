using System;

namespace OpenWheels.Plotting
{
    public struct HsvColor
    {
        public readonly float H;
        public readonly float S;
        public readonly float V;

        public HsvColor(float h, float s, float v)
        {
            H = NormalizeHue(h);
            S = s < 0 ? 0 : (s > 1 ? 1 : s);
            V = v < 0 ? 0 : (v > 1 ? 1 : v);
        }

        public override string ToString()
        {
            return $"H: {H}°; S: {(int) (S * 100)}%, V: {(int) (V * 100)}%";
        }

        private static float NormalizeHue(float h)
        {
            return (h % 360 + 360) % 360;
        }

        public static HsvColor Lerp(HsvColor c1, HsvColor c2, float t)
        {
            var d = c2.H - c1.H;
            var delta = d + (Math.Abs(d) > 180 ? (d < 0 ? 360 : -360) : 0);

            return new HsvColor(
                c1.H + t * delta,
                c1.S + t * (c2.S - c1.S),
                c1.V + t * (c2.V - c1.V));
        }

        public static implicit operator Color(HsvColor hsv)
        {
            return HsvToRgb(hsv);
        }

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
                h = ((g - b / d) + 6) % 6;
            else if (max == g)
                h = (b - r) / d + 2;
            else
                h = (r - g) / d + 4;
            var s = max == 0 ? 0 : d / max;
            return new HsvColor(h, s, max);
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
