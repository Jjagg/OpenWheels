using System.Collections;
using System.Collections.Generic;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A mapping from UTF-32 encoded characters to their bounds.
    /// </summary>
    public abstract class GlyphMap<T> : IEnumerable<T>
    {
        /// <summary>
        /// Get the <see cref="Font"/> of this <see cref="GlyphMap"/>.
        /// </summary>
        public Font Font { get; }

        internal readonly CharacterRange[] CharacterRanges;
        protected readonly T[] GlyphData;

        internal GlyphMap(Font font, CharacterRange[] characterRanges, T[] glyphData)
        {
            Font = font;
            CharacterRanges = characterRanges;
            GlyphData = glyphData;
        }

        private int FindCharacterIndex(int character)
        {
            var rangeIndex = -1;
            var l = 0;
            var r = CharacterRanges.Length - 1;
            while (l <= r)
            {
                var m = (l + r) >> 1;
                if (CharacterRanges[m].Range.End < character)
                {
                    l = m + 1;
                }
                else if (CharacterRanges[m].Range.Start > character)
                {
                    r = m - 1;
                }
                else
                {
                    rangeIndex = m;
                    break;
                }
            }

            return rangeIndex;
        }

        /// <summary>
        /// Get the glyph data for the given character. Returns the fallback character
        /// if the given character is not present in this glyph map.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <param name="fallback">The fallback <see cref="Fonts.GlyphData"/> to use if the glyph is not found in the map.</param>
        /// <returns>
        ///   The glyph data for the given character or the given fallback glyph data
        ///   if the given character is not present in this glyph map.
        /// </returns>
        public ref readonly T GetGlyphData(int character, in T fallback)
        {
            var rangeIndex = FindCharacterIndex(character);
            if (rangeIndex == -1)
                return ref fallback;

            var index = CharacterRanges[rangeIndex].GetIndex(character);
            return ref GlyphData[index];
        }

        /// <summary>
        /// Get a dictionary that maps characters to their glyph data.
        /// </summary>
        /// <returns>A dictionary mapping UTF-32 encoded characters to their glyph data.</returns>
        public Dictionary<int, T> ToDictionary()
        {
            var dict = new Dictionary<int, T>();

            foreach (var cr in CharacterRanges)
            {
                var length = cr.Range.End - cr.Range.Start;
                for (var i = 0; i < length; i++)
                {
                    var index = cr.StartIndex + i;
                    dict.Add(cr.Range.Start + i, GlyphData[index]);
                }
            }

            return dict;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>) GlyphData).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}