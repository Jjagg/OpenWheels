using System.Numerics;

namespace OpenWheels.Rendering
{
    public interface IRenderer
    {
        /// <summary>
        /// Get a unique identifier for a named texture.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture identifier.</returns>
        int GetTexture(string name);

        /// <summary>
        /// Get a unique identifier for a named font.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font identifier.</returns>
        int GetFont(string name);

        /// <summary>
        /// Get the size of a texture.
        /// </summary>
        /// <param name="texture">The identifier of the texture.</param>
        /// <returns>The size of the texture in pixels.</returns>
        Point2 GetTextureSize(int texture);

        /// <summary>
        /// Get the size of some text.
        /// </summary>
        /// <param name="text">Text to measure.</param>
        /// <param name="font">Font of the text.</param>
        /// <returns>The size of the text if rendered in the given font.</returns>
        Vector2 GetTextSize(string text, int font);

        /// <summary>
        /// Get the current viewport of the renderer.
        /// </summary>
        /// <returns>The bounds of the viewport.</returns>
        Rectangle GetViewport();

        /// <summary>
        /// Called right before calls to <see cref="DrawBatch"/> to indicate that batches will be drawn.
        /// </summary>
        void BeginRender();

        /// <summary>
        /// Draw a batch of vertices.
        /// </summary>
        void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData);

        /// <summary>
        /// Called after a set of batches is drawn.
        /// </summary>
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
        public Point2 GetTextureSize(int texture) => Point2.Zero;
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

        public Point2 GetTextureSize(int texture)
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
