using System;
using System.Collections.Generic;
using System.Linq;
using OpenWheels.Fonts;
using SixLabors.Fonts;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// A font renderer for OpenWheels using SixLabors.Fonts for layout.
    /// </summary>
    public class FontsTextRenderer : IBitmapFontRenderer
    {
        private List<TextureFont> _textureFonts;

        /// <summary>
        /// Create a new FontsTextRenderer.
        /// </summary>
        public FontsTextRenderer()
        {
            _textureFonts = new List<TextureFont>();
        }

        /// <summary>
        /// Add a texture font to the prerendered fonts for this renderer.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="textureFont"/> is <c>null</c>.</exception>
        public void AddFont(TextureFont textureFont)
        {
            if (textureFont == null)
                throw new ArgumentNullException(nameof(textureFont));

            _textureFonts.Add(textureFont);
        }

        /// <inhertidoc/>
        /// <exception cref="ArgumentException">
        ///   If no font matching <paramref name="fontInfo"/> was added with <see name="AddFont"/>
        ///   before calling this method.
        /// </exception>
        public void RenderText(Batcher batcher, in FontInfo fontInfo, string text, in TextLayoutOptions tlo, Color color)
        {
            var textureFont = GetTextureFont(fontInfo, out var scale);

            if (textureFont == null)
                throw new ArgumentException($"The font '{fontInfo.Name}' was not rendered to the atlas.", nameof(fontInfo.Name));

            var slFont = textureFont.Font;
            // TODO other dpi support
            var dpi = 72 * scale;
            // TODO there is no setter for Font on RendererOptions, once there is we can cache an instance
            var ro = new RendererOptions(slFont, dpi, dpi, tlo.Position);
            ro.HorizontalAlignment = ToHorizontalAlignment(tlo.HorizontalAlignment);
            ro.VerticalAlignment = ToVerticalAlignment(tlo.VerticalAlignment);
            ro.WrappingWidth = tlo.WrappingWidth;
            ro.TabWidth = tlo.TabWidth;

            // TODO this generates garbage; should push for an overload to use an existing collection
            // TODO StringBuilder or ReadOnlySpan<char> overload
            TextMeasurer.TryMeasureCharacterBounds(text, ro, out var gms);

            foreach (var gm in gms)
            {
                var rect = new RectangleF(gm.Bounds.X, gm.Bounds.Y, gm.Bounds.Width, gm.Bounds.Height);
                var gd = textureFont.GetGlyphData(gm.Codepoint);

                batcher.SetUvSprite(textureFont.Texture, gd.Bounds);
                batcher.FillRect(rect, color);
            }
        }

        private HorizontalAlignment ToHorizontalAlignment(TextAlignment horizontalAlignment)
        {
            switch (horizontalAlignment)
            {
                case TextAlignment.Start:
                    return HorizontalAlignment.Left;
                case TextAlignment.Center:
                    return HorizontalAlignment.Center;
                case TextAlignment.End:
                    return HorizontalAlignment.Right;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private VerticalAlignment ToVerticalAlignment(TextAlignment verticalAlignment)
        {
            switch (verticalAlignment)
            {
                case TextAlignment.Start:
                    return VerticalAlignment.Top;
                case TextAlignment.Center:
                    return VerticalAlignment.Center;
                case TextAlignment.End:
                    return VerticalAlignment.Bottom;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TextureFont GetTextureFont(in FontInfo fi, out float scale)
        {
            // TODO font style

            var bestSize = -1f;
            TextureFont tf = null;

            // find the closest bigger size in the atlas for the font
            // or the closest smaller size if no bigger size exists
            foreach (var candidate in _textureFonts)
            {
                if (!candidate.Font.Name.Equals(fi.Name))
                    continue;

                if ((bestSize < fi.Size && bestSize < candidate.Font.Size) ||
                    (bestSize > fi.Size && candidate.Font.Size > fi.Size && candidate.Font.Size < bestSize))
                {
                    bestSize = candidate.Font.Size;
                    tf = candidate;
                }
            }

            scale = tf == null ? 0 : fi.Size / tf.Font.Size;
            return tf;
        }
    }
}
