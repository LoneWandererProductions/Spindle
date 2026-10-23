/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        ColorRgba.cs
 * PURPOSE:     Color Struct, representing a color with 4 channels: red, green, blue, alpha.
 *              Planned for use in software rasterization and OpenGL rendering pipelines.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Runtime.CompilerServices;

namespace FolkTales.Structs
{
    /// <summary>
    /// Represents a color with 4 channels: red, green, blue, alpha.
    /// </summary>
    public readonly struct ColorRgba
    {
        /// <summary>
        /// The red channel
        /// </summary>
        public readonly byte R;

        /// <summary>
        /// The green channel
        /// </summary>
        public readonly byte G;

        /// <summary>
        /// The blue channel
        /// </summary>
        public readonly byte B;

        /// <summary>
        /// The alpha transparency channel
        /// </summary>
        public readonly byte A;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRgba"/> struct.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="a">The alpha component.</param>
        public ColorRgba(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Creates a ColorRGBA from ARGB components.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        /// <returns>A new ColorRgba instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorRgba FromArgb(byte a, byte r, byte g, byte b)
        {
            return new ColorRgba(r, g, b, a);
        }

        /// <summary>
        /// Converts to bgra.
        /// </summary>
        /// <returns>A 32-bit unsigned integer representing the color in BGRA format.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ToBgra()
        {
            return (uint)A << 24 | (uint)R << 16 | (uint)G << 8 | B;
        }

        /// <summary>
        /// Converts to gl.
        /// </summary>
        /// <returns>A tuple representing the color components as floats in the range 0 to 1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float R, float G, float B, float A) ToGl()
        {
            const float inv = 1f / 255f;
            return (R * inv, G * inv, B * inv, A * inv);
        }

        /// <summary>
        /// Froms the bgra.
        /// </summary>
        /// <param name="bgra">The bgra.</param>
        /// <returns>A new ColorRgba instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorRgba FromBgra(uint bgra)
        {
            return new ColorRgba(
                r: (byte)(bgra >> 16 & 0xFF),
                g: (byte)(bgra >> 8 & 0xFF),
                b: (byte)(bgra & 0xFF),
                a: (byte)(bgra >> 24 & 0xFF)
            );
        }

        /// <summary>
        /// Gets the cornflower blue.
        /// </summary>
        /// <value>
        /// The cornflower blue.
        /// </value>
        public static ColorRgba CornflowerBlue => new ColorRgba(100, 149, 237, 255);

        /// <summary>
        /// Gets the red.
        /// </summary>
        /// <value>
        /// The red.
        /// </value>
        public static ColorRgba Red => new ColorRgba(255, 0, 0, 255);

        /// <summary>
        /// Gets the black.
        /// </summary>
        /// <value>
        /// The black.
        /// </value>
        public static ColorRgba? Black => new ColorRgba(255, 255, 255, 255);

        /// <summary>
        /// Gets the dark gray.
        /// </summary>
        /// <value>
        /// The dark gray.
        /// </value>
        public static ColorRgba DarkGray => new ColorRgba(169, 169, 169, 255);

        /// <summary>
        /// Gets the gray.
        /// </summary>
        /// <value>
        /// The gray.
        /// </value>
        public static ColorRgba Gray => new ColorRgba(128, 128, 128, 255);

        /// <summary>
        /// Gets the light gray.
        /// </summary>
        /// <value>
        /// The light gray.
        /// </value>
        public static ColorRgba LightGray => new ColorRgba(211, 211, 211, 255);

        /// <summary>
        /// Gets the transparent.
        /// </summary>
        /// <value>
        /// The transparent.
        /// </value>
        public static ColorRgba Transparent => new ColorRgba(0, 0, 0, 0);

        /// <summary>
        /// Gets the white.
        /// </summary>
        /// <value>
        /// The white.
        /// </value>
        public static ColorRgba White => new ColorRgba(255, 255, 255, 1);
    }
}