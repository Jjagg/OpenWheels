using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// Holds the information passed to the update, finish and cancel methods of a Timer.
    /// </summary>
    public class TimerData
    {
        /// <summary>
        /// Duration of the timer.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Elapsed time since the timer was started.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Delta time of the last update.
        /// </summary>
        public TimeSpan Delta { get; set; }

        /// <summary>
        /// Get or set if the timer is canceled. 
        /// A canceled timer will be removed and its cancel method will be called on the next update
        /// of the <see cref="ServiceRunner" /> that runs it.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Normalized value in the [0, 1] range that indicates how much time has elapsed relative to the total duration of the timer.
        /// </summary>
        public float Progress => MathHelper.Clamp((float) ((double) Elapsed.Ticks / Duration.Ticks), 0f, 1f);

        /// <summary>
        /// <c>True</c> if the timer's elapsed time is equal to or exceeds its duration, <c>false</c> otherwise.
        /// </summary>
        public bool Done => Elapsed >= Duration;

        /// <summary>
        /// Create a new timer context.
        /// </summary>
        public TimerData()
        {
        }
    }
}