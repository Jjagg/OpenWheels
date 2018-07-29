using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using OpenWheels;
using OpenWheels.Fonts;
using OpenWheels.Fonts.ImageSharp;
using OpenWheels.Rendering;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenWheels.Rendering.ImageSharp
{
    /// <summary>
    /// Extension methods for <see cref="StringIdBatcher" /> to load and register textures and fonts from a stream or path with a single method call.
    /// </summary>
    public static class StringIdBatcherExtensions
    {

#if !NETSTANDARD1_1

        /// <summary>
        /// Load an image from a path and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the texture in.</param>
        /// <param name="path">Path to the image.</param>
        /// <param name="name">Identifier for the texture.</param>
        public static void LoadTexture(this StringIdBatcher batcher, string path, string name)
        {
            using (var img = Image.Load<Rgba32>(path))
                RegisterImage(batcher, img, name);
        }

        /// <summary>
        /// Load a font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, string path, float size, string name)
        {
            LoadFont(batcher, path, size, (int?) null, name);
        }

        /// <summary>
        /// Load a font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Fallback character for the font. Set to <c>null</c> for no fallback.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, string path, float size, int? fallbackCharacter, string name)
        {
            FontAtlasHelpers.CreateFont(path, size, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, name);
            image.Dispose();
        }

        /// <summary>
        /// Load a font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, string path, float size, IEnumerable<Range<int>> ranges, string name)
        {
            LoadFont(batcher, path, size, ranges, (int?) null, name);
        }

        /// <summary>
        /// Load a font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>null</c> (no fallback).</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, string path, float size, IEnumerable<Range<int>> ranges, int? fallbackCharacter, string name)
        {
            FontAtlasHelpers.CreateFont(path, size, ranges, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, name);
            image.Dispose();
        }

        /// <summary>
        /// Load a system font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="id">Identifier for the font.</param>
        public static void LoadSystemFont(this StringIdBatcher batcher, string name, float size, string id)
        {
            LoadSystemFont(batcher, name, size, (int?) null, id);
        }

        /// <summary>
        /// Load a system font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>null</c> (no fallback).</param>
        /// <param name="id">Identifier for the font.</param>
        public static void LoadSystemFont(this StringIdBatcher batcher, string name, float size, int? fallbackCharacter, string id)
        {
            FontAtlasHelpers.CreateSystemFont(name, size, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, id);
            image.Dispose();
        }

        /// <summary>
        /// Load a system font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="id">Identifier for the font.</param>
        public static void LoadSystemFont(this StringIdBatcher batcher, string name, float size, IEnumerable<Range<int>> ranges, string id)
        {
            LoadSystemFont(batcher, name, size, ranges, (int?) null, id);
        }

        /// <summary>
        /// Load a system font and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>null</c> (no fallback).</param>
        /// <param name="id">Identifier for the font.</param>
        public static void LoadSystemFont(this StringIdBatcher batcher, string name, float size, IEnumerable<Range<int>> ranges, int? fallbackCharacter, string id)
        {
            FontAtlasHelpers.CreateSystemFont(name, size, ranges, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, id);
            image.Dispose();
        }

#endif

        /// <summary>
        /// Load an image from a stream and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the texture in.</param>
        /// <param name="imageStream">Stream of the image data.</param>
        /// <param name="name">Identifier for the texture.</param>
        public static void LoadTexture(this StringIdBatcher batcher, Stream imageStream, string name)
        {
            using (var img = Image.Load<Rgba32>(imageStream))
                RegisterImage(batcher, img, name);
        }

        /// <summary>
        /// Load a font from a stream and register it in the batcher. Includes Unicode latin characters.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, Stream fontStream, float size, string name)
        {
            LoadFont(batcher, fontStream, size, (int?) null, name);
        }

        /// <summary>
        /// Load a font from a stream and register it in the batcher. Includes Unicode latin characters.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Fallback character for the font. Set to <c>null</c> for no fallback.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, Stream fontStream, float size, int? fallbackCharacter, string name)
        {
            FontAtlasHelpers.CreateFont(fontStream, size, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, name);
            image.Dispose();
        }

        /// <summary>
        /// Load a font from a stream and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, Stream fontStream, float size, IEnumerable<Range<int>> ranges, string name)
        {
            LoadFont(batcher, fontStream, size, ranges, null, name);
        }

        /// <summary>
        /// Load a font from a stream and register it in the batcher.
        /// </summary>
        /// <param name="batcher">The batcher to register the font in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Fallback character for the font. Set to <c>null</c> for no fallback.</param>
        /// <param name="name">Identifier for the font.</param>
        public static void LoadFont(this StringIdBatcher batcher, Stream fontStream, float size, IEnumerable<Range<int>> ranges, int? fallbackCharacter, string name)
        {
            FontAtlasHelpers.CreateFont(fontStream, size, ranges, out var glyphMap, out var image);
            RegisterFont(batcher, image, glyphMap, fallbackCharacter, name);
            image.Dispose();
        }

        private static void RegisterImage(StringIdBatcher batcher, Image<Rgba32> img, string name)
        {
            var pixelSpan = MemoryMarshal.Cast<Rgba32, Color>(img.GetPixelSpan());
            batcher.RegisterTexture(name, pixelSpan, img.Width, img.Height);
        }

        private static void RegisterFont(StringIdBatcher batcher, Image<Rgba32> img, GlyphMap glyphMap, int? fallbackCharacter, string name)
        {
            var pixelSpan = MemoryMarshal.Cast<Rgba32, Color>(img.GetPixelSpan());
            batcher.RegisterFont(name, glyphMap, pixelSpan, img.Width, img.Height, fallbackCharacter);
        }
    }
}