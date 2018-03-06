using System.IO;
using System.Numerics;

namespace OpenWheels.Rendering
{
    public interface IRenderer
    {
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
        /// <returns>Bounds of the viewport.</returns>
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
        public Point2 GetTextureSize(int texture) => Point2.Zero;
        public Vector2 GetTextSize(string text, int font) => Vector2.Zero;
        public Rectangle GetViewport() => Rectangle.Empty;
        public void BeginRender() { }
        public void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData) { }
        public void EndRender() { }
    }

    /// <summary>
    /// A renderer implementation that stores can write out method calls and
    /// delegates calls to another renderer.
    /// </summary>
    public class TraceRenderer : IRenderer
    {
        public IRenderer DelegateRenderer { get; set; }
        public TextWriter Writer { get; set; }
        public string Prefix { get; set; }

        public TraceRenderer()
        {
            DelegateRenderer = new NullRenderer();
        }

        private void Write(string message)
        {
            if (Writer != null)
            {
                var pre = Prefix ?? string.Empty;
                Writer.WriteLine(pre + message);
            }
        }

        public Vector2 GetTextSize(string text, int font)
        {
            Write($"GetTextSize(${text}, ${font})");
            return DelegateRenderer.GetTextSize(text, font);
        }

        public Point2 GetTextureSize(int texture)
        {
            Write($"GetTextureSize(${texture})");
            return DelegateRenderer.GetTextureSize(texture);
        }

        public Rectangle GetViewport()
        {
            Write("GetViewport()");
            return DelegateRenderer.GetViewport();
        }

        public void BeginRender()
        {
            Write("BeginRender()");
            DelegateRenderer.BeginRender();
        }

        public void DrawBatch(GraphicsState state, Vertex[] vertexBuffer, int[] indexBuffer, int startIndex, int indexCount, object batchUserData)
        {
            Write($"DrawBatch({state}, {vertexBuffer}, {indexBuffer}, {startIndex}, {indexCount}, {batchUserData})");
            DelegateRenderer.DrawBatch(state, vertexBuffer, indexBuffer, startIndex, indexCount, batchUserData);
        }

        public void EndRender()
        {
            Write("EndRender()");
            DelegateRenderer.EndRender();
        }
    }
}
