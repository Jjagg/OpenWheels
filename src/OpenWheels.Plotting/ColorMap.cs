using System.Collections;
using System.Collections.Generic;

namespace OpenWheels.Plotting
{
    public class ColorMap : IEnumerable<ColorPoint>
    {
        private readonly SortedList<float, ColorPoint> _colorPoints;
        private static readonly Color ZeroColor = Color.Black;

        public ColorMap()
        {
            _colorPoints = new SortedList<float, ColorPoint>();
        }

        public void Add(float value, HsvColor color)
        {
            _colorPoints.Add(value, new ColorPoint(value, color));
        }

        public void Add(ColorPoint cp)
        {
            _colorPoints.Add(cp.Position, cp);
        }

        public Color Map(float value)
        {
            if (_colorPoints.Count == 0)
                return ZeroColor;

            int i;
            for (i = 0; i < _colorPoints.Count; i++)
            {
                if (_colorPoints.Values[i].Position >= value)
                {
                    i--;
                    break;
                }
            }

            if (i < 0)
                return _colorPoints.Values[0].Color;
            if (i >= _colorPoints.Count - 1)
                return _colorPoints.Values[_colorPoints.Count - 1].Color;

            var c1 = _colorPoints.Values[i];
            var c2 = _colorPoints.Values[i + 1];
            var dc = c2.Position - c1.Position;
            var t = (value - c1.Position) / dc;
            return HsvColor.Lerp(c1.Color, c2.Color, t);
        }

        public IEnumerator<ColorPoint> GetEnumerator()
        {
            foreach (var cp in _colorPoints)
                yield return cp.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}