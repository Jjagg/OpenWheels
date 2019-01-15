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
        public float X;

        /// <summary>
        /// Y coordinate of the top left of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public float Y;

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public float Width;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public float Height;

        /// <summary>
        /// Top of the rectangle. Equal to <see cref="Y"/>.
        /// </summary>
        public float Top
        {
            get => Y;
            set => Y = value;
        }

        /// <summary>
        /// Bottom of the rectangle. Equal to <code><see cref="Y"/> + <see cref="Height"/></code>.
        /// </summary>
        public float Bottom
        {
            get => Y + Height;
            set => Height = value - Y;
        }

        /// <summary>
        /// Left of the rectangle. Same as <see cref="X"/>.
        /// </summary>
        public float Left
        {
            get => X;
            set => X = value;
        }

        /// <summary>
        /// Right of the rectangle. Equal to <code><see cref="X"/> + <see cref="Width"/></code>.
        /// </summary>
        public float Right 
        {
            get => X + Width;
            set => Width = value - X;
        }

        /// <summary>
        /// Location of the top left corner of the rectangle.
        /// </summary>
        public Vector2 TopLeft
        {
            get => new Vector2(Left, Top);
            set { Left = value.X; Top = value.Y; }
        }
        
        /// <summary>
        /// Location of the top right corner of the rectangle.
        /// </summary>
        public Vector2 TopRight
        {
            get => new Vector2(Right, Top);
            set { Right = value.X; Top = value.Y; }
        }

        /// <summary>
        /// Location of the bottom right corner of the rectangle.
        /// </summary>
        public Vector2 BottomRight
        {
            get => new Vector2(Right, Bottom);
            set { Right = value.X; Bottom = value.Y; }
        }

        /// <summary>
        /// Location of the bottom left corner of the rectangle.
        /// </summary>
        public Vector2 BottomLeft
        {
            get => new Vector2(Left, Bottom);
            set { Left = value.X; Bottom = value.Y; }
        }
        
        /// <summary>
        /// Center of the rectangle.
        /// </summary>
        public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

        /// <summary>
        /// Size of the rectangle.
        /// </summary>
        public Vector2 Size
        {
            get => new Vector2(Width, Height);
            set { Width = value.X; Height = value.Y; }
        }

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
        /// Check if this rectangle contains a point.
        /// </summary>
        /// <param name="point">Coordinates to check for if they're inside the rectangle.</param>
        public bool Contains(Point2 point) => Left < point.X && point.X < Right && Top < point.Y && point.Y < Bottom;

        /// <summary>
        /// Check if this rectangle contains a vector.
        /// </summary>
        /// <param name="vector">Coordinates to check for if they're inside the rectangle.</param>
        public bool Contains(Vector2 vector) => Left < vector.X && vector.X < Right && Top < vector.Y && vector.Y < Bottom;

        /// <summary>
        /// Check if this rectangle contains a coordinate pair.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public bool Contains(float x, float y) => Left < x && x < Right && Top < y && y < Bottom;

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
        /// <param name="left">Left of the rectangle.</param>
        /// <param name="top">Top of the rectangle.</param>
        /// <param name="bottom">Bottom of the rectangle.</param>
        /// <param name="right">Right of the rectangle.</param>
        public static RectangleF FromExtremes(float left, float top, float bottom, float right)
        {
            return FromExtremes(new Vector2(left, top), new Vector2(bottom, right));
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

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="center">Center of the rectangle.</param>
        /// <param name="extents">Size of the rectangle.</param>
        public static RectangleF FromExtents(Vector2 center, Vector2 extents)
        {
            return new RectangleF(center - extents / 2, extents);
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
