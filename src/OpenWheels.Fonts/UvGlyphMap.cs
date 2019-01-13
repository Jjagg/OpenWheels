using System;
using System.Text;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A mapping from UTF-32 encoded characters to their bounds in UV coordinates.
    /// </summary>
    public class UvGlyphMap : GlyphMap<UvGlyphData>
    {
        internal UvGlyphMap(Font font, CharacterRange[] characterRanges, UvGlyphData[] glyphData)
            : base(font, characterRanges, glyphData)
        {
        }

        /// <summary>
        /// Get the glyph data for the given character.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <returns>The glyph data for the given character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the character is not in this glyph map.</exception>
        public ref readonly UvGlyphData GetGlyphData(int character)
        {
            ref readonly var gd = ref GetGlyphData(character, UvGlyphData.Default);
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
        public bool TryGetGlyphData(int character, out UvGlyphData glyphData)
        {
            glyphData = GetGlyphData(character, UvGlyphData.Default);
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
    }
}