using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Interface for font renderers that cache glyphs in a bitmap and
    /// can submit vertices to a batcher to render text.
    /// </summary>
    public interface IBitmapFontRenderer
    {
        /// <summary>
        /// Render the requested text to a batcher.
        /// </summary>
        /// <param name="batcher">Batcher to render text to.</param>
        /// <param name="font">Font to use.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="scale">Scaling factor to render text at.</param>
        /// <param name="tlo">Text layout options.</param>
        /// <param name="color">Color of the text.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="batcher"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="font"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
        void RenderText(Batcher batcher, TextureFont font, string text, float scale, in TextLayoutOptions tlo, Color color);
    }

    /// <summary>
    /// Dummy implementation of <see cref="IBitmapFontRenderer"/> that does not do anything.
    /// </summary>
    public class NullBitmapFontRenderer : IBitmapFontRenderer
    {
        /// <summary>
        /// The singleton instance of the <see cref="NullRenderer" />.
        /// </summary>
        public static NullBitmapFontRenderer Instance { get; } = new NullBitmapFontRenderer();
        private NullBitmapFontRenderer() { }
        public void RenderText(Batcher batcher, TextureFont font, string text, float scale, in TextLayoutOptions tlo, Color color) { }
    }
}
