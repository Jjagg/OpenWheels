using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A mapping from UTF-32 encoded characters to their bounds
    /// </summary>
    public class GlyphMap : IEnumerable<GlyphData>
    {
        /// <summary>
        /// Get the <see cref="Font"/> of this <see cref="GlyphMap"/>.
        /// </summary>
        public Font Font { get; }

        private readonly CharacterRange[] _characterRanges;
        private readonly GlyphData[] _glyphData;

        internal GlyphMap(Font font, CharacterRange[] characterRanges, GlyphData[] glyphData)
        {
            Font = font;
            _characterRanges = characterRanges;
            _glyphData = glyphData;
        }

        private int FindCharacterIndex(int character)
        {
            var rangeIndex = -1;
            var l = 0;
            var r = _characterRanges.Length - 1;
            while (l <= r)
            {
                var m = (l + r) >> 1;
                if (_characterRanges[m].Range.End < character)
                {
                    l = m + 1;
                }
                else if (_characterRanges[m].Range.Start > character)
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
        /// Get the glyph data for the given character.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <returns>The glyph data for the given character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the character is not in this glyph map.</exception>
        public ref readonly GlyphData GetGlyphData(int character)
        {
            ref readonly var gd = ref GetGlyphData(character, GlyphData.Default);
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
            glyphData = GetGlyphData(character, GlyphData.Default);
            return glyphData.Character == character;
        }

        /// <summary>
        /// Get the glyph data for the given character. Returns the fallback character
        /// if the given character is not present in this glyph map.
        /// </summary>
        /// <param name="character">UTF-32 encoded character to get glyph data for.</param>
        /// <param name="fallback">The fallback <see cref="GlyphData"/> to use if the glyph is not found in the map.</param>
        /// <returns>
        ///   The glyph data for the given character or the given fallback glyph data
        ///   if the given character is not present in this glyph map.
        /// </returns>
        public ref readonly GlyphData GetGlyphData(int character, in GlyphData fallback)
        {
            var rangeIndex = FindCharacterIndex(character);
            if (rangeIndex == -1)
                return ref fallback;

            var index = _characterRanges[rangeIndex].GetIndex(character);
            return ref _glyphData[index];
        }

        /// <summary>
        /// Get a dictionary that maps characters to their glyph data.
        /// </summary>
        /// <returns>A dictionary mapping UTF-32 encoded characters to their glyph data.</returns>
        public Dictionary<int, GlyphData> ToDictionary()
        {
            var dict = new Dictionary<int, GlyphData>();

            foreach (var cr in _characterRanges)
            {
                var length = cr.Range.End - cr.Range.Start;
                for (var i = 0; i < length; i++)
                {
                    var index = cr.StartIndex + i;
                    dict.Add(cr.Range.Start + i, _glyphData[index]);
                }
            }

            return dict;
        }

        /// <inheritdoc />
        public IEnumerator<GlyphData> GetEnumerator()
        {
            return ((IEnumerable<GlyphData>) _glyphData).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var g in _glyphData)
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