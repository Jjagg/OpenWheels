using System.Collections;
using System.Collections.Generic;

namespace OpenWheels
{
    /// <summary>
    /// A color gradient with multiple color points and linear interpolation in HSV colorspace between points.
    /// </summary>
    public class ColorGradient : IEnumerable<ColorPoint>
    {
        private readonly List<ColorPoint> _colorPoints;
        private static readonly Color ZeroColor = Color.Black;

        /// <summary>
        /// Create a new ColorGradient.
        /// </summary>
        public ColorGradient()
        {
            _colorPoints = new List<ColorPoint>();
        }

        /// <summary>
        /// Create a new ColorGradient.
        /// </summary>
        /// <param name="zeroColor">The color at t = 0.</param>
        /// <param name="oneColor">The color at t = 1.</param>
        public ColorGradient(Color zeroColor, Color oneColor)
        {
            _colorPoints = new List<ColorPoint>
            {
                new ColorPoint(0f, zeroColor),
                new ColorPoint(1f, oneColor)
            };
        }

        /// <summary>
        /// Add a color point at t = <paramref name="value" /> with the given color.
        /// </summary>
        /// <param name="value">Value of the stop point.</param>
        /// <param name="value">Color of the stop point.</param>
        public void Add(float value, HsvColor color)
        {
            var cp = new ColorPoint(value, color);
            Add(cp);
        }

        /// <summary>
        /// Add a color point.
        /// </summary>
        /// <param name="cp">The color point.</param>
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

        /// <summary>
        /// Get the HSV color at the given value.
        /// </summary>
        /// <param name="value">Value to evaluate the gradient at.</param>
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

        /// <summary>
        /// Get the RGB color at the given value.
        /// </summary>
        /// <param name="value">Value to evaluate the gradient at.</param>
        public Color Map(float value)
        {
            return (Color) MapHsv(value);
        }

        /// </inheritdoc>
        public IEnumerator<ColorPoint> GetEnumerator()
        {
            foreach (var cp in _colorPoints)
                yield return cp;
        }

        /// </inheritdoc>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}