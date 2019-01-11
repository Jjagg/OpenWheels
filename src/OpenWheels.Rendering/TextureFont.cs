using System;
using OpenWheels.Fonts;
using SixLabors.Fonts;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// A font atlas to render glyphs from.
    /// </summary>
    public class TextureFont
    {
        private GlyphMap _glyphMap;
        private GlyphData _fallbackGlyphData;

        /// <summary>
        /// Get the texture id of the texture that contains the font atlas pixel data.
        /// </summary>
        public int Texture { get; }

        /// <summary>
        /// Get the font.
        /// </summary>
        public Font Font => _glyphMap.Font;

        /// <summary>
        /// Get the font info.
        /// </summary>
        public FontInfo FontInfo => new FontInfo(Font.Name, Font.Size);

        /// <summary>
        /// Get or set the character to render when a glyph is missing.
        /// Set to <c>0</c> to throw when a missing glyph is requested.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the fallback character passed does has no glyph in the glyph map.</exception>
        public int FallbackCharacter
        {
            get => _fallbackGlyphData.Character;
            set
            {
                if (value != 0)
                    _fallbackGlyphData = _glyphMap.GetGlyphData(value);
                else
                    _fallbackGlyphData = default;
            }
        }

        /// <summary>
        /// Get a value indicating if this TextureFont has a fallback character set.
        /// </summary>
        public bool HasFallback => _fallbackGlyphData.Character == 0;

        /// <summary>
        /// Create a new <see cref="TextureFont"/>.
        /// </summary>
        /// <param name="glyphMap">Font atlas for the fonts.</param>
        /// <param name="texture">Texture containing the font atlas.</param>
        /// <param name="fallbackCharacter">
        ///   Optional fallback character for the font.
        ///   Pass <c>0</c> to use no fallback; an exception will be thrown when a glyph is missing.
        ///   Defaults to U+FFFD, the replacement character (question mark in a diamond).
        /// </param>
        /// <exception cref="ArgumentNullException">If <paramref name="glyphMap" /> is <c>null</c>.</exception>
        public TextureFont(GlyphMap glyphMap, int texture, int fallbackCharacter = '\uFFFD')
        {
            if (glyphMap == null)
                throw new ArgumentNullException(nameof(glyphMap));

            _glyphMap = glyphMap;
            Texture = texture;
            FallbackCharacter = fallbackCharacter;
        }

        /// <summary>
        /// Get glyph data for the given character.
        /// If the glyph for the character is not found the fallback character glyph
        /// is returned if it is set.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   If the character glyph is not found and no fallback character is set.
        /// </exception>
        public ref readonly GlyphData GetGlyphData(int codePoint)
            => ref _fallbackGlyphData.Character == 0 ? 
                ref _glyphMap.GetGlyphData(codePoint) :
                ref _glyphMap.GetGlyphData(codePoint, _fallbackGlyphData);
    }
}