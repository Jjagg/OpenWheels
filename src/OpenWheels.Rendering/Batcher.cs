using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        public const int DefaultMaxVertices = 2048;
        public const int DefaultMaxIndices = 4096;

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

        private readonly Vertex[] _vb;
        private readonly int[] _ib;

        private int _nextToDraw;
        private int _indicesInBatch;
        private GraphicsState _lastGraphicsState;
        private int _verticesSubmitted;

        private readonly List<BatchInfo> _batches;
        private Sprite _sprite;
        private RectangleF _spriteUv;
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
                if (value.Texture != _lastGraphicsState.Texture)
                    Flush();
                _sprite = value;
                var texSize = Renderer.GetTextureSize(_sprite.Texture);
                _spriteUv = new RectangleF(
                    (float) _sprite.SrcRect.X / texSize.X,
                    (float) _sprite.SrcRect.Y / texSize.Y,
                    (float) _sprite.SrcRect.Width / texSize.X,
                    (float) _sprite.SrcRect.Height / texSize.Y);
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
        /// Create a <see cref="Batcher"/> with a <see cref="NullRenderer"/>.
        /// </summary>
        public Batcher()
            : this(new NullRenderer())
        {
        }

        /// <summary>
        /// Create a batcher with a renderer.
        /// </summary>
        /// <param name="renderer">Renderer to execute draw calls.</param>
        public Batcher(IRenderer renderer)
        {
            Renderer = renderer;

            _vb = new Vertex[DefaultMaxVertices];
            _ib = new int[DefaultMaxIndices];

            _batches = new List<BatchInfo>();
            _lastGraphicsState = GraphicsState.Default;

            TransformMatrix = Matrix4x4.Identity;
        }

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
            Sprite = new Sprite(texture, new Rectangle(0, 0, size.X, size.Y));
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
        public void DrawLine(Vector2 p1, Vector2 p2, Color color, float lineWidth = 1, RectangleF? uvRect = null)
        {
            var d = Vector2.Normalize(p2 - p1);
            var dt = new Vector2(-d.Y, d.X) * (lineWidth / 2f);

            var ur = uvRect ?? RectangleF.Unit;

            var v1 = CreateVertex(p1 - dt, ur.TopLeft, color);
            var v2 = CreateVertex(p1 + dt, ur.TopRight, color);
            var v3 = CreateVertex(p2 + dt, ur.BottomRight, color);
            var v4 = CreateVertex(p2 - dt, ur.BottomLeft, color);
            FillQuad(v1, v2, v3, v4);
        }

        /// <summary>
        /// Draw a line strip. Draws lines between subsequent passed points.
        /// </summary>
        /// <param name="points">The points on the line strip.</param>
        /// <param name="color">Color of the line strip.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <exception cref="Exception">If <paramref name="points"/> has less than 2 points.</exception>
        public void DrawLineStrip(IEnumerable<Vector2> points, Color color, float lineWidth = 1)
        {
            if (points.CountLessThan(2))
                throw new Exception("Need at least 2 vertices for a line strip.");

            var p1 = points.First();
            foreach (var p2 in points.Skip(1))
            {
                DrawLine(p1, p2, color, lineWidth);
                p1 = p2;
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
            FillTriangleStrip(Extensions.Yield(v1, v2, v3).Select(v => CreateVertex(v, Vector2.Zero, c)));
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
        /// <param name="segments">Number of segments for triangulation of the corners.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="lineWidth">Stroke width.</param>
        public void DrawRoundedRect(RectangleF rectangle, float radius, int segments, Color color, int lineWidth = 1)
        {
            DrawRoundedRect(rectangle, radius, segments, radius, segments, radius, segments, radius, segments, color, lineWidth);
        }

        /// <summary>
        /// Draw the outline of a rectangle with rounded corners.
        /// </summary>
        /// <param name="rectangle">The outer bounds of the rectangle.</param>
        /// <param name="radiusTl">Radius of the top left corner.</param>
        /// <param name="segmentsTl">Number of segments for triangulation for the top left corner.</param>
        /// <param name="radiusTr">Radius of the top right corner.</param>
        /// <param name="segmentsTr">Number of segments for triangulation for the top right corner.</param>
        /// <param name="radiusBr">Radius of the bottom right corner.</param>
        /// <param name="segmentsBr">Number of segments for triangulation for the bottom right corner.</param>
        /// <param name="radiusBl">Radius of the bottom left corner.</param>
        /// <param name="segmentsBl">Number of segments for triangulation for the bottom left corner.</param>
        /// <param name="color">Color of the outline.</param>
        /// <param name="lineWidth">Stroke width.</param>
        /// <exception cref="Exception">
        ///   If any of the radii is larger than half the width or height of the rectangle.
        /// </exception>
        public void DrawRoundedRect(RectangleF rectangle,
            float radiusTl, int segmentsTl,
            float radiusTr, int segmentsTr,
            float radiusBr, int segmentsBr,
            float radiusBl, int segmentsBl,
            Color color, int lineWidth = 1)
        {
            if (radiusTl > rectangle.Width / 2f || radiusTl > rectangle.Height / 2f ||
                radiusTr > rectangle.Width / 2f || radiusTr > rectangle.Height / 2f ||
                radiusBr > rectangle.Width / 2f || radiusBr > rectangle.Height / 2f ||
                radiusBl > rectangle.Width / 2f || radiusBl > rectangle.Height / 2f)
                throw new Exception("Radius too large");

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
                DrawCircleSegment(tl, radiusTl, LeftAngle, TopAngle, color, segmentsTl, lineWidth);
            if (radiusTr > 0)
                DrawCircleSegment(tr, radiusTr, TopAngle, RightEndAngle, color, segmentsTr, lineWidth);
            if (radiusBr > 0)
                DrawCircleSegment(br, radiusBr, RightStartAngle, BotAngle, color, segmentsBr, lineWidth);
            if (radiusBl > 0)
                DrawCircleSegment(bl, radiusBl, BotAngle, LeftAngle, color, segmentsBl, lineWidth);
        }

        /// <summary>
        /// Fill a quad.
        /// </summary>
        /// <param name="v0">The first vertex.</param>
        /// <param name="v1">The second vertex.</param>
        /// <param name="v2">The third vertex.</param>
        /// <param name="v3">The fourth vertex.</param>
        public void FillQuad(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
        {
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
        /// <param name="segments">Number of segments to triangulate the corners.</param>
        /// <param name="color">Color to fill with.</param>
        /// <param name="uvRect">Uv rectangle inside the sprite to source. Probably not very useful for user code.</param>
        /// <exception cref="Exception">If the radius is larger than half the width or height of the rectangle.</exception>
        public void FillRoundedRect(RectangleF rectangle, float radius, int segments, Color color, RectangleF? uvRect = null)
        {
            if (radius > rectangle.Width / 2f || radius > rectangle.Height / 2f)
                throw new Exception("Radius too large");

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
            FillCircleSegment(tl, radius, LeftAngle,       TopAngle,      color, segments, MathHelper.LinearMap(tlRect, outerRect, ur));
            FillCircleSegment(tr, radius, TopAngle,        RightEndAngle, color, segments, MathHelper.LinearMap(trRect, outerRect, ur));
            FillCircleSegment(br, radius, RightStartAngle, BotAngle,      color, segments, MathHelper.LinearMap(brRect, outerRect, ur));
            FillCircleSegment(bl, radius, BotAngle,        LeftAngle,     color, segments, MathHelper.LinearMap(blRect, outerRect, ur));
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
        /// <param name="sides">Number of sides to the circle for triangulation.</param>
        /// <param name="lineWidth">Stroke width of the outline.</param>
        public void DrawCircle(Vector2 center, float radius, Color color, int sides, float lineWidth = 1)
        {
            DrawCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, sides, lineWidth);
        }

        /// <summary>
        /// Draw the outline of a circle segment.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="start">Start angle of the segment in radians. Angle of 0 is right (positive x-axis).</param>
        /// <param name="end">End angle of the segment in radians.</param>
        /// <param name="color">Color of the circle segment.</param>
        /// <param name="sides">Number of sides to the circle for triangulation.</param>
        /// <param name="lineWidth">Stroke width of the outline.</param>
        public void DrawCircleSegment(Vector2 center, float radius, float start, float end, Color color, int sides, float lineWidth = 1)
        {
            var ps = CreateCircleSegment(center, radius, sides, start, end);
            DrawLineStrip(ps, color, lineWidth);
        }

        /// <summary>
        /// Fill a circle.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="color">Color of the circle.</param>
        /// <param name="sides">Number of sides to the circle for triangulation.</param>
        /// <param name="uvRect">
        ///   The rectangle inside the sprite to source uv coordinates from.
        ///   Probably not very useful for user code.
        /// </param>
        public void FillCircle(Vector2 center, float radius, Color color, int sides, RectangleF? uvRect = null)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, sides, uvRect);
        }

        /// <summary>
        /// Fill a circle segment.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="start">Start angle of the segment in radians. Angle of 0 is right (positive x-axis).</param>
        /// <param name="end">End angle of the segment in radians.</param>
        /// <param name="color">Color of the circle segment.</param>
        /// <param name="sides">Number of sides to the circle for triangulation.</param>
        /// <param name="uvRect">
        ///   The rectangle inside the sprite to source uv coordinates from.
        ///   Probably not very useful for user code.
        /// </param>
        public void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, int sides, RectangleF? uvRect = null)
        {
            var ps = CreateCircleSegment(center, radius, sides, start, end);
            var fromRect = RectangleF.FromHalfExtents(center, new Vector2(radius));
            var toRect = uvRect ?? RectangleF.Unit;
            var vs = ps.Select(p => CreateVertex(p, MathHelper.LinearMap(p, fromRect, toRect), color));
            var vCenter = CreateVertex(center, MathHelper.LinearMap(center, fromRect, toRect), color);
            FillTriangleFan(vCenter, vs);
        }

        private static IEnumerable<Vector2> CreateCircleSegment(Vector2 center, float radius, int sides, float start, float end)
        {
            var step = (end - start) / sides;
            var theta = start;

            for (var i = 0; i < sides; i++)
            {
                yield return center + new Vector2((float) (radius * Math.Cos(theta)), (float) (radius * Math.Sin(theta)));
                theta += step;
            }
            yield return center + new Vector2((float) (radius * Math.Cos(end)), (float) (radius * Math.Sin(end)));
        }
 
        #endregion

        #region Low level

        /// <summary>
        /// Fill a triangle strip.
        /// </summary>
        /// <param name="ps">Vertices of the triangle strip.</param>
        /// <exception cref="Exception">If less than 3 vertices are passed.</exception>
        public void FillTriangleStrip(IEnumerable<Vertex> ps)
        {
            if (ps.CountLessThan(3))
                throw new Exception("Need at least 3 vertices for a triangle strip.");

            using (var en = ps.GetEnumerator())
            {
                en.MoveNext();
                var v1 = AddVertex(en.Current);
                en.MoveNext();
                var v2 = AddVertex(en.Current);

                while (en.MoveNext())
                {
                    var v3 = AddVertex(en.Current);
                    AddIndex(v1);
                    AddIndex(v2);
                    AddIndex(v3);
                    v1 = v2;
                    v2 = v3;
                }
            }
        }

        /// <summary>
        /// Fill a triangle fan.
        /// </summary>
        /// <param name="center">The center vertex.</param>
        /// <param name="vs">The other vertices.</param>
        /// <exception cref="Exception">If <paramref name="vs"/> has less than 2 vertices.</exception>
        public void FillTriangleFan(Vertex center, IEnumerable<Vertex> vs)
        {
            if (vs.CountLessThan(2))
                throw new Exception("Need at least 3 vertices for a triangle fan.");

            using (var en = vs.GetEnumerator())
            {
                en.MoveNext();
                var centerIndex = AddVertex(center);
                var v0 = AddVertex(en.Current);
                var v1 = v0;
                while (en.MoveNext())
                {
                    var v2 = AddVertex(en.Current);
                    AddIndex(centerIndex);
                    AddIndex(v1);
                    AddIndex(v2);
                    v1 = v2;
                }
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
            _verticesSubmitted = 0;

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

            Renderer.BeginRender();

            foreach (var batch in _batches)
                Renderer.DrawBatch(batch.GraphicsState, _vb, _ib, batch.Startindex, batch.IndexCount, batch.UserData);

            Renderer.EndRender();
        }

        private void CheckFlush()
        {
            if (_indicesInBatch == 0 && BatchData == null)
                return;

            var gs = CreateCurrentGraphicsState();
            if (!_lastGraphicsState.Equals(gs))
                Flush(gs);
        }

        /// <summary>
        /// End the current batch.
        /// </summary>
        public void Flush()
        {
            var gs = CreateCurrentGraphicsState();
            Flush(gs);
        }

        private void Flush(GraphicsState gs)
        {
            _lastGraphicsState = gs;
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

        private Vertex CreateVertex(Vector2 p, Vector2 uv, Color c)
        {
            // remap uv from unit rectangle to the uv rectangle of our sprite
            var actualUv = MathHelper.LinearMap(uv, RectangleF.Unit, _spriteUv);
            if (_useMatrix)
                p = Vector2.Transform(p, TransformMatrix);
            var v3 = new Vector3(p.X, p.Y, 0);
            return new Vertex(v3, actualUv, c);
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
            TransformVertexPosition(ref v, ref _transformMatrix);
            var i = _verticesSubmitted;
            _vb[i] = v;
            _verticesSubmitted++;
            return i;
        }

        private void AddIndex(int index)
        {
            var i = _nextToDraw + _indicesInBatch;
            _ib[i] = index;
            _indicesInBatch++;
        }

        private static void TransformVertexPosition(ref Vertex vertex, ref Matrix4x4 transformMatrix)
        {
            var newPos = Vector3.Transform(vertex.Position, transformMatrix);
            vertex = new Vertex(newPos, vertex.Uv, vertex.Color);
        }

        private GraphicsState CreateCurrentGraphicsState()
        {
            return new GraphicsState(Sprite.Texture, BlendState, SamplerState, ScissorRect);
        }
    }
}
