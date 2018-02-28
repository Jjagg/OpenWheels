using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels.Rendering
{
    public static class Extensions
    {
        public static bool CountLessThan<T>(this IEnumerable<T> en, int count)
        {
            using (var e = en.GetEnumerator())
            {
                while (count > 0)
                {
                    if (!e.MoveNext())
                        return true;
                    count--;
                }
            }
            return false;
        }

        public static IEnumerable<T> Yield<T>(T item)
        {
            yield return item;
        }

        public static IEnumerable<T> Yield<T>(T item1, T item2)
        {
            yield return item1;
            yield return item2;
        }

        public static IEnumerable<T> Yield<T>(T item1, T item2, T item3)
        {
            yield return item1;
            yield return item2;
            yield return item3;
        }

        public static Vector2 TopLeft(this RectangleF rect)
        {
            return new Vector2(rect.Left, rect.Top);
        }

        public static Vector2 TopRight(this RectangleF rect)
        {
            return new Vector2(rect.Right, rect.Top);
        }

        public static Vector2 BottomRight(this RectangleF rect)
        {
            return new Vector2(rect.Right, rect.Bottom);
        }

        public static Vector2 BottomLeft(this RectangleF rect)
        {
            return new Vector2(rect.Left, rect.Bottom);
        }
    }
}
