namespace OpenWheels.Rendering
{
    /// <summary>
    /// Commonly supported sampler states for sampling from textures.
    /// </summary>
    public enum SamplerState
    {
        /// <summary>
        /// Linear filtering and texture coordinate clamping.
        /// </summary>
        LinearClamp,
        /// <summary>
        /// Linear filtering and texture coordinate wrapping.
        /// </summary>
        LinearWrap,
        /// <summary>
        /// Point filtering and texture coordinate clamping.
        /// </summary>
        PointClamp,
        /// <summary>
        /// Point filtering and texture coordinate wrapping.
        /// </summary>
        PointWrap,
        /// <summary>
        /// Anisotropic filtering and texture coordinate clamping.
        /// </summary>
        AnisotropicClamp,
        /// <summary>
        /// Anisotropic filtering and texture coordinate wrapping.
        /// </summary>
        AnisotropicWrap
    }
}