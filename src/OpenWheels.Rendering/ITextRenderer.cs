using System;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Submits vertices to a batcher to render text.
    /// </summary>
    public interface ITextRenderer
    {
        /// <summary>
        /// Render the requested text to a batcher.
        /// </summary>
        /// <param name="batcher">Batcher to render text to.</param>
        /// <param name="fi">Font to use.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="tlo">Text layout options.</param>
        /// <exception cref="InvalidOperationException">
        ///   If a character from the given text has no glyph registered in the
        ///   font and no fallback character is set.
        /// </exception>
        void RenderText(Batcher batcher, in FontInfo fi, ReadOnlySpan<char> text, in TextLayoutOptions tlo);
    }
}
