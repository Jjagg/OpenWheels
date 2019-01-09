using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Holds graphics state for rendering.
    /// </summary>
    public struct GraphicsState : IEquatable<GraphicsState>
    {
        /// <summary>
        /// The texture to render.
        /// </summary>
        public readonly int Texture;

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
        /// <param name="samplerState">Sampler state.</param>
        /// <param name="scissorRect">Scissor rectangle. <see cref="Rectangle.Empty"/> means no scissor rectangle is set.</param>
        public GraphicsState(int texture, SamplerState samplerState, Rectangle scissorRect)
        {
            Texture = texture;
            SamplerState = samplerState;
            ScissorRect = scissorRect;
        }

        public bool Equals(GraphicsState other)
        {
            return Texture == other.Texture && SamplerState == other.SamplerState && ScissorRect.Equals(other.ScissorRect);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GraphicsState && Equals((GraphicsState) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Texture;
                hashCode = (hashCode * 397) ^ (int) SamplerState;
                hashCode = (hashCode * 397) ^ ScissorRect.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(GraphicsState left, GraphicsState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GraphicsState left, GraphicsState right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Get the default <see cref="GraphicsState"/>.
        /// </summary>
        public static GraphicsState Default => new GraphicsState(-1,  0, Rectangle.Empty);
    }
}
