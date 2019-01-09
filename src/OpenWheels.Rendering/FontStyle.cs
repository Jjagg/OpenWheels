using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Style of a font.
    /// </summary>
    [Flags]
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1 << 0,
        Italic = 1 << 1,
        /// <summary>
        /// Both <see cref="Bold"/> and <see cref="Italic"/>
        /// </summary>
        BoldItalic = Bold | Italic
    }
}
