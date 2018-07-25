using System.Collections.Generic;
using System.Linq;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A font and list of characters to be rendered to a font atlas.
    /// </summary>
    internal class FontAtlasEntry
    {
        /// <summary>
        /// Data of the font to use.
        /// </summary>
        public FontData Font { get; }

        /// <summary>
        /// Characters to include in this entry.
        /// </summary>
        public List<Range<int>> CharacterRanges { get; }

        /// <summary>
        /// Indicates if the font is a system font.
        /// </summary>
        public bool IsSystemFont { get; }

        /// <summary>
        /// Index of this entry in the list of entries.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Create a new <see cref="FontAtlasEntry"/>.
        /// </summary>
        /// <param name="font">Data of the font to use.</param>
        /// <param name="characterRanges">Characters to include in this entry.</param>
        /// <param name="isSystemFont">Indicates if the font is a system font.</param>
        /// <param name="index">Index of this entry in the list of entries.</param>
        public FontAtlasEntry(FontData font, IEnumerable<Range<int>> characterRanges, bool isSystemFont, int index)
        {
            Font = font;
            CharacterRanges = characterRanges.ToList();
            IsSystemFont = isSystemFont;
            Index = index;
        }

        /// <summary>
        /// Add the given character ranges to this <see cref="FontAtlasEntry"/>.
        /// Ranges are merged when they overlap.
        /// </summary>
        /// <param name="ranges">The character ranges to add.</param>
        public void AddCharacterRanges(IEnumerable<Range<int>> ranges)
        {
            CharacterRanges.AddRange(ranges);
            FixCharacterRanges();
        }

        private void FixCharacterRanges()
        {
            if (CharacterRanges.Count == 0)
                return;

            CharacterRanges.Sort((rl, rr) => rl.Start - rr.Start);
            var prevRange = CharacterRanges[CharacterRanges.Count - 1];
            for (var i = CharacterRanges.Count - 2; i >= 0; i--)
            {
                var range = CharacterRanges[i];
                if (prevRange.Start <= range.End - 1)
                {
                    prevRange = new Range<int>(prevRange.Start, range.End > prevRange.End ? range.End : prevRange.End);
                    CharacterRanges.RemoveAt(i + 1);
                }
                else
                {
                    CharacterRanges[i] = prevRange;
                    prevRange = range;
                }
            }

            CharacterRanges[0] = prevRange;
        }

        /// <summary>
        /// Total number of characters.
        /// </summary>
        public int GetCharacterCount() => CharacterRanges.Sum(r => r.End - r.Start + 1);

        /// <summary>
        /// Enumerate all characters in the <see cref="CharacterRanges"/> of this <see cref="FontAtlasEntry"/>.
        /// </summary>
        /// <returns>All characters in the <see cref="CharacterRanges"/>.</returns>
        public IEnumerable<int> GetAllCharacters()
        {
            foreach (var r in CharacterRanges)
            {
                for (var i = r.Start; i <= r.End; i++)
                    yield return i;
            }
        }
    }
}