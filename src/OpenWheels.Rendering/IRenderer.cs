using System;
using System.IO;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Interface for renderers that can be used to draw data batched by a <see cref="Batcher"/>.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Get the size of a texture.
        /// </summary>
        /// <param name="texture">The identifier of the texture.</param>
        /// <returns>The size of the texture in pixels.</returns>
        Point2 GetTextureSize(int texture);

        /// <summary>
        /// Get the current viewport of the renderer.
        /// </summary>
        /// <returns>Bounds of the viewport.</returns>
        Rectangle GetViewport();

        /// <summary>
        /// Called right before calls to <see cref="DrawBatch"/> to indicate that batches will be drawn.
        /// </summary>
        void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount);

        /// <summary>
        /// Draw a batch of vertices.
        /// </summary>
        void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData);

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
        /// <inheritdoc />
        public Point2 GetTextureSize(int texture) => Point2.Zero;
        /// <inheritdoc />
        public Rectangle GetViewport() => Rectangle.Empty;
        /// <inheritdoc />
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount) { }
        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData) { }
        /// <inheritdoc />
        public void EndRender() { }
    }

    /// <summary>
    /// A renderer implementation that can write out method calls and
    /// delegates calls to another renderer.
    /// </summary>
    public sealed class TraceRenderer : IRenderer
    {
        private IRenderer _delegateRenderer;

        /// <summary>
        /// The renderer that this renderer delegates its calls to after writing them out to the <see cref="Writer"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <c>null</c> is passed to the setter.</exception>
        public IRenderer DelegateRenderer
        {
            get => _delegateRenderer;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _delegateRenderer = value;
            }
        }

        /// <summary>
        /// The writer to write method calls to.
        /// </summary>
        public TextWriter Writer { get; set; }

        /// <summary>
        /// A function that returns the prefix for every line written with the <see cref="Writer"/>.
        /// If <c>null</c> no prefix is added.
        /// </summary>
        public Func<string> Prefix { get; set; }

        /// <summary>
        /// Create a new <see cref="TraceRenderer"/> with its <see cref="DelegateRenderer"/>
        /// set to a <see cref="NullRenderer"/>.
        /// </summary>
        public TraceRenderer()
        {
            DelegateRenderer = new NullRenderer();
        }

        private void Write(string message)
        {
            if (Writer != null)
            {
                var pre = string.Empty;
                if (Prefix != null)
                    pre = Prefix();
                Writer.WriteLine(pre + message);
            }
        }

        /// <inheritdoc />
        public Point2 GetTextureSize(int texture)
        {
            Write($"GetTextureSize(${texture})");
            return DelegateRenderer.GetTextureSize(texture);
        }

        /// <inheritdoc />
        public Rectangle GetViewport()
        {
            Write("GetViewport()");
            return DelegateRenderer.GetViewport();
        }

        /// <inheritdoc />
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount)
        {
            Write($"BeginRender(vb, ib, {vertexCount}, {indexCount})");
            DelegateRenderer.BeginRender(vertexBuffer, indexBuffer, vertexCount, indexCount);
        }

        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData)
        {
            Write($"DrawBatch(state, {startIndex}, {indexCount}, {batchUserData})");
            DelegateRenderer.DrawBatch(state, startIndex, indexCount, batchUserData);
        }

        /// <inheritdoc />
        public void EndRender()
        {
            Write("EndRender()");
            DelegateRenderer.EndRender();
        }
    }
}
