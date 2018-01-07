using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenWheels.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Color Color;
        public readonly Vector2 Uv;

        public Vertex(Vector3 position, Vector2 uv, Color color)
        {
            Position = position;
            Uv = uv;
            Color = color;
        }
    }
}