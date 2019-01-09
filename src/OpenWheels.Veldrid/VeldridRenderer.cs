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

        private TextureStorage<Texture> _textureStorage;
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
        public VeldridRenderer(GraphicsDevice graphicsDevice, TextureStorage<Texture> textureStorage)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            GraphicsDevice = graphicsDevice;
            _currentTarget = GraphicsDevice.SwapchainFramebuffer;

            _textureStorage = textureStorage;
            _textureViews = new TextureView[textureStorage.TextureCount];
            _textureResourceSets = new ResourceSet[textureStorage.TextureCount];
            // We lazily create and cache texture views and resource sets when required.
            // When a texture is destroyed the matching cached values are destroyed as well.
            _textureStorage.TextureDestroyed += (s, a) => RemoveTextureResourceSet(a.TextureId);

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

            var texSet = GetTextureResourceSet(state.Texture);

            _commandList.SetGraphicsResourceSet(0, _wvpSet);
            _commandList.SetGraphicsResourceSet(1, texSet);
            _commandList.SetGraphicsResourceSet(2, sampler);

            // Issue a Draw command for a single instance with 4 indices.
            _commandList.DrawIndexed(
                indexCount: (uint) indexCount,
                instanceCount: 1,
                indexStart: (uint) startIndex,
                vertexOffset: 0,
                instanceStart: 0);
        }

        /// <summary>
        /// Lazily creates texture resource sets.
        /// </summary>
        private ResourceSet GetTextureResourceSet(int id)
        {
            var tex = _textureStorage.GetTexture(id);
            if (id < _textureResourceSets.Length)
            {
                GrowResourceSets();
            }

            if (_textureResourceSets[id] == null)
            {
                _textureViews[id] = GraphicsDevice.ResourceFactory.CreateTextureView(tex);
                _textureResourceSets[id] = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    _textureLayout,
                    _textureViews[id]));
            }

            return _textureResourceSets[id];
        }

        private void GrowResourceSets()
        {
            Array.Resize(ref _textureViews, _textureViews.Length * 2);
            Array.Resize(ref _textureResourceSets, _textureResourceSets.Length * 2);
        }

        private void RemoveTextureResourceSet(int id)
        {
            if (id < _textureResourceSets.Length && _textureResourceSets[id] != null)
            {
                _textureViews[id].Dispose();
                _textureViews[id] = null;
                _textureResourceSets[id].Dispose();
                _textureResourceSets[id] = null;
            }
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
            var gpd = new GraphicsPipelineDescription();
            gpd.BlendState = BlendStateDescription.SingleAlphaBlend;
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
