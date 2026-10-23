/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        DrawCommand.cs
 * PURPOSE:     Command struct for deferred drawing of terrain triangles and sprites.
 *              Only used by the software rasterizer, not the GPU renderer.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;
using FolkTales.Enums;

namespace FolkTales.Structs
{
    /// <summary>
    /// Unified command for the software rasterizer.
    /// Packs all necessary data for triangles and sprites without heap allocation.
    /// </summary>
    public readonly struct DrawCommand
    {
        /// <summary>
        /// The type
        /// </summary>
        public readonly CommandType Type;

        /// <summary>
        /// The sort depth
        /// For the Painter's Sort
        /// </summary>
        public readonly float SortDepth;

        // --- Data for the Rasterizer (No logic here, just storage) ---

        /// <summary>
        /// The s coordinates for the triangle vertices in screen space. For sprites, S0 is used as the center position and S1/S2 are ignored.
        /// </summary>
        public readonly Vec2 S0, S1, S2;

        /// <summary>
        /// The z coordinates for depth testing in the rasterizer. For sprites, Z0 is used and Z1/Z2 are ignored.
        /// </summary>
        public readonly float Z0, Z1, Z2;

        /// <summary>
        /// The uv0
        /// </summary>
        public readonly Vector2 Uv0, Uv1, Uv2;

        /// <summary>
        /// The texture identifier
        /// </summary>
        public readonly int? TextureId;

        /// <summary>
        /// The fallback
        /// </summary>
        public readonly ColorRgba Fallback;

        /// <summary>
        /// The pixel diameter
        /// </summary>
        public readonly int PixelDiameter;

        /// <summary>
        /// Creates the triangle.
        /// </summary>
        /// <param name="s0">The s0.</param>
        /// <param name="z0">The z0.</param>
        /// <param name="uv0">The uv0.</param>
        /// <param name="s1">The s1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="uv1">The uv1.</param>
        /// <param name="s2">The s2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="tex">The texture ID.</param>
        /// <param name="col">The fallback color.</param>
        /// <param name="sort">The sort depth.</param>
        /// <returns>
        /// The draw command for the triangle.
        /// </returns>
        public static DrawCommand CreateTriangle(Vec2 s0, float z0, Vector2 uv0, Vec2 s1, float z1, Vector2 uv1,
            Vec2 s2, float z2, Vector2 uv2, int? tex, ColorRgba col, float sort)
        {
            return new DrawCommand(CommandType.Triangle, sort, s0, s1, s2, z0, z1, z2, uv0, uv1, uv2, tex, col, 0);
        }

        /// <summary>
        /// Creates the sprite.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="z">The z.</param>
        /// <param name="diameter">The diameter.</param>
        /// <param name="tex">The texture ID.</param>
        /// <param name="col">The fallback color.</param>
        /// <returns>The draw command for the sprite.</returns>
        public static DrawCommand CreateSprite(Vec2 pos, float z, int diameter, int? tex, ColorRgba col)
        {
            // Parameter 2: SortDepth (used for the drawList.Sort)
            // Parameter 6: Z0 (used by the Rasterizer Z-Buffer check)
            // Parameter 14: PixelDiameter (the actual size)
            return new DrawCommand(
                CommandType.PaperDoll,
                z, // SortDepth
                pos, // S0 (Center Position)
                default, // S1
                default, // S2
                z, // Z0 (Depth for Z-Buffer)
                0, // Z1
                0, // Z2
                Vector2.Zero, // UV0
                Vector2.Zero, // UV1
                Vector2.Zero, // UV2
                tex, // TextureId
                col, // Fallback Color
                diameter // PixelDiameter
            );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawCommand"/> struct.
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <param name="sort">The sort depth.</param>
        /// <param name="s0">The s0.</param>
        /// <param name="s1">The s1.</param>
        /// <param name="s2">The s2.</param>
        /// <param name="z0">The z0.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="uv0">The uv0.</param>
        /// <param name="uv1">The uv1.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="tex">The texture ID.</param>
        /// <param name="col">The fallback color.</param>
        /// <param name="diam">The pixel diameter.</param>
        private DrawCommand(CommandType type, float sort, Vec2 s0, Vec2 s1, Vec2 s2, float z0, float z1, float z2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2, int? tex, ColorRgba col, int diam)
        {
            Type = type;
            SortDepth = sort;
            S0 = s0;
            S1 = s1;
            S2 = s2;
            Z0 = z0;
            Z1 = z1;
            Z2 = z2;
            Uv0 = uv0;
            Uv1 = uv1;
            Uv2 = uv2;
            TextureId = tex;
            Fallback = col;
            PixelDiameter = diam;
        }
    }
}