using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Value type representing a rectangle with float coordinates.
    /// </summary>
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
        public readonly float X;

        /// <summary>
        /// Y coordinate of the top left of the rectangle.
        /// </summary>
        public readonly float Y;

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public readonly float Width;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
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
        /// The size of the rectangle.
        /// </summary>
        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Half of the size of the rectangle.
        /// </summary>
        public Vector2 HalfExtents => new Vector2(Width / 2, Height / 2);

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="x">The x coordinate of the top left of the rectangle.</param>
        /// <param name="y">The y coordinate of the top left of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
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
        /// <param name="size">The size of the rectangle.</param>
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
        /// Create a rectangle with the same center, but <paramref name="v"/> larger than this rectangle.
        /// </summary>
        /// <param name="v">Amount to inflate the rectangle.</param>
        public RectangleF Inflate(float v)
        {
            return Inflate(v, v);
        }


        /// <summary>
        /// Create a rectangle with the same center, but expanded by <paramref name="h"/> in the horizontal
        /// direction and <paramref name="v"/> in the vertical direction.
        /// </summary>
        /// <param name="h">Amount to inflate the rectangle in the horizontal direction.</param>
        /// <param name="v">Amount to inflate the rectangle in the vertical direction.</param>
        public RectangleF Inflate(float h, float v)
        {
            return new RectangleF(X - h, Y - v, Width + h * 2, Height + v * 2);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="tl">The top left of the rectangle.</param>
        /// <param name="br">The bottom right of the rectangle.</param>
        public static RectangleF FromExtremes(Vector2 tl, Vector2 br)
        {
            return new RectangleF(tl, br - tl);
        }

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="center">The center of the rectangle.</param>
        /// <param name="halfExtents">Half of the size of the rectangle.</param>
        public static RectangleF FromHalfExtents(Vector2 center, Vector2 halfExtents)
        {
            return new RectangleF(center - halfExtents, halfExtents * 2);
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