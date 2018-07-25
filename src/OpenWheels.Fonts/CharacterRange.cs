using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenWheels.Fonts
{
    internal struct CharacterRange : IEnumerable<int>
    {
        public Range<int> Range { get; }
        public int StartIndex { get; }

        public CharacterRange(int start, int end, int startIndex)
        {
            Range = new Range<int>(start, end);
            StartIndex = startIndex;
        }

        public CharacterRange(Range<int> range, int startIndex)
        {
            Range = range;
            StartIndex = startIndex;
        }

        public int GetIndex(int character) => StartIndex + (character - Range.Start);
        public int GetCharacter(int index) => index - StartIndex + Range.Start;

        public IEnumerator<int> GetEnumerator()
        {
            return Enumerable.Range(Range.Start, Range.End - Range.Start + 1).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}