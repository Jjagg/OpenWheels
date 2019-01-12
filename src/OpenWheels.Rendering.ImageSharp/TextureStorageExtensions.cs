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
    /// Extension methods for <see cref="ITextureStorage" /> to load and register textures and fonts from a stream or path with a single method call.
    /// </summary>
    public static class TextureStorageExtensions
    {

#if !NETSTANDARD1_1

        /// <summary>
        /// Load an image from a path and register it in the texture storage.
        /// </summary>
        /// <param name="storage">The texture storage to register the texture in.</param>
        /// <param name="path">Path to the image.</param>
        public static int LoadTexture(this ITextureStorage storage, string path)
        {
            using (var img = Image.Load<Rgba32>(path))
                return RegisterImage(storage, img);
        }

        /// <summary>
        /// Load a font and register it in the texture storage.
        /// </summary>
        /// <param name="storage">The texture storage to register the font atlas.</param>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadFont(this ITextureStorage storage, string path, float size, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateFont(path, size, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

        /// <summary>
        /// Load a font and register  it in the texture storage.
        /// </summary>
        /// <param name="path">Path to the font file.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadFont(this ITextureStorage storage, string path, float size, IEnumerable<Range<int>> ranges, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateFont(path, size, ranges, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

        /// <summary>
        /// Load a system font and register  it in the texture storage.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadSystemFont(this ITextureStorage storage, string name, float size, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateSystemFont(name, size, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

        /// <summary>
        /// Load a system font and register  it in the texture storage.
        /// </summary>
        /// <param name="name">Name of the system font.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadSystemFont(this ITextureStorage storage, string name, float size, IEnumerable<Range<int>> ranges, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateSystemFont(name, size, ranges, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

#endif

        /// <summary>
        /// Load an image from a stream and register it in the texture storage.
        /// </summary>
        /// <param name="storage">The texture storage to register the texture in.</param>
        /// <param name="imageStream">Stream of the image data.</param>
        public static int LoadTexture(this ITextureStorage storage, Stream imageStream)
        {
            using (var img = Image.Load<Rgba32>(imageStream))
                return RegisterImage(storage, img);
        }

        /// <summary>
        /// Load a font from a stream and register  it in the texture storage. Includes Unicode latin characters.
        /// </summary>
        /// <param name="storage">The texture storage to register the texture in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadFont(this ITextureStorage storage, Stream fontStream, float size, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateFont(fontStream, size, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

        /// <summary>
        /// Load a font from a stream and register  it in the texture storage.
        /// </summary>
        /// <param name="storage">The texture storage to register the texture in.</param>
        /// <param name="fontStream">Stream of the font data.</param>
        /// <param name="size">Size to render the font at.</param>
        /// <param name="ranges">The character ranges to render to the font atlas.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>0</c> (no fallback).</param>
        public static TextureFont LoadFont(this ITextureStorage storage, Stream fontStream, float size, IEnumerable<Range<int>> ranges, int fallbackCharacter = 0)
        {
            FontAtlasHelpers.CreateFont(fontStream, size, ranges, out var glyphMap, out var image);
            var texId = RegisterImage(storage, image);
            image.Dispose();
            return new TextureFont(glyphMap, texId, fallbackCharacter);
        }

        private static int RegisterImage(ITextureStorage storage, Image<Rgba32> img)
        {
            var id = storage.CreateTexture(img.Width, img.Height, TextureFormat.Rgba32);
            ReadOnlySpan<Rgba32> pixelSpan = img.GetPixelSpan();
            storage.SetData(id, pixelSpan);
            return id;
        }
    }
}
