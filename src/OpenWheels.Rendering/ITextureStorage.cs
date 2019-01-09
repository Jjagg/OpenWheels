using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Manages textures.
    /// </summary>
    public interface ITextureStorage
    {
        /// <summary>
        /// Create a new texture and return its id.
        /// </summary>
        /// <param name="width">Width of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <returns>Id of the new texture.</returns>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        int CreateTexture(int width, int height);

        /// <summary>
        /// Get the size of a texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <returns>Size of the matching texture, <cref name="Size.Empty"/> if there is no matching texture.</returns>
        Size GetTextureSize(int id);


        /// <summary>
        /// Set the pixel data of a texture. Does nothing if there is no matching texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <param name="data">Color data to set to the texture.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception name="ArgumentException">If <see cref="data.Length"/> is not equal to <c>width * height</c> of the matching texture.</exception>
        void SetData(int id, Span<Color> data);
        /* TODO C# 8
        {
            (var w, var h) = GetTextureSize(id);
            if (data.Length != w * h)
                throw new ArgumentException($"Length of data (${data.Length}) did not match width * height of the texture (${w * h}).", nameof(data));
            SetData(id, new Rectangle(0, 0, w, h), data);
        }*/

        /// <summary>
        /// Set the pixel data of a subregion of a texture. Does nothing if there is no matching texture.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <param name="rectangle">The subregion within the texture to copy data to.</param>
        /// <param name="data">Color data to set to the texture.</param>
        /// <exception name="ArgumentException">If (a part of) <see cref="subRect"/> falls outside the texture bounds.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception name="ArgumentException">If <c>data.Length</c> is not equal to <c>width * height</c> of <paramref name="subRect"/>.</exception>
        void SetData(int id, in Rectangle subRect, Span<Color> data);
    }

    /// <summary>
    /// A dummy implementation of <see cref="ITextureStorage"/>.
    /// </summary>
    public class NullTextureStorage : ITextureStorage
    {
        public static NullTextureStorage Instance { get; } = new NullTextureStorage();

        private NullTextureStorage() { }

        public int CreateTexture(int width, int height) => 0;
        public Size GetTextureSize(int id) => Size.Empty;
        public void SetData(int id, Span<Color> data) { }
        public void SetData(int id, in Rectangle subRect, Span<Color> data) { }
    }
}
