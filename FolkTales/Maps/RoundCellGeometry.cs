/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        RoundCellGeometry.cs
 * PURPOSE:     Pure 2D geometry of a round cell (CellShape.Round): the wall ring with a mouth on every open edge, and the
 *              walkable region used by collision. The renderer draws exactly these segments and the collision keeps bodies
 *              inside exactly this region, so what you see is what you bump into. No GL, no map access beyond one cell.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// One straight piece of the ring wall of a round cell, in world X/Z metres, running counter-clockwise
    /// (x east, z south) so the face looking at the centre of the cell is the one on the right of A to B.
    /// </summary>
    /// <param name="A">Start point.</param>
    /// <param name="B">End point.</param>
    public readonly record struct RingSegment(Vector2 A, Vector2 B);

    /// <summary>
    /// A mouth: the gap in the ring where the cell opens (or has a crawl hole) towards one neighbour.
    /// </summary>
    /// <param name="Direction">Which edge.</param>
    /// <param name="Wall">The wall type of that edge.</param>
    /// <param name="RingLeft">Ring end point on the clockwise side of the gap (viewed from the centre).</param>
    /// <param name="RingRight">Ring end point on the counter-clockwise side of the gap.</param>
    /// <param name="BorderLeft">Same lateral position as <see cref="RingLeft"/>, but on the cell border.</param>
    /// <param name="BorderRight">Same lateral position as <see cref="RingRight"/>, but on the cell border.</param>
    /// <param name="HalfWidth">Half width of the mouth in metres.</param>
    public readonly record struct RoundMouth(
        Direction Direction, WallType Wall,
        Vector2 RingLeft, Vector2 RingRight, Vector2 BorderLeft, Vector2 BorderRight, float HalfWidth);

    /// <summary>
    /// Layout of a round cell: centre, radius and mouths.
    /// </summary>
    public sealed class RoundCellLayout
    {
        /// <summary>World X/Z of the centre.</summary>
        public Vector2 Center { get; init; }

        /// <summary>Radius of the ring wall (inner face) in metres.</summary>
        public float Radius { get; init; }

        /// <summary>Half the cell edge in metres.</summary>
        public float HalfCell { get; init; }

        /// <summary>The wall ring pieces (the gaps are left out).</summary>
        public List<RingSegment> Segments { get; } = new();

        /// <summary>The mouths, in the order east, south, west, north. Only edges that are NOT a plain blocking wall have one.</summary>
        public List<RoundMouth> Mouths { get; } = new();
    }

    /// <summary>
    /// Builds <see cref="RoundCellLayout"/> and resolves bodies against it.
    /// </summary>
    public static class RoundCellGeometry
    {
        /// <summary>Gap between the ring and the flat edge walls, as a fraction of the cell edge (stops z-fighting).</summary>
        private const float RingClearance = 0.03f;

        /// <summary>Widest mouth as a fraction of the ring radius: sin(40 degrees).</summary>
        private const float MaxMouthRatio = 0.64f;

        /// <summary>Radius of the ring (inner face) of a round cell in metres.</summary>
        /// <param name="cell">The cell.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <returns>Radius in metres.</returns>
        public static float RingRadius(MapCell3D cell, float scale) =>
            0.5f * scale - (cell.WallThickness + RingClearance) * scale;

        /// <summary>Centre of a cell in world X/Z.</summary>
        /// <param name="cell">The cell.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <returns>World X/Z.</returns>
        public static Vector2 CenterOf(MapCell3D cell, float scale) =>
            new((cell.X + 0.5f) * scale, (cell.Y + 0.5f) * scale);

        /// <summary>Unit vector of a direction in world X/Z (north = -Z).</summary>
        /// <param name="direction">The direction.</param>
        /// <returns>The unit vector.</returns>
        public static Vector2 Axis(Direction direction) => direction switch
        {
            Direction.East => new Vector2(1f, 0f),
            Direction.South => new Vector2(0f, 1f),
            Direction.West => new Vector2(-1f, 0f),
            _ => new Vector2(0f, -1f)
        };

        /// <summary>
        /// Half width of the mouth for an edge type: the crawl hole width for crawl walls, the round mouth width otherwise.
        /// Never wider than 64 % of the ring radius (a 40 degree gap), so two neighbouring mouths always leave a piece of ring between them.
        /// </summary>
        /// <param name="wall">Edge wall type.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="radius">Ring radius.</param>
        /// <returns>Half width in metres.</returns>
        public static float MouthHalfWidth(WallType wall, float scale, float radius)
        {
            var half = wall == WallType.Crawl
                ? CrawlHole.HalfWidth(scale)
                : MathF.Min(MapDimensions.RoundCellMouthWidth, 0.7f * scale) * 0.5f;
            return MathF.Min(half, MaxMouthRatio * radius);
        }

        /// <summary>
        /// Is the ring open on that edge? Everything but a plain blocking wall opens the ring; a crawl wall opens it
        /// (the hole), but only a crawling body can walk through - see <see cref="Resolve"/>.
        /// </summary>
        /// <param name="wall">The edge wall type.</param>
        /// <returns><c>true</c> if the ring has a mouth there.</returns>
        public static bool HasMouth(WallType wall) => wall is WallType.None or WallType.DoorOpen or WallType.Crawl;

        /// <summary>
        /// Computes the ring and the mouths of a round cell.
        /// </summary>
        /// <param name="cell">The cell (its four wall types decide the mouths).</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <returns>The layout.</returns>
        public static RoundCellLayout Build(MapCell3D cell, float scale)
        {
            var center = CenterOf(cell, scale);
            var halfCell = 0.5f * scale;
            var radius = RingRadius(cell, scale);
            var layout = new RoundCellLayout { Center = center, Radius = radius, HalfCell = halfCell };

            // Angles use atan2(z, x) with z pointing south, so: east 0, south pi/2, west pi, north 3pi/2.
            Direction[] order = { Direction.East, Direction.South, Direction.West, Direction.North };
            var gaps = new List<(float Start, float End)>();

            foreach (var dir in order)
            {
                var wall = cell.GetWall(dir);
                if (!HasMouth(wall)) continue;

                var axis = Axis(dir);
                var side = new Vector2(-axis.Y, axis.X); // counter-clockwise (right-hand) lateral
                var hw = MouthHalfWidth(wall, scale, radius);
                var depth = MathF.Sqrt(radius * radius - hw * hw);
                var theta = MathF.Atan2(axis.Y, axis.X);
                var alpha = MathF.Asin(hw / radius);
                if (theta < 0f) theta += MathF.Tau;

                var start = theta - alpha;
                var end = theta + alpha;
                gaps.Add((start, end));

                var ringCw = center + axis * depth - side * hw;
                var ringCcw = center + axis * depth + side * hw;
                var borderCw = center + axis * halfCell - side * hw;
                var borderCcw = center + axis * halfCell + side * hw;
                layout.Mouths.Add(new RoundMouth(dir, wall, ringCw, ringCcw, borderCw, borderCcw, hw));
            }

            var step = MathF.Tau / MapDimensions.RoundCellSegments;

            if (gaps.Count == 0)
            {
                AddArc(layout, 0f, MathF.Tau, step);
                return layout;
            }

            // Arcs between the end of one gap and the start of the next one (wrapping around).
            for (var i = 0; i < gaps.Count; i++)
            {
                var from = gaps[i].End;
                var to = gaps[(i + 1) % gaps.Count].Start;
                if (i == gaps.Count - 1) to += MathF.Tau;
                if (to - from > 1e-3f) AddArc(layout, from, to, step);
            }

            return layout;
        }

        /// <summary>Adds the straight pieces of an arc from <paramref name="a0"/> to <paramref name="a1"/> (radians, increasing).</summary>
        private static void AddArc(RoundCellLayout layout, float a0, float a1, float step)
        {
            var n = Math.Max(1, (int)MathF.Ceiling((a1 - a0) / step - 1e-4f));
            var prev = Point(layout, a0);
            for (var i = 1; i <= n; i++)
            {
                var next = Point(layout, a0 + (a1 - a0) * i / n);
                layout.Segments.Add(new RingSegment(prev, next));
                prev = next;
            }
        }

        /// <summary>Point on the ring at an angle.</summary>
        private static Vector2 Point(RoundCellLayout layout, float angle) =>
            layout.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * layout.Radius;

        /// <summary>
        /// Moves a body that is inside a round cell back into the part of the cell it may stand in: the disc of the chamber
        /// (shrunk by the body radius) plus a straight corridor to every mouth the body can pass (open edges always, crawl
        /// holes only when <paramref name="crawling"/>). Where the body is already inside, it is returned unchanged;
        /// otherwise it is moved to the nearest allowed point, so it slides along the wall instead of sticking.
        /// </summary>
        /// <param name="position">Body position in world X/Z.</param>
        /// <param name="cell">The round cell the body is in.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="radius">Body radius.</param>
        /// <param name="crawling">Stance of the body.</param>
        /// <returns>The resolved position.</returns>
        public static Vector2 Resolve(Vector2 position, MapCell3D cell, float scale, float radius, bool crawling)
        {
            var center = CenterOf(cell, scale);
            var halfCell = 0.5f * scale;
            var ring = RingRadius(cell, scale);
            var disc = MathF.Max(0f, ring - radius);

            var best = position;
            var bestDistSq = float.MaxValue;
            var inside = false;

            // 1. The disc.
            var offset = position - center;
            var len = offset.Length();
            if (len <= disc) inside = true;
            else
            {
                var onDisc = len > 1e-6f ? center + offset * (disc / len) : center;
                Consider(onDisc, position, ref best, ref bestDistSq);
            }

            // 2. One corridor per mouth the body can pass.
            foreach (var dir in new[] { Direction.East, Direction.South, Direction.West, Direction.North })
            {
                var wall = cell.GetWall(dir);
                if (!HasMouth(wall) || !wall.IsOpenFor(crawling)) continue;

                var axis = Axis(dir);
                var side = new Vector2(-axis.Y, axis.X);
                var hw = MathF.Max(0f, MouthHalfWidth(wall, scale, ring) - radius);

                var local = position - center;
                var along = Vector2.Dot(local, axis);
                var lateral = Vector2.Dot(local, side);
                var clampedAlong = Math.Clamp(along, 0f, halfCell);
                var clampedLateral = Math.Clamp(lateral, -hw, hw);

                if (along >= 0f && along <= halfCell && MathF.Abs(lateral) <= hw) inside = true;
                else Consider(center + axis * clampedAlong + side * clampedLateral, position, ref best, ref bestDistSq);
            }

            return inside ? position : best;
        }

        /// <summary>Keeps the candidate that is closest to the body.</summary>
        private static void Consider(Vector2 candidate, Vector2 position, ref Vector2 best, ref float bestDistSq)
        {
            var d = Vector2.DistanceSquared(candidate, position);
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = candidate;
            }
        }
    }
}