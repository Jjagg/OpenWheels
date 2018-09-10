using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using OpenWheels.Rendering;

using Veldrid;

namespace OpenWheels.Veldrid
{
    /// <summary>
    /// <see cref="IRenderer"/> implementation that uses Veldrid to render.
    /// </summary>
    public class VeldridRenderer : IRenderer, IDisposable
    {
        public GraphicsDevice GraphicsDevice { get; }
        private const int InitialTextureCount = 64;

        private readonly List<int> _freeIds;
        private Texture[] _textures;
        private TextureView[] _textureViews;
        private ResourceSet[] _textureResourceSets;
        private Lazy<ResourceSet>[] _samplerResourceSets;

        private Sampler _linearClamp;
        private Sampler _pointClamp;
        private Sampler _anisotropicClamp;

        private CommandList _commandList;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private ShaderSetDescription _shaderSet;

        private ResourceLayout _wvpLayout;
        private ResourceLayout _textureLayout;
        private ResourceLayout _samplerLayout;
        private ResourceLayout[] _resourceLayouts;
        private ResourceSet _wvpSet;
        private DeviceBuffer _wvpBuffer;

        private Dictionary<GraphicsState, Pipeline> _pipelines;

        private bool _disposed;

        private Framebuffer _currentTarget;

        /// <summary>
        /// Create a new renderer.
        /// </summary>
        /// <param name="graphicsDevice">The <see cref="global::Veldrid.GraphicsDevice"/> to render with.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="graphicsDevice"/> is <c>null</c>.</exception>
        public VeldridRenderer(GraphicsDevice graphicsDevice)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            GraphicsDevice = graphicsDevice;
            _currentTarget = GraphicsDevice.SwapchainFramebuffer;

            _freeIds = new List<int>(InitialTextureCount);
            _freeIds.AddRange(Enumerable.Range(0, InitialTextureCount));
            _textures = new Texture[InitialTextureCount];
            _textureViews = new TextureView[InitialTextureCount];
            _textureResourceSets = new ResourceSet[InitialTextureCount];

            CreateResources();
            _pipelines = new Dictionary<GraphicsState, Pipeline>();
        }

        private void CreateResources()
        {
            var rf = GraphicsDevice.ResourceFactory;

            var vbDescription = new BufferDescription(
                Batcher.InitialMaxVertices * Vertex.SizeInBytes,
                BufferUsage.VertexBuffer);
            _vertexBuffer = rf.CreateBuffer(ref vbDescription);
            var ibDescription = new BufferDescription(
                Batcher.InitialMaxIndices * sizeof(int),
                BufferUsage.IndexBuffer);
            _indexBuffer = rf.CreateBuffer(ref ibDescription);

            _commandList = rf.CreateCommandList();

            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm),
                new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2));

            _vertexShader = VeldridHelper.LoadShader(rf, "SpriteShader", ShaderStages.Vertex, "VS");
            _fragmentShader = VeldridHelper.LoadShader(rf, "SpriteShader", ShaderStages.Fragment, "FS");

            _shaderSet = new ShaderSetDescription(
                new[] {vertexLayout},
                new[] {_vertexShader, _fragmentShader});

            _wvpLayout = rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Wvp", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            _textureLayout = rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));
            _samplerLayout = rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            CreateSamplerResourceSets();

            _resourceLayouts = new[] {_wvpLayout, _textureLayout, _samplerLayout};

            _wvpBuffer = rf.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            UpdateWvp();

            _wvpSet = rf.CreateResourceSet(new ResourceSetDescription(_wvpLayout, _wvpBuffer));
        }

        private void CreateSamplerResourceSets()
        {
            _samplerResourceSets = new Lazy<ResourceSet>[6];

            // Linear Clamp
            _samplerResourceSets[0] = new Lazy<ResourceSet>(() =>
            {
                var linearClampDescr = SamplerDescription.Linear;
                linearClampDescr.AddressModeU = SamplerAddressMode.Clamp;
                linearClampDescr.AddressModeV = SamplerAddressMode.Clamp;
                linearClampDescr.AddressModeW = SamplerAddressMode.Clamp;
                _linearClamp = GraphicsDevice.ResourceFactory.CreateSampler(linearClampDescr);
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, _linearClamp);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);

            // Linear Wrap
            _samplerResourceSets[1] = new Lazy<ResourceSet>(() =>
            {
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, GraphicsDevice.LinearSampler);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);

            // Point Clamp
            _samplerResourceSets[2] = new Lazy<ResourceSet>(() =>
            {
                var pointClampDescr = SamplerDescription.Point;
                pointClampDescr.AddressModeU = SamplerAddressMode.Clamp;
                pointClampDescr.AddressModeV = SamplerAddressMode.Clamp;
                pointClampDescr.AddressModeW = SamplerAddressMode.Clamp;
                _pointClamp = GraphicsDevice.ResourceFactory.CreateSampler(pointClampDescr);
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, _pointClamp);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);

            // Point Wrap
            _samplerResourceSets[3] = new Lazy<ResourceSet>(() =>
            {
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, GraphicsDevice.PointSampler);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);

            // Anisotropic Clamp
            _samplerResourceSets[4] = new Lazy<ResourceSet>(() =>
            {
                var anisoClampDescr = SamplerDescription.Aniso4x;
                anisoClampDescr.AddressModeU = SamplerAddressMode.Clamp;
                anisoClampDescr.AddressModeV = SamplerAddressMode.Clamp;
                anisoClampDescr.AddressModeW = SamplerAddressMode.Clamp;
                _anisotropicClamp = GraphicsDevice.ResourceFactory.CreateSampler(anisoClampDescr);
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, _anisotropicClamp);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);

            // Anisotropic Wrap
            _samplerResourceSets[5] = new Lazy<ResourceSet>(() =>
            {
                var resourceSetDescr = new ResourceSetDescription(_samplerLayout, GraphicsDevice.Aniso4xSampler);
                return GraphicsDevice.ResourceFactory.CreateResourceSet(ref resourceSetDescr);
            }, false);
        }

        /// <summary>
        /// Set the render target to the swapchain framebuffer of the <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <seealso cref="global::Veldrid.GraphicsDevice.SwapchainFramebuffer"/>
        public void SetTarget()
        {
            SetTarget(GraphicsDevice.SwapchainFramebuffer);
        }

        /// <summary>
        /// Set the render target.
        /// </summary>
        /// <param name="target">The framebuffer to render to.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="target"/> is <c>null</c>.</exception>
        public void SetTarget(Framebuffer target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target != _currentTarget && !target.OutputDescription.Equals(_currentTarget.OutputDescription))
            {
                // TODO graphics pipelines
                throw new NotImplementedException();
            }
            _currentTarget = target;
        }

        /// <summary>
        /// Update the transformation matrix so coordinates match pixel coordinates on the
        /// currently active <see cref="Framebuffer"/>. Call this after changing the render
        /// target with <see cref="SetTarget"/> or when the active <see cref="Framebuffer"/> is resized.
        /// </summary>
        public void UpdateWvp()
        {
            UpdateWvp((int) _currentTarget.Width, (int) _currentTarget.Height);
        }

        private void UpdateWvp(int width, int height)
        {
            var wvp = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            GraphicsDevice.UpdateBuffer(_wvpBuffer, 0, ref wvp);
        }

        private void Grow()
        {
            _freeIds.AddRange(Enumerable.Range(_textures.Length, _textures.Length));
            Array.Resize(ref _textures, _textures.Length * 2);
            Array.Resize(ref _textureViews, _textureViews.Length * 2);
            Array.Resize(ref _textureResourceSets, _textureResourceSets.Length * 2);
        }

        /// <summary>
        /// Register a texture.
        /// </summary>
        /// <param name="texture">The texture to register.</param>
        /// <returns>The identifier of the texture.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="texture"/> is <c>null</c>.</exception>
        public int Register(Texture texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            if (_freeIds.Count == 0)
                Grow();

            var id = _freeIds[0];
            _freeIds.RemoveAt(0);
            _textures[id] = texture;
            _textureViews[id] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);
            _textureResourceSets[id] = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                _textureLayout,
                _textureViews[id]));

            return id;
        }

        /// <summary>
        /// Release a registered texture. If no texture with the given name is registered, nothing happens.
        /// The texture itself is not disposed by this method.
        /// </summary>
        /// <param name="id">The id of the texture to release.</param>
        public void Release(int id)
        {
            if (_textures[id] == null)
                return;

            _textures[id] = null;
            _textureViews[id].Dispose();
            _textureViews[id] = null;
            _textureResourceSets[id].Dispose();
            _textureResourceSets[id] = null;
            _freeIds.Insert(~_freeIds.BinarySearch(id), id);
        }

        /// <inheritdoc />
        public unsafe int RegisterTexture(Span<Color> pixels, int width, int height)
        {
            var factory = GraphicsDevice.ResourceFactory;
            Texture staging = factory.CreateTexture(
                TextureDescription.Texture2D((uint) width, (uint) height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));

            Texture ret = factory.CreateTexture(
                TextureDescription.Texture2D((uint) width, (uint) height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

            CommandList cl = factory.CreateCommandList();
            cl.Begin();

            MappedResource map = GraphicsDevice.Map(staging, MapMode.Write, 0);
            if (width == map.RowPitch / 4)
            {
                var dst = new Span<Color>(map.Data.ToPointer(), width * height);
                pixels.CopyTo(dst);
            }
            else
            {
                for (var y = 0; y < height; y++)
                {
                    var dst = new Span<Color>(IntPtr.Add(map.Data, y * (int) map.RowPitch).ToPointer(), width);
                    var src = pixels.Slice(y * width, width);
                    src.CopyTo(dst);
                }
            }

            GraphicsDevice.Unmap(staging);

            cl.CopyTexture(staging, ret);
            cl.End();

            GraphicsDevice.SubmitCommands(cl);
            GraphicsDevice.DisposeWhenIdle(staging);
            GraphicsDevice.DisposeWhenIdle(cl);

            return Register(ret);
        }

        /// <inheritdoc />
        public Size GetTextureSize(int texture)
        {
            var t = _textures[texture];
            return new Size((int) t.Width, (int) t.Height);
        }

        /// <inheritdoc />
        public Rectangle GetViewport()
        {
            return new Rectangle(0, 0, (int) _currentTarget.Width, (int) _currentTarget.Height);
        }

        /// <summary>
        /// Clear the background.
        /// </summary>
        /// <param name="color">Color to set the background to.</param>
        public void Clear(Color color)
        {
            if (_disposed)
                throw new ObjectDisposedException("Can't use renderer after it has been disposed.");

            _commandList.Begin();

            _commandList.SetFramebuffer(_currentTarget);
            var c = new RgbaFloat(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            _commandList.ClearColorTarget(0, c);
            _commandList.End();

            GraphicsDevice.SubmitCommands(_commandList);
        }

        /// <inheritdoc />
        public void BeginRender(Vertex[] vertexBuffer, int[] indexBuffer, int vertexCount, int indexCount)
        {
            if (_disposed)
                throw new ObjectDisposedException("Can't use renderer after it has been disposed.");

            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, ref vertexBuffer[0], (uint) (vertexCount * Vertex.SizeInBytes));
            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, ref indexBuffer[0], (uint) (indexCount * sizeof(int)));

            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            _commandList.SetFramebuffer(_currentTarget);

            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
        }

        /// <inheritdoc />
        public void DrawBatch(GraphicsState state, int startIndex, int indexCount, object batchUserData)
        {
            if (!_pipelines.TryGetValue(state, out var pipeline))
                pipeline = AddPipeline(state);

            _commandList.SetPipeline(pipeline);

            var sampler = GetSamplerResourceSet(state.SamplerState);

            _commandList.SetGraphicsResourceSet(0, _wvpSet);
            _commandList.SetGraphicsResourceSet(1, _textureResourceSets[state.Texture]);
            _commandList.SetGraphicsResourceSet(2, sampler);

            // Issue a Draw command for a single instance with 4 indices.
            _commandList.DrawIndexed(
                indexCount: (uint) indexCount,
                instanceCount: 1,
                indexStart: (uint) startIndex,
                vertexOffset: 0,
                instanceStart: 0);
        }

        private ResourceSet GetSamplerResourceSet(SamplerState samplerState)
        {
            return _samplerResourceSets[(int) samplerState].Value;
        }

        /// <inheritdoc />
        public void EndRender()
        {
            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            GraphicsDevice.SubmitCommands(_commandList);
        }

        private Pipeline AddPipeline(GraphicsState state)
        {
            var bds = state.BlendState == BlendState.AlphaBlend
                ? BlendStateDescription.SingleAlphaBlend
                : BlendStateDescription.SingleOverrideBlend;
            var gpd = new GraphicsPipelineDescription();
            gpd.BlendState = bds;
            gpd.DepthStencilState = DepthStencilStateDescription.Disabled;
            gpd.RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid,
                FrontFace.Clockwise, true, state.UseScissorRect);
            gpd.PrimitiveTopology = PrimitiveTopology.TriangleList;
            gpd.ShaderSet = _shaderSet;
            gpd.ResourceLayouts = _resourceLayouts;
            gpd.Outputs = _currentTarget.OutputDescription;
            var pipeline = GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(gpd);
            _pipelines[state] = pipeline;
            return pipeline;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var pipeline in _pipelines.Values)
                pipeline.Dispose();

            foreach (var t in _textureResourceSets)
                t?.Dispose();
            foreach (var t in _textureViews)
                t?.Dispose();
            _textureLayout.Dispose();

            _wvpSet.Dispose();
            _wvpBuffer.Dispose();
            _wvpLayout.Dispose();

            foreach (var s in _samplerResourceSets)
            {
                if (s.IsValueCreated)
                    s.Value.Dispose();
            }

            _linearClamp?.Dispose();
            _pointClamp?.Dispose();
            _anisotropicClamp?.Dispose();

            _samplerLayout.Dispose();

            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();

            _disposed = true;
        }
    }

    internal static class VeldridHelper
    {
        public static Stream OpenEmbeddedAssetStream(string name, Type t) => t.Assembly.GetManifestResourceStream(name);

        public static Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = $"{set}-{stage.ToString().ToLower()}.{GetExtension(factory.BackendType)}";
            return factory.CreateShader(new ShaderDescription(stage, ReadEmbeddedAssetBytes(name), entryPoint));
        }

        public static byte[] ReadEmbeddedAssetBytes(string name)
        {
            using (Stream stream = OpenEmbeddedAssetStream(name, typeof(VeldridHelper)))
            {
                var info = typeof(VeldridHelper).Assembly.GetManifestResourceInfo(name);
                byte[] bytes = new byte[stream.Length];
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    stream.CopyTo(ms);
                    return bytes;
                }
            }
        }

        private static string GetExtension(GraphicsBackend backendType)
        {
            return (backendType == GraphicsBackend.Direct3D11)
                ? "hlsl.bytes"
                : (backendType == GraphicsBackend.Vulkan)
                    ? "450.glsl.spv"
                    : (backendType == GraphicsBackend.Metal)
                        ? "ios.metallib"
                        : (backendType == GraphicsBackend.OpenGL)
                            ? "330.glsl"
                            : "300.glsles";
        }
    }
}
