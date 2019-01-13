using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenWheels.Fonts.ImageSharp
{
    /// <summary>
    /// Helper methods to easily create a font atlas.
    /// </summary>
    public static class FontAtlasHelpers
    {

#if !NETSTANDARD1_1

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateSystemFont(string name, float size, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateSystemFont(name, size, ranges, out glyphMap, out image);
        }

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateSystemFont(string name, float size, IEnumerable<Range<int>> ranges, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddSystemFont(name, size, ranges);
            RenderGlyphMap(fab, out glyphMap, out image);
        }

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(string path, float size, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateFont(path, size, ranges, out glyphMap, out image);
        }

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font.
        /// </summary>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(string path, float size, IEnumerable<Range<int>> ranges, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddFont(path, size, ranges);
            RenderGlyphMap(fab, out glyphMap, out image);
        }

#endif

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(Stream fontStream, float size, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateFont(fontStream, size, ranges, out glyphMap, out image);
        }

        /// <summary>
        /// Create a <see cref="PixelGlyphMap"/> for the given system font.
        /// </summary>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="glyphMap">The glyph map of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(Stream fontStream, float size, IEnumerable<Range<int>> ranges, out PixelGlyphMap glyphMap, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddFont(fontStream, size, ranges);
            RenderGlyphMap(fab, out glyphMap, out image);
        }

        private static void RenderGlyphMap(FontAtlasBuilder fab, out PixelGlyphMap glyphMap, out Image<Rgba32> img)
        {
            var atlas = fab.CreateAtlas();
            glyphMap = atlas[0];
            img = atlas.RenderImage();
        }
    }
}
