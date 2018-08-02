using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using SixLabors.Fonts;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// <p>
    ///   A platform-agnostic renderer that batches draw operations.
    ///   Uses the primitive drawing operation of a <see cref="IRenderer"/> to abstract the graphics back end.
    ///   Provides operations to draw basic shape outlines or fill basic shapes.
    /// </p>
    /// <p>
    ///   <see cref="Batcher"/>, like the name implies, attempts to batch as many sequential drawing operations
    ///   as possible. Operations can be batched as long as you do not change the graphics state, the texture
    ///   or the scissor rectangle. The Batcher API is stateful. All graphics state is set in between draw calls.
    ///   You can change the graphics state by setting the properties <see cref="BlendState"/> and
    ///   <see cref="SamplerState"/>. You can set the texture by calling <see cref="SetTexture(int)"/> or
    ///   <see cref="SetSprite(int,Rectangle)"/>. <seealso cref="Sprite"/>
    ///   Note that setting a sprite from the same texture will not finish a batch, since this just requires
    ///   to compute UV coordinates differently.
    /// </p>
    /// <p>
    ///   To abstract the texture and font representation, these data types are represented by an integer identifier.
    /// </p>
    /// <p>
    ///   Call <see cref="Start"/> to clear the last batch and to prepare for batching new draw calls.
    ///   Call <see cref="Finish"/> to send all queued batches to the <see cref="IRenderer"/> backend for actual rendering.
    /// </p>
    /// <p>
    ///   All 2D drawing operations generate vertices in the XY-plane. They can be remapped to
    ///   the desired plane (if necessary) by using the <see cref="TransformMatrix"/>.
    ///   Unless the order is implied by the method, indices are generated so vertices are in a clockwise order.
    /// </p>
    /// </summary>
    // TODO: Maybe add support for having multiple buffers so we can sort draw operations in them by graphics state
    //       and reduce batch count. This adds complexity for 2D stuff, because most of the time we care about draw
    //       order in favor of using a depth value (which is currently not even supported).
    public class Batcher
    {
        private class BatchInfo
        {
            public readonly GraphicsState GraphicsState;
            public readonly int Startindex;
            public readonly int IndexCount;
            public readonly object UserData;

            public BatchInfo(GraphicsState graphicsState, int startindex, int indexCount, object userData)
            {
                GraphicsState = graphicsState;
                Startindex = startindex;
                IndexCount = indexCount;
                UserData = userData;
            }
        }

        /// <summary>
        /// The initial number of vertices this batcher can hold. The batcher will grow its buffer if needed.
        /// </summary>
        public const int InitialMaxVertices = 4096;

        /// <summary>
        /// The initial number of indices this batcher can hold. The batcher will grow its buffer if needed.
        /// </summary>
        public const int InitialMaxIndices = 8192;

        /// <summary>
        /// The minimal increase in vertex buffer size. If the vertex buffer needs more space it will grow
        /// by at least this amount; more if necessary.
        /// </summary>
        public const int MinVertexInc = 512;

        /// <summary>
        /// The minimal increase in index buffer size. If the index buffer needs more space it will grow
        /// by at least this amount; more if necessary.
        /// </summary>
        public const int MinIndexInc = 512;

        /// <summary>
        /// The renderer that actually executes the draw calls.
        /// </summary>
        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _renderer = value;
            }
        }

        private Vertex[] _vb;
        private int[] _ib;

        private int _nextToDraw;
        private int _indicesInBatch;

        /// <summary>
        /// The size of the vertex buffer. Initialized to <see cref="InitialMaxVertices"/>.
        /// </summary>
        public int VertexBufferSize => _vb.Length;

        /// <summary>
        /// The size of the index buffer. Initialized to <see cref="InitialMaxIndices"/>.
        /// </summary>
        public int IndexBufferSize => _ib.Length;

        /// <summary>
        /// The number of indices submitted since the last call to <see cref="Clear"/>.
        /// </summary>
        public int IndicesSubmitted => _nextToDraw + _indicesInBatch;

        /// <summary>
        /// The number of indices submitted since the last call to <see cref="Clear"/>.
        /// </summary>
        public int VerticesSubmitted { get; private set; }

        private readonly List<BatchInfo> _batches;
        private Sprite _sprite;
        private TextureFont _font;
        private RectangleF _spriteUv;
        private bool _useSpriteUv;
        private IRenderer _renderer;
        private BlendState _blendState;
        private SamplerState _samplerState;
        private Rectangle _scissorRect;
        private Matrix4x4 _transformMatrix;
        private bool _useMatrix;

        /// <summary>
        /// Get or set the transformation matrix to apply to vertex positions.
        /// </summary>
        public Matrix4x4 TransformMatrix
        {
            get => _transformMatrix;
            set
            {
                _transformMatrix = value;
                _useMatrix = _transformMatrix != Matrix4x4.Identity;
            }
        }

        /// <summary>
        /// Get or set the sprite to use for texturing.
        /// </summary>
        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (value.Texture != Sprite.Texture)
                    Flush();
                _sprite = value;
                var texSize = Renderer.GetTextureSize(_sprite.Texture);
                if (texSize == _sprite.SrcRect.Size)
                {
                    _spriteUv = RectangleF.Unit;
                    _useSpriteUv = false;
                }
                else
                {
                    _spriteUv = new RectangleF(
                        (float) _sprite.SrcRect.X / texSize.Width,
                        (float) _sprite.SrcRect.Y / texSize.Height,
                        (float) _sprite.SrcRect.Width / texSize.Width,
                        (float) _sprite.SrcRect.Height / texSize.Height);
                    _useSpriteUv = true;
                }
            }
        }

        /// <summary>
        /// Get or set the font used for rendering text.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the value passed to the setter is <c>null</c>.</exception>
        public TextureFont TextureFont
        {
            get => _font;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _font = value;
                // TODO glyph map contains pixel boundaries, maybe change to UV
                //      less generic but more efficient for us
            }
        }

        /// <summary>
        /// Get or set the blend state.
        /// </summary>
        public BlendState BlendState
        {
            get => _blendState;
            set
            {
                if (_blendState != value)
                    Flush();
                _blendState = value;
            }
        }

        /// <summary>
        /// Get or set the sampler state.
        /// </summary>
        public SamplerState SamplerState
        {
            get => _samplerState;
            set
            {
                if (_samplerState != value)
                    Flush();
                _samplerState = value;
            }
        }

        /// <summary>
        /// Get or set the scissor rectangle.
        /// </summary>
        public Rectangle ScissorRect
        {
            get => _scissorRect;
            set
            {
                if (_scissorRect != value)
                    Flush();
                _scissorRect = value;
            }
        }

        /// <summary>
        /// Number of <see cref="Flush"/> calls since the last <see cref="Clear"/> call.
        /// </summary>
        public int BatchCount { get; private set; }

        /// <summary>
        /// The value of this property is attached to a batch when it is finished.
        /// Can be used for custom rendering in combination with <see cref="Flush"/>
        /// to force finishing a batch when graphics state unknown to <see cref="Batcher"/>
        /// needs to be changed.
        /// </summary>
        public object BatchData { get; set; }

        /// <summary>
        /// A blank white sprite. Implementors are expected to follow this convention, either
        /// by having the texture with id 0 be completely white or by having the texture with id
        /// 0 having a white first pixel.
        /// </summary>
        public static readonly Sprite BlankSprite = new Sprite(0, Rectangle.Unit);

        /// <summary>
        /// Create a <see cref="Batcher"/> with a <see cref="NullRenderer"/>.
        /// </summary>
        public Batcher()
            : this(NullRenderer.Instance)
        {
        }

        /// <summary>
        /// Create a batcher with a renderer.
        /// </summary>
        /// <param name="renderer">Renderer to execute draw calls.</param>
        public Batcher(IRenderer renderer)
        {
            Renderer = renderer;

            _vb = new Vertex[InitialMaxVertices];
            _ib = new int[InitialMaxIndices];

            _batches = new List<BatchInfo>();

            TransformMatrix = Matrix4x4.Identity;
        }

        #region Register Texture

        /// <summary>
        /// Register a texture given the image data and its dimensions.
        /// </summary>
        /// <param name="pixels">Span of the image data in row-major order.</param>
        /// <param name="width">Width of the texture in pixels.</param>
        /// <param name="height">Height of the texture in pixels.</param>
        /// <returns>The texture identifier after registration.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="pixels" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="width" /> is zero or negative.</exception>
        /// <exception cref="ArgumentException">If <paramref name="height" /> is zero or negative.</exception>
        public int RegisterTexture(Span<Color> pixels, int width, int height)
        {
            return Renderer.RegisterTexture(pixels, width, height);
        }

        #endregion

        #region Set Texture and Matrix

        /// <summary>
        /// Set <see cref="TransformMatrix"/> so the renderer draws to the plane of the viewport of the renderer.
        /// </summary>
        public void SetMatrix2D()
        {
            var vp = Renderer.GetViewport();
            TransformMatrix = Matrix4x4.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
        }

        /// <summary>
        /// Set the texture for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        public void SetTexture(int texture)
        {
            var size = Renderer.GetTextureSize(texture);
            Sprite = new Sprite(texture, new Rectangle(0, 0, size.Width, size.Height));
        }

        /// <summary>
        /// Set the fill texture to a white pixel.
        /// </summary>
        public void SetBlankSprite()
        {
            Sprite = BlankSprite;
        }

        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        /// <param name="srcRect">Source rectangle of the sprite inside the texture.</param>
        public void SetSprite(int texture, Rectangle srcRect)
        {
            Sprite = new Sprite(texture, srcRect);
        }

        #endregion

        #region Line

        /// <summary>
        /// Draw a line.
        /// </summary>
        /// <param name="p1">First point of the line.</param>
        /// <param name="p2">Second point of the line.</param>
        /// <param name="color">Color of the line.</param>
        /// <param name="lineWidth">Stroke width of the line.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source. Probably not very useful for user code.</param>
        public void DrawLine(Vector2 p1, Vector2 p2, Color color, float lineWidth = 1, in RectangleF? uvRect = null)
        {
            CreateLine(p1, p2, color, lineWidth, uvRect ?? RectangleF.Unit, out var v1, out var v2, out var v3, out var v4);
            FillQuad(v1, v2, v3, v4);
        }

        /// <summary>
        /// Draw a line strip. Draws lines between subsequent passed points.
        /// </summary>
        /// <param name="points">The points on the line strip.</param>
        /// <param name="color">Color of the line strip.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <exception cref="ArgumentException">If <paramref name="points"/> has less than 2 points.</exception>
        public void DrawLineStrip(Span<Vector2> points, Color color, float lineWidth = 1)
        {

            var c = points.Length;
            if (c < 2)
                throw new ArgumentException("Need at least 2 vertices for a line strip.", nameof(points));

            var vertexCount = c * 4;
            var indexCount = c * 6 + (c - 1) * 3;
            EnsureFree(vertexCount, indexCount);

            void FillQuad(in Vertex ve1, in Vertex ve2, in Vertex ve3, in Vertex ve4, out int i1, out int i2, out int i3, out int i4)
            {
                i1 = AddVertex(ve1);
                i2 = AddVertex(ve2);
                i3 = AddVertex(ve3);
                i4 = AddVertex(ve4);
                AddIndex(i1);
                AddIndex(i2);
                AddIndex(i4);
                AddIndex(i4);
                AddIndex(i2);
                AddIndex(i3);
            }

            var p1 = points[0];
            var p2 = points[1];
            var d1 = p2 - p1;
            CreateLine(p1, p2, color, lineWidth, RectangleF.Unit, out var v1, out var v2, out var v3, out var v4);
            FillQuad(v1, v2, v3, v4, out _, out _, out var i3prev, out var i4prev);

            p1 = p2;

            for (var i = 2; i < points.Length; i++)
            {
                p2 = points[i];

                CreateLine(p1, p2, color, lineWidth, RectangleF.Unit, out v1, out v2, out v3, out v4);
                FillQuad(v1, v2, v3, v4, out var i1, out var i2, out var i3, out var i4);

                // draw a triangle between the lines to nicely connect them
                var d2 = p2 - p1;
                var cross = MathHelper.Cross2D(d1, d2);
                AddIndex(i4prev);
                AddIndex(i3prev);
                if (cross > 0) // right-hand turn
                    AddIndex(i2);
                else if (cross < 0) // left-hand turn
                    AddIndex(i1);

                DrawLine(p1, p2, color, lineWidth);

                p1 = p2;
                d1 = d2;
                i3prev = i3;
                i4prev = i4;
            }
        }

        #endregion

        #region Triangle

        /// <summary>
        /// Fill a triangle.
        /// </summary>
        /// <param name="v1">First point of the triangle.</param>
        /// <param name="v2">Second point of the triangle.</param>
        /// <param name="v3">Third point of the triangle.</param>
        /// <param name="c">Color of the triangle.</param>
        public void FillTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Color c)
        {
            Span<Vertex> triangle = stackalloc Vertex[3];
            triangle[0] = CreateVertex(v1, Vector2.Zero, c);
            triangle[1] = CreateVertex(v2, Vector2.Zero, c);
            triangle[2] = CreateVertex(v3, Vector2.Zero, c);
            FillTriangleStrip(triangle);
        }

        #endregion

        #region Rectangle

        /// <summary>
        /// Draw the outline of a rectangle.
        /// </summary>
        /// <param name="rect">The bounds of the rectangle.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="lineWidth">Stroke width.</param>
        public void DrawRect(RectangleF rect, Color color, float lineWidth = 1)
        {
            var p1 = new Vector2(rect.Left, rect.Top);
            var p2 = new Vector2(rect.Right, rect.Top);
            var p3 = new Vector2(rect.Right, rect.Bottom);
            var p4 = new Vector2(rect.Left, rect.Bottom);

            DrawLine(p1, p2, color, lineWidth);
            DrawLine(p2, p3, color, lineWidth);
            DrawLine(p3, p4, color, lineWidth);
            DrawLine(p4, p1, color, lineWidth);
        }

        /// <summary>
        /// Draw the outline of a rectangle with rounded corners.
        /// </summary>
        /// <param name="rectangle">The outer bounds of the rectangle.</param>
        /// <param name="radius">Radius of the corners.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        public void DrawRoundedRect(RectangleF rectangle, float radius, Color color, int lineWidth = 1, float maxError = .25f)
        {
            DrawRoundedRect(rectangle, radius, radius, radius, radius, color, lineWidth);
        }

        /// <summary>
        /// Draw the outline of a rectangle with rounded corners.
        /// </summary>
        /// <param name="rectangle">The outer bounds of the rectangle.</param>
        /// <param name="radiusTl">Radius of the top left corner.</param>
        /// <param name="radiusTr">Radius of the top right corner.</param>
        /// <param name="radiusBr">Radius of the bottom right corner.</param>
        /// <param name="radiusBl">Radius of the bottom left corner.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   If any of the radii is larger than half the width or height of the rectangle.
        /// </exception>
        public void DrawRoundedRect(RectangleF rectangle,
            float radiusTl, float radiusTr, float radiusBr, float radiusBl,
            Color color, int lineWidth = 1, float maxError = .25f)
        {
            if (radiusTl > rectangle.Width / 2f || radiusTl > rectangle.Height / 2f ||
                radiusTr > rectangle.Width / 2f || radiusTr > rectangle.Height / 2f ||
                radiusBr > rectangle.Width / 2f || radiusBr > rectangle.Height / 2f ||
                radiusBl > rectangle.Width / 2f || radiusBl > rectangle.Height / 2f)
                throw new ArgumentException(
                    $"Radius too large. The rectangle size is ({rectangle.Size.X}, {rectangle.Size.Y}), " +
                    $"radii of the corners are (TL: {radiusTl}, TR: {radiusTr}, BR: {radiusBr}, Bl: {radiusBl}).");

            if (radiusTl == 0 && radiusTr == 0 && radiusBr == 0 && radiusBl == 0)
            {
                DrawRect(rectangle, color, lineWidth);
                return;
            }

            var outerRect = rectangle;

            var tl = new Vector2(outerRect.Left + radiusTl, outerRect.Top + radiusTl);
            var tr = new Vector2(outerRect.Right - radiusTr, outerRect.Top + radiusTr);
            var bl = new Vector2(outerRect.Left + radiusBl, outerRect.Bottom - radiusBl);
            var br = new Vector2(outerRect.Right - radiusBr, outerRect.Bottom - radiusBr);

            DrawLine(new Vector2(tl.X, outerRect.Top), new Vector2(tr.X, outerRect.Top), color, lineWidth);
            DrawLine(new Vector2(outerRect.Right, tr.Y), new Vector2(outerRect.Right, br.Y), color, lineWidth);
            DrawLine(new Vector2(br.X, outerRect.Bottom), new Vector2(bl.X, outerRect.Bottom), color, lineWidth);
            DrawLine(new Vector2(outerRect.Left, bl.Y), new Vector2(outerRect.Left, tr.Y), color, lineWidth);
            if (radiusTl > 0)
                DrawCircleSegment(tl, radiusTl, LeftAngle, TopAngle, color, lineWidth, maxError);
            if (radiusTr > 0)
                DrawCircleSegment(tr, radiusTr, TopAngle, RightEndAngle, color, lineWidth, maxError);
            if (radiusBr > 0)
                DrawCircleSegment(br, radiusBr, RightStartAngle, BotAngle, color, lineWidth, maxError);
            if (radiusBl > 0)
                DrawCircleSegment(bl, radiusBl, BotAngle, LeftAngle, color, lineWidth, maxError);
        }

        /// <summary>
        /// Fill a quad. Assumes vertices are passed in clockwise order.
        /// </summary>
        /// <param name="v0">The first vertex.</param>
        /// <param name="v1">The second vertex.</param>
        /// <param name="v2">The third vertex.</param>
        /// <param name="v3">The fourth vertex.</param>
        public void FillQuad(in Vertex v0, in Vertex v1, in Vertex v2, in Vertex v3)
        {
            EnsureFree(4, 6);

            var i1 = AddVertex(v0);
            var i2 = AddVertex(v1);
            var i3 = AddVertex(v2);
            var i4 = AddVertex(v3);
            AddIndex(i1);
            AddIndex(i2);
            AddIndex(i4);
            AddIndex(i4);
            AddIndex(i2);
            AddIndex(i3);
        }

        /// <summary>
        /// Fill a rectangle.
        /// </summary>
        /// <param name="rect">Bounds of the rectangle.</param>
        /// <param name="c">Color of the rectangle.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source. Probably not very useful for user code.</param>
        public void FillRect(RectangleF rect, Color c, RectangleF? uvRect = null)
        {
            FillRect(rect, c, c, c, c, uvRect);
        }

        /// <summary>
        /// Fill a rectangle. Interpolates colors between the corners.
        /// </summary>
        /// <param name="rect">Bounds of the rectangle.</param>
        /// <param name="c1">Color of the top left corner.</param>
        /// <param name="c2">Color of the top right corner.</param>
        /// <param name="c3">Color of the bottom right corner.</param>
        /// <param name="c4">Color of the bottom left corner.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source. Probably not very useful for user code.</param>
        public void FillRect(RectangleF rect, Color c1, Color c2, Color c3, Color c4, RectangleF? uvRect = null)
        {
            var r = uvRect ?? RectangleF.Unit;
            var v1 = CreateVertex(rect.TopLeft,     r.TopLeft,     c1);
            var v2 = CreateVertex(rect.TopRight,    r.TopRight,    c2);
            var v3 = CreateVertex(rect.BottomRight, r.BottomRight, c3);
            var v4 = CreateVertex(rect.BottomLeft,  r.BottomLeft,  c4);

            FillQuad(v1, v2, v3, v4);
        }

        /// <summary>
        /// Fill a rectangle with rounded corners.
        /// </summary>
        /// <param name="rectangle">Outer bounds of the rectangle.</param>
        /// <param name="radius">Radius of the corners.</param>
        /// <param name="color">Color to fill with.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source. Probably not very useful for user code.</param>
        /// <exception cref="ArgumentException">If the radius is larger than half the width or height of the rectangle.</exception>
        public void FillRoundedRect(RectangleF rectangle, float radius, Color color, float maxError = .25f, RectangleF? uvRect = null)
        {
            if (radius > rectangle.Width / 2f || radius > rectangle.Height / 2f)
                throw new ArgumentException($"Radius too large. The rectangle size is ({rectangle.Size.X}, {rectangle.Size.Y}), the radius is {radius}.", nameof(radius));

            if (radius == 0)
            {
                FillRect(rectangle, color);
                return;
            }

            var ur = uvRect ?? RectangleF.Unit;
            var outerRect = rectangle;
            var innerRect = rectangle;
            innerRect = innerRect.Inflate(-2 * radius, -2 * radius);

            FillRect(innerRect, color, MathHelper.LinearMap(innerRect, outerRect, ur));

            var leftRect = new RectangleF(outerRect.Left, innerRect.Top, radius, innerRect.Height);
            var rightRect = new RectangleF(innerRect.Right, innerRect.Top, radius, innerRect.Height);
            var topRect = new RectangleF(innerRect.Left, outerRect.Top, innerRect.Width, radius);
            var bottomRect = new RectangleF(innerRect.Left, innerRect.Bottom, innerRect.Width, radius);

            FillRect(leftRect,   color, MathHelper.LinearMap(leftRect, outerRect, ur)); // left
            FillRect(rightRect,  color, MathHelper.LinearMap(rightRect, outerRect, ur)); // right
            FillRect(topRect,    color, MathHelper.LinearMap(topRect, outerRect, ur)); // top
            FillRect(bottomRect, color, MathHelper.LinearMap(bottomRect, outerRect, ur)); // top

            var tl = innerRect.TopLeft;
            var tr = innerRect.TopRight;
            var br = innerRect.BottomRight;
            var bl = innerRect.BottomLeft;
            var radiusVec = new Vector2(radius);
            var tlRect = RectangleF.FromHalfExtents(tl, radiusVec);
            var trRect = RectangleF.FromHalfExtents(tr, radiusVec);
            var brRect = RectangleF.FromHalfExtents(br, radiusVec);
            var blRect = RectangleF.FromHalfExtents(bl, radiusVec);
            FillCircleSegment(tl, radius, LeftAngle,       TopAngle,      color, maxError, MathHelper.LinearMap(tlRect, outerRect, ur));
            FillCircleSegment(tr, radius, TopAngle,        RightEndAngle, color, maxError, MathHelper.LinearMap(trRect, outerRect, ur));
            FillCircleSegment(br, radius, RightStartAngle, BotAngle,      color, maxError, MathHelper.LinearMap(brRect, outerRect, ur));
            FillCircleSegment(bl, radius, BotAngle,        LeftAngle,     color, maxError, MathHelper.LinearMap(blRect, outerRect, ur));
        }

        #endregion

        #region Circle

        private const float LeftAngle = (float) Math.PI;
        private const float TopAngle = (float) (1.5 * Math.PI);
        private const float RightStartAngle = 0;
        private const float RightEndAngle = (float) (2 * Math.PI);
        private const float BotAngle = (float) (.5 * Math.PI);

        /// <summary>
        /// Draw the outline of a circle segment.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="color">Color of the circle.</param>
        /// <param name="lineWidth">Stroke width of the outline.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        public void DrawCircle(Vector2 center, float radius, Color color, float lineWidth = 1, float maxError = .25f)
        {
            DrawCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, lineWidth, maxError);
        }

        /// <summary>
        /// Draw the outline of a circle segment.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="start">Start angle of the segment in radians. Angle of 0 is right (positive x-axis).</param>
        /// <param name="end">End angle of the segment in radians.</param>
        /// <param name="color">Color of the circle segment.</param>
        /// <param name="lineWidth">Stroke width of the outline.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        public void DrawCircleSegment(Vector2 center, float radius, float start, float end, Color color, float lineWidth = 1, float maxError = .25f)
        {
            ComputeSegments(radius, maxError, out var step, out var segments);

            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, ref points);
            DrawLineStrip(points, color, lineWidth);
        }

        /// <summary>
        /// Fill a circle.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="color">Color of the circle.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        /// <param name="uvRect">
        ///   The rectangle inside the sprite to source uv coordinates from.
        ///   Probably not very useful for user code.
        /// </param>
        public void FillCircle(Vector2 center, float radius, Color color, float maxError = .25f, RectangleF? uvRect = null)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, maxError, uvRect);
        }

        /// <summary>
        /// Fill a circle segment.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="start">Start angle of the segment in radians. Angle of 0 is right (positive x-axis).</param>
        /// <param name="end">End angle of the segment in radians.</param>
        /// <param name="color">Color of the circle segment.</param>
        /// <param name="maxError">
        ///   The maximum distance from any point on the drawn circle to the actual circle.
        ///   The number of segments to draw is calculated from this value.
        /// </param>
        /// <param name="uvRect">
        ///   The rectangle inside the sprite to source uv coordinates from.
        ///   Probably not very useful for user code.
        /// </param>
        public void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, float maxError, RectangleF? uvRect = null)
        {
            ComputeSegments(radius, maxError, out var step, out var segments);

            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, ref points);
            var fromRect = RectangleF.FromHalfExtents(center, new Vector2(radius));
            var toRect = uvRect ?? RectangleF.Unit;

            Span<Vertex> vs = stackalloc Vertex[points.Length];
            for (var i = 0; i < points.Length; i++)
                vs[i] = CreateVertex(points[i], MathHelper.LinearMap(points[i], fromRect, toRect), color);

            var vCenter = CreateVertex(center, MathHelper.LinearMap(center, fromRect, toRect), color);
            FillTriangleFan(vCenter, vs);
        }

        private static void CreateCircleSegment(Vector2 center, float radius, float step, float start, float end, ref Span<Vector2> result)
        {
            var i = 0;
            float theta;
            for (theta = start; theta < end; theta += step)
                result[i++] = new Vector2((float) (center.X + radius * Math.Cos(theta)), (float) (center.Y + radius * Math.Sin(theta)));

            if (theta != end)
                result[i] = center + new Vector2((float) (radius * Math.Cos(end)), (float) (radius * Math.Sin(end)));
        }

        #endregion

        #region Text

        /// <summary>
        /// Render text with the active font. Changes the active <see cref="Sprite"/>.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Position to draw the text at.</param>
        /// <param name="color">Color of the text.</param>
        /// <param name="scale">Scale of the text.</param>
        /// <param name="ha">Horizontal alignment of the text.</param>
        /// <param name="va">Vertical alignment of the text.</param>
        /// <param name="wrappingWidth">Width to wrap the text at. Pass -1 to not wrap (default).</param>
        /// <param name="tabWidth">Number of spaces in a tab.</param>
        /// <exception cref="InvalidOperationException">If <see cref="TextureFont"/> is not set.</exception>
        /// <exception cref="InvalidOperationException">
        ///   If a character from the given text has no glyph registered in the
        ///   active font and no fallback character is set.
        /// </exception>
        /// <seealso cref="Font"/>
        public void DrawText(string text, Vector2 position, Color color, float scale = 1f,
            HorizontalAlignment ha = HorizontalAlignment.Left, VerticalAlignment va = VerticalAlignment.Top,
            float wrappingWidth = -1f, float tabWidth = 4)
        {
            if (TextureFont == null)
                throw new InvalidOperationException("No font is set.");

            var slFont = TextureFont.GlyphMap.Font;
            // TODO other dpi support
            var dpi = 72 * scale;
            // TODO there is no setter for Font on RendererOptions, once there is we can cache an instance
            var ro = new RendererOptions(slFont, dpi, dpi, position);
            ro.HorizontalAlignment = ha;
            ro.VerticalAlignment = va;
            ro.WrappingWidth = wrappingWidth;
            ro.TabWidth = tabWidth;
            // TODO this generates garbage; should push for an overload to use an existing collection
            // TODO StringBuilder overload
            TextMeasurer.TryMeasureCharacterBounds(text, ro, out var gms);

            foreach (var gm in gms)
            {
                var gd = TextureFont.GlyphMap.GetGlyphData(gm.Codepoint);
                if (gd.Character == 0)
                {
                    if (TextureFont.FallbackCharacter.HasValue)
                        throw new InvalidOperationException($"Character '{gm.Character}' is missing from the glyph map of active font and no fallback character is set.");
                    gd = TextureFont.FallbackGlyphData;
                }

                Sprite = new Sprite(TextureFont.Texture, gd.Bounds);
                FillRect(gm.Bounds.ToOwRect(), color);
            }
        }

        #endregion

        #region Low level

        /// <summary>
        /// Fill a triangle strip.
        /// </summary>
        /// <param name="ps">Vertices of the triangle strip.</param>
        /// <exception cref="ArgumentException">If less than 3 vertices are passed.</exception>
        public void FillTriangleStrip(Span<Vertex> ps)
        {
            var c = ps.Length;
            if (c < 3)
                throw new ArgumentException("Need at least 3 vertices for a triangle strip.", nameof(ps));

            var vertexCount = c;
            var indexCount = (c - 2) * 3;
            EnsureFree(vertexCount, indexCount);

            var v1 = AddVertex(ps[0]);
            var v2 = AddVertex(ps[1]);

            for (var i = 2; i < c; i++)
            {
                var v3 = AddVertex(ps[i]);
                AddIndex(v1);
                AddIndex(v2);
                AddIndex(v3);
                v1 = v2;
                v2 = v3;
            }
        }

        /// <summary>
        /// Fill a triangle fan.
        /// </summary>
        /// <param name="center">The center vertex.</param>
        /// <param name="vs">The other vertices.</param>
        /// <exception cref="ArgumentException">If <paramref name="vs"/> has less than 2 vertices.</exception>
        public void FillTriangleFan(Vertex center, Span<Vertex> vs)
        {
            var c = vs.Length;
            if (c < 2)
                throw new ArgumentException("Need at least 3 vertices for a triangle fan.", nameof(vs));

            var vertexCount = c + 1;
            var indexCount = (c - 1) * 3;
            EnsureFree(vertexCount, indexCount);

            var centerIndex = AddVertex(center);
            var v1 = AddVertex(vs[0]);
            for (var i = 1; i < c; i++)
            {
                var v2 = AddVertex(vs[i]);
                AddIndex(centerIndex);
                AddIndex(v1);
                AddIndex(v2);
                v1 = v2;
            }
        }

        #endregion

        #region Management

        /// <summary>
        /// Remove all unflushed batches.
        /// </summary>
        public void Clear()
        {
            _batches.Clear();
            _nextToDraw = 0;
            _indicesInBatch = 0;
            VerticesSubmitted = 0;

            BatchCount = 0;
        }

        /// <summary>
        /// Call this to clear flushed batches and prepare for a new frame.
        /// </summary>
        public void Start()
        {
            Clear();
        }

        /// <summary>
        /// Call this to finish a set of batches and let the <see cref="Renderer"/> draw them.
        /// </summary>
        public void Finish()
        {
            // register last batch if necessary
            Flush();

            Renderer.BeginRender(_vb, _ib, VerticesSubmitted, IndicesSubmitted);

            foreach (var batch in _batches)
                Renderer.DrawBatch(batch.GraphicsState, batch.Startindex, batch.IndexCount, batch.UserData);

            Renderer.EndRender();
        }

        /// <summary>
        /// End the current batch.
        /// </summary>
        public void Flush()
        {
            var gs = CreateCurrentGraphicsState();

            // if nothing to flush
            if (_indicesInBatch == 0 && BatchData == null)
                return;

            var bi = new BatchInfo(gs, _nextToDraw, _indicesInBatch, BatchData);
            _batches.Add(bi);

            _nextToDraw = _nextToDraw + _indicesInBatch;
            _indicesInBatch = 0;

            BatchCount++;
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Ensure that the vertex and index buffers have at least the specified amount of free spots.
        /// Grows the vertex and index buffer if necessary.
        /// </summary>
        /// <param name="vertexCount">Number of free vertex elements to ensure.</param>
        /// <param name="indexCount">Number of free index elements to ensure.</param>
        public void EnsureFree(int vertexCount, int indexCount)
        {
            var vfree = _vb.Length - VerticesSubmitted;
            if (vfree < vertexCount)
                Array.Resize(ref _vb, Math.Max(_vb.Length + MinVertexInc, _vb.Length + vertexCount - vfree));

            var ifree = _ib.Length - IndicesSubmitted;
            if (ifree < indexCount)
                Array.Resize(ref _ib, Math.Max(_ib.Length + MinIndexInc, _ib.Length + indexCount - ifree));
        }

        private static Vertex CreateVertex(Vector2 p, Vector2 uv, Color c)
        {
            return new Vertex(new Vector3(p.X, p.Y, 0), uv, c);
        }

        private int AddVertex(Vector2 position, Color color)
        {
            return AddVertex(CreateVertex(position, Vector2.Zero, color));
        }

        private int AddVertex(Vector2 position, Color color, Vector2 uv)
        {
            return AddVertex(CreateVertex(position, uv, color));
        }

        private int AddVertex(Vertex v)
        {
            TransformVertex(ref v);
            var i = VerticesSubmitted;
            _vb[i] = v;
            VerticesSubmitted++;
            return i;
        }

        private void AddIndex(int index)
        {
            var i = _nextToDraw + _indicesInBatch;
            _ib[i] = index;
            _indicesInBatch++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TransformVertex(ref Vertex vertex)
        {
            // remap uv from unit rectangle to the uv rectangle of our sprite
            if (_useSpriteUv)
                vertex.Uv = MathHelper.LinearMap(vertex.Uv, RectangleF.Unit, _spriteUv);
            if (_useMatrix)
                vertex.Position = Vector3.Transform(vertex.Position, TransformMatrix);
        }

        private GraphicsState CreateCurrentGraphicsState()
        {
            return new GraphicsState(Sprite.Texture, BlendState, SamplerState, ScissorRect);
        }

        private void CreateLine(Vector2 p1, Vector2 p2, Color color, float lineWidth, in RectangleF ur, out Vertex v1, out Vertex v2, out Vertex v3, out Vertex v4)
        {
            var d = Vector2.Normalize(p2 - p1);
            var dt = new Vector2(-d.Y, d.X) * (lineWidth / 2f);

            v1 = CreateVertex(p1 + dt, ur.TopLeft, color);
            v2 = CreateVertex(p1 - dt, ur.TopRight, color);
            v3 = CreateVertex(p2 - dt, ur.BottomRight, color);
            v4 = CreateVertex(p2 + dt, ur.BottomLeft, color);
        }

        private void ComputeSegments(float radius, float maxError, out float step, out int segments)
        {
            var invErrRad = 1 - maxError / radius;
            step = (float) Math.Acos(2 * invErrRad * invErrRad - 1);
            segments = (int) Math.Ceiling(MathHelper.TwoPi / step);
        }

        #endregion
    }
}
