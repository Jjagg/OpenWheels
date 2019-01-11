using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Flags for text layout or rendering.
    /// </summary>
    [Flags]
    public enum TextLayoutFlags
    {
        None = 0,
        /// <summary>
        /// Indicates if kerning should be applied.
        /// TODO explain kerning
        /// </summary>
        Kerning = 1 << 0,
        // - ligatures
        // - combining marks
        // - composition
        // - RTL
        // - FullColor
    }
}
