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
        /// Create a new <see cref="GraphicsState"/> instance.
        /// </summary>
        /// <param name="texture">Id of the texture to render.</param>
        /// <param name="samplerState">Sampler state.</param>
        public GraphicsState(int texture, SamplerState samplerState)
        {
            Texture = texture;
            SamplerState = samplerState;
        }

        public bool Equals(GraphicsState other)
        {
            return Texture == other.Texture && SamplerState == other.SamplerState;
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
        public static GraphicsState Default => new GraphicsState(-1, 0);
    }
}
