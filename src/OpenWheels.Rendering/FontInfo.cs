namespace OpenWheels.Rendering
{
    /// <summary>
    /// Identifies a font.
    /// </summary>
    public struct FontInfo
    {
        /// <summary>
        /// Name of the font.
        /// </summary>
        public string Name;

        /// <summary>
        /// Style of the font.
        /// </summary>
        public FontStyle Style;

        /// <summary>
        /// The character used for missing glyphs.
        /// </summary>
        public int? FallbackCharacter;

        /// <summary>
        /// Create font info with the given font name and optionally a fallback character.
        /// </summary>
        public FontInfo(string name, int? fallbackCharacter = null)
            : this(name, FontStyle.Regular, fallbackCharacter)
        {
        }

        /// <summary>
        /// Create font info with the given font name and style, and optionally a fallback character.
        /// </summary>
        public FontInfo(string name, FontStyle style, int? fallbackCharacter = null)
        {
            Name = name;
            Style = style;
            FallbackCharacter = fallbackCharacter;
        }
    }
}
