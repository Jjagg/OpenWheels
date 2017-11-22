using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels.GameTools
{
    public struct RectangleF
    {
        public static readonly RectangleF Empty = new RectangleF();
        public static readonly RectangleF Unit = new RectangleF(0, 0, 1, 1);
        
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Top => Y;
        public float Bottom => Y + Height;
        public float Left => X;
        public float Right => X + Width;

        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);
        public Vector2 BottomRight => new Vector2(Right, Bottom);
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        
        public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);
        public Vector2 Size => new Vector2(Width, Height);
        public Vector2 HalfExtents => new Vector2(Width / 2, Height / 2);

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        private RectangleF(Vector2 pos, Vector2 size)
            : this(pos.X, pos.Y, size.X, size.Y)
        {
        }

        public IEnumerable<Vector2> GetPoints()
        {
            yield return TopLeft;
            yield return TopRight;
            yield return BottomRight;
            yield return BottomLeft;
        }

        public RectangleF Inflate(float v)
        {
            return Inflate(v, v);
        }

        public RectangleF Inflate(float h, float v)
        {
            return new RectangleF(X - h, Y - v, Width + h * 2, Height + v * 2);
        }

        public static RectangleF FromExtremes(Vector2 tl, Vector2 br)
        {
            return new RectangleF(tl, br - tl);
        }

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