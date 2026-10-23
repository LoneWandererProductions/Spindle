/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        Vec2.cs
 * PURPOSE:     Vec2 is a custom Vector 2 structure.
 *              Right now it is used for integer coordinates, but it can be initialized from double. 
 *              Only used in SoftwareRasterizer for now, but can be used in other places as well.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;
using System.Runtime.CompilerServices;

namespace FolkTales.Structs
{
    /// <summary>
    /// Represents a 2D point or coordinate (integer master), can be initialized from double.
    /// </summary>
    public readonly struct Vec2
    {
        /// <summary>
        /// The x coordinate.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// The y coordinate.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Creates a Vec2 from integers.
        /// </summary>
        public Vec2(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a Vec2 from doubles (truncated to integers).
        /// </summary>
        public Vec2(double x, double y)
        {
            X = (int)x;
            Y = (int)y;
        }

        /// <summary>
        /// Offset by integers.
        /// </summary>
        public Vec2 Offset(int dx, int dy) => new Vec2(X + dx, Y + dy);

        /// <summary>
        /// Offset by doubles (truncated to integers).
        /// </summary>
        public Vec2 Offset(double dx, double dy) => new Vec2(X + dx, Y + dy);

        /// <summary>
        /// Converts to vec2.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>Vec2 from Vector2</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 ToVec2(Vector2 p) => new Vec2((int)p.X, (int)p.Y); // truncate

        /// <summary>
        /// Converts to vector2.
        /// </summary>
        /// <returns>Vec2 to Vector2</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 ToVector2() => new Vector2(X, Y);

        /// <summary>
        /// Froms the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>new Vec2</returns>
        public static Vec2 From(float x, float y) => new Vec2((int)x, (int)y);

        /// <summary>
        /// Froms the specified p.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>new Vec2</returns>
        public static Vec2 From(Vector2 p) => new Vec2((int)p.X, (int)p.Y);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"Vec2({X}, {Y})";
    }
}