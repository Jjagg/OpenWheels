using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using OpenWheels.BinPack;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// A class that can build a font atlas for different fonts.
    /// Register fonts using the <see cref="AddFont"/> methods, then
    /// build the atlas using <see cref="CreateAtlas"/>.
    ///
    /// A font atlas builder can be reused after calling <see cref="Clear"/> to unregister all fonts.
    /// </summary>
    public class FontAtlasBuilder
    {
        /// <summary>
        /// The default dpi used when <c>null</c> is passed to <see cref="CreateAtlas"/>.
        /// </summary>
        public static readonly float DefaultDpi = 72;

        private FontCollection _fontCollection;
        private readonly List<FontAtlasEntry> _atlasEntries;

        /// <summary>
        /// Create a new font atlas builder.
        /// </summary>
        public FontAtlasBuilder()
        {
            _fontCollection = new FontCollection();
            _atlasEntries = new List<FontAtlasEntry>();
        }

        /// <summary>
        /// Remove all registered fonts.
        /// </summary>
        public void Clear()
        {
            _fontCollection = new FontCollection();
            _atlasEntries.Clear();
        }

#if !NETSTANDARD1_1

        /// <summary>
        /// Add a system font with the given name.
        /// </summary>
        /// <param name="fontFamilyName">Name of the family of the font to add.</param>
        /// <param name="size">Size of the font to add.</param>
        /// <param name="characterRanges">Ranges of characters to add the glyphs for.</param>
        /// <param name="style">Style of the font.</param>
        /// <returns>
        ///   FontData for the added font. This can later be used to index into the <see cref="FontAtlas"/>
        ///   using the indexer or <see cref="FontAtlas.TryGetGlyphMap"/>.
        /// </returns>
        /// <seealso cref="TextUtil.ToUtf32"/>
        /// <seealso cref="TextUtil.GroupInRanges"/>
        /// <exception cref="ArgumentException">If the font is not found.</exception>
        /// <exception cref="ArgumentNullException">
        ///   If <paramref name="fontFamilyName"/> or <paramref name="characterRanges"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is not strictly positive.</exception>
        public FontData AddSystemFont(string fontFamilyName, float size, IEnumerable<Range<int>> characterRanges,
            FontStyle style = FontStyle.Regular)
        {
            if (!SystemFonts.TryFind(fontFamilyName, out _))
                throw new ArgumentException("System font not found.", nameof(fontFamilyName));

            return AddInternal(fontFamilyName, size, characterRanges, style, true);
        }

        /// <summary>
        /// Add a font from a path.
        /// </summary>
        /// <param name="fontPath">Path of the font.</param>
        /// <param name="size">Size of the font to add.</param>
        /// <param name="characterRanges">Ranges of characters to add the glyphs for.</param>
        /// <param name="style">Style of the font.</param>
        /// <returns>
        ///   FontData for the added font. This can later be used to index into the <see cref="FontAtlas"/>
        ///   using the indexer or <see cref="FontAtlas.TryGetGlyphMap"/>.
        /// </returns>
        /// <seealso cref="TextUtil.ToUtf32"/>
        /// <seealso cref="TextUtil.GroupInRanges"/>
        /// <exception cref="ArgumentNullException">
        ///   If <paramref name="characterRanges"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is not strictly positive.</exception>
        public FontData AddFont(string fontPath, float size, IEnumerable<Range<int>> characterRanges,
            FontStyle style = FontStyle.Regular)
        {
            var ff = _fontCollection.Install(fontPath);
            return AddInternal(ff.Name, size, characterRanges, style, false);
        }

#endif

        /// <summary>
        /// Add a font from a stream.
        /// </summary>
        /// <param name="fontStream">Stream of the font.</param>
        /// <param name="size">Size of the font to add.</param>
        /// <param name="characterRanges">Ranges of characters to add the glyphs for.</param>
        /// <param name="style">Style of the font.</param>
        /// <returns>
        ///   FontData for the added font. This can later be used to index into the <see cref="FontAtlas"/>
        ///   using the indexer or <see cref="FontAtlas.TryGetGlyphMap"/>.
        /// </returns>
        /// <seealso cref="TextUtil.ToUtf32"/>
        /// <seealso cref="TextUtil.GroupInRanges"/>
        /// <exception cref="ArgumentNullException">If <paramref name="characterRanges"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> is not strictly positive.</exception>
        public FontData AddFont(Stream fontStream, float size, IEnumerable<Range<int>> characterRanges,
            FontStyle style = FontStyle.Regular)
        {
            var ff = _fontCollection.Install(fontStream);
            return AddInternal(ff.Name, size, characterRanges, style, false);
        }

        private FontData AddInternal(string familyName, float size, IEnumerable<Range<int>> characterRanges,
            FontStyle style, bool system)
        {
            if (characterRanges == null)
                throw new ArgumentNullException(nameof(characterRanges));
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be larger than 0.");

            var key = new FontData(familyName, size, style);
            var existingEntry = _atlasEntries.FirstOrDefault(e => e.Font == key);
            if (existingEntry != null)
            {
                existingEntry.AddCharacterRanges(characterRanges);
            }
            else
            {
                var entry = new FontAtlasEntry(key, characterRanges, system, _atlasEntries.Count);
                _atlasEntries.Add(entry);
            }

            return key;
        }

        /// <summary>
        /// Create a font atlas with all registered <see cref="FontAtlasEntry"/> instances.
        /// </summary>
        /// <param name="dpi">Dots per inch to use. Usually 96 (Windows) or 72 (MacOS).</param>
        public FontAtlas CreateAtlas(Vector2? dpi = null)
        {
            var dpiVal = dpi ?? new Vector2(DefaultDpi);
            var characterSizes = new Size[_atlasEntries.Count][];
            var characterRanges = new CharacterRange[_atlasEntries.Count][];
            var characterBounds = new Rectangle[_atlasEntries.Count][];
            var fonts = new Font[_atlasEntries.Count];

            var packer = new MaxRectsBin(64, 64);
            // pad 1 pixel at all borders
            packer.PaddingWidth = 1;
            packer.PaddingHeight = 1;

            var sb = new StringBuilder();
            for (var i = 0; i < _atlasEntries.Count; i++)
            {
                var e = _atlasEntries[i];

                characterSizes[i] = new Size[e.GetCharacterCount()];
                characterRanges[i] = new CharacterRange[e.CharacterRanges.Count];

                Font font;
                if (e.IsSystemFont)
#if !NETSTANDARD1_1
                    font = SystemFonts.CreateFont(e.Font.FamilyName, e.Font.Size, e.Font.Style);
#else
                    throw new Exception("System fonts are not supported in the .NET Standard 1.1 version of this library.");
#endif
                else
                    font = _fontCollection.CreateFont(e.Font.FamilyName, e.Font.Size, e.Font.Style);

                fonts[i] = font;

                var renderOptions = new RendererOptions(font, dpiVal.X, dpiVal.Y);
                var characterIndex = 0;
                for (var characterRangeIndex = 0; characterRangeIndex < e.CharacterRanges.Count; characterRangeIndex++)
                {
                    // TODO reduce allocations
                    var cr = e.CharacterRanges[characterRangeIndex];
                    characterRanges[i][characterRangeIndex] = new CharacterRange(cr, characterIndex);

                    sb.Clear();
                    for (var c = cr.Start; c <= cr.End; c++)
                        sb.Append(char.ConvertFromUtf32(c));

                    if (!TextMeasurer.TryMeasureCharacterBounds(sb.ToString().AsSpan(), renderOptions, out var glyphMetrics))
                        continue;

                    foreach (var gm in glyphMetrics)
                    {
                        // we should round the size up so nothing gets lost
                        characterSizes[i][characterIndex] = new Size((int) (gm.Bounds.Width + .9999f), (int) (gm.Bounds.Height + .9999f));
                        characterIndex++;
                    }
                }
            }

            for (var i = 0; i < _atlasEntries.Count; i++)
                characterBounds[i] = packer.Insert(characterSizes[i]);

            var uw = (float) packer.UsedWidth;
            var uh = (float) packer.UsedHeight;

            var glyphMaps = new PixelGlyphMap[_atlasEntries.Count];
            for (var i = 0; i < _atlasEntries.Count; i++)
            {
                var glyphData = new GlyphData[characterSizes[i].Length];
                var gi = 0;
                foreach (var cr in characterRanges[i])
                {
                    foreach (var c in cr)
                    {
                        var rect = characterBounds[i][gi];
                        glyphData[gi] = new GlyphData(c,rect);
                        gi++;
                    }
                }

                glyphMaps[i] = new PixelGlyphMap(fonts[i], characterRanges[i], glyphData);
            }

            return new FontAtlas(packer.UsedWidth, packer.UsedHeight, dpiVal, glyphMaps);
        }
    }
}
