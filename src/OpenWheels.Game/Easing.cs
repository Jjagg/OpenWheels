using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// Signature for easing functions.
    /// </summary>
    public delegate float Ease(float t);

    /// <summary>
    /// Static class containing easing function implementations.
    /// Power ease in functions of power t are implemented as <c>f(t) = t^n</c>.
    /// Power ease out functions of power t are implemented as <c>f(t) = 1 - (1 - t)^n</c>.
    /// Power ease in-out functions use the respective ease in function for <c>t &lt; 0.5</c> and 
    /// ease out function for <c>t &gt; 0.5</c>.
    /// </summary>
    public static class Easing
    {
        /// <summary>
        /// Linear interpolation. This is simply the identity function.
        /// </summary>
        public static readonly Ease Linear = LinearImpl;

        /// <summary>
        /// Quadratic ease in.
        /// </summary>
        public static readonly Ease QuadraticIn = QuadraticInImpl;
        /// <summary>
        /// Quadratic ease out.
        /// </summary>
        public static readonly Ease QuadraticOut = QuadraticOutImpl;
        /// <summary>
        /// Quadratic ease in-out.
        /// </summary>
        public static readonly Ease QuadraticInOut = QuadraticInOutImpl;

        /// <summary>
        /// Cubic ease in.
        /// </summary>
        public static readonly Ease CubicIn = CubicInImpl;
        /// <summary>
        /// Cubic ease out.
        /// </summary>
        public static readonly Ease CubicOut = CubicOutImpl;
        /// <summary>
        /// Cubic ease in-out.
        /// </summary>
        public static readonly Ease CubicInOut = CubicInOutImpl;

        /// <summary>
        /// Quartic ease in.
        /// </summary>
        public static readonly Ease QuarticIn = QuarticInImpl;
        /// <summary>
        /// Quartic ease out.
        /// </summary>
        public static readonly Ease QuarticOut = QuarticOutImpl;
        /// <summary>
        /// Quartic ease in-out.
        /// </summary>
        public static readonly Ease QuarticInOut = QuarticInOutImpl;

        /// <summary>
        /// Ease in-out interpolation using a sine function.
        /// </summary>
        public static readonly Ease Sine = SineImpl;


        private static float LinearImpl(float t) => t;

        private static float QuadraticInImpl(float t) => t * t;
        private static float QuadraticOutImpl(float t) => t * (2 - t);
        private static float QuadraticInOutImpl(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * QuadraticInImpl(t2) : .5f + .5f * QuadraticOutImpl(t2 - 1);
        }

        private static float CubicInImpl(float t) => t * t * t;
        private static float CubicOutImpl(float t)
        {
            var invT = (1 - t);
            return 1 - invT * invT * invT;
        }
        private static float CubicInOutImpl(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * CubicInImpl(t2) : .5f + .5f * CubicOutImpl(t2 - 1);
        }

        private static float QuarticInImpl(float t)
        {
            var t2 = t * t;
            return t2 * t2;
        }
        private static float QuarticOutImpl(float t)
        {
            var invT = (1 - t);
            var invT2 = invT * invT;
            return 1 - invT2 * invT2;
        }
        private static float QuarticInOutImpl(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * QuarticInImpl(t2) : .5f + .5f * QuarticOutImpl(t2 - 1);
        }

        private static float SineImpl(float t)
        {
            return (float) (1 - Math.Cos(t * Math.PI)) *.5f;
        }
    }
}
