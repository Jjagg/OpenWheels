using System.Collections.Generic;

#if NETSTANDARD2_0
using System.Diagnostics.Contracts;
using System.ComponentModel;
using System.Runtime.Serialization;
#endif

namespace OpenWheels
{
    /// <summary>
    /// Value type representing a rectangle with integer coordinates.
    /// </summary>
#if NETSTANDARD2_0
    [TypeConverter(typeof(RectangleConverter))]
    [DataContract]
#endif
    public struct Rectangle
    {
        /// <summary>
        /// Rectangle with position and size of (0, 0).
        /// </summary>
        public static readonly Rectangle Empty = new Rectangle();

        /// <summary>
        /// Rectangle with position (0, 0) and size (1, 1)
        /// </summary>
        public static readonly Rectangle Unit = new Rectangle(0, 0, 1, 1);
        
        /// <summary>
        /// X coordinate of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int X;
 
        /// <summary>
        /// Y coordinate of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Y;

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Width;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
#if NETSTANDARD2_0
        [DataMember]
#endif
        public readonly int Height;

        /// <summary>
        /// Top of the rectangle. Same as <see cref="Y"/>.
        /// </summary>
        public int Top => Y;

        /// <summary>
        /// Bottom of the rectangle. Equal to <code><see cref="Y"/> + <see cref="Height"/></code>.
        /// </summary>
        public int Bottom => Y + Height;

        /// <summary>
        /// Left of the rectangle. Same as <see cref="X"/>.
        /// </summary>
        public int Left => X;

        /// <summary>
        /// Right of the rectangle. Equal to <code><see cref="X"/> + <see cref="Width"/></code>.
        /// </summary>
        public int Right => X + Width;

        /// <summary>
        /// Location of the top left corner of the rectangle.
        /// </summary>
        public Point2 TopLeft => new Point2(Left, Top);
        
        /// <summary>
        /// Location of the top right corner of the rectangle.
        /// </summary>
        public Point2 TopRight => new Point2(Right, Top);

        /// <summary>
        /// Location of the bottom right corner of the rectangle.
        /// </summary>
        public Point2 BottomRight => new Point2(Right, Bottom);

        /// <summary>
        /// Location of the bottom left corner of the rectangle.
        /// </summary>
        public Point2 BottomLeft => new Point2(Left, Bottom);
        
        /// <summary>
        /// Center of the rectangle.
        /// </summary>
        public Point2 Center => new Point2(X + Width / 2, Y + Height / 2);

        /// <summary>
        /// Size of the rectangle.
        /// </summary>
        public Point2 Size => new Point2(Width, Height);

        /// <summary>
        /// Half of the size of the rectangle.
        /// </summary>
        public Point2 HalfExtents => new Point2(Width / 2, Height / 2);

        /// <summary>
        /// Aspect ratio of the rectangle. Equal to <see cref="Width"/> / <see cref="Height"/>.
        /// </summary>
        public float AspectRatio => Width / (float) Height;

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="x">X coordinate of the rectangle.</param>
        /// <param name="y">Y coordinate of the rectangle.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        public Rectangle(int x, int y, int width, int height)
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
        public Rectangle(Point2 pos, Point2 size)
            : this(pos.X, pos.Y, size.X, size.Y)
        {
        }

        /// <summary>
        /// Create a new rectangle.
        /// </summary>
        /// <param name="pos">Coordinates of the top left point of the rectangle.</param>
        /// <param name="size">Size of the rectangle.</param>
        public Rectangle(Point2 pos, Size size)
            : this(pos.X, pos.Y, size.Width, size.Height)
        {
        }

        /// <summary>
        /// Get the corners of the rectangle. Order is top left, top right, bottom right, bottom left.
        /// </summary>
        public IEnumerable<Point2> GetPoints()
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
        public Rectangle Inflate(int v)
        {
            return Inflate(v, v);
        }

        /// <summary>
        /// Create a rectangle with the same center, but expanded by <paramref name="h"/> at the horizontal sides
        /// and by <paramref name="v"/> at the vertical sides.
        /// </summary>
        /// <param name="h">Amount to inflate the rectangle at the left and right sides.</param>
        /// <param name="v">Amount to inflate the rectangle at the top and bottom sides.</param>
        /// <remarks>A negative value can be passed. This create a shrinked rectangle.</remarks>
#if NETSTANDARD2_0
        [Pure]
#endif
        public Rectangle Inflate(int h, int v)
        {
            var halfH = h / 2;
            var halfV = v / 2;
            return new Rectangle(X - halfH, Y - halfV, Width + h, Height + v);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="left">Left of the rectangle.</param>
        /// <param name="top">Top of the rectangle.</param>
        /// <param name="bottom">Bottom of the rectangle.</param>
        /// <param name="right">Right of the rectangle.</param>
        public static Rectangle FromExtremes(int left, int top, int bottom, int right)
        {
            return FromExtremes(new Point2(left, top), new Point2(bottom, right));
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="tl">Top left of the rectangle.</param>
        /// <param name="br">Bottom right of the rectangle.</param>
        public static Rectangle FromExtremes(Point2 tl, Point2 br)
        {
            return new Rectangle(tl, br - tl);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="center">Center of the rectangle.</param>
        /// <param name="halfExtents">Half of the size of the rectangle.</param>
        public static Rectangle FromHalfExtents(Point2 center, Point2 halfExtents)
        {
            return new Rectangle(center - halfExtents, halfExtents * 2);
        }

        public void Deconstruct(out int x, out int y, out int width, out int height)
        {
            x = X;
            y = Y;
            width = Width;
            height = Height;
        }

        public void Deconstruct(out Point2 position, out Point2 size)
        {
            position = TopLeft;
            size = Size;
        }

        public bool Equals(Rectangle other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Rectangle && Equals((Rectangle) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                return hashCode;
            }
        }

        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}";
        }
    }
}
