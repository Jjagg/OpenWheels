using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// A value clamped between a minimum and maximum.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    public class ClampedValue<T> where T : struct, IComparable<T>
    {
        private T _value;
        private T _min;
        private T _max;

        /// <summary>
        /// The value clamped between <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        public T Value
        {
            get => _value;
            set => _value = MathHelper.Clamp(value, Min, Max);
        }

        /// <summary>
        /// The minimal value.
        /// </summary>
        public T Min
        {
            get => _min;
            set
            {
                _min = value;
                if (_value.CompareTo(_min) < 0)
                    _value = _min;
            }
        }

        /// <summary>
        /// The maximal value.
        /// </summary>
        public T Max
        {
            get => _max;
            set
            {
                _max = value;
                if (_value.CompareTo(_max) > 0)
                    _value = _max;
            }
        }
    }
}