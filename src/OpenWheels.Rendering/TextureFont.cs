using System;
using OpenWheels.Fonts;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// A font rendered to a font atlas.
    /// </summary>
    public class TextureFont
    {
        /// <summary>
        /// Get the glyph map containing the locations of the glyphs on the atlas <see cref="Texture"/>.
        /// </summary>
        public GlyphMap GlyphMap { get; }

        /// <summary>
        /// Get the texture id of the texture that contains the font atlas.
        /// </summary>
        public int Texture { get; }

        /// <summary>
        /// Get or set the character to render when a glyph is missing.
        /// </summary>
        public int? FallbackCharacter
        {
            get => FallbackGlyphData.Character == 0 ? null : (int?) FallbackGlyphData.Character;
            set
            {
                if (value.HasValue)
                    FallbackGlyphData = GlyphMap.GetGlyphData(value.GetValueOrDefault());
                else
                    FallbackGlyphData = default;
            }
        }

        /// <summary>
        /// Get the fallback glyph data for the set <see cref="FallbackCharacter"/>.
        /// Undefined if the <see cref="FallbackCharacter"/> is <c>null</c>.
        /// </summary>
        public GlyphData FallbackGlyphData { get; private set; }

        /// <summary>
        /// Create a new <see cref="TextureFont"/>.
        /// </summary>
        /// <param name="glyphMap">Glyph map of the font.</param>
        /// <param name="texture">Texture containing the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>null</c> (no fallback; an exception will be thrown when a glyph is missing).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="glyphMap" /> is <c>null</c>.</exception>
        public TextureFont(GlyphMap glyphMap, int texture, int? fallbackCharacter = null)
        {
            if (glyphMap == null)
                throw new ArgumentNullException(nameof(glyphMap));

            GlyphMap = glyphMap;
            Texture = texture;
            FallbackCharacter = fallbackCharacter;
        }
    }
}