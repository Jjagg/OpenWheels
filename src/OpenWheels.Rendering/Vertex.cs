using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenWheels.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Color Color;
        public Vector2 Uv;

        public Vertex(Vector3 position, Vector2 uv, Color color)
        {
            Position = position;
            Uv = uv;
            Color = color;
        }

        public const int SizeInBytes = 24;
    }
}