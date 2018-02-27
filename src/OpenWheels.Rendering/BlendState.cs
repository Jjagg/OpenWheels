namespace OpenWheels.Rendering
{
    /// <summary>
    /// Commonly supported blend states for rendering.
    /// </summary>
    public enum BlendState
    {
        /// <summary>
        /// Alpha blend source on top of destination assuming premultiplied alpha.
        /// I.e. blend = src.rgb + (dst.rgb * (1 - src.a))
        /// </summary>
        AlphaBlend,
        /// <summary>
        /// Render the source color without blending.
        /// I.e. blend = src.rgb
        /// </summary>
        Opaque
    }
}