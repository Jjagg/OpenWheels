using System;
using System.Numerics;
using OpenWheels.Rendering;

namespace OpenWheels.Plotting
{
    public static class BatcherExtensions
    {
        public static void DrawBarUp(this Batcher batcher, float x, float bottom, float height, Color color, float thickness)
        {
            var t = new Vector2(x, bottom - height);
            var b = new Vector2(x, bottom);
            batcher.DrawLine(t, b, color, thickness);
        }

        public static void DrawBarDown(this Batcher batcher, float x, float top, float height, Color color, float thickness)
        {
            var t = new Vector2(x, top);
            var b = new Vector2(x, top + height);
            batcher.DrawLine(t, b, color, thickness);
        }

        public static void DrawBarRight(this Batcher batcher, float y, float left, float width, Color color, float thickness)
        {
            var l = new Vector2(left, y);
            var r = new Vector2(left + width, y);
            batcher.DrawLine(l, r, color, thickness);
        }

        public static void DrawBarLeft(this Batcher batcher, float y, float right, float width, Color color, float thickness)
        {
            var l = new Vector2(right - width, y);
            var r = new Vector2(right, y);
            batcher.DrawLine(l, r, color, thickness);
        }
    }
}
