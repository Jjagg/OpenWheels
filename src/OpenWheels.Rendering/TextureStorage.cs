using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Manages textures of type <typeparamref name="T"/> identified with an integer value.
    /// </summary>
    /// <typeparam name="T">The native texture type.</typeparam>
    public abstract class TextureStorage<T> : ITextureStorage
    {
        /// <inheritdoc />
        public abstract int TextureCount { get; }
        /// <inheritdoc />
        public abstract int CreateTexture(int width, int height);
        /// <inheritdoc />
        public abstract void DestroyTexture(int id);
        /// <inheritdoc />
        public abstract Size GetTextureSize(int id);
        /// <inheritdoc />
        public abstract void SetData(int id, Span<Color> data);
        /// <inheritdoc />
        public abstract void SetData(int id, in Rectangle subRect, Span<Color> data);

        /// <summary>
        /// Get the native texture with the matching id.
        /// </summary>
        /// <param name="id">Id of the texture.</param>
        /// <returns>The native texture with the matching id or <c>default(T)</c> if there is no matching texture.</returns>
        public abstract T GetTexture(int id);

        /// <inheritdoc />
        public event EventHandler<TextureCreatedEventArgs> TextureCreated;
        /// <inheritdoc />
        public event EventHandler<TextureDestroyedEventArgs> TextureDestroyed;

        protected void OnTextureCreated(int id, int width, int height)
            => TextureCreated?.Invoke(this, new TextureCreatedEventArgs(id, width, height));

        protected void OnTextureDestroyed(int id)
            => TextureDestroyed?.Invoke(this, new TextureDestroyedEventArgs(id));
    }
}
