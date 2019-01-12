using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public override int CreateTexture(int width, int height, TextureFormat format)
        {
            CheckDisposed();
            PixelFormat pFormat;
            switch (format)
            {
                case TextureFormat.Red8:
                    pFormat = PixelFormat.R8_UNorm;
                    break;
                case TextureFormat.Rgba32:
                    pFormat = PixelFormat.R8_G8_B8_A8_UNorm;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }

            var textureDescr = TextureDescription.Texture2D((uint) width, (uint) height, 1, 1, pFormat, TextureUsage.Sampled);
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
        public override bool HasTexture(int id)
        {
            CheckDisposed();
            return id < _textures.Length && _textures[id] != null;
        }

        /// <inheritdoc/>
        public override Texture GetTexture(int id) 
        {
            CheckDisposed();
            return _textures[id];
        }

        /// <inheritdoc/>
        public override Size GetTextureSize(int id)
        {
            CheckDisposed();

            var tex = _textures[id];
            return new Size((int) tex.Width, (int) tex.Height);
        }

        /// <inheritdoc/>
        public override TextureFormat GetTextureFormat(int id)
        {
            CheckDisposed();
            var tex = _textures[id];
            return tex.Format == PixelFormat.R8_UNorm ? TextureFormat.Red8 : TextureFormat.Rgba32;
        }

        /// <inheritdoc/>
        public unsafe override void SetData<T>(int id, ReadOnlySpan<T> data)
        {
            CheckDisposed();

            var tex = _textures[id];
            var bpp = tex.Format == PixelFormat.R8_UNorm ? 1 : 4;
            if (data.Length * Marshal.SizeOf<T>() != tex.Width * tex.Height * bpp)
                throw new ArgumentException($"Length of data ({data.Length * Marshal.SizeOf<T>()}) did not match width * height * bpp of the texture ({tex.Width * tex.Height * bpp}).", nameof(data));

            var byteSpan = MemoryMarshal.Cast<T, byte>(data);
            CopyData(tex, byteSpan, 0, 0, tex.Width, tex.Height);
        }

        /// <inheritdoc/>
        public override void SetData<T>(int id, in Rectangle subRect, ReadOnlySpan<T> data)
        {
            CheckDisposed();

            var tex = _textures[id];
            if (data.Length * Marshal.SizeOf<T>() != tex.Width * tex.Height)
                throw new ArgumentException($"Length of data (${data.Length * Marshal.SizeOf<T>()}) did not match width * height of the texture (${tex.Width * tex.Height}).", nameof(data));

            var byteSpan = MemoryMarshal.Cast<T, byte>(data);
            CopyData(tex, byteSpan, (uint) subRect.X, (uint) subRect.Y, (uint) subRect.Width, (uint) subRect.Height);
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

        private unsafe void CopyData(Texture tex, ReadOnlySpan<byte> data, uint tx, uint ty, uint width, uint height)
        {
            var staging = GetStaging(width, height);

            _setDataCommandList.Begin();

            var bpp = tex.Format == PixelFormat.R8_UNorm ? 1 : 4;
            var rowBytes = (int) width * bpp;

            var map = _gd.Map(staging, MapMode.Write, 0);
            if (rowBytes == map.RowPitch)
            {
                var dst = new Span<byte>((byte*) map.Data.ToPointer() + rowBytes * ty, (int) (rowBytes * height));
                data.CopyTo(dst);
            }
            else
            {
                var stagingPtr = (byte*) map.Data.ToPointer();
                for (var y = 0; y < height; y++)
                {
                    var dst = new Span<byte>(stagingPtr + y * (int) map.RowPitch, rowBytes);
                    var src = data.Slice(y * rowBytes, rowBytes);
                    src.CopyTo(dst);
                }
            }

            _gd.Unmap(staging);

            _setDataCommandList.CopyTexture(staging, 0, 0, 0, 0, 0, tex, tx, ty, 0, 0, 0, width, height, 1, 1);
            _setDataCommandList.End();

            _gd.SubmitCommands(_setDataCommandList);
        }

        private Texture GetStaging(uint width, uint height)
        {
            if (_staging == null)
            {
                var td = TextureDescription.Texture2D(width, height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging);
                _staging = _gd.ResourceFactory.CreateTexture(ref td);
            }
            else if (_staging.Width < width || _staging.Height < height)
            {
                var newWidth = width > _staging.Width ? width : _staging.Width;
                var newHeight = height > _staging.Height ? height : _staging.Height;
                var td = TextureDescription.Texture2D(newWidth, newHeight, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging);
                _gd.DisposeWhenIdle(_staging);
                _staging = _gd.ResourceFactory.CreateTexture(ref td);
            }

            return _staging;
        }
    }
}
