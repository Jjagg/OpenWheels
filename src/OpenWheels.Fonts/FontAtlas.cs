using System.Linq;
using System.Numerics;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A font atlas containing <see cref="GlyphMap"/> instances for different fonts. <see cref="FontData"/>
    /// </summary>
    public class FontAtlas
    {
        private readonly GlyphMap[] _glyphMaps;

        /// <summary>
        /// Width of the atlas.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the atlas.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Dots per inch.
        /// </summary>
        public Vector2 Dpi { get; }

        /// <summary>
        /// The number of <see cref="GlyphMap"/> instances on this font atlas.
        /// </summary>
        public int MapCount => _glyphMaps.Length;

        /// <summary>
        /// Create a font atlas.
        /// </summary>
        /// <param name="width">Width of the font atlas.</param>
        /// <param name="height">Height of the font atlas.</param>
        /// <param name="dpi">Dots per inch.</param>
        /// <param name="glyphMaps">The glyph maps on the font atlas.</param>
        public FontAtlas(int width, int height, Vector2 dpi, GlyphMap[] glyphMaps)
        {
            Width = width;
            Height = height;
            Dpi = dpi;
            _glyphMaps = glyphMaps;
        }

        /// <summary>
        /// Get a glyph map by index.
        /// </summary>
        /// <param name="index">Index of the glyph map.</param>
        public GlyphMap this[int index] => _glyphMaps[index];

        /// <summary>
        /// Get a glyph map by <see cref="FontData"/>.
        /// </summary>
        /// <param name="fontData">Font data to get the glyph map for.</param>
        public GlyphMap this[FontData fontData] => _glyphMaps.First(g => FontMatchesFontData(g.Font, fontData));

        /// <summary>
        /// Find a <see cref="GlyphMap"/> exactly matching the given <paramref name="fontData"/>.
        /// </summary>
        /// <param name="fontData">The font data to get the glyph map for.</param>
        /// <param name="glyphMap">The retrieved glyph map if the font data is found, <c>null</c> if it isn't.</param>
        /// <returns><c>true</c> if the font data is found, <c>false</c> if it isn't.</returns>
        public bool TryGetGlyphMap(in FontData fontData, out GlyphMap glyphMap)
        {
            glyphMap = null;
            for (var i = 0; i < _glyphMaps.Length; i++)
            {
                var gm = _glyphMaps[i];
                if (FontMatchesFontData(gm.Font, fontData))
                {
                    glyphMap = gm;
                    break;
                }
            }

            return glyphMap != null;
        }

        private bool FontMatchesFontData(Font font, in FontData fd)
        {
            var style = (font.Bold ? FontStyle.Bold : 0) | (font.Italic ? FontStyle.Italic: 0);
            return font.Family.Name == fd.FamilyName && font.Size == fd.Size && style == fd.Style;
        }
    }
}