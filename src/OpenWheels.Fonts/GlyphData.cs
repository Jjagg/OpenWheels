namespace OpenWheels.Fonts
{
    /// <summary>
    /// Data for a glyph.
    /// </summary>
    public struct GlyphData
    {
        /// <summary>
        /// UTF-32 encoded character of the glyph.
        /// </summary>
        public int Character { get; }
        // TODO support glyphs more than 1 codepoint

        /// <summary>
        /// Bounds of the glyph on the texture atlas in pixels.
        /// </summary>
        public Rectangle Bounds { get; }

        /// <summary>
        /// Create a new <see cref="GlyphData"/> instance.
        /// </summary>
        /// <param name="character">UTF-32 encoded character of the glyph</param>
        /// <param name="bounds">Bounds of the glyph on the texture atlas in pixels.</param>
        public GlyphData(int character, Rectangle bounds)
        {
            Character = character;
            Bounds = bounds;
        }

        internal static GlyphData Default = new GlyphData();
    }
}