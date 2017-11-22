namespace OpenWheels.GameTools.Rendering
{
    public struct GraphicsState
    {
        public readonly int Texture;
        public readonly BlendState BlendState;
        public readonly DepthStencilState DepthStencilState;
        public readonly RasterizerState RasterizerState;
        public readonly SamplerState SamplerState;
        public readonly Rectangle ScissorRect;

        public bool UseScissorRect => ScissorRect != Rectangle.Empty;

        public GraphicsState(int texture, BlendState blendState, DepthStencilState depthStencilState,
            RasterizerState rasterizerState, SamplerState samplerState, Rectangle scissorRect)
        {
            Texture = texture;
            BlendState = blendState;
            DepthStencilState = depthStencilState;
            RasterizerState = rasterizerState;
            SamplerState = samplerState;
            ScissorRect = scissorRect;
        }

        public static GraphicsState Default => new GraphicsState(-1, 0, 0, 0, 0, Rectangle.Empty);
    }
}