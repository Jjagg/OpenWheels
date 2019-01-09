using System.Numerics;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Options for laying out text.
    /// </summary>
    public struct TextLayoutOptions
    {
        /// <summary>
        /// Position to render the text at. Where the text is rendered relative to this
        /// point is determined by <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Point size to render the font at.
        /// </summary>
        public float Size;

        /// <summary>
        /// Alignment mode for text along the horizontal axis.
        /// <summary>
        public TextAlignment HorizontalAlignment;

        /// <summary>
        /// Alignment mode for text along the vertical axis.
        /// <summary>
        public TextAlignment VerticalAlignment;

        /// <summary>
        /// Width to wrap the text at.
        /// Set to -1 to not wrap text.
        /// Defaults to -1.
        /// <summary>
        public float WrappingWidth;

        /// <summary>
        /// Width of a tab in terms of the width of spaces.
        /// I.e. setting this to 2 will make a tab take up twice as much whitespace as a space.
        /// Defaults to 4.
        /// </summary>
        public float TabWidth;

        /// <summary>
        /// Create new text layout options.
        /// </summary>
        public TextLayoutOptions(Vector2 position, float size, TextAlignment ha = TextAlignment.Start, TextAlignment va = TextAlignment.Start, float wrappingWidth = -1f, float tabWith = 4f)
        {
            Position = position;
            Size = size;
            HorizontalAlignment = ha;
            VerticalAlignment = va;
            WrappingWidth = wrappingWidth;
            TabWidth = tabWith;
        }

        /// <summary>
        /// Get the default text layout options.
        /// Use position <see cref="Vector2.Zero"/> and size 24f.
        /// Other settings are set to their default values from the constructor
        /// <see cref="TextLayoutOptions(Vector2, float, TextAlignment, TextAlignment, float, float)"/>.
        /// </summary>
        public static TextLayoutOptions Default => new TextLayoutOptions(Vector2.Zero, 24f);
    }
}
