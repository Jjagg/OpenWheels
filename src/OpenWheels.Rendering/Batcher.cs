﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenWheels.Rendering
{
    /// <summary>
    /// <p>
    ///   A platform-agnostic renderer that batches draw operations.
    ///   Exports instances of <see cref="BatchInfo"/> to abstract the graphics back end.
    ///   Provides operations to draw basic shape outlines or fill basic shapes.
    /// </p>
    /// <p>
    ///   <see cref="Batcher"/>, like the name implies, attempts to batch as many sequential drawing operations
    ///   as possible. Operations can be batched as long as you do not change the graphics state, the texture
    ///   or the scissor rectangle. The Batcher API is stateful. All graphics state is set in between draw calls.
    ///   You can change the sampler state by setting the <see cref="SamplerState"/> property. You can set the
    ///   texture by calling <see cref="SetTexture(int)"/> or <see cref="SetSprite"/>. <seealso cref="Sprite"/>
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
    ///   the desired plane (if necessary) by using the <see cref="PositionTransform"/>.
    ///   Unless the order is implied by the method, indices are generated so vertices are in a clockwise order.
    ///   Most rendering functions assign UV coordinates to the vertices. These can be manipulated similar to the
    ///   vertex position using <see cref="UvTransform"/>.
    /// </p>
    /// </summary>
    // TODO: Maybe add support for having multiple buffers so we can sort draw operations in them by graphics state
    //       and reduce batch count. This adds complexity for 2D stuff, because most of the time we care about draw
    //       order in favor of using a depth value (which is currently not even supported).
    public class Batcher
    {
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

        private Vertex[] _vb;
        private int[] _ib;

        private int _nextToDraw;
        private int _indicesInBatch;

        /// <summary>
        /// The vertex buffer used to batch vertices.
        /// </summary>
        public Vertex[] VertexBuffer => _vb;

        /// <summary>
        /// The size of the vertex buffer. Initialized to <see cref="InitialMaxVertices"/>.
        /// </summary>
        public int VertexBufferSize => _vb.Length;

        /// <summary>
        /// The index buffer.
        /// </summary>
        public int[] IndexBuffer => _ib;

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

        /// <summary>
        /// The renderer for text used in <see cref="DrawText"/>.
        /// </summary>
        public IBitmapFontRenderer TextRenderer { get; }

        private readonly List<BatchInfo> _batches;
        private bool _finished;

        private RectangleF _spriteUv;
        private bool _useSpriteUv;
        private BlendState _blendState;
        private SamplerState _samplerState;
        private Matrix3x2 _positionTransform;
        private Matrix3x2 _uvTransform;
        private bool _usePosMatrix;
        private bool _useUvMatrix;

        /// <summary>
        /// Get or set the z coordinate used for all vertices.
        /// This can be used for clipping purposes as renderers are expected to have depth
        /// testing enabled (see <see cref="IRenderer"/>).
        /// </summary>
        public float PositionZ { get; set; }

        /// <summary>
        /// Get or set the transformation matrix to apply to vertex positions.
        /// </summary>
        public Matrix3x2 PositionTransform
        {
            get => _positionTransform;
            set
            {
                _positionTransform = value;
                _usePosMatrix = _positionTransform != Matrix3x2.Identity;
            }
        }

        /// <summary>
        /// Get or set the transformation matrix to apply to vertex UV coordinates.
        /// </summary>
        public Matrix3x2 UvTransform
        {
            get => _uvTransform;
            set
            {
                _uvTransform = value;
                _useUvMatrix = _uvTransform != Matrix3x2.Identity;
            }
        }

        /// <summary>
        /// Get the active texture.
        /// To change the texture use one of <see cref="SetTexture"/>, <see cref="SetSprite"/>
        /// or <see cref="SetUvSprite"/>.
        /// </summary>
        public int Texture { get; private set; }

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
        /// Number of <see cref="Flush"/> calls since the last <see cref="Clear"/> call.
        /// </summary>
        public int BatchCount { get; private set; }

        /// <summary>
        /// The value of this property is attached to a batch when it is finished
        /// through the <see cref="BatchInfo.UserData"/> property.
        /// Can be used for custom rendering in combination with <see cref="Flush"/>
        /// to force finishing a batch when graphics state unknown to <see cref="Batcher"/>
        /// needs to be changed.
        /// </summary>
        public object BatchData { get; set; }

        /// <summary>
        /// Create a <see cref="Batcher"/> with a new <see cref="FontsTextRenderer"/>.
        /// </summary>
        public Batcher() : this(new FontsTextRenderer()) { }

        /// <summary>
        /// Create a batcher with a text renderer.
        /// </summary>
        /// <param name="textRenderer">Text renderer to use.</param>
        /// <exception cref="ArgumentNullException">
        ///   If <paramref name="textRenderer" /> is <c>null</c>.
        ///   Pass <see cref="NullBitmapFontRenderer.Instance"/> to use a text renderer that does nothing.
        /// </exception>
        public Batcher(IBitmapFontRenderer textRenderer)
        {
            if (textRenderer == null)
                throw new ArgumentNullException(nameof(textRenderer));

            TextRenderer = textRenderer;

            _vb = new Vertex[InitialMaxVertices];
            _ib = new int[InitialMaxIndices];

            _batches = new List<BatchInfo>();

            PositionTransform = Matrix3x2.Identity;
        }

        #region Set State

        /// <summary>
        /// Set <see cref="PositionTransform"/> so vertex coordinates are mapped from viewport pixel space to
        /// the [-1, 1] range.
        /// </summary>
        public void SetMatrix2D(Size size)
        {
            SetMatrix2D(Vector2.Zero, size);
        }

        /// <summary>
        /// Set <see cref="PositionTransform"/> so vertex coordinates are mapped from viewport pixel space
        /// with some offset to the [-1, 1] range.
        /// </summary>
        /// <param name="origin">
        ///   Coordinates in the viewport that map to [-1, -1] in pixels.
        ///   Defaults to <see cref="Vector2.Zero"/>.
        /// </param>
        public void SetMatrix2D(Vector2 origin, Size size)
        {
            var mat = new Matrix3x2();
            mat.M11 = 2f / size.Width;
            mat.M22 = 2f / size.Height;
            mat.M31 = -2 * origin.X / size.Width + 1;
            mat.M32 = -2 * origin.Y / size.Height + 1;

            PositionTransform = mat;
        }

        /// <summary>
        /// Set the texture for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        public void SetTexture(int texture) => SetUvSprite(texture, RectangleF.Unit);

        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="sprite">The sprite to set.</param>
        /// <param name="texStorage">Texture storage that stores the sprite texture. Used to get the texture size.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="texStorage" /> is <c>null</c>.</exception>
        public void SetSprite(in Sprite sprite, ITextureStorage texStorage)
            => SetSprite(sprite.Texture, sprite.SrcRect, texStorage);

        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        /// <param name="srcRect">Source rectangle of the sprite in the texture in pixels.</param>
        /// <param name="texStorage">Texture storage that stores the sprite texture. Used to get the texture size.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="texStorage" /> is <c>null</c>.</exception>
        public void SetSprite(int texture, in Rectangle srcRect, ITextureStorage texStorage)
        {
            if (texStorage == null)
                throw new ArgumentNullException(nameof(texStorage));

            var size = texStorage.GetTextureSize(texture);
            SetSprite(texture, srcRect, size);
        }


        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="sprite">The sprite to set.</param>
        /// <param name="size">Size of the texture. Used to convert <paramref name="srcRect"/> to a UV rect.</param>
        public void SetSprite(in Sprite sprite, Size size) => SetSprite(sprite.Texture, sprite.SrcRect, size);

        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        /// <param name="srcRect">Source rectangle of the sprite in the texture in pixels.</param>
        /// <param name="size">Size of the texture. Used to convert <paramref name="srcRect"/> to a UV rect.</param>
        public void SetSprite(int texture, in Rectangle srcRect, Size size)
        {
            var uvRect = new RectangleF(
                    (float) srcRect.X / size.Width,
                    (float) srcRect.Y / size.Height,
                    (float) srcRect.Width / size.Width,
                    (float) srcRect.Height / size.Height);

            SetUvSprite(texture, uvRect);
        }

        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="sprite">Sprite to set.</param>
        public void SetUvSprite(in UvSprite sprite) => SetUvSprite(sprite.Texture, sprite.SrcRect);
 
        /// <summary>
        /// Set the sprite for fills.
        /// </summary>
        /// <param name="texture">Texture identifier.</param>
        /// <param name="srcRect">Source rectangle of the sprite in the texture in UV coordinates.</param>
        public void SetUvSprite(int texture, in RectangleF srcRect)
        {
            if (texture != this.Texture)
                Flush();

            Texture = texture;

            if (srcRect == RectangleF.Unit)
            {
                _spriteUv = RectangleF.Unit;
                _useSpriteUv = false;
            }
            else
            {
                _spriteUv = srcRect;
                _useSpriteUv = true;
            }
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
        public void DrawLine(Vector2 p1, Vector2 p2, Color color, float lineWidth = 1)
        {
            DrawLine(p1, p2, color, lineWidth, RectangleF.Unit);
        }

        /// <summary>
        /// Draw a line.
        /// </summary>
        /// <param name="p1">First point of the line.</param>
        /// <param name="p2">Second point of the line.</param>
        /// <param name="color">Color of the line.</param>
        /// <param name="lineWidth">Stroke width of the line.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source.</param>
        private void DrawLine(Vector2 p1, Vector2 p2, Color color, float lineWidth, in RectangleF uvRect)
        {
            CreateLine(p1, p2, color, lineWidth, uvRect, out var v1, out var v2, out var v3, out var v4);
            FillQuad(v1, v2, v3, v4);
        }

        /// <summary>
        /// Draw a line strip. Draws lines between subsequent passed points.
        /// </summary>
        /// <param name="points">The points on the line strip.</param>
        /// <param name="color">Color of the line strip.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <exception cref="ArgumentException">If <paramref name="points"/> has less than 2 points.</exception>
        public void DrawLineStrip(ReadOnlySpan<Vector2> points, Color color, float lineWidth = 1)
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

        /// <summary>
        /// Draw a quadratic Bézier curve.
        /// </summary>
        /// <param name="qb">The Bézier curve to draw.</param>
        /// <param name="color">Color of the curve.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <param name="segmentsPerLength">Number of line segments to subdivide the curve in per unit of length.</param>
        public void DrawCurve(QuadraticBezier qb, Color color, float lineWidth = 1, float segmentsPerLength = .05f)
        {
            var lengthMax = qb.MaxLength();
            var segments = (int) (lengthMax * segmentsPerLength + .999f);
            Span<Vector2> ps = stackalloc Vector2[segments + 1];

            ps[0] = qb.A;

            var step = 1f / segments;

            for (var i = 1; i < segments; i++)
            {
                var t = i * step;
                ps[i] = qb.Evaluate(t);
            }

            ps[segments] = qb.C;

            DrawLineStrip(ps, color, lineWidth);
        }

        /// <summary>
        /// Draw a cubic Bézier curve.
        /// </summary>
        /// <param name="qb">The Bézier curve to draw.</param>
        /// <param name="color">Color of the curve.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <param name="segmentsPerLength">Number of line segments to subdivide the curve in per unit of length.</param>
        public void DrawCurve(CubicBezier qb, Color color, float lineWidth = 1, float segmentsPerLength = .1f)
        {
            var lengthMax = qb.MaxLength();
            var segments = (int) (lengthMax * segmentsPerLength + .999f);
            Span<Vector2> ps = stackalloc Vector2[segments + 1];

            ps[0] = qb.A;

            var step = 1f / segments;

            for (var i = 1; i < segments; i++)
            {
                var t = i * step;
                ps[i] = qb.Evaluate(t);
            }

            ps[segments] = qb.D;

            DrawLineStrip(ps, color, lineWidth);
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
        public void DrawRect(in RectangleF rect, Color color, float lineWidth = 1)
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
        public void DrawRoundedRect(in RectangleF rectangle, float radius, Color color, int lineWidth = 1, float maxError = .25f)
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
        public void DrawRoundedRect(in RectangleF rectangle, float radiusTl, float radiusTr, float radiusBr, float radiusBl, Color color, int lineWidth = 1, float maxError = .25f)
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

            var tl = new Vector2(rectangle.Left + radiusTl, rectangle.Top + radiusTl);
            var tr = new Vector2(rectangle.Right - radiusTr, rectangle.Top + radiusTr);
            var bl = new Vector2(rectangle.Left + radiusBl, rectangle.Bottom - radiusBl);
            var br = new Vector2(rectangle.Right - radiusBr, rectangle.Bottom - radiusBr);

            DrawLine(new Vector2(tl.X, rectangle.Top), new Vector2(tr.X, rectangle.Top), color, lineWidth);
            DrawLine(new Vector2(rectangle.Right, tr.Y), new Vector2(rectangle.Right, br.Y), color, lineWidth);
            DrawLine(new Vector2(br.X, rectangle.Bottom), new Vector2(bl.X, rectangle.Bottom), color, lineWidth);
            DrawLine(new Vector2(rectangle.Left, bl.Y), new Vector2(rectangle.Left, tr.Y), color, lineWidth);
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
        public void FillRect(in RectangleF rect, Color c)
        {
            FillRect(rect, c, RectangleF.Unit);
        }

        /// <summary>
        /// Fill a rectangle.
        /// </summary>
        /// <param name="rect">Bounds of the rectangle.</param>
        /// <param name="c">Color of the rectangle.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source</param>
        private void FillRect(in RectangleF rect, Color c, in RectangleF uvRect)
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
        public void FillRect(in RectangleF rect, Color c1, Color c2, Color c3, Color c4)
        {
            FillRect(rect, c1, c2, c3, c4, RectangleF.Unit);
        }

        /// <summary>
        /// Fill a rectangle. Interpolates colors between the corners.
        /// </summary>
        /// <param name="rect">Bounds of the rectangle.</param>
        /// <param name="c1">Color of the top left corner.</param>
        /// <param name="c2">Color of the top right corner.</param>
        /// <param name="c3">Color of the bottom right corner.</param>
        /// <param name="c4">Color of the bottom left corner.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source.</param>
        private void FillRect(in RectangleF rect, Color c1, Color c2, Color c3, Color c4, in RectangleF uvRect)
        {
            var r = uvRect;
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
        /// <exception cref="ArgumentException">If the radius is larger than half the width or height of the rectangle.</exception>
        public void FillRoundedRect(in RectangleF rectangle, float radius, Color color, float maxError = .25f)
        {
            if (radius > rectangle.Width / 2f || radius > rectangle.Height / 2f)
                throw new ArgumentException($"Radius too large. The rectangle size is ({rectangle.Size.X}, {rectangle.Size.Y}), the radius is {radius}.", nameof(radius));

            if (radius == 0)
            {
                FillRect(rectangle, color);
                return;
            }

            var outerRect = rectangle;
            var innerRect = rectangle;
            innerRect = innerRect.Inflate(-2 * radius, -2 * radius);

            FillRect(innerRect, color, MathHelper.LinearMap(innerRect, outerRect, RectangleF.Unit));

            var leftRect = new RectangleF(outerRect.Left, innerRect.Top, radius, innerRect.Height);
            var rightRect = new RectangleF(innerRect.Right, innerRect.Top, radius, innerRect.Height);
            var topRect = new RectangleF(innerRect.Left, outerRect.Top, innerRect.Width, radius);
            var bottomRect = new RectangleF(innerRect.Left, innerRect.Bottom, innerRect.Width, radius);

            FillRect(leftRect,   color, MathHelper.LinearMap(leftRect, outerRect, RectangleF.Unit)); // left
            FillRect(rightRect,  color, MathHelper.LinearMap(rightRect, outerRect, RectangleF.Unit)); // right
            FillRect(topRect,    color, MathHelper.LinearMap(topRect, outerRect, RectangleF.Unit)); // top
            FillRect(bottomRect, color, MathHelper.LinearMap(bottomRect, outerRect, RectangleF.Unit)); // top

            var tl = innerRect.TopLeft;
            var tr = innerRect.TopRight;
            var br = innerRect.BottomRight;
            var bl = innerRect.BottomLeft;
            var radiusVec = new Vector2(radius);
            var tlRect = RectangleF.FromHalfExtents(tl, radiusVec);
            var trRect = RectangleF.FromHalfExtents(tr, radiusVec);
            var brRect = RectangleF.FromHalfExtents(br, radiusVec);
            var blRect = RectangleF.FromHalfExtents(bl, radiusVec);
            FillCircleSegment(tl, radius, LeftAngle,       TopAngle,      color, maxError, MathHelper.LinearMap(tlRect, outerRect, RectangleF.Unit));
            FillCircleSegment(tr, radius, TopAngle,        RightEndAngle, color, maxError, MathHelper.LinearMap(trRect, outerRect, RectangleF.Unit));
            FillCircleSegment(br, radius, RightStartAngle, BotAngle,      color, maxError, MathHelper.LinearMap(brRect, outerRect, RectangleF.Unit));
            FillCircleSegment(bl, radius, BotAngle,        LeftAngle,     color, maxError, MathHelper.LinearMap(blRect, outerRect, RectangleF.Unit));
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
            ComputeCircleSegments(radius, maxError, end - start, out var step, out var segments);

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
        public void FillCircle(Vector2 center, float radius, Color color, float maxError = .25f)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, maxError, RectangleF.Unit);
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
        public void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, float maxError = .25f)
        {
            FillCircleSegment(center, radius, start, end, color, maxError, RectangleF.Unit);
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
        /// <param name="uvRect">The rectangle inside the sprite to source uv coordinates from.</param>
        private void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, float maxError, in RectangleF uvRect)
        {
            ComputeCircleSegments(radius, maxError, end - start, out var step, out var segments);

            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, ref points);
            var fromRect = RectangleF.FromHalfExtents(center, new Vector2(radius));

            Span<Vertex> vs = stackalloc Vertex[points.Length];
            for (var i = 0; i < points.Length; i++)
                vs[i] = CreateVertex(points[i], MathHelper.LinearMap(points[i], fromRect, uvRect), color);

            var vCenter = CreateVertex(center, MathHelper.LinearMap(center, fromRect, uvRect), color);
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
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Position to start rendering the font (top left).</param>
        /// <param name="color">Color of the text.</param>
        public void DrawText(TextureFont font, string text, Vector2 position, Color color)
            => DrawText(font, text.AsSpan(), 1f, new TextLayoutOptions(position), color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Position to start rendering the font (top left).</param>
        /// <param name="color">Color of the text.</param>
        public void DrawText(TextureFont font, ReadOnlySpan<char> text, Vector2 position, Color color)
            => DrawText(font, text, 1f, new TextLayoutOptions(position), color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Position to start rendering the font (top left).</param>
        /// <param name="scale">Scaling factor to render text at.</param>
        /// <param name="color">Color of the text.</param>
        public void DrawText(TextureFont font, string text, Vector2 position, float scale, Color color)
            => DrawText(font, text.AsSpan(), scale, new TextLayoutOptions(position), color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Position to start rendering the font (top left).</param>
        /// <param name="scale">Scaling factor to render text at.</param>
        /// <param name="color">Color of the text.</param>
        public void DrawText(TextureFont font, ReadOnlySpan<char> text, Vector2 position, float scale, Color color)
            => DrawText(font, text, scale, new TextLayoutOptions(position), color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="tlo">Layout options for rendering the text.</param>
        /// <param name="color">Color of the text.</param>
        /// <returns>The bounding rectangle for the rendered text.</returns>
        public RectangleF DrawText(TextureFont font, string text, in TextLayoutOptions tlo, Color color)
            => DrawText(font, text.AsSpan(), 1f, tlo, color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="tlo">Layout options for rendering the text.</param>
        /// <param name="color">Color of the text.</param>
        /// <returns>The bounding rectangle for the rendered text.</returns>
        public RectangleF DrawText(TextureFont font, ReadOnlySpan<char> text, in TextLayoutOptions tlo, Color color)
            => DrawText(font, text, 1f, tlo, color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="scale">Scaling factor to render text at.</param>
        /// <param name="tlo">Layout options for rendering the text.</param>
        /// <param name="color">Color of the text.</param>
        /// <returns>The bounding rectangle for the rendered text.</returns>
        public RectangleF DrawText(TextureFont font, string text, float scale, in TextLayoutOptions tlo, Color color)
            => DrawText(font, text.AsSpan(), scale, tlo, color);

        /// <summary>
        /// Render text. Changes the active <see cref="Texture"/> if required.
        /// </summary>
        /// <param name="font">The font to use.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="scale">Scaling factor to render text at.</param>
        /// <param name="tlo">Layout options for rendering the text.</param>
        /// <param name="color">Color of the text.</param>
        /// <returns>The bounding rectangle for the rendered text.</returns>
        public RectangleF DrawText(TextureFont font, ReadOnlySpan<char> text, float scale, in TextLayoutOptions tlo, Color color)
            => TextRenderer.RenderText(this, font, text, scale, tlo, color);

        #endregion

        #region Low level

        /// <summary>
        /// Fill a triangle strip.
        /// </summary>
        /// <param name="ps">Vertices of the triangle strip.</param>
        /// <exception cref="ArgumentException">If less than 3 vertices are passed.</exception>
        public void FillTriangleStrip(ReadOnlySpan<Vertex> ps)
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
        public void FillTriangleFan(Vertex center, ReadOnlySpan<Vertex> vs)
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
        /// Remove all flushed batches.
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
        /// Does nothing if the batcher was not finished, so this can called to ensure draw calls can be made.
        /// </summary>
        public void Start()
        {
            if (!_finished)
                return;

            Clear();
            _finished = false;
        }

        /// <summary>
        /// Call this to finish a set of batches.
        /// </summary>
        /// <returns>The batches created since the last call to <see cref="Start"/> or <see cref="Clear"/>.
        public IEnumerable<BatchInfo> Finish()
        {
            if (_finished)
                return _batches;

            _finished = true;

            // register last batch if necessary
            Flush();
            return _batches;
        }

        /// <summary>
        /// Call this to finish a set of batches (if not yet finished) and render them with the passed in renderer.
        /// </summary>
        /// <param name="renderer">Renderer to draw the batches with.</param>
        public void Render(IRenderer renderer)
        {
            var batches = Finish();

            renderer.BeginRender(_vb, _ib, VerticesSubmitted, IndicesSubmitted);

            foreach (var batch in batches)
                renderer.DrawBatch(batch.GraphicsState, batch.Startindex, batch.IndexCount, batch.UserData);

            renderer.EndRender();
        }

        /// <summary>
        /// End the current batch.
        /// </summary>
        public void Flush()
        {
            // if nothing to flush
            if (_indicesInBatch == 0 && BatchData == null)
                return;

            var gs = new GraphicsState(Texture, SamplerState);
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

        private Vertex CreateVertex(Vector2 p, Vector2 uv, Color c)
        {
            // remap uv from unit rectangle to the uv rectangle of our sprite
            if (_useSpriteUv)
                uv = MathHelper.LinearMap(uv, RectangleF.Unit, _spriteUv);
            if (_useUvMatrix)
                uv = Vector2.Transform(uv, _uvTransform);
            if (_usePosMatrix)
                p = Vector2.Transform(p, _positionTransform);

            return new Vertex(new Vector3(p, PositionZ), uv, c);
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

        private void CreateLine(Vector2 p1, Vector2 p2, Color color, float lineWidth, in RectangleF ur, out Vertex v1, out Vertex v2, out Vertex v3, out Vertex v4)
        {
            var d = Vector2.Normalize(p2 - p1);
            var dt = new Vector2(-d.Y, d.X) * (lineWidth / 2f);

            v1 = CreateVertex(p1 + dt, ur.TopLeft, color);
            v2 = CreateVertex(p1 - dt, ur.TopRight, color);
            v3 = CreateVertex(p2 - dt, ur.BottomRight, color);
            v4 = CreateVertex(p2 + dt, ur.BottomLeft, color);
        }

        private void ComputeCircleSegments(float radius, float maxError, float range, out float step, out int segments)
        {
            var invErrRad = 1 - maxError / radius;
            step = (float) Math.Acos(2 * invErrRad * invErrRad - 1);
            segments = (int) (range / step + 0.999f);
        }

        #endregion
    }
}
