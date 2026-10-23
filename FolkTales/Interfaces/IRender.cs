/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        IRender.cs
 * PURPOSE:     Streamlined Interface for Z-buffered triangle rendering.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Structs;
using System.Drawing;
using System.Numerics;

namespace FolkTales.Interfaces
{
    /// <summary>
    /// Defines a lightweight 2D/3D renderer interface focused on triangles and Z-culling.
    /// </summary>
    public interface IRender
    {
        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        int Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        int Height { get; }

        /// <summary>
        /// Clears color and depth buffers.
        /// </summary>
        void Clear(ColorRgba color);

        // Atomic 3D primitives (Assumed already projected to screen space)
        /// <summary>
        /// Draws the solid triangle.
        /// </summary>
        /// <param name="p0">The p0.</param>
        /// <param name="z0">The z0.</param>
        /// <param name="p1">The p1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="color">The color.</param>
        void DrawSolidTriangle(Vec2 p0, float z0, Vec2 p1, float z1, Vec2 p2, float z2, ColorRgba color);

        /// <summary>
        /// Draws the textured triangle.
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
        /// <param name="textureId">The texture identifier.</param>
        void DrawTexturedTriangle(Vec2 p0, float z0, Vector2 uv0, Vec2 p1, float z1, Vector2 uv1, Vec2 p2, float z2,
            Vector2 uv2, int textureId);

        /// <summary>
        /// Draws the line.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="color">The color.</param>
        void DrawLine(Vec2 p1, Vec2 p2, ColorRgba? color);

        // 2D/Billboard primitives

        /// <summary>
        /// Draws the sprite.
        /// </summary>
        /// <param name="topLeft">The top left.</param>
        /// <param name="z">The z.</param>
        /// <param name="spriteId">The sprite identifier.</param>
        void DrawSprite(Vec2 topLeft, float z, int spriteId);

        /// <summary>
        /// Draws the filled rect.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="color">The color.</param>
        void DrawFilledRect(int x, int y, int width, int height, ColorRgba? color);

        /// <summary>
        /// Determines whether the specified identifier has texture.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if the specified identifier has texture; otherwise, <c>false</c>.
        /// </returns>
        bool HasTexture(int id);

        /// <summary>
        /// Enables the updates.
        /// </summary>
        void EnableUpdates();

        /// <summary>
        /// Disables the updates.
        /// </summary>
        void DisableUpdates();

        /// <summary>
        /// Gets the frame.
        /// </summary>
        /// <returns>A <see cref="Bitmap"/> representing the current frame, or null if unavailable.</returns>
        Bitmap? GetFrame();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        void Dispose();
    }
}