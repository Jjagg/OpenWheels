using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels.GameTools.Rendering
{
    public interface IRenderer
    {
        int GetTexture(string name);

        int GetFont(string name);

        Vector2 GetTextSize(string text, int font);

        Vector2 GetTextureSize(int texture);

        Rectangle GetViewport();

        void BeginRender();
        void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData);
        void EndRender();
    }

    /// <summary>
    /// A renderer implementation that does nothing.
    /// </summary>
    public sealed class NullRenderer : IRenderer
    {
        public int GetTexture(string name) => -1;
        public int GetFont(string name) => -1;
        public Vector2 GetTextSize(string text, int font) => Vector2.Zero;
        public Vector2 GetTextureSize(int texture) => Vector2.Zero;
        public Rectangle GetViewport() => Rectangle.Empty;
        public void BeginRender() { }
        public void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData) { }
        public void EndRender() { }
    }

    /// <summary>
    /// A renderer implementation that stores a set amount of batches and optionally prints out debugging information.
    /// </summary>
    public sealed class DebugRenderer : IRenderer
    {
        public int GetTexture(string name)
        {
            throw new System.NotImplementedException();
        }

        public int GetFont(string name)
        {
            throw new System.NotImplementedException();
        }

        public Vector2 GetTextSize(string text, int font)
        {
            throw new System.NotImplementedException();
        }

        public Vector2 GetTextureSize(int texture)
        {
            throw new System.NotImplementedException();
        }

        public Rectangle GetViewport()
        {
            throw new System.NotImplementedException();
        }

        public void BeginRender()
        {
            throw new System.NotImplementedException();
        }

        public void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData)
        {
            throw new System.NotImplementedException();
        }

        public void EndRender()
        {
            throw new System.NotImplementedException();
        }
    }
}