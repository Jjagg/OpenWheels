using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A class with utility functions for handling text.
    /// </summary>
    public static class TextUtil
    {
        /// <summary>
        /// Starting character of the latin set.
        /// </summary>
        public static readonly int LatinStart = 32;

        /// <summary>
        /// End character of the latin set.
        /// </summary>
        public static readonly int LatinEnd = 126;

        /// <summary>
        /// Convert a string of UTF-16 encoded characters to UTF-32.
        /// </summary>
        /// <param name="text">UTF-16 encoded string.</param>
        /// <returns>UTF-32 encoded characters.</returns>
        public static IEnumerable<int> ToUtf32(string text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            for (var i = 0; i < text.Length; i++)
            {
                int c;
                if (char.IsHighSurrogate(text[i]))
                {
                    c = char.ConvertToUtf32(text, i);
                    i++;
                }
                else if (char.IsLowSurrogate(text[i]))
                    continue;
                else
                    c = text[i];

                yield return c;
            }
        }

        /// <summary>
        /// Group characters into a ranges of characters.
        /// Note that since this sorts the characters and does not care about duplicates,
        /// you can pass in arbitrary text to get ranges that contain all characters in it.
        /// </summary>
        /// <param name="characters">Characters to group in ranges.</param>
        /// <returns>Collection of character ranges that - when expanded - are equal to the set of given characters.</returns>
        public static IEnumerable<Range<int>> GroupInRanges(IEnumerable<int> characters)
        {
            characters = characters.OrderBy(c => c);
            var rangeStart = characters.First();
            var prevChar = rangeStart;
            foreach (var c in characters.Skip(1))
            {
                if (c - rangeStart > 1)
                {
                    yield return new Range<int>(rangeStart, prevChar);
                    rangeStart = c;
                }

                prevChar = c;
            }

            yield return new Range<int>(rangeStart, prevChar);
        }

        /// <summary>
        /// Get the characters in a string of text as ranges of UTF-32 characters.
        /// </summary>
        /// <param name="characters">String containing the characters to group in ranges.</param>
        /// <returns>Collection of character ranges including all characters in the given string.</returns>
        public static IEnumerable<Range<int>> GetCharacterRanges(string characters)
            => GroupInRanges(ToUtf32(characters));

        /// <summary>
        /// Create ranges by specifying the beginning and end of each in sequence.
        /// </summary>
        /// <param name="args">The beginning and end of the ranges.</param>
        /// <returns>
        ///   A range for every two parameters passed that begin with the first and end with the second parameter.
        /// </returns>
        /// <exception cref="ArgumentException">If an uneven amount of arguments is passed.</exception>
        public static IEnumerable<Range<int>> CreateRanges(params int[] args)
        {
            if (args.Length % 2 != 0)
                throw new ArgumentException("Expected beginning and end of ranges, but number of parameters is not a multiple of two!", nameof(args));
            for (var i = 0; i < args.Length; i += 2)
                yield return new Range<int>(args[i], args[i + 1]);
        }
    }
}