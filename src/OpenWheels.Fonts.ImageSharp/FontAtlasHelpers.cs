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
        /// Create a <see cref="FontAtlas"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateSystemFont(string name, float size, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateSystemFont(name, size, ranges, out fontAtlas, out image);
        }

        /// <summary>
        /// Create a <see cref="FontAtlas"/> for the given system font.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateSystemFont(string name, float size, IEnumerable<Range<int>> ranges, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddSystemFont(name, size, ranges);
            RenderAtlas(fab, out fontAtlas, out image);
        }

        /// <summary>
        /// Create a <see cref="FontAtlas"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(string path, float size, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateFont(path, size, ranges, out fontAtlas, out image);
        }

        /// <summary>
        /// Create a <see cref="FontAtlas"/> for the given system font.
        /// </summary>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(string path, float size, IEnumerable<Range<int>> ranges, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddFont(path, size, ranges);
            RenderAtlas(fab, out fontAtlas, out image);
        }
#endif

        /// <summary>
        /// Create a <see cref="FontAtlas"/> for the given system font. Includes Unicode latin characters.
        /// </summary>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(Stream fontStream, float size, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var ranges = TextUtil.CreateRanges(TextUtil.LatinStart, TextUtil.LatinEnd);
            CreateFont(fontStream, size, ranges, out fontAtlas, out image);
        }

        /// <summary>
        /// Create a <see cref="FontAtlas"/> for the given system font.
        /// </summary>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fontAtlas">The data of the font atlas.</param>
        /// <param name="image">The image of the font atlas.</param>
        public static void CreateFont(Stream fontStream, float size, IEnumerable<Range<int>> ranges, out FontAtlas fontAtlas, out Image<Rgba32> image)
        {
            var fab = new FontAtlasBuilder();
            fab.AddFont(fontStream, size, ranges);
            RenderAtlas(fab, out fontAtlas, out image);
        }

        private static void RenderAtlas(FontAtlasBuilder fab, out FontAtlas atlas, out Image<Rgba32> img)
        {
            atlas = fab.CreateAtlas();
            img = atlas.RenderImage();
        }
    }
}
