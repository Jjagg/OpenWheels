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
        LinearClamp = 0,
        /// <summary>
        /// Linear filtering and texture coordinate wrapping.
        /// </summary>
        LinearWrap = 1,
        /// <summary>
        /// Point filtering and texture coordinate clamping.
        /// </summary>
        PointClamp = 2,
        /// <summary>
        /// Point filtering and texture coordinate wrapping.
        /// </summary>
        PointWrap = 3,
        /// <summary>
        /// Anisotropic filtering and texture coordinate clamping.
        /// </summary>
        AnisotropicClamp = 4,
        /// <summary>
        /// Anisotropic filtering and texture coordinate wrapping.
        /// </summary>
        AnisotropicWrap = 5
    }
}