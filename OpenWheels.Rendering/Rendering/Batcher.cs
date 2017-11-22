using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenWheels.GameTools.Rendering
{
    /// <summary>
    /// <para>
    ///   A platform-agnostic renderer that batches draw operations.
    ///   Uses the primitive drawing operation of a <see cref="IRenderer"/> to abstract the graphics back end.
    ///   Provides operations to draw basic shape outlines or fill basic shapes.
    /// </para>
    /// <para>
    ///   <see cref="Batcher"/>, like the name implies, attempts to batch as many sequential drawing operations
    ///   as possible. Operations can be batched as long as you do not change the graphics state, the texture
    ///   or the scissor rectangle. The Batcher API is stateful. All graphics state is set in between draw calls.
    ///   You can change the graphics state by setting the properties 
    ///   <see cref="BlendState"/>, <see cref="DepthState"/>, <see cref="RasterizerState"/> and 
    ///   <see cref="SamplerState"/>. You can set the texture by calling <see cref="SetTexture(int)"/> or 
    ///   <see cref="SetSprite(int,RectangleF)"/>. <seealso cref="Sprite"/>
    ///   Note that setting a sprite from the same texture will not finish a batch, since this just requires
    ///   to compute UV coordinates differently.
    /// </para>
    /// <para>
    ///   To abstract the texture and font representation, these data types are represented by an integer identifier.
    ///   For convenience the <see cref="IRenderer"/> can provide a string mapping to textures to allow users
    ///   to set textures by name using <see cref="SetTexture(string)"/> and <see cref="SetSprite(string,RectangleF)"/>.
    /// </para>
    /// <para>
    ///   Call <see cref="Finish"/> to send all queued batches to the <see cref="IRenderer"/> backend for actual rendering.
    /// </para>
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

        public IRenderer Renderer { get; set; }

        private readonly Vertex[] _vb;
        private readonly int[] _ib;

        private int _nextToDraw;
        private int _indicesInBatch;
        private GraphicsState _lastGraphicsState;
        private int _verticesSubmitted;

        private readonly List<BatchInfo> _batches;

        public Matrix4x4 TransformMatrix { get; private set; } = Matrix4x4.Identity;
        public Sprite Sprite { get; set; }
        public BlendState BlendState { get; set; }
        public DepthStencilState DepthState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public SamplerState SamplerState { get; set; }
        public Rectangle ScissorRect { get; set; }
        
        public int BatchCount { get; private set; }
        
        /// <summary>
        /// The value of this property is attached to a batch when it is finished.
        /// Can be used for custom rendering in combination with <see cref="Flush"/>
        /// to force finishing a batch when graphics state unknown to <see cref="Batcher"/>
        /// needs to be changed.
        /// </summary>
        public object BatchData { get; set; }
        
        public Batcher(IRenderer renderer)
        {
            Renderer = renderer;

            _vb = new Vertex[DefaultMaxVertices];
            _ib = new int[DefaultMaxIndices];

            _batches = new List<BatchInfo>();
        }
        
        #region Set Texture and Matrix

        public void SetMatrix(Matrix4x4 matrix)
        {
            TransformMatrix = matrix;
        }

        public void SetMatrix2D()
        {
            var vp = Renderer.GetViewport();
            TransformMatrix = Matrix4x4.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);
        }

        public void SetTexture(string texture)
        {
            SetTexture(Renderer.GetTexture(texture));
        }

        public void SetTexture(int texture)
        {
            var size = Renderer.GetTextureSize(texture);
            Sprite = new Sprite(texture, new RectangleF(0, 0, size.X, size.Y));
        }

        public void SetSprite(string texture, RectangleF uvs)
        {
            SetSprite(Renderer.GetTexture(texture), uvs);
        }

        public void SetSprite(int texture, RectangleF uvs)
        {
            Sprite = new Sprite(texture, uvs);
        }
        
        #endregion

        #region Line

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
        
        public void FillTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Color c)
        {
            FillTriangleStrip(Extensions.Yield(v1, v2, v3).Select(v => CreateVertex(v, Vector2.Zero, c)));
        }

        #endregion

        #region Rectangle

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

        public void DrawRoundedRect(RectangleF rectangle, float radius, int segments, Color color, int lineWidth = 1)
        {
            DrawRoundedRect(rectangle, radius, segments, radius, segments, radius, segments, radius, segments, color, lineWidth);
        }

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

        public void FillQuad(Vertex v0, Vertex v1, Vertex v2, Vertex v3)
        {
            CheckFlush();

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

        public void FillRect(RectangleF rect, Color c, RectangleF? uvRect = null)
        {
            FillRect(rect, c, c, c, c, uvRect);
        }

        public void FillRect(RectangleF rect, Color c1, Color c2, Color c3, Color c4, RectangleF? uvRect = null)
        {
            var r = uvRect ?? RectangleF.Unit;
            var v1 = CreateVertex(rect.TopLeft,     r.TopLeft,     c1);
            var v2 = CreateVertex(rect.TopRight,    r.TopRight,    c2);
            var v3 = CreateVertex(rect.BottomRight, r.BottomRight, c3);
            var v4 = CreateVertex(rect.BottomLeft,  r.BottomLeft,  c4);
            
            FillQuad(v1, v2, v3, v4);
        }

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
            innerRect = innerRect.Inflate(-radius, -radius);

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
            var tlRect = RectangleF.FromHalfExtents(tl, new Vector2(radius));
            var trRect = RectangleF.FromHalfExtents(tr, new Vector2(radius));
            var brRect = RectangleF.FromHalfExtents(br, new Vector2(radius));
            var blRect = RectangleF.FromHalfExtents(bl, new Vector2(radius));
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

        public void DrawCircle(Vector2 center, float radius, Color color, int sides, float lineWidth = 1)
        {
            DrawCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, sides, lineWidth);
        }

        public void DrawCircleSegment(Vector2 center, float radius, float start, float end, Color color, int sides, float lineWidth = 1)
        {
            var ps = CreateCircleSegment(center, radius, sides, start, end);
            DrawLineStrip(ps, color, lineWidth);
        }

        public void FillCircle(Vector2 center, float radius, Color color, int sides, RectangleF? uvRect = null)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, sides, uvRect);
        }

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
        /// Fill a triangle strip
        /// </summary>
        /// <param name="ps"></param>
        /// <exception cref="Exception"></exception>
        public void FillTriangleStrip(IEnumerable<Vertex> ps)
        {
            CheckFlush();

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

        protected void FillTriangleFan(Vertex center, IEnumerable<Vertex> vs)
        {
            CheckFlush();

            if (vs.CountLessThan(3))
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
            _lastGraphicsState = GraphicsState.Default;

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

            foreach (var batch in _batches)
                Renderer.DrawBatch(batch.GraphicsState, _vb, _ib, batch.Startindex, batch.IndexCount, batch.UserData);
        }

        protected void CheckFlush()
        {
            var gs = CreateCurrentGraphicsState();
            if (!_lastGraphicsState.Equals(gs))
                Flush();

            _lastGraphicsState = gs;
        }

        /// <summary>
        /// End the current batch.
        /// </summary>
        public void Flush()
        {
            // if nothing to flush
            if (_indicesInBatch == 0 && BatchData == null)
                return;

            var bi = new BatchInfo(_lastGraphicsState, _nextToDraw, _indicesInBatch, BatchData);
            _batches.Add(bi);

            _nextToDraw = _nextToDraw + _indicesInBatch;
            _indicesInBatch = 0;

            BatchCount++;
        }

        #endregion

        protected static Vertex CreateVertex(Vector2 p, Vector2 uv, Color c)
        {
            var v3 = new Vector3(p.X, p.Y, 0);
            return new Vertex(v3, uv, c);
        }

        protected int AddVertex(Vector2 position, Color color)
        {
            return AddVertex(CreateVertex(position, Vector2.Zero, color));
        }

        protected int AddVertex(Vector2 position, Color color, Vector2 uv)
        {
            return AddVertex(CreateVertex(position, uv, color));
        }

        protected int AddVertex(Vertex v)
        {
            TransformVertexPosition(ref v, TransformMatrix);
            var i = _verticesSubmitted;
            _vb[i] = v;
            _verticesSubmitted++;
            return i;
        }

        protected void AddIndex(int index)
        {
            var i = _nextToDraw + _indicesInBatch;
            _ib[i] = index;
            _indicesInBatch++;
        }

        private void TransformVertexPosition(ref Vertex vertex, Matrix4x4 transformMatrix)
        {
            var newPos = Vector3.Transform(vertex.Position, transformMatrix);
            vertex = new Vertex(newPos, vertex.Uv, vertex.Color);
        }

        private GraphicsState CreateCurrentGraphicsState()
        {
            return new GraphicsState(Sprite.Texture, BlendState, DepthState, RasterizerState, SamplerState, ScissorRect);
        }
    }
}
