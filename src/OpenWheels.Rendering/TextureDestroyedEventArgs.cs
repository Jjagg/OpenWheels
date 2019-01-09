namespace OpenWheels.Rendering
{
    /// <summary>
    /// Argument for the <see cref="ITextureStorage.TextureDestroyed"/> event.
    /// </summary>
    public struct TextureDestroyedEventArgs
    {
        /// <summary>
        /// The id of the destroyed texture.
        /// <summary>
        public int TextureId { get; }

        internal TextureDestroyedEventArgs(int id) => TextureId = id;
    }
}
