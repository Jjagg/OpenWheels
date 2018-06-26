using System;
using System.Collections.Generic;

namespace OpenWheels
{
    /// <summary>
    /// A range or interval with a start and end value.
    /// </summary>
    /// <typeparam name="T">Type of the start and end values.</typeparam>
    public struct Range<T> where T : IComparable<T>
    {
        /// <summary>
        /// Start of the range.
        /// </summary>
        public T Start { get; }

        /// <summary>
        /// End of the range.
        /// </summary>
        public T End { get; }

        /// <summary>
        /// Create a new range with the given start and end values.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="end">End of the range.</param>
        /// <exception cref="ArgumentException">
        ///   If <paramref name="end"/> is less than <paramref name="start"/>.
        /// </exception>
        public Range(T start, T end)
        {
            if (start.CompareTo(end) > 0)
                throw new ArgumentException("End must be larger than or equal to start.");
            Start = start;
            End = end;
        }

        /// <summary>
        /// Check if the range contains the given value.
        /// A range contains a value if it is larger than or equal to <see cref="Start"/>
        /// and smaller than or equal to <see cref="End"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is in this range, <c>false</c> otherwise.</returns>
        public bool Contains(T value)
        {
            return Start.CompareTo(value) <= 0 && End.CompareTo(value) >= 0;
        }

        /// <summary>
        /// Check if two ranges overlap.
        /// </summary>
        /// <param name="left">The first range to check.</param>
        /// <param name="right">The second range to check.</param>
        /// <returns><c>true</c> if the ranges overlap, <c>false</c> if they don't.</returns>
        public static bool Overlaps(Range<T> left, Range<T> right)
        {
            return left.Start.CompareTo(right.End) <= 0 && left.End.CompareTo(right.Start) >= 0;
        }

        /// <summary>
        /// Merge two overlapping ranges.
        /// This is equivalent to <see cref="Wrap"/> except this checks if the ranges overlap first.
        /// </summary>
        /// <param name="left">The first range.</param>
        /// <param name="right">The second range.</param>
        /// <returns>A range with the minimum of the start and maximum of the end of both ranges.</returns>
        /// <exception cref="ArgumentException">If the ranges don't overlap.</exception>
        public static Range<T> Merge(Range<T> left, Range<T> right)
        {
            if (!Overlaps(left, right))
                throw new ArgumentException("The two given ranges don't overlap.");
            return Wrap(left, right);
        }

        /// <summary>
        /// Get the range wrapping the two given ranges.
        /// I.e. the range from the minimum of the start to the maximum of the end of the given ranges.
        /// </summary>
        /// <param name="left">The first range.</param>
        /// <param name="right">The second range.</param>
        /// <returns>The range wrapping the given ranges.</returns>
        public static Range<T> Wrap(Range<T> left, Range<T> right)
        {
            var start = left.Start.CompareTo(right.Start) < 0 ? left.Start : right.Start;
            var end = left.End.CompareTo(right.End) < 0 ? right.End : left.End;
            return new Range<T>(start, end);
        }

        /// <summary>
        /// <see cref="IComparer{T}"/> implementation for ranges that compares <see cref="Start"/>.
        /// </summary>
        public static Lazy<IComparer<Range<T>>> StartComparer =
            new Lazy<IComparer<Range<T>>>(() => new StartComparerImpl());

        /// <summary>
        /// <see cref="IComparer{T}"/> implementation for ranges that compares <see cref="End"/>.
        /// </summary>
        public static Lazy<IComparer<Range<T>>> EndComparer =
            new Lazy<IComparer<Range<T>>>(() => new EndComparerImpl());

        private class StartComparerImpl : IComparer<Range<T>>
        {
            public int Compare(Range<T> left, Range<T> right)
            {
                return left.Start.CompareTo(right.Start);
            }
        }

        private class EndComparerImpl : IComparer<Range<T>>
        {
            public int Compare(Range<T> left, Range<T> right)
            {
                return left.End.CompareTo(right.End);
            }
        }
    }
}