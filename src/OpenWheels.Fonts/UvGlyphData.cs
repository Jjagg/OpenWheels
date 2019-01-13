namespace OpenWheels.Fonts
{
    /// <summary>
    /// Data for a glyph with bounds in UV coordinates.
    /// </summary>
    public struct UvGlyphData
    {
        /// <summary>
        /// UTF-32 encoded character of the glyph.
        /// </summary>
        public int Character { get; }

        /// <summary>
        /// Bounds of the glyph on the texture atlas in UV coordinates.
        /// </summary>
        public RectangleF Bounds { get; }

        /// <summary>
        /// Create a new <see cref="GlyphData"/> instance.
        /// </summary>
        /// <param name="character">UTF-32 encoded character of the glyph</param>
        /// <param name="bounds">Bounds of the glyph on the texture atlas in UV coordinates.</param>
        public UvGlyphData(int character, RectangleF bounds)
        {
            Character = character;
            Bounds = bounds;
        }

        internal static UvGlyphData Default = new UvGlyphData();
    }

}