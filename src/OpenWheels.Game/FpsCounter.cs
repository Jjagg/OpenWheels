using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// Compute frames per second and average seconds per frame over the last n frames.
    /// </summary>
    public class FpsCounter : IUpdatable
    {
        private TimeSpan[] _frameTimes;
        private int _index;
        private TimeSpan _timeSinceUpdate;

        /// <summary>
        /// Get or set the number of frames to computer fps over.
        /// </summary>
        public int BufferSize
        {
            get => _frameTimes.Length;
            set => Array.Resize(ref _frameTimes, value);
        }

        /// <summary>
        /// Average time a frame took over the last '<see cref="BufferSize" />' frames.
        /// </summary>
        public TimeSpan AverageFrameTime { get; private set; }

        /// <summary>
        /// Time between fps updates.
        /// </summary>
        public TimeSpan UpdateTime { get; private set; }

        /// <summary>
        /// Frames per second over the last '<see cref="BufferSize" />' frames.
        /// </summary>
        public float Fps => (float) (1 / AverageFrameTime.TotalSeconds);

        /// <summary>
        /// Create a new fps counter that updates fps every frame.
        /// </summary>
        /// <param name="bufferSize">Number of frames to count fps over. Defaults to 60.</param>
        public FpsCounter(int bufferSize = 60)
            : this(TimeSpan.Zero, bufferSize)
        {
        }

        /// <summary>
        /// Create a new fps counter.
        /// </summary>
        /// <param name="updateTime">Time between fps updates.</param>
        /// <param name="bufferSize">Number of frames to count fps over. Defaults to 60.</param>
        public FpsCounter(TimeSpan updateTime, int bufferSize = 60)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be larger than 0.");

            _frameTimes = new TimeSpan[bufferSize];
            UpdateTime = updateTime;
        }

        /// <summary>
        /// Update the fps counter.
        /// </summary>
        /// <param name="delta">Time passed since last frame.</param>
        public void Update(TimeSpan delta)
        {
            _frameTimes[_index] = delta;
            _index = (_index + 1) % _frameTimes.Length;

            _timeSinceUpdate += delta;
            if (_timeSinceUpdate >= UpdateTime)
            {
                _timeSinceUpdate = TimeSpan.Zero;

                var totalTime = TimeSpan.Zero;

                for (var i = 0; i < _frameTimes.Length; i++)
                    totalTime += _frameTimes[i];

                AverageFrameTime = new TimeSpan((long) (totalTime.Ticks / (double) BufferSize));
                FpsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked after frames per second is recalculated.
        /// </summary>
        public EventHandler<EventArgs> FpsUpdated;
    }
}