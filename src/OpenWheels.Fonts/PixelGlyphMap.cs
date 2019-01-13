using System;
using System.Text;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A mapping from UTF-32 encoded characters to their bounds in pixels.
    /// </summary>
    public class PixelGlyphMap : GlyphMap<GlyphData>
    {
        internal PixelGlyphMap(Font font, CharacterRange[] characterRanges, GlyphData[] glyphData)
            : base(font, characterRanges, glyphData)
        {
        }

        /// <summary>
        /// Get the glyph data for the given character.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <returns>The glyph data for the given character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the character is not in this glyph map.</exception>
        public ref readonly GlyphData GetGlyphData(int character)
        {
            ref readonly var gd = ref GetGlyphData(character, Fonts.GlyphData.Default);
            if (gd.Character != character)
                throw new ArgumentOutOfRangeException(nameof(character), $"Character '{character} ({char.ConvertFromUtf32(character)})' not found in glyph map.");

            return ref gd;
        }

        /// <summary>
        /// Get the glyph data for the given character.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <param name="glyphData">The glyph data for the given character.</param>
        /// <returns><c>true</c> if the glyph for the given character was found, <c>false</c> if it wasn't.</returns>
        public bool TryGetGlyphData(int character, out GlyphData glyphData)
        {
            glyphData = GetGlyphData(character, Fonts.GlyphData.Default);
            return glyphData.Character == character;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var g in GlyphData)
            {
                sb.Append(char.ConvertFromUtf32(g.Character));
                sb.Append(':');
                sb.Append(' ');
                sb.Append(g.Bounds);
                sb.Append(',');
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert this glyph map so the glyph coordinates are stored as UV coordinates instead of pixels.
        /// </summary>
        /// <param name="texWidth">Width of the texture to divide coordinates in pixels by to get UV coordinates.</param>
        /// <param name="texHeight">Height of the texture to divide coordinates in pixels by to get UV coordinates.</param>
        public UvGlyphMap ToUvGlyphMap(int texWidth, int texHeight)
        {
            var uvGlyphData = new UvGlyphData[GlyphData.Length];
            for (var i = 0; i < uvGlyphData.Length; i++)
            {
                var gd = GlyphData[i];
                var rect = gd.Bounds;
                var uvRect = new RectangleF(
                    (float) rect.X / texWidth, (float) rect.Y / texHeight,
                    (float) rect.Width / texWidth, (float) rect.Height / texHeight);
                uvGlyphData[i] = new UvGlyphData(gd.Character, uvRect);
            }

            return new UvGlyphMap(Font, CharacterRanges, uvGlyphData);
        }
    }
}