using System.Runtime.InteropServices;

namespace OpenWheels
{
    /// <summary>
    /// Value type representing an ABGR color with 1 byte per channel (values in [0-255]).
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Color
    {
        /// <summary>
        /// Red channel value of the color.
        /// </summary>
        [FieldOffset(3)]
        public readonly byte R;

        /// <summary>
        /// Green channel value of the color.
        /// </summary>
        [FieldOffset(2)]
        public readonly byte G;

        /// <summary>
        /// Blue channel value of the color.
        /// </summary>
        [FieldOffset(1)]
        public readonly byte B;

        /// <summary>
        /// Alpha channel value of the color.
        /// </summary>
        [FieldOffset(0)]
        public readonly byte A;
        
        /// <summary>
        /// Value of the 4 channels packed together. Values are ordered ABGR with A as the most significant byte.
        /// </summary>
        [FieldOffset(0)]
        public readonly uint Packed;

        /// <summary>
        /// Create a new color with the given r, g, b and a values.
        /// Values should be in the range [0, 255].
        /// </summary>
        /// <param name="r">Red channel value.</param>
        /// <param name="g">Green channel value.</param>
        /// <param name="b">Blue channel value.</param>
        /// <param name="a">Alpha channel value. Defaults to opaque (255).</param>
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            Packed = 0;
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        /// <summary>
        /// Create a new color with the given r, g, b and a values.
        /// Values should be in the range [0, 255].
        /// </summary>
        /// <param name="r">Red channel value.</param>
        /// <param name="g">Green channel value.</param>
        /// <param name="b">Blue channel value.</param>
        /// <param name="a">Alpha channel value. Defaults to opaque (255).</param>
        public Color(int r, int g, int b, int a = 255)
        {
            Packed = 0;
            R = (byte) MathHelper.Clamp(r, 0, 255);
            G = (byte) MathHelper.Clamp(g, 0, 255);
            B = (byte) MathHelper.Clamp(b, 0, 255);
            A = (byte) MathHelper.Clamp(a, 0, 255);
        }

        /// <summary>
        /// Create a new color with the given packed value.
        /// The packed value is ordered ABGR with A at the most significant byte.
        /// </summary>
        /// <param name="packed">The packed value of this color.</param>
        public Color(uint packed)
        {
            R = G = B = A = 0;
            Packed = packed;
        }

        public static readonly Color White = new Color(0xffffffff);
        public static readonly Color Black = new Color(0xff000000);
        public static readonly Color Red = new Color(0xff0000ff);
        public static readonly Color Green = new Color(0xff00ff00);
        public static readonly Color Blue = new Color(0xffff0000);
    }
}