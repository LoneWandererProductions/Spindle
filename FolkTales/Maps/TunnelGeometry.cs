/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        TunnelGeometry.cs
 * PURPOSE:     Cross section (arch) of tunnels, crawl holes and round-cell mouths, and the walkable strip of a tunnel cell.
 *              One definition for the renderer and the collision, so the arch you see is the arch you fit through.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Size of an arch: half the width, the height at the middle and the shape exponent.
    /// </summary>
    /// <param name="HalfWidth">Half width in metres.</param>
    /// <param name="Height">Height at the middle above the floor in metres.</param>
    /// <param name="Exponent">Shape exponent (2 = ellipse, higher = boxier).</param>
    public readonly record struct ArchSize(float HalfWidth, float Height, float Exponent);

    /// <summary>
    /// Arch profile and tunnel layout.
    /// </summary>
    public static class TunnelGeometry
    {
        /// <summary>
        /// Height of the arch above the floor at a lateral position. <paramref name="u"/> is the position across the arch,
        /// -1 at the left foot, 0 in the middle, 1 at the right foot.
        /// </summary>
        /// <param name="u">Normalised lateral position, clamped to -1..1.</param>
        /// <param name="size">The arch size.</param>
        /// <returns>Height above the floor in metres.</returns>
        public static float ArchHeightAt(float u, ArchSize size)
        {
            var a = MathF.Min(1f, MathF.Abs(u));
            return size.Height * MathF.Pow(MathF.Max(0f, 1f - MathF.Pow(a, size.Exponent)), 1f / size.Exponent);
        }

        /// <summary>
        /// Arch of a mouth or hole in an edge of a cell: crawl holes are low and narrow, everything else is walkable.
        /// </summary>
        /// <param name="wall">The wall type of the edge.</param>
        /// <param name="cell">The cell the edge belongs to.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="storey">Floor to ceiling in metres.</param>
        /// <returns>The arch size.</returns>
        public static ArchSize MouthSize(WallType wall, MapCell3D cell, float scale, float storey)
        {
            var ring = RoundCellGeometry.RingRadius(cell, scale);
            var half = RoundCellGeometry.MouthHalfWidth(wall, scale, ring);

            return wall == WallType.Crawl
                ? new ArchSize(half, CrawlHole.Height(storey), MapDimensions.CrawlArchExponent)
                : new ArchSize(half, MapDimensions.TunnelHeightRatio * storey, MapDimensions.TunnelArchExponent);
        }

        /// <summary>
        /// Finds the axis of a tunnel cell: the one axis (east-west or north-south) that has open edges. Cells with open edges
        /// on both axes, or none, have no axis (bends and junctions are round cells).
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="eastWest"><c>true</c> if the tunnel runs east-west.</param>
        /// <returns><c>true</c> if the cell has a usable axis.</returns>
        public static bool TryGetAxis(MapCell3D cell, out bool eastWest)
        {
            var ns = (RoundCellGeometry.HasMouth(cell.WallNorth) ? 1 : 0) +
                     (RoundCellGeometry.HasMouth(cell.WallSouth) ? 1 : 0);
            var ew = (RoundCellGeometry.HasMouth(cell.WallEast) ? 1 : 0) +
                     (RoundCellGeometry.HasMouth(cell.WallWest) ? 1 : 0);
            eastWest = ew > 0;
            return (ns > 0) != (ew > 0);
        }

        /// <summary>
        /// Arch of the lining of a tunnel cell: the crawl size if either end is a crawl wall, else the walkable size.
        /// </summary>
        /// <param name="cell">The tunnel cell.</param>
        /// <param name="eastWest">The tunnel axis, see <see cref="TryGetAxis"/>.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="storey">Floor to ceiling in metres.</param>
        /// <returns>The arch size.</returns>
        public static ArchSize TunnelSize(MapCell3D cell, bool eastWest, float scale, float storey)
        {
            var a = eastWest ? cell.WallWest : cell.WallNorth;
            var b = eastWest ? cell.WallEast : cell.WallSouth;
            var crawl = a == WallType.Crawl || b == WallType.Crawl;
            return MouthSize(crawl ? WallType.Crawl : WallType.None, cell, scale, storey);
        }

        /// <summary>
        /// Keeps a body inside the walkable strip of a tunnel cell (sideways only; the ends are the edge walls' job).
        /// Cells that are not tunnels, or have no axis, are returned unchanged.
        /// </summary>
        /// <param name="position">Body position in world X/Z.</param>
        /// <param name="cell">The cell the body is in.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="radius">Body radius.</param>
        /// <returns>The resolved position.</returns>
        public static Vector2 Resolve(Vector2 position, MapCell3D cell, float scale, float radius)
        {
            if (cell.Shape != CellShape.Tunnel || !TryGetAxis(cell, out var eastWest)) return position;

            var size = TunnelSize(cell, eastWest, scale, (cell.CeilingHeight - cell.FloorHeight) * scale);
            var centre = RoundCellGeometry.CenterOf(cell, scale);
            var limit = MathF.Max(0f, size.HalfWidth - radius);

            if (eastWest) position.Y = centre.Y + Math.Clamp(position.Y - centre.Y, -limit, limit);
            else position.X = centre.X + Math.Clamp(position.X - centre.X, -limit, limit);
            return position;
        }
    }
}