namespace OpenWheels.BinPack
{
    /// <summary>
    /// How to grow the bin when a rectangel can't be placed.
    /// </summary>
    public enum GrowRule
    {
        /// <summary>
        /// Don't grow the bin, throw an exception when full.
        /// </summary>
        None,
        /// <summary>
        /// Grow the bins width.
        /// </summary>
        Width,
        /// <summary>
        /// Grow the bins height.
        /// </summary>
        Height,
        /// <summary>
        /// Alternate growing the bins width and height. Starts with width.
        /// </summary>
        Both
    }
}