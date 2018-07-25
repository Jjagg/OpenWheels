using System;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Text;

namespace OpenWheels.Fonts.ImageSharp
{
    /// <summary>
    /// Extensions to render a <see cref="FontAtlas"/> to an <see cref="Image"/>.
    /// </summary>
    public static class FontAtlasExtensions
    {
        /// <summary>
        /// Render a <see cref="FontAtlas"/> to an <see cref="Image"/> with only an alpha channel.
        /// </summary>
        /// <param name="fa">Font atlas to render.</param>
        /// <returns>The created image.</returns>
        public static Image<Alpha8> RenderAlphaImage(this FontAtlas fa)
        {
            return RenderImage(fa, new Alpha8(1f));
        }

        /// <summary>
        /// Render a <see cref="FontAtlas"/> to an <see cref="Image"/> with 8-bit RGBA channels.
        /// </summary>
        /// <param name="fa">Font atlas to render.</param>
        /// <returns>The created image.</returns>
        public static Image<Rgba32> RenderImage(this FontAtlas fa)
        {
            return RenderImage(fa, Rgba32.White);
        }

        private static Image<T> RenderImage<T>(this FontAtlas fa, T color) where T : struct, IPixel<T>
        {
            // TODO we need the extra pixels here because ImageSharp renders out of bounds and crashes.
            var img = new Image<T>(Configuration.Default, fa.Width + 2, fa.Height);
            var tgo = new TextGraphicsOptions(true);
            tgo.HorizontalAlignment = HorizontalAlignment.Left;
            tgo.VerticalAlignment = VerticalAlignment.Top;

            for (var i = 0; i < fa.MapCount; i++)
            {
                var gm = fa[i];
                var font = gm.Font;

                foreach (var gd in gm)
                {
                    // TODO lower level control to render single character? This API sucks balls for FA layout + rendering
                    var charStr = char.ConvertFromUtf32(gd.Character);
                    var pos = (Vector2) gd.Bounds.TopLeft;
                    var ro = new RendererOptions(font);
                    TextMeasurer.TryMeasureCharacterBounds(charStr, ro, out var cbs);
                    var cb = cbs[0];
                    img.Mutate(c => c.DrawText(tgo, charStr, font, color, pos - (Vector2) cb.Bounds.Location));
                }
            }

            return img;
        }
    }
}