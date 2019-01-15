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
        /// <summary>
        /// Create a new FontsTextRenderer.
        /// </summary>
        public FontsTextRenderer()
        {
        }

        /// <inheritdoc/>
        public RectangleF RenderText(Batcher batcher, TextureFont textureFont, ReadOnlySpan<char> text, float scale, in TextLayoutOptions tlo, Color color)
        {
            if (textureFont == null)
                throw new ArgumentNullException(nameof(textureFont));

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
            TextMeasurer.TryMeasureCharacterBounds(text, ro, out var gms);

            if (gms.Length == 0)
                return default;

            var firstBounds = gms[0].Bounds;
            var bounding = new RectangleF(firstBounds.X, firstBounds.Y, firstBounds.Width, firstBounds.Height);

            foreach (var gm in gms)
            {
                // We need to ceil the size because the UV coordinates are computed from rounded pixel coordinates.
                // Not sure if this is optimal when scaling is used.
                var rect = new RectangleF(gm.Bounds.X, gm.Bounds.Y, (int) Math.Ceiling(gm.Bounds.Width), (int) Math.Ceiling(gm.Bounds.Height));
                var gd = textureFont.GetGlyphData(gm.Codepoint);

                batcher.SetUvSprite(textureFont.Texture, gd.Bounds);
                batcher.FillRect(rect, color);

                if (gm.Bounds.Left < bounding.Left)
                    bounding.Left = gm.Bounds.Left;
                if (gm.Bounds.Top < bounding.Top)
                    bounding.Top = gm.Bounds.Top;
                if (gm.Bounds.Right > bounding.Right)
                    bounding.Right = gm.Bounds.Right;
                if (gm.Bounds.Bottom > bounding.Bottom)
                    bounding.Bottom = gm.Bounds.Bottom;
            }

            return bounding;
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
    }
}
