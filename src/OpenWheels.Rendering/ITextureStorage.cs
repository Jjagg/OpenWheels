using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Manages textures.
    /// </summary>
    public interface ITextureStorage
    {
        /// <summary>
        /// The number of textures stored.
        /// </summary>
        int TextureCount { get; }

        /// <summary>
        /// Create a new texture and return its id.
        /// </summary>
        /// <param name="width">Width of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="format">Format of the texture.</param>
        /// <returns>Id of the new texture.</returns>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        int CreateTexture(int width, int height, TextureFormat format);

        /// <summary>
        /// Destroy the texture with the given id.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        void DestroyTexture(int id);

        /// <summary>
        /// Check if a texture exists.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <returns><c>true</c> if there is a matching texture, <c>false</c> if not.</returns>
        bool HasTexture(int id);

        /// <summary>
        /// Get the size of a texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <returns>Size of the matching texture, <cref name="Size.Empty"/> if there is no matching texture.</returns>
        Size GetTextureSize(int id);

        /// <summary>
        /// Get the format of a texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <returns>Format of the matching texture, undefined if there is no matching texture.</returns>
        TextureFormat GetTextureFormat(int id);

        /// <summary>
        /// Set the pixel data of a texture. Data should be in row-major order. Does nothing if there is no matching texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <param name="data">Pixel data to set to the texture.</param>
        /// <exception name="ArgumentException">If <see cref="data.Length"/> is not equal to <c>width * height * format.GetBytesPerPixel()</c> of the matching texture.</exception>
        void SetData<T>(int id, ReadOnlySpan<T> data) where T : struct;

        /// <summary>
        /// Set the pixel data of a subregion of a texture. Data should be in row-major order. Does nothing if there is no matching texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <param name="rectangle">The subregion within the texture to copy data to.</param>
        /// <param name="data">Pixel data to set to the texture.</param>
        /// <exception name="ArgumentException">If (a part of) <see cref="subRect"/> falls outside the texture bounds.</exception>
        /// <exception name="ArgumentException">If <c>data.Length</c> is not equal to <c>width * height</c> of <paramref name="subRect"/>.</exception>
        void SetData<T>(int id, in Rectangle subRect, ReadOnlySpan<T> data) where T : struct;

        /// <summary>
        /// Invoked when a texture is created.
        /// </summary>
        event EventHandler<TextureCreatedEventArgs> TextureCreated;

        /// <summary>
        /// Invoked when a texture is destroyed.
        /// </summary>
        event EventHandler<TextureDestroyedEventArgs> TextureDestroyed;
    }

    /// <summary>
    /// A dummy implementation of <see cref="ITextureStorage"/>. Can be useful for renderers that
    /// do not use textures or in for debugging e.g. with a <see cref="TraceRenderer"/>.
    /// </summary>
    public class NullTextureStorage : ITextureStorage
    {
        public static NullTextureStorage Instance { get; } = new NullTextureStorage();

        public int TextureCount => 0;

        private NullTextureStorage() { }

        public int CreateTexture(int width, int height, TextureFormat format) => 0;
        public void DestroyTexture(int id) { }
        public bool HasTexture(int id) => false;
        public TextureFormat GetTextureFormat(int id) => TextureFormat.Rgba32;
        public Size GetTextureSize(int id) => Size.Empty;
        public void SetData<T>(int id, ReadOnlySpan<T> data) where T : struct { }
        public void SetData<T>(int id, in Rectangle subRect, ReadOnlySpan<T> data) where T : struct { }

        public event EventHandler<TextureCreatedEventArgs> TextureCreated { add { } remove { } }
        public event EventHandler<TextureDestroyedEventArgs> TextureDestroyed { add { } remove { } }
    }
}
