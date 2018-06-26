using System;
using System.Collections.Generic;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// An image containing multiple sprites.
    /// Retrieve a <see cref="Sprite"/> with it's key.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="Sprite"/> keys.</typeparam>
    public class SpriteSheet<T>
    {
        /// <summary>
        /// The texture of this <see cref="SpriteSheet{T}"/>.
        /// </summary>
        public int Texture { get; }

        private readonly Dictionary<T, Rectangle> _spriteMap;

        /// <summary>
        /// Create a <see cref="SpriteSheet{T}"/>.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        /// <param name="spriteMap">Dictionary of sprite coordinates in pixels.</param>
        public SpriteSheet(int texture, Dictionary<T, Rectangle> spriteMap)
        {
            Texture = texture;
            _spriteMap = spriteMap;
        }

        /// <summary>
        /// Get the <see cref="Sprite"/> associated with the given key.
        /// </summary>
        /// <param name="key">The key of the <see cref="Sprite"/> to retrieve.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <code>null</code>.</exception>
        /// <exception cref="KeyNotFoundException">If the key does not exist in the sprite map.</exception>
        public Sprite this[T key] => new Sprite(Texture, _spriteMap[key]);

        /// <summary>
        /// Get the <see cref="Sprite"/> associated with the given key.
        /// </summary>
        /// <param name="key">The key of the <see cref="Sprite"/> to retrieve.</param>
        /// <param name="sprite">
        ///   The retrieved <see cref="Sprite"/> or the default sprite value if the
        ///   key does not exist in the sprite map.
        /// </param>
        /// <returns>
        ///   <code>True</code> if <paramref name="key"/> exists in the sprite map and
        ///   the sprite was succesfully retrieved, <code>false</code> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <code>null</code>.</exception>
        public bool TryGetSprite(T key, out Sprite sprite)
        {
            if (_spriteMap.TryGetValue(key, out var rect))
            {
                sprite = new Sprite(Texture, rect);
                return true;
            }

            sprite = default;
            return false;
        }
    }
}