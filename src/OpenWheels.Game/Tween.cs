using System;

namespace OpenWheels.Game
{
    /// <summary>
    /// Non-generic base type for tweens.
    /// </summary>
    public abstract class Tween
    {
        /// <summary>
        /// Apply the tween given the t value for interpolation.
        /// </summary>
        /// <param name="t">Interpolation value in range [0, 1].</param>
        public abstract void Apply(float t);
    }

    /// <summary>
    /// Data needed to perform a tween. A tween is a smooth modification of a value over time.
    /// </summary>
    /// <typeparam name="TItem">Type of the item of which a field is tweened.</typeparam>
    /// <typeparam name="TProperty">The type of the field being tweened.</typeparam>
    public class Tween<TItem, TProperty> : Tween
    {
        /// <summary>
        /// The item of which a field is being tweened.
        /// </summary>
        public TItem Item;
        /// <summary>
        /// Function to get a ref to the value that is tweened.
        /// </summary>
        public GetRef<TItem, TProperty> Selector;
        /// <summary>
        /// Function to linearly interpolate the value.
        /// </summary>
        public Lerp<TProperty> Lerp;
        /// <summary>
        /// Initial value of the property.
        /// </summary>
        public TProperty From;
        /// <summary>
        /// Target value of the property.
        /// </summary>
        public TProperty To;
        /// <summary>
        /// Easing function to apply.
        /// </summary>
        public Ease EasingFunction;

        /// <inheritdoc />
        public override void Apply(float t)
        {
            var p = EasingFunction(t);
            ref TProperty prop = ref Selector(Item);
            prop = Lerp(From, To, p);
        }
    }
}