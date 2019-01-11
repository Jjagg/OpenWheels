namespace OpenWheels.Rendering
{
    /// <summary>
    /// Contains name and size of a font.
    /// </summary>
    public struct FontInfo
    {
        /// <summary>
        /// Name of the font.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Point size of the font.
        /// </summary>
        public readonly float Size;

        /// <summary>
        /// Create a new FontInfo instance.
        /// </summary>
        public FontInfo(string name, float size)
        {
            Name = name;
            Size = size;
        }
    }
}
