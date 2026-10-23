/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        TerrainChunk.cs
 * PURPOSE:     Describes a chunk of terrain cells for rendering, including its grid position, contained cells, cached GL commands, and bounding box for culling.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Maps;
using System.Numerics;

namespace FolkTales
{
    /// <summary>
    /// TerrainChunk represents a section of the terrain grid, containing multiple MapCell3D instances. It tracks its position in the grid, the cells it contains, and an axis-aligned bounding box (AABB) for efficient culling during rendering.
    /// </summary>
    public class TerrainChunk
    {
        /// <summary>
        /// Gets the grid position.
        /// </summary>
        /// <value>
        /// The grid position.
        /// </value>
        public Vector2 GridPos { get; }

        /// <summary>
        /// Gets the cells.
        /// </summary>
        /// <value>
        /// The cells.
        /// </value>
        public List<MapCell3D> Cells { get; } = new();

        // --- The "AABB" (Axis-Aligned Bounding Box), used for culling during rendering ---

        /// <summary>
        /// Gets the minimum x.
        /// </summary>
        /// <value>
        /// The minimum x.
        /// </value>
        public float MinX { get; private set; } = float.MaxValue;

        /// <summary>
        /// Gets the maximum x.
        /// </summary>
        /// <value>
        /// The maximum x.
        /// </value>
        public float MaxX { get; private set; } = float.MinValue;

        /// <summary>
        /// Gets the minimum y.
        /// </summary>
        /// <value>
        /// The minimum y.
        /// </value>
        public float MinY { get; private set; } = float.MaxValue;

        /// <summary>
        /// Gets the maximum y.
        /// </summary>
        /// <value>
        /// The maximum y.
        /// </value>
        public float MaxY { get; private set; } = float.MinValue;

        /// <summary>
        /// Gets the minimum z.
        /// </summary>
        /// <value>
        /// The minimum z.
        /// </value>
        public float MinZ { get; private set; } = float.MaxValue;

        /// <summary>
        /// Gets the maximum z.
        /// </summary>
        /// <value>
        /// The maximum z.
        /// </value>
        public float MaxZ { get; private set; } = float.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainChunk"/> class.
        /// </summary>
        /// <param name="gridPos">The grid position.</param>
        public TerrainChunk(Vector2 gridPos)
        {
            GridPos = gridPos;
        }

        /// <summary>
        /// Updates the bounding box to encapsulate a new cell.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="height">The height.</param>
        public void UpdateBounds(float x, float z, float scale, float height)
        {
            var worldX0 = x * scale;
            var worldX1 = (x + 1) * scale;
            var worldZ0 = z * scale;
            var worldZ1 = (z + 1) * scale;

            if (worldX0 < MinX) MinX = worldX0;
            if (worldX1 > MaxX) MaxX = worldX1;
            if (worldZ0 < MinZ) MinZ = worldZ0;
            if (worldZ1 > MaxZ) MaxZ = worldZ1;

            // Capture vertical terrain vertex data points for proper 3D culling!
            if (height < MinY) MinY = height;
            if (height > MaxY) MaxY = height;
        }
    }
}