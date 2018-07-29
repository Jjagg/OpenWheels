using System;
using System.Collections.Generic;
using OpenWheels.Fonts;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Class that extends <see cref="Batcher"> and adds support for registering and setting
    /// sprites and fonts with a string identifier. <see cref="Renderer"> should be set before
    /// registering anything through this class.
    /// </summary>
    public class StringIdBatcher : Batcher
    {
        /// <summary>
        /// The name to <see cref="Sprite" /> mapping managed by this <see cref="StringIdBatcher" />.
        /// </summary>
        public Dictionary<string, Sprite> Sprites { get; }

        /// <summary>
        /// The name to <see cref="TextureFont" /> mapping managed by this <see cref="StringIdBatcher" />.
        /// </summary>
        public Dictionary<string, TextureFont> Fonts { get; }

        /// <summary>
        /// Create a new instance of <see cref="StringIdBatcher" />.
        /// </summary>
        public StringIdBatcher()
        {
            Sprites = new Dictionary<string, Sprite>();
            Fonts = new Dictionary<string, TextureFont>();
        }

        /// <summary>
        /// Register a texture given a name, the image data and its dimensions.
        /// </summary>
        /// <param name="name">Name of the texture.</param>
        /// <param name="pixels">Span of the image data in row-major order.</param>
        /// <param name="width">Width of the texture in pixels.</param>
        /// <param name="height">Height of the texture in pixels.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If a sprite or texture with the same name is already registered.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        public void RegisterTexture(string name, Span<Color> pixels, int width, int height)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var id = RegisterTexture(pixels, width, height);
            var size = Renderer.GetTextureSize(id);
            Sprites.Add(name, new Sprite(id, new Rectangle(0, 0, size.Width, size.Height)));
        }

        /// <summary>
        /// Register all sprites from a sprite sheet given the sprite map, the atlas image data and its dimensions.
        /// The map is flattened and each sprite can be set by its key in the map using <see cref="SetSprite" />.
        /// </summary>
        /// <param name="spriteMap">Mapping of sprite names to their bounds on the image in pixels.</param>
        /// <param name="pixels">Span of the image data in row-major order.</param>
        /// <param name="width">Width of the texture in pixels.</param>
        /// <param name="height">Height of the texture in pixels.</param>
        /// <exception cref="ArgumentNullException">If the key for a sprite in <paramref name="spriteMap" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If a texture or sprite with name equal to a key in <paramref name="spriteMap" /> is already registered.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        public void RegisterSpriteSheet(Dictionary<string, Rectangle> spriteMap, Span<Color> pixels, int width, int height)
        {
            var texId = RegisterTexture(pixels, width, height);

            foreach (var spriteMapping in spriteMap)
            {
                if (spriteMapping.Key == null)
                    throw new ArgumentNullException("The key for a sprite can't be null.", nameof(spriteMap));

                var sprite = new Sprite(texId, spriteMapping.Value);
                Sprites.Add(spriteMapping.Key, sprite);
            }
        }

        /// <summary>
        /// Register a font given a name, a <see cref="GlyphMap" />, the image data and dimensions of the atlas, and optionally a fallback character.
        /// </summary>
        /// <param name="name">Name of the font.</param>
        /// <param name="glyphMap">Glyph map for the font atlas.</param>
        /// <param name="pixels">Span of the image data of the atlas in row-major order.</param>
        /// <param name="width">Width of the font atlas in pixels.</param>
        /// <param name="height">Height of the font atlas in pixels.</param>
        /// <param name="fallbackCharacter">Optional fallback character for the font. Defaults to <c>null</c> (no fallback).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="glyphMap" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If a font with the same name is already registered.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        public void RegisterFont(string name, GlyphMap glyphMap, Span<Color> pixels, int width, int height, int? fallbackCharacter = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var id = RegisterTexture(pixels, width, height);
            var tf = new TextureFont(glyphMap, id, fallbackCharacter);
            Fonts.Add(name, tf);
        }

        /// <summary>
        /// Make the sprite or texture registered with the given name the active sprite on this batcher.
        /// </summary>
        /// <param name="name">Name of the sprite.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If no sprite with the given name is registered.</exception>
        public void SetSprite(string name)
        {
            Sprite = Sprites[name];
        }

        /// <summary>
        /// Make the font registered with the given name the active font on this batcher.
        /// </summary>
        /// <param name="name">Name of the font.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name" /> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">If no font with the given name is registered.</exception>
        public void SetFont(string name)
        {
            TextureFont = Fonts[name];
        }
    }
}