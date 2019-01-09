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

        private static int RegisterImage(ITextureStorage storage, Image<Rgba32> img)
        {
            var pixelSpan = MemoryMarshal.Cast<Rgba32, Color>(img.GetPixelSpan());
            var id = storage.CreateTexture(img.Width, img.Height);
            storage.SetData(id, pixelSpan);
            return id;
        }
    }
}
