namespace OpenWheels.GameTools
{
    /// <summary>
    /// An immutable value type representing a point with an integer x and y coordinate.
    /// </summary>
    public struct Point2
    {
        /// <summary>
        /// The Point2 at (0, 0).
        /// </summary>
        public static readonly Point2 Zero = new Point2();

        /// <summary>
        /// The x value of the point.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// The y value of the point.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Create a <see cref="Point2"/> with the given X and Y values.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public Point2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point2 operator +(Point2 p1, Point2 p2)
        {
            return new Point2(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point2 operator *(int s, Point2 p)
        {
            return new Point2(s * p.X, s * p.Y);
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
