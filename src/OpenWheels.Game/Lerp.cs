using System.Linq.Expressions;

namespace OpenWheels.Game
{
    /// <summary>
    /// Delegate for linearly interpolating a value.
    /// </summary>
    public delegate T Lerp<T>(T from, T to, float t);

    /// <summary>
    /// Utility class that dynamically generates code for linear interpolation of types.
    /// Types need to implement the following three operators:
    /// - T Addition(T, T)
    /// - T Subtraction(T, T)
    /// - T Multiply(float, T)
    ///
    /// The code is generated and compiled to IL in the static initializer.
    /// If not all three operators are implemented compilation will fail.
    /// The compiled function is stored in <see cref="Lerp"/>.
    /// </summary>
    /// <typeparam name="T">Type to generate linear interpolation function for.</typeparam>
    public static class LerpGen<T>
    {
        /// <summary>
        /// The dynamically generated and compiled linear interpolation function for type <typeparamref name="T" />.
        /// </summary>
        public static Lerp<T> Lerp;

        static LerpGen()
        {
            var from = Expression.Parameter(typeof(T));
            var to = Expression.Parameter(typeof(T));
            var t = Expression.Parameter(typeof(float));

            var diff = Expression.Subtract(to, from);
            var mult = Expression.Multiply(t, diff);
            var add = Expression.Add(from, mult);
            Lerp = Expression.Lambda<Lerp<T>>(add, from, to, t).Compile();
        }
    }
}