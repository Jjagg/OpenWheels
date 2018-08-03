using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels
{
    /// <summary>
    /// A immutable struct representing a quadratic Bézier curve.
    /// </summary>
    public readonly struct QuadraticBezier
    {
        /// <summary>
        /// The starting point of the curve.
        /// </summary>
        public Vector2 A {get;}

        /// <summary>
        /// The middle control point of the curve.
        /// </summary>
        public Vector2 B {get;}

        /// <summary>
        /// The end point of the curve.
        /// </summary>
        public Vector2 C {get;}

        /// <summary>
        /// Create a quadratic Bézier curve with the given control points.
        /// </summary>
        /// <param name="a">The starting point of the curve.</param>
        /// <param name="b">The middle control point of the curve.</param>
        /// <param name="c">The end point of the curve.</param>
        public QuadraticBezier(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>
        /// Evaluate the curve at the given <paramref name="t" />.
        /// </summary>
        public Vector2 Evaluate(float t)
        {
            if (t < 0 || t > 1)
                throw new ArgumentOutOfRangeException(nameof(t), t, "Parameter t must be between 0 and 1 (inclusive).");

            var invT = 1 - t;
            return invT * invT * A + 2 * invT * t * B + t * t * C;
        }

        /// <summary>
        /// Evaluate the derivative of the curve at the given <paramref name="t" />.
        /// </summary>
        public Vector2 EvaluateDeriv(float t)
        {
            if (t < 0 || t > 1)
                throw new ArgumentOutOfRangeException(nameof(t), t, "Parameter t must be between 0 and 1 (inclusive).");

            var invT = 1 - t;
            return 2 * invT * (B - A) + 2 * t * (C - B);
        }

        /// <summary>
        /// Get an upper bound for the lenght of this curve by summing the lengths of the lines between subsequent control points.
        /// </summary>
        public float MaxLength()
        {
            return (B - A).Length() + (C - B).Length();
        }

        /// <summary>
        /// Divide the curve into straight segments and return the resulting points.
        /// The first and last point are always <see cref="A"/> and <see cref="C"/> respectively.
        /// </summary>
        public IEnumerable<Vector2> Segmentize(int segments)
        {
            if (segments <= 0)
                throw new ArgumentOutOfRangeException(nameof(segments), segments, "Number of segments must be larger than 0.");

            yield return A;

            var step = 1f / segments;
            for (var t = step; t < 1f; t += step)
                yield return Evaluate(t);

            yield return C;
        }
    }
}