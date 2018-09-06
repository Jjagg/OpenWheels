using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// Interface for classes that have a time-dependent Update method.
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// Update this item.
        /// </summary>
        /// <param name="delta">Time since the last update call.</param>
        void Update(TimeSpan delta);
    }
}