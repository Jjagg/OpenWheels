namespace OpenWheels.Rendering
{
    /// <summary>
    /// Holds graphics state for rendering.
    /// </summary>
    public struct GraphicsState
    {
        /// <summary>
        /// The texture to render.
        /// </summary>
        public readonly int Texture;

        /// <summary>
        /// Blend state to apply.
        /// </summary>
        public readonly BlendState BlendState;

        /// <summary>
        /// Sampler state to apply.
        /// </summary>
        public readonly SamplerState SamplerState;

        /// <summary>
        /// Bounds of the scissor rectangle. Note that this scissor rectangle should only be applied
        /// if <see cref="UseScissorRect"/> is set to <code>true</code>.
        /// </summary>
        public readonly Rectangle ScissorRect;

        /// <summary>
        /// Indicates if the scissor rectangle should be applied.
        /// </summary>
        public bool UseScissorRect => ScissorRect != Rectangle.Empty;

        /// <summary>
        /// Create a new <see cref="GraphicsState"/> instance.
        /// </summary>
        /// <param name="texture">Id of the texture to render.</param>
        /// <param name="blendState">Blend state.</param>
        /// <param name="samplerState">Sampler state.</param>
        /// <param name="scissorRect">Scissor rectangle. <see cref="Rectangle.Empty"/> means no scissor rectangle is set.</param>
        public GraphicsState(int texture, BlendState blendState,
            SamplerState samplerState, Rectangle scissorRect)
        {
            Texture = texture;
            BlendState = blendState;
            SamplerState = samplerState;
            ScissorRect = scissorRect;
        }

        /// <summary>
        /// Get the default <see cref="GraphicsState"/>.
        /// </summary>
        public static GraphicsState Default => new GraphicsState(-1, 0, 0, Rectangle.Empty);
    }
}
