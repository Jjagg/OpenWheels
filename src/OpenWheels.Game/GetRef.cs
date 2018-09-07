namespace OpenWheels.Game
{
    /// <summary>
    /// Signature for tweening selectors.
    /// </summary>
    /// <seealso cref="ServiceRunner.Tween"/>
    public delegate ref TReturn GetRef<TItem, TReturn>(TItem item);
}