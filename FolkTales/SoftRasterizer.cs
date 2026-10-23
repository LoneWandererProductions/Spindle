/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        SoftRasterizer.cs
 * PURPOSE:     Lightweight software rasterizer with pixel-level Z-culling.
 *              Basic Tester to validate our GLRender. 
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using FolkTales.Interfaces;
using FolkTales.Structs;
using Imaging.Objects;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FolkTales
{
    /// <inheritdoc cref="IRender" />
    /// <summary>
    /// Software rasterizer that implements basic triangle filling, texturing, and Z-buffering.
    /// </summary>
    /// <seealso cref="FolkTales.Interfaces.IRender" />
    /// <seealso cref="System.IDisposable" />
    public class SoftRasterizer : IRender, IDisposable
    {
        /// <summary>
        /// Native memory pointer handle for unmanaged Z-buffer allocation (0 LOH Heap Bytes)
        /// </summary>
        private IntPtr _zBufferAllocatedPtr;

        /// <summary>
        /// Fast pointer accessor to the unmanaged float z buffer
        /// </summary>
        private readonly unsafe float* _zBufferPtr;

        /// <summary>
        /// Total pixel count tracking size of the z buffer
        /// </summary>
        private readonly int _pixelCount;

        /// <summary>
        /// The color buffer
        /// </summary>
        private readonly UnmanagedImageBuffer _colorBuffer;

        /// <summary>
        /// The textures
        /// </summary>
        private readonly Dictionary<int, UnmanagedImageBuffer> _textures = new();

        /// <summary>
        /// The tile size
        /// </summary>
        private const int TileSize = 32;

        /// <summary>
        /// The tiles x
        /// </summary>
        private readonly int _tilesX;

        /// <summary>
        /// The tiles y
        /// </summary>
        private readonly int _tilesY;

        /// <summary>
        /// The dirty tiles
        /// </summary>
        private readonly bool[] _dirtyTiles;

        /// <summary>
        /// The touched tiles
        /// </summary>
        private readonly HashSet<int> _touchedTiles = new();

        /// <summary>
        /// The use dirty rectangles
        /// </summary>
        private bool _useDirtyRectangles = false;

        /// <summary>
        /// The dirty lock
        /// </summary>
        private readonly ReaderWriterLockSlim _dirtyLock = new();

        ///<inheritdoc/>
        public int Width => _colorBuffer.Width;

        ///<inheritdoc/>
        public int Height => _colorBuffer.Height;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftRasterizer"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="textures">The textures.</param>
        /// <param name="logger">The logger.</param>
        public unsafe SoftRasterizer(int width, int height, Dictionary<int, UnmanagedImageBuffer>? textures,
            ILogger? logger = null)
        {
            _logger = logger;
            _colorBuffer = new UnmanagedImageBuffer(width, height);
            _pixelCount = width * height;

            // Allocate Z-buffer directly in OS native memory via Marshal.AllocHGlobal
            _zBufferAllocatedPtr = Marshal.AllocHGlobal(_pixelCount * sizeof(float));
            _zBufferPtr = (float*)_zBufferAllocatedPtr;

            if (textures != null) _textures = textures;

            _tilesX = (Width + TileSize - 1) / TileSize;
            _tilesY = (Height + TileSize - 1) / TileSize;
            _dirtyTiles = new bool[_tilesX * _tilesY];

            Clear(new ColorRgba(0, 0, 0, 255));
        }

        ///<inheritdoc/>
        public void EnableUpdates() => _useDirtyRectangles = true;

        ///<inheritdoc/>
        public void DisableUpdates() => _useDirtyRectangles = false;

        ///<inheritdoc/>
        public unsafe void Clear(ColorRgba color)
        {
            // Clear Color Buffer using the high-speed Span approach
            var colorValue = PackBgra(color.A, color.R, color.G, color.B);
            MemoryMarshal.Cast<byte, uint>(_colorBuffer.BufferSpan).Fill(colorValue);

            // Still need to clear the Z-Buffer manually (floats are 4 bytes too)
            // You can use Span<float>.Fill here too!
            new Span<float>(_zBufferPtr, _pixelCount).Fill(float.MaxValue);

            if (_useDirtyRectangles)
            {
                MarkRectDirty(0, 0, Width, Height);
            }
        }

        ///<inheritdoc/>
        public void DrawSolidTriangle(Vec2 p0, float z0, Vec2 p1, float z1, Vec2 p2, float z2, ColorRgba color)
        {
            FillTriangleInternal(p0, z0, Vector2.Zero, p1, z1, Vector2.Zero, p2, z2, Vector2.Zero, color, null);

            if (_useDirtyRectangles)
            {
                var (x, y, w, h) = GetTriangleBounds(p0, p1, p2);
                MarkRectDirty(x, y, w, h);
            }
        }

        ///<inheritdoc/>
        public void DrawTexturedTriangle(Vec2 p0, float z0, Vector2 uv0, Vec2 p1, float z1, Vector2 uv1, Vec2 p2,
            float z2, Vector2 uv2, int textureId)
        {
            if (!_textures.TryGetValue(textureId, out var tex)) return;

            FillTriangleInternal(p0, z0, uv0, p1, z1, uv1, p2, z2, uv2, ColorRgba.White, tex);

            if (_useDirtyRectangles)
            {
                var (x, y, w, h) = GetTriangleBounds(p0, p1, p2);
                MarkRectDirty(x, y, w, h);
            }
        }

        ///<inheritdoc/>
        public unsafe void DrawSprite(Vec2 topLeft, float z, int spriteId)
        {
            if (!_textures.TryGetValue(spriteId, out var sprite)) return;

            for (var y = 0; y < sprite.Height; y++)
            {
                var py = topLeft.Y + y;
                if (py < 0 || py >= Height) continue;

                var rowOffset = py * Width;

                for (var x = 0; x < sprite.Width; x++)
                {
                    var px = topLeft.X + x;
                    if (px < 0 || px >= Width) continue;

                    var idx = rowOffset + px;
                    if (z < _zBufferPtr[idx])
                    {
                        var (r, g, b, a) = sprite.GetPixel(x, y);
                        if (a < 10) continue;

                        _zBufferPtr[idx] = z;
                        _colorBuffer.SetPixel(px, py, r, g, b, a);
                    }
                }
            }

            if (_useDirtyRectangles)
            {
                MarkRectDirty(topLeft.X, topLeft.Y, sprite.Width, sprite.Height);
            }
        }

        ///<inheritdoc/>
        public void DrawFilledRect(int x, int y, int width, int height, ColorRgba? color)
        {
            var col = color ?? ColorRgba.White;
            var xEnd = Math.Min(x + width, Width);
            var yEnd = Math.Min(y + height, Height);

            for (var iy = Math.Max(0, y); iy < yEnd; iy++)
            {
                for (var ix = Math.Max(0, x); ix < xEnd; ix++)
                {
                    _colorBuffer.SetPixel(ix, iy, col.R, col.G, col.B, col.A);
                }
            }

            if (_useDirtyRectangles)
            {
                MarkRectDirty(x, y, width, height);
            }
        }

        ///<inheritdoc/>
        public void DrawLine(Vec2 p1, Vec2 p2, ColorRgba? color)
        {
            var x1 = p1.X;
            var y1 = p1.Y;
            var x2 = p2.X;
            var y2 = p2.Y;

            var dx = Math.Abs(x2 - x1);
            var dy = Math.Abs(y2 - y1);
            var sx = x1 < x2 ? 1 : -1;
            var sy = y1 < y2 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x1 >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                {
                    if (color.HasValue)
                    {
                        _colorBuffer.SetPixel(x1, y1, color.Value.R, color.Value.G, color.Value.B, color.Value.A);
                    }
                }

                if (x1 == x2 && y1 == y2) break;

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }

            if (_useDirtyRectangles)
            {
                var minX = Math.Min(p1.X, p2.X);
                var minY = Math.Min(p1.Y, p2.Y);
                MarkRectDirty(minX, minY, Math.Abs(p1.X - p2.X) + 1, Math.Abs(p1.Y - p2.Y) + 1);
            }
        }

        /// <summary>
        /// Fills the triangle internal.
        /// </summary>
        /// <param name="p0">The p0.</param>
        /// <param name="z0">The z0.</param>
        /// <param name="uv0">The uv0.</param>
        /// <param name="p1">The p1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="uv1">The uv1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="color">The color.</param>
        /// <param name="tex">The texture ID.</param>
        private unsafe void FillTriangleInternal(Vec2 p0, float z0, Vector2 uv0, Vec2 p1, float z1, Vector2 uv1,
            Vec2 p2,
            float z2, Vector2 uv2, ColorRgba color, UnmanagedImageBuffer? tex)
        {
            var minX = Math.Max(0, Math.Min(p0.X, Math.Min(p1.X, p2.X)));
            var maxX = Math.Min(Width - 1, Math.Max(p0.X, Math.Max(p1.X, p2.X)));
            var minY = Math.Max(0, Math.Min(p0.Y, Math.Min(p1.Y, p2.Y)));
            var maxY = Math.Min(Height - 1, Math.Max(p0.Y, Math.Max(p1.Y, p2.Y)));

            var denom = (float)(p1.Y - p2.Y) * (p0.X - p2.X) + (float)(p2.X - p1.X) * (p0.Y - p2.Y);
            if (Math.Abs(denom) < 0.0001f) return;

            // Pre-compute inverse denominator once to replace loop division with fast multiplication
            var invDenom = 1.0f / denom;

            var p1Yp2Y = (float)(p1.Y - p2.Y);
            var p2Xp1X = (float)(p2.X - p1.X);
            var p2Yp0Y = (float)(p2.Y - p0.Y);
            var p0Xp2X = (float)(p0.X - p2.X);

            var texWidth = tex?.Width ?? 0;
            var texHeight = tex?.Height ?? 0;
            var texWidthMinus1 = texWidth - 1;
            var texHeightMinus1 = texHeight - 1;

            for (var y = minY; y <= maxY; y++)
            {
                var rowOffset = y * Width;
                var dy = y - p2.Y;

                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - p2.X;

                    var w0 = (p1Yp2Y * dx + p2Xp1X * dy) * invDenom;
                    var w1 = (p2Yp0Y * dx + p0Xp2X * dy) * invDenom;
                    var w2 = 1.0f - w0 - w1;

                    if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                    {
                        var pixelZ = w0 * z0 + w1 * z1 + w2 * z2;
                        var idx = rowOffset + x;

                        if (pixelZ < _zBufferPtr[idx])
                        {
                            _zBufferPtr[idx] = pixelZ;

                            if (tex != null)
                            {
                                var u = w0 * uv0.X + w1 * uv1.X + w2 * uv2.X;
                                var v = w0 * uv0.Y + w1 * uv1.Y + w2 * uv2.Y;

                                var tx = (int)(u * texWidthMinus1) % texWidth;
                                var ty = (int)(v * texHeightMinus1) % texHeight;
                                if (tx < 0) tx += texWidth;
                                if (ty < 0) ty += texHeight;

                                var (r, g, b, a) = tex.GetPixel(tx, ty);
                                _colorBuffer.SetPixel(x, y, r, g, b, a);
                            }
                            else
                            {
                                _colorBuffer.SetPixel(x, y, color.R, color.G, color.B, color.A);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks the rect dirty.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="w">The w.</param>
        /// <param name="h">The h.</param>
        private void MarkRectDirty(int x, int y, int w, int h)
        {
            var x0 = Math.Clamp(x, 0, Width - 1);
            var y0 = Math.Clamp(y, 0, Height - 1);
            var x1 = Math.Clamp(x + w - 1, 0, Width - 1);
            var y1 = Math.Clamp(y + h - 1, 0, Height - 1);

            for (var ty = y0 / TileSize; ty <= y1 / TileSize; ty++)
            {
                for (var tx = x0 / TileSize; tx <= x1 / TileSize; tx++)
                {
                    var idx = ty * _tilesX + tx;
                    if (!_dirtyTiles[idx])
                    {
                        _dirtyTiles[idx] = true;
                        _touchedTiles.Add(idx);
                    }
                }
            }
        }

        /// <summary>
        /// Uploads to writeable bitmap.
        /// </summary>
        /// <param name="wb">The writeable Bitmap.</param>
        public void UploadToWriteableBitmap(WriteableBitmap? wb)
        {
            if (wb == null) return;

            wb.Lock();
            try
            {
                if (!_useDirtyRectangles) UploadFullBuffer(wb);
                else UploadDirtyTiles(wb);
            }
            finally
            {
                wb.Unlock();
            }
        }

        /// <summary>
        /// Uploads the dirty tiles.
        /// </summary>
        /// <param name="wb">The writeable Bitmap.</param>
        private unsafe void UploadDirtyTiles(WriteableBitmap? wb)
        {
            _dirtyLock.EnterWriteLock();
            try
            {
                var stride = wb.BackBufferStride;
                var ptrBase = (byte*)wb.BackBuffer.ToPointer();

                foreach (var idx in _touchedTiles)
                {
                    _dirtyTiles[idx] = false;
                    var tx = idx % _tilesX;
                    var ty = idx / _tilesX;
                    var x = tx * TileSize;
                    var y = ty * TileSize;
                    var w = Math.Min(TileSize, Width - x);
                    var h = Math.Min(TileSize, Height - y);

                    for (var row = 0; row < h; row++)
                    {
                        var src = _colorBuffer.GetPixelSpan(x, y + row, w);
                        var dst = ptrBase + (y + row) * stride + x * 4;
                        CopyRowSimd(src, dst, w * 4);
                    }

                    wb.AddDirtyRect(new Int32Rect(x, y, w, h));
                }

                _touchedTiles.Clear();
            }
            finally
            {
                _dirtyLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Uploads the full buffer.
        /// </summary>
        /// <param name="wb">The writeable Bitmap.</param>
        private unsafe void UploadFullBuffer(WriteableBitmap? wb)
        {
            var stride = wb.BackBufferStride;
            var dstBase = (byte*)wb.BackBuffer.ToPointer();

            for (var y = 0; y < Height; y++)
            {
                var src = _colorBuffer.GetPixelSpan(0, y, Width);
                var dst = dstBase + y * stride;
                CopyRowSimd(src, dst, Width * 4);
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
        }

        /// <summary>
        /// Copies the row simd.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="dstPtr">The DST PTR.</param>
        /// <param name="length">The length.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void CopyRowSimd(Span<byte> src, byte* dstPtr, int length)
        {
            var vecSize = Vector<byte>.Count;
            var i = 0;
            fixed (byte* pSrc = src)
            {
                while (i <= length - vecSize)
                {
                    *(Vector<byte>*)(dstPtr + i) = *(Vector<byte>*)(pSrc + i);
                    i += vecSize;
                }
            }

            for (; i < length; i++) dstPtr[i] = src[i];
        }

        /// <summary>
        /// Gets the triangle bounds.
        /// </summary>
        /// <param name="p0">The p0.</param>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns>Triangle bounds as a tuple (x, y, width, height).</returns>
        private (int x, int y, int w, int h) GetTriangleBounds(Vec2 p0, Vec2 p1, Vec2 p2)
        {
            var minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
            var maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
            var minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
            var maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));
            return (minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        ///<inheritdoc/>
        public bool HasTexture(int id) => _textures.ContainsKey(id);

        ///<inheritdoc/>
        public Bitmap GetFrame() => _colorBuffer.ToBitmap();

        /// <summary>
        /// Packs the bgra.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        /// <returns>The packed BGRA value as a 32-bit unsigned integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PackBgra(byte a, byte r, byte g, byte b)
        {
            // On Little-Endian, this results in memory: [B, G, R, A]
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _colorBuffer.Dispose();

            if (_zBufferAllocatedPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_zBufferAllocatedPtr);
                _zBufferAllocatedPtr = IntPtr.Zero;
            }

            _dirtyLock.Dispose();
        }
    }
}