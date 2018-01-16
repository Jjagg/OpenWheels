#if NETSTANDARD2_0
using System.Diagnostics.Contracts;
using System.ComponentModel;
using System.Runtime.Serialization;
#endif

namespace OpenWheels
{
    /// <summary>
    /// An immutable value type representing a point with an integer x and y coordinate.
    /// </summary>
#if NETSTANDARD2_0
    [TypeConverter(typeof(Point2Converter))]
    [DataContract]
#endif
    public struct Size
    {
        /// <summary>
        /// A Size of (0, 0).
        /// </summary>
        public static readonly Size Empty = new Size();

        /// <summary>
        /// Width of the size.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Width;

        /// <summary>
        /// Height of the size.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Height;

        /// <summary>
        /// Create a <see cref="Size"/> with the given Width and Height.
        /// </summary>
        /// <param name="width">Width value.</param>
        /// <param name="height">Height value.</param>
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Component-wise add two sizes.
        /// </summary>
        /// <param name="p1">First size.</param>
        /// <param name="p2">Second size.</param>
        /// <returns><code>new Size(p1.Width + p2.Width, p1.Height + p2.Height)</code></returns>
        public static Size operator +(Size p1, Size p2)
        {
            return new Size(p1.Width + p2.Width, p1.Height + p2.Height);
        }

        /// <summary>
        /// Component-wise subtract one point from another.
        /// </summary>
        /// <param name="p1">Point to subtract from.</param>
        /// <param name="p2">Point to subtract.</param>
        /// <returns><code>new Size(p1.Width - p2.Width, p1.Height - p2.Height)</code></returns>
        public static Size operator -(Size p1, Size p2)
        {
            return new Size(p1.Width - p2.Width, p1.Height - p2.Height);
        }

        /// <summary>
        /// Multiply a points components by a scalar value.
        /// </summary>
        /// <param name="s">Factor to scale by.</param>
        /// <param name="p">Point to scale.</param>
        /// <returns><code>new Size(s * p.Width, s * p.Height)</code></returns>
        public static Size operator *(int s, Size p)
        {
            return new Size(s * p.Width, s * p.Height);
        }

        /// <summary>
        /// Multiply a points components by a scalar value.
        /// </summary>
        /// <param name="p">Point to scale.</param>
        /// <param name="s">Factor to scale by.</param>
        /// <returns><code>new Size(s * p.Width, s * p.Height)</code></returns>
        public static Size operator *(Size p,int s)
        {
            return new Size(s * p.Width, s * p.Height);
        }

        public void Deconstruct(out int x, out int y)
        {
            x = Width;
            y = Height;
        }

#if NETSTANDARD2_0
        public static implicit operator Size((int x, int y) t)
        {
            return new Size(t.x, t.y);
        }
#endif

        /// <summary>
        /// Copy this point, but replace the x value.
        /// </summary>
        /// <param name="x">Width value for the new point.</param>
#if NETSTANDARD2_0
        [Pure]
#endif
        public Size WithWidth(int x)
        {
            return new Size(x, Height);
        }

        /// <summary>
        /// Copy this point, but replace the y value.
        /// </summary>
        /// <param name="y">Height value for the new point.</param>
#if NETSTANDARD2_0
        [Pure]
#endif
        public Size WithHeight(int y)
        {
            return new Size(Width, y);
        }

        public bool Equals(Size other)
        {
            return Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Size) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Size left, Size right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Size left, Size right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"{Width:g5} {Height:g5}";
        }
    }
}