using System;

namespace OpenWheels.Game
{
    public delegate float Ease(float t);

    public static class Easing
    {
        public static float Linear(float t) => t;

        public static float QuadraticIn(float t) => t * t;
        public static float QuadraticOut(float t) => t * (2 - t);
        public static float QuadraticInOut(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * QuadraticIn(t2) : .5f + .5f * QuadraticOut(t2 - 1);
        }

        public static float CubicIn(float t) => t * t * t;
        public static float CubicOut(float t)
        {
            var invT = (1 - t);
            return 1 - invT * invT * invT;
        }
        public static float CubicInOut(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * CubicIn(t2) : .5f + .5f * CubicOut(t2 - 1);
        }

        public static float QuarticIn(float t)
        {
            var t2 = t * t;
            return t2 * t2;
        }
        public static float QuarticOut(float t)
        {
            var invT = (1 - t);
            var invT2 = invT * invT;
            return 1 - invT2 * invT2;
        }
        public static float QuarticInOut(float t)
        {
            var t2 = 2 * t;
            return t < 1 ? .5f * QuarticIn(t2) : .5f + .5f * QuarticOut(t2 - 1);
        }
    }
}
