using System.Collections.Generic;
using System.Numerics;

#if NETSTANDARD2_0
using System.Diagnostics.Contracts;
using System.ComponentModel;
using System.Runtime.Serialization;
#endif

namespace OpenWheels
{
    /// <summary>
    /// Value type representing a rectangle with float coordinates.
    /// </summary>
#if NETSTANDARD2_0
    [TypeConverter(typeof(RectangleFConverter))]
    [DataContract]
#endif
    public struct RectangleF
    {
        /// <summary>
        /// Rectangle with position and size of (0, 0).
        /// </summary>
        public static readonly RectangleF Empty = new RectangleF();

        /// <summary>
        /// Rectangle with position (0, 0) and size (1, 1)
        /// </summary>
        public static readonly RectangleF Unit = new RectangleF(0, 0, 1, 1);
        
        /// <summary>
        /// X coordinate of the top left of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly float X;

        /// <summary>
        /// Y coordinate of the top left of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly float Y;

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly float Width;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly float Height;

        /// <summary>
        /// Top of the rectangle. Equal to <see cref="Y"/>.
        /// </summary>
        public float Top => Y;

        /// <summary>
        /// Bottom of the rectangle. Equal to <code><see cref="Y"/> + <see cref="Height"/></code>.
        /// </summary>
        public float Bottom => Y + Height;

        /// <summary>
        /// Left of the rectangle. Same as <see cref="X"/>.
        /// </summary>
        public float Left => X;

        /// <summary>
        /// Right of the rectangle. Equal to <code><see cref="X"/> + <see cref="Width"/></code>.
        /// </summary>
        public float Right => X + Width;

        /// <summary>
        /// Location of the top left corner of the rectangle.
        /// </summary>
        public Vector2 TopLeft => new Vector2(Left, Top);
        
        /// <summary>
        /// Location of the top right corner of the rectangle.
        /// </summary>
        public Vector2 TopRight => new Vector2(Right, Top);

        /// <summary>
        /// Location of the bottom right corner of the rectangle.
        /// </summary>
        public Vector2 BottomRight => new Vector2(Right, Bottom);

        /// <summary>
        /// Location of the bottom left corner of the rectangle.
        /// </summary>
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        
        /// <summary>
        /// Center of the rectangle.
        /// </summary>
        public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

        /// <summary>
        /// Size of the rectangle.
        /// </summary>
        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Half of the size of the rectangle.
        /// </summary>
        public Vector2 HalfExtents => new Vector2(Width / 2, Height / 2);

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="x">X coordinate of the top left of the rectangle.</param>
        /// <param name="y">Y coordinate of the top left of the rectangle.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Create a new rectangle.
        /// </summary>
        /// <param name="pos">Coordinates of the top left point of the rectangle.</param>
        /// <param name="size">Size of the rectangle.</param>
        public RectangleF(Vector2 pos, Vector2 size)
            : this(pos.X, pos.Y, size.X, size.Y)
        {
        }

        /// <summary>
        /// Get the corners of the rectangle. Order is top left, top right, bottom right, bottom left.
        /// </summary>
        public IEnumerable<Vector2> GetPoints()
        {
            yield return TopLeft;
            yield return TopRight;
            yield return BottomRight;
            yield return BottomLeft;
        }

        /// <summary>
        /// Create a rectangle with the same center, but expanded by <paramref name="v"/> at all sides.
        /// </summary>
        /// <param name="v">Amount to inflate the rectangle at the four sides.</param>
        /// <remarks>A negative value can be passed. This create a shrinked rectangle.</remarks>
#if NETSTANDARD2_0
        [Pure]
#endif
        public RectangleF Inflate(float v)
        {
            return Inflate(v, v);
        }

        /// <summary>
        /// Create a rectangle with the same center, but expanded by <paramref name="h"/> at the horizontal sides
        /// and by <paramref name="v"/> at the vertical sides.
        /// </summary>
        /// <param name="h">Amount to inflate the rectangle at the left and right sides.</param>
        /// <param name="v">Amount to inflate the rectangle at the top and bottom sides.</param>
#if NETSTANDARD2_0
        [Pure]
#endif
        public RectangleF Inflate(float h, float v)
        {
            var halfH = h / 2;
            var halfV = v / 2;
            return new RectangleF(X - halfH, Y - halfV, Width + h, Height + v);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="tl">Top left of the rectangle.</param>
        /// <param name="br">Bottom right of the rectangle.</param>
        public static RectangleF FromExtremes(Vector2 tl, Vector2 br)
        {
            return new RectangleF(tl, br - tl);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="center">Center of the rectangle.</param>
        /// <param name="halfExtents">Half of the size of the rectangle.</param>
        public static RectangleF FromHalfExtents(Vector2 center, Vector2 halfExtents)
        {
            return new RectangleF(center - halfExtents, halfExtents * 2);
        }

        public static implicit operator RectangleF(Rectangle rect)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public bool Equals(RectangleF other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RectangleF && Equals((RectangleF) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Width.GetHashCode();
                hashCode = (hashCode * 397) ^ Height.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
        }
    }
}