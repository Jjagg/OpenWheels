using System;
using System.IO;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// Interface for renderers that can be used to draw data batched by a <see cref="Batcher"/>.
    /// Note that clients do not have to use a renderer directly, unless they need custom rendering
    /// functionality (using <see cref="Batcher.BatchData" />) or additional functionality not supported
    /// by <see cref="Batcher" />.
    /// Implementations are expected to use alpha blending and enable depth read/write if they support it.
    /// </summary>
    public interface IRenderer
    {
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
        private NullRenderer() { }

        /// <inheritdoc />
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount) { }
        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData) { }
        /// <inheritdoc />
        public void EndRender() { }

        /// <summary>
        /// The singleton instance of the <see cref="NullRenderer" />.
        /// </summary>
        public static NullRenderer Instance { get; } = new NullRenderer();
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
            DelegateRenderer = NullRenderer.Instance;
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
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount)
        {
            Write($"BeginRender(<vertexBuffer>, <indexBuffer>, {vertexCount}, {indexCount})");
            DelegateRenderer.BeginRender(vertexBuffer, indexBuffer, vertexCount, indexCount);
        }

        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData)
        {
            Write($"DrawBatch(<state>, {startIndex}, {indexCount}, {batchUserData})");
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
