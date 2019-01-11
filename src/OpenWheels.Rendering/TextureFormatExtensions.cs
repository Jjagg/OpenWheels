using System;

namespace OpenWheels.Rendering
{
    public static class TextureFormatExtensions
    {
        /// <summary>
        /// Get the number of bytes needed to represent a single pixel in the given format.
        /// </summary>
        public static int GetBytesPerPixel(this TextureFormat textureFormat)
        {
            switch (textureFormat)
            {
                case TextureFormat.Rgba32:
                    return 4;
                case TextureFormat.Red8:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textureFormat));
            }
        }
    }
}