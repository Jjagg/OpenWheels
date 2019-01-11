namespace OpenWheels.Rendering
{
    /// <summary>
    /// A sprite value type, represented with a texture identifier and a source rectangle (in pixels) in the texture.
    /// </summary>
    public struct Sprite
    {
        /// <summary>
        /// Texture identifier.
        /// </summary>
        public readonly int Texture;
        
        /// <summary>
        /// Source rectangle of the sprite in the texture in pixels.
        /// </summary>
        public readonly Rectangle SrcRect;

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        /// <param name="texture">The texture identifier.</param>
        /// <param name="srcRect">The source rectangle of the sprite in pixels.</param>
        public Sprite(int texture, Rectangle srcRect)
        {
            Texture = texture;
            SrcRect = srcRect;
        }
        
        public override string ToString()
            => $"{nameof(Texture)}: {Texture}, {nameof(SrcRect)}: {SrcRect}";
    }
}
