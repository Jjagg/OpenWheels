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
        /// <param name="fontInfo">Font and size to use.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="tlo">Text layout options.</param>
        /// <param name="color">Color of the text.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="batcher"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///   If the passed font is not prerendered to the bitmap and the implementation
        ///   does not support dynamic font atlas creation.
        /// </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
        void RenderText(Batcher batcher, in FontInfo fontInfo, string text, in TextLayoutOptions tlo, Color color);
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
        public void RenderText(Batcher batcher, in FontInfo fontInfo, string text, in TextLayoutOptions tlo, Color color) { }
    }
}
