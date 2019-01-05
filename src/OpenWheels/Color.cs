using System;
using System.Runtime.InteropServices;
#if NETSTANDARD2_0
using System.ComponentModel;
using System.Runtime.Serialization;
#endif

namespace OpenWheels
{
    /// <summary>
    /// Value type representing an ABGR color with 1 byte per channel (values in [0-255]).
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
#if NETSTANDARD2_0
    [TypeConverter(typeof(ColorConverter))]
    [DataContract]
#endif
    public partial struct Color
    {
        /// <summary>
        /// Red channel value of the color.
        /// </summary>
        [FieldOffset(0)]
        public readonly byte R;

        /// <summary>
        /// Green channel value of the color.
        /// </summary>
        [FieldOffset(1)]
        public readonly byte G;

        /// <summary>
        /// Blue channel value of the color.
        /// </summary>
        [FieldOffset(2)]
        public readonly byte B;

        /// <summary>
        /// Alpha channel value of the color.
        /// </summary>
        [FieldOffset(3)]
        public readonly byte A;
        
        /// <summary>
        /// Value of the 4 channels packed together. Values are ordered ABGR with A as the most significant byte.
        /// </summary>
        [FieldOffset(0)]
#if NETSTANDARD2_0
        [DataMember]
#endif
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
        /// Create a new color with the given r, g, b and a values.
        /// Values should be in the range [0, 1].
        /// </summary>
        /// <param name="r">Red channel value.</param>
        /// <param name="g">Green channel value.</param>
        /// <param name="b">Blue channel value.</param>
        /// <param name="a">Alpha channel value. Defaults to opaque (1f).</param>
        public Color(float r, float g, float b, float a = 1f)
            : this((int) (r * 255), (int) (g * 255), (int) (b * 255), (int) (a * 255))
        {
        }

        /// <summary>
        /// Create a new color with the given r, g, b and a values.
        /// R, g and b values should be in the range [0, 255], alpha should be in [0,1].
        /// </summary>
        /// <param name="r">Red channel value in range [0, 255].</param>
        /// <param name="g">Green channel value in range [0, 255].</param>
        /// <param name="b">Blue channel value in range [0, 255].</param>
        /// <param name="a">Alpha channel value in range [0, 1].</param>
        public Color(byte r, byte g, byte b, float a)
        {
            Packed = 0;
            R = r;
            G = g;
            B = b;
            A = (byte) MathHelper.Clamp(a * 255, 0, 255);
        }

        /// <summary>
        /// Create a new color with the rgb values from the given color and a new alpha channel value.
        /// Alpha should be in [0,1].
        /// </summary>
        /// <param name="rgb">Color to take the rgb values from.</param>
        /// <param name="a">Alpha channel value in range [0, 1].</param>
        public Color(Color rgb, float a)
        {
            R = G = B = 0;
            Packed = rgb.Packed;
            A = (byte) MathHelper.Clamp(a * 255, 0, 255);
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

        /// <summary>
        /// Create a color that matches the given RGBA hex value where R is the most significant byte.
        /// Note that this is reversed from the packed value representation of this color.
        /// </summary>
        public static Color FromHexRgba(uint rgba)
        {
            var packed = ((rgba & 0xff) << 24) | (((rgba >> 8) & 0xff) << 16) | (((rgba >> 16) & 0xff) << 8) | ((rgba >> 24) & 0xff); 
            return new Color(packed);
        }

        /// <summary>
        /// Create an opaque color that matches the given RGB hex value where R is the most significant byte.
        /// Note that this is reversed from the packed value representation of this color.
        /// </summary>
        public static Color FromHexRgb(uint rgb)
        {
            var packed = 0xff000000 | ((rgb & 0xff) << 16) | (((rgb >> 8) & 0xff) << 8) | ((rgb >> 16) & 0xff); 
            return new Color(packed);
        }

        /// <summary>
        /// Parse a <see cref="Color"/> from a <see cref="string"/>.
        /// Expected format is 3 or 4 byte values separated by any number of ',' or ' '.
        /// </summary>
        /// <remarks>
        /// Values are parsed as R, G, B, A in that order. Alpha is optional.
        /// All of the following are valid:
        /// - "212, 120, 27"
        /// - "100, 232, 242, 250"
        /// - "0 255 255 255"
        /// - "   0, , ,, 8, ,  , 210,    255"
        /// </remarks>
        /// <param name="str">String to parse.</param>
        /// <returns>The parsed color.</returns>
        /// <exception cref="FormatException">If the given string does not match the expected format.</exception>
        public static Color Parse(string str)
        {
            var s = str.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length < 3)
                throw new FormatException("Expected at least 3 parts separated by either ',' or ' ' (or both).");
            if (s.Length > 4)
                throw new FormatException("Expected at most 4 parts separated by either ',' or ' ' (or both).");
            var r = byte.Parse(s[0]);
            var g = byte.Parse(s[1]);
            var b = byte.Parse(s[2]);
            var a = s.Length == 3 ? (byte) 255 : byte.Parse(s[3]);
            return new Color(r, g, b, a);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var aStr = A == 255 ? string.Empty : ", " + A;
            return $"{R}, {G}, {B}{aStr}";
        }

        public static bool operator !=(Color left, Color right) => left.Packed != right.Packed;

        public static bool operator ==(Color left, Color right) => left.Packed == right.Packed;

        public bool Equals(Color other) => Packed == other.Packed;

        public override int GetHashCode() => (int)Packed;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Color other && Equals(other);
        }
    }
}
