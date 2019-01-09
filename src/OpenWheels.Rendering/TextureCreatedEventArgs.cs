namespace OpenWheels.Rendering
{
    /// <summary>
    /// Argument for the <see cref="ITextureStorage.TextureCreated"/> event.
    /// </summary>
    public struct TextureCreatedEventArgs
    {
        /// <summary>
        /// The id of the created texture.
        /// <summary>
        public int TextureId { get; }

        /// <summary>
        /// The width of the texture.
        /// <summary>
        public int Width { get; }

        /// <summary>
        /// The height of the texture.
        /// <summary>
        public int Height { get; }

        internal TextureCreatedEventArgs(int id, int width, int height)
        {
            TextureId = id;
            Width = width;
            Height = height;
        }
    }
}
