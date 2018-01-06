namespace OpenWheels.Rendering
{
    /// <summary>
    /// Value type representing a rectangle with integer coordinates.
    /// </summary>
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
        public readonly int X;
 
        /// <summary>
        /// Y coordinate of the rectangle.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Height of the rectangle.
        /// </summary>
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