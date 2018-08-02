using System;
using System.Numerics;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace OpenWheels.Rendering.ImageSharp
{
    /// <summary>
    /// A software renderer for testing and debugging purposes.
    ///
    /// This renderer does not support textures, it only draws shapes in a solid color. It also does not
    /// support multiple colors in a single triangle. Each triangle is rendered in the color of the first
    /// vertex.
    /// </summary>
    public class ImageSharpDebugRenderer : IRenderer
    {
        /// <summary>
        /// The ImageSharp <see cref="SixLabors.ImageSharp.Image"/> that everything is rendered to.
        /// </summary>
        public Image<Rgba32> Image;
        private Vertex[] _vertexBuffer;
        private int[] _indexBuffer;

        private PointF[] _points = new PointF[3];
        private Rgba32 _culledColor;

        /// <summary>
        /// The graphics options used to render triangles.
        /// </summary>
        public GraphicsOptions GraphicsOptions { get; set;}

        /// <summary>
        /// If set to true, renders counter-clockwise triangles with the color set in <see cref="CulledColor"/>.
        /// </summary>
        public bool CullCcw { get; set; }

        /// <summary>
        /// The color to render counter-clockwise winding triangles if <see cref="CullCcw"/> is set to <c>true</c>.
        /// </summary>
        public Color CulledColor
        {
            get => new Color(_culledColor.PackedValue);
            set => _culledColor = new Rgba32(value.Packed);
        }

        /// <summary>
        /// Create a new ImageSharpDebugRenderer with an image of the given size.
        /// </summary>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        public ImageSharpDebugRenderer(int width, int height)
        {
            Image = new Image<Rgba32>(width, height);
            GraphicsOptions = GraphicsOptions.Default;
            CullCcw = true;
            CulledColor = new Color(0);
        }

        /// <summary>
        /// Clear the image to the given color.
        /// </summary>
        /// <param name="color">Color to clear the image to.</param>
        public void Clear(Color color)
        {
            var pc = new Rgba32(color.R, color.G, color.B, color.A);
            Image.Mutate(c => c.BackgroundColor(pc));
        }

        /// <inheritdoc />
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount)
        {
            _vertexBuffer = vertexBuffer;
            _indexBuffer = indexBuffer;
        }

        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData)
        {
            for (var i = 0; i < indexCount; i += 3)
            {
                var ii = startIndex + i;
                var index1 = _indexBuffer[ii];
                var index2 = _indexBuffer[ii + 1];
                var index3 = _indexBuffer[ii + 2];
                var v1 = _vertexBuffer[index1];
                var v2 = _vertexBuffer[index2];
                var v3 = _vertexBuffer[index3];

                var color = new Rgba32(v1.Color.Packed);

                if (CullCcw)
                {
                    var d1 = new Vector2(v2.Position.X - v1.Position.X, v2.Position.Y - v1.Position.Y);
                    var d2 = new Vector2(v3.Position.X - v1.Position.X, v3.Position.Y - v1.Position.Y);
                    if (MathHelper.Cross2D(d1, d2) < 0)
                        color = _culledColor;
                }

                _points[0] = new PointF(v1.Position.X, v1.Position.Y);
                _points[1] = new PointF(v2.Position.X, v2.Position.Y);
                _points[2] = new PointF(v3.Position.X, v3.Position.Y);

                // Let's just use the first vertex' color for now
                Image.Mutate(c => c.FillPolygon(GraphicsOptions, color, _points));
            }
        }

        /// <inheritdoc />
        public void EndRender()
        {
        }

        /// <inheritdoc />
        public Size GetTextureSize(int texture)
        {
            return new Size(1, 1);
        }

        /// <inheritdoc />
        public Rectangle GetViewport()
        {
            return new Rectangle(0, 0, Image.Width, Image.Height);
        }

        /// <inheritdoc />
        public int RegisterTexture(Span<Color> pixels, int width, int height)
        {
            return 0;
        }
    }
}