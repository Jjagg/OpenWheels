namespace OpenWheels.Rendering
{
    /// <summary>
    /// A sprite value type, represented with a texture identifier and a source rectangle (in uv coordinates) in the texture.
    /// </summary>
    public struct UvSprite
    {
        /// <summary>
        /// Texture identifier.
        /// </summary>
        public readonly int Texture;
        
        /// <summary>
        /// Source rectangle of the sprite in the texture in UV coordinates.
        /// </summary>
        public readonly RectangleF SrcRect;

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        /// <param name="texture">The texture identifier.</param>
        /// <param name="srcRect">The source rectangle of the sprite in UV coordinates.</param>
        public UvSprite(int texture, RectangleF srcRect)
        {
            Texture = texture;
            SrcRect = srcRect;
        }

        public override string ToString()
        {
            var t = $"{nameof(Texture)}: {Texture}";
            if (SrcRect != Rectangle.Unit)
                t = t + $", {nameof(SrcRect)}: {SrcRect}";
            return t;
        }
 
    }
}
