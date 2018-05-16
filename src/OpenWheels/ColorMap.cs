using System.Collections;
using System.Collections.Generic;

namespace OpenWheels
{
    public class ColorMap : IEnumerable<ColorPoint>
    {
        private readonly List<ColorPoint> _colorPoints;
        private static readonly Color ZeroColor = Color.Black;

        public ColorMap()
        {
            _colorPoints = new List<ColorPoint>();
        }

        public ColorMap(Color zeroColor, Color oneColor)
        {
            _colorPoints = new List<ColorPoint>
            {
                new ColorPoint(0f, zeroColor),
                new ColorPoint(1f, oneColor)
            };
        }

        public void Add(float value, HsvColor color)
        {
            var cp = new ColorPoint(value, color);
            Add(cp);
        }

        public void Add(ColorPoint cp)
        {
            for (var i = 0; i < _colorPoints.Count; i++)
            {
                if (_colorPoints[i].Position < cp.Position)
                {
                    _colorPoints.Insert(i + 1, cp);
                    return;
                }
            }

            _colorPoints.Add(cp);
        }

        public HsvColor MapHsv(float value)
        {
            if (_colorPoints.Count == 0)
                return ZeroColor;

            int i;
            for (i = 0; i < _colorPoints.Count; i++)
            {
                if (_colorPoints[i].Position >= value)
                {
                    i--;
                    break;
                }
            }

            if (i < 0)
                return _colorPoints[0].Color;
            if (i >= _colorPoints.Count - 1)
                return _colorPoints[_colorPoints.Count - 1].Color;

            var c1 = _colorPoints[i];
            var c2 = _colorPoints[i + 1];
            var dc = c2.Position - c1.Position;
            var t = (value - c1.Position) / dc;
            return HsvColor.Lerp(c1.Color, c2.Color, t);
        }

        public Color Map(float value)
        {
            return (Color) MapHsv(value);
        }

        public IEnumerator<ColorPoint> GetEnumerator()
        {
            foreach (var cp in _colorPoints)
                yield return cp;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}