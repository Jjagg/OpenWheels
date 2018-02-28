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
    public struct Point2
    {
        /// <summary>
        /// The Point2 at (0, 0).
        /// </summary>
        public static readonly Point2 Zero = new Point2();

        /// <summary>
        /// X value of the point.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int X;

        /// <summary>
        /// Y value of the point.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Y;

        /// <summary>
        /// Create a <see cref="Point2"/> with the given X and Y values.
        /// </summary>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public Point2(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Component-wise add two points.
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <returns><code>new Point2(p1.X + p2.X, p1.Y + p2.Y)</code></returns>
        public static Point2 operator +(Point2 p1, Point2 p2)
        {
            return new Point2(p1.X + p2.X, p1.Y + p2.Y);
        }

        /// <summary>
        /// Component-wise subtract one point from another.
        /// </summary>
        /// <param name="p1">Point to subtract from.</param>
        /// <param name="p2">Point to subtract.</param>
        /// <returns><code>new Point2(p1.X - p2.X, p1.Y - p2.Y)</code></returns>
        public static Point2 operator -(Point2 p1, Point2 p2)
        {
            return new Point2(p1.X - p2.X, p1.Y - p2.Y);
        }

        /// <summary>
        /// Multiply a points components by a scalar value.
        /// </summary>
        /// <param name="s">Factor to scale by.</param>
        /// <param name="p">Point to scale.</param>
        /// <returns><code>new Point2(s * p.X, s * p.Y)</code></returns>
        public static Point2 operator *(int s, Point2 p)
        {
            return new Point2(s * p.X, s * p.Y);
        }

        /// <summary>
        /// Multiply a points components by a scalar value.
        /// </summary>
        /// <param name="p">Point to scale.</param>
        /// <param name="s">Factor to scale by.</param>
        /// <returns><code>new Point2(s * p.X, s * p.Y)</code></returns>
        public static Point2 operator *(Point2 p,int s)
        {
            return new Point2(s * p.X, s * p.Y);
        }

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

#if NETSTANDARD2_0
        public static implicit operator Point2((int x, int y) t)
        {
            return new Point2(t.x, t.y);
        }
#endif

        /// <summary>
        /// Copy this point, but replace the x value.
        /// </summary>
        /// <param name="x">X value for the new point.</param>
#if NETSTANDARD2_0
        [Pure]
#endif
        public Point2 WithX(int x)
        {
            return new Point2(x, Y);
        }

        /// <summary>
        /// Copy this point, but replace the y value.
        /// </summary>
        /// <param name="y">Y value for the new point.</param>
#if NETSTANDARD2_0
        [Pure]
#endif
        public Point2 WithY(int y)
        {
            return new Point2(X, y);
        }

        public bool Equals(Point2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Point2) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Point2 left, Point2 right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Point2 left, Point2 right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"{X:g5} {Y:g5}";
        }
    }
}
