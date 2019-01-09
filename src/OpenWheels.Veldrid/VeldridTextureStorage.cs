using System;
using System.Collections.Generic;
using System.Linq;
using OpenWheels.Rendering;
using Veldrid;

namespace OpenWheels.Veldrid
{
    /// <summary>
    /// A texture storage for Velrid <see cref="Texture"/> instances.
    /// </summary>
    public class VeldridTextureStorage : TextureStorage<Texture>, IDisposable
    {
        public const int InitialTextureCount = 64;

        private int _textureCount;

        private GraphicsDevice _gd;

        private CommandList _setDataCommandList;
        private Texture _staging;

        private readonly List<int> _freeIds;
        private Texture[] _textures;

        public override int TextureCount => _textureCount;

        /// <summary>
        /// Create a texture storage for Veldrid textures.
        /// </summary>
        /// <exception name="ArgumentNullException">If <paramref name="rf"/> is <c>null</c>.</exception>
        public VeldridTextureStorage(GraphicsDevice gd)
        {
            if (gd == null)
                throw new ArgumentNullException(nameof(gd));

            _gd = gd;
            _setDataCommandList = _gd.ResourceFactory.CreateCommandList();

            _freeIds = new List<int>(InitialTextureCount);
            _freeIds.AddRange(Enumerable.Range(0, InitialTextureCount));
            _textures = new Texture[InitialTextureCount];
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Created textures are 2D, non-multisampled, non-mipmapped textures with RGBA8 UNorm format and
        /// texture usage <see cref="TextureUsage.Sampled"/>.
        /// </remarks>
        public override int CreateTexture(int width, int height)
        {
            CheckDisposed();

            var textureDescr = TextureDescription.Texture2D((uint) width, (uint) height, 0, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            var tex = _gd.ResourceFactory.CreateTexture(ref textureDescr);
            return AddTexture(tex);
        }

        /// <inheritdoc/>
        public override void DestroyTexture(int id)
        {
            CheckDisposed();

            if (_textures[id] == null)
                return;

            _textures[id] = null;
            _freeIds.Insert(~_freeIds.BinarySearch(id), id);
            _textureCount--;

            OnTextureDestroyed(id);
        }

        /// <inheritdoc/>
        public override Texture GetTexture(int id) 
        {
            CheckDisposed();
            return _textures[id];
        }

        public override Size GetTextureSize(int id)
        {
            CheckDisposed();

            var tex = _textures[id];
            return new Size((int) tex.Width, (int) tex.Height);
        }

        public unsafe override void SetData(int id, Span<Color> data)
        {
            CheckDisposed();

            var tex = _textures[id];
            if (data.Length != tex.Width * tex.Height)
                throw new ArgumentException($"Length of data (${data.Length}) did not match width * height of the texture (${tex.Width * tex.Height}).", nameof(data));

            CopyData(tex, data, 0, 0, tex.Width, tex.Height);
        }

        public override void SetData(int id, in Rectangle subRect, Span<Color> data)
        {
            CheckDisposed();

            var tex = _textures[id];
            if (data.Length != tex.Width * tex.Height)
                throw new ArgumentException($"Length of data (${data.Length}) did not match width * height of the texture (${tex.Width * tex.Height}).", nameof(data));

            CopyData(tex, data, (uint) subRect.X, (uint) subRect.Y, (uint) subRect.Width, (uint) subRect.Height);
        }

        #region IDisposable

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var t in _textures)
                        t?.Dispose();

                    _setDataCommandList.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose() => Dispose(true);

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Cannot use this texture storage after it has been disposed.");
        }

        #endregion

        private void Grow()
        {
            _freeIds.AddRange(Enumerable.Range(_textures.Length, _textures.Length));
            Array.Resize(ref _textures, _textures.Length * 2);
        }

        private int AddTexture(Texture texture)
        {
            if (_freeIds.Count == 0)
                Grow();

            var id = _freeIds[0];
            _freeIds.RemoveAt(0);
            _textures[id] = texture;

            _textureCount++;
            OnTextureCreated(id, (int) texture.Width, (int) texture.Height);

            return id;
        }

        private unsafe void CopyData(Texture tex, Span<Color> data, uint tx, uint ty, uint width, uint height)
        {
            var staging = GetStaging(width, height);

            _setDataCommandList.Begin();

            var map = _gd.Map(staging, MapMode.Write, 0);
            if (width == map.RowPitch / 4)
            {
                var dst = new Span<Color>(map.Data.ToPointer(), (int) (width * height));
                data.CopyTo(dst);
            }
            else
            {
                var dataPtr = (byte*) map.Data.ToPointer();
                for (var y = 0; y < height; y++)
                {
                    var dst = new Span<Color>(dataPtr + y * (int) map.RowPitch, (int) width);
                    var src = data.Slice(y * (int) width, (int) width);
                    src.CopyTo(dst);
                }
            }

            _gd.Unmap(staging);

            _setDataCommandList.CopyTexture(staging, 0, 0, 0, 0, 0, tex, tx, ty, 0, 0, 0, width, height, 0, 1);
            _setDataCommandList.End();

            _gd.SubmitCommands(_setDataCommandList);
        }

        private Texture GetStaging(uint width, uint height)
        {
            if (_staging == null)
            {
                var td = new TextureDescription(width, height, 1, 0, 0, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging, TextureType.Texture2D);
                _staging = _gd.ResourceFactory.CreateTexture(ref td);
            }
            else if (_staging.Width < width || _staging.Height < height)
            {
                var newWidth = width > _staging.Width ? width : _staging.Width;
                var newHeight = height > _staging.Height ? height : _staging.Height;
                var td = new TextureDescription(newWidth, newHeight, 1, 0, 0, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging, TextureType.Texture2D);
                _gd.DisposeWhenIdle(_staging);
                _staging = _gd.ResourceFactory.CreateTexture(ref td);
            }

            return _staging;
        }
    }
}
