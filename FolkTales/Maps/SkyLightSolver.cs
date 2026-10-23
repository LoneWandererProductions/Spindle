/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        SkyLightSolver.cs
 * PURPOSE:     Works out where daylight can come in: cells with nothing above them, and the cells next to them that it
 *              spills into through doorways and windows. Used for dungeons and for the structures of a hybrid world.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Fills the sky light grid of a <see cref="DungeonMap"/>. A cell is OPEN when it is built, has no ceiling and nothing
    /// built above it blocks the sky. Open cells get <see cref="DungeonMap.SkyLightLevel"/>; the light then spreads into
    /// the neighbouring built cells, stopped by the same walls, ceilings and floors that stop torch light, and weakened
    /// by <see cref="Falloff"/> per cell. How bright it is right now (time of day, weather) is not decided here, see
    /// <see cref="DungeonMap.GetEffectiveLightAt"/>.
    /// </summary>
    public static class SkyLightSolver
    {
        /// <summary>
        /// Light lost per cell the sky light spreads. Steeper than torches: a hole lights its surroundings, not the whole
        /// room.
        /// </summary>
        private const int Falloff = 2;

        /// <summary>
        /// Recomputes the sky light of the whole map.
        /// </summary>
        /// <param name="map">The map.</param>
        public static void Recalculate(DungeonMap map)
        {
            map.ClearSkyLight();

            var queue = new Queue<(int X, int Z, int L)>();

            SeedOpenCells(map, queue);
            SeedOpenAirNeighbours(map, queue);
            Spread(map, queue);
        }

        /// <summary>
        /// Open cells: from the top of every column down, up to the first thing that closes the column.
        /// </summary>
        private static void SeedOpenCells(DungeonMap map, Queue<(int X, int Z, int L)> queue)
        {
            // A hybrid world: the ground is the ceiling of every level below the surface.
            var groundCovers = map.OpenAirFromLevel < map.Levels;

            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var covered = false;

                for (var level = map.Levels - 1; level >= 0; level--)
                {
                    if (groundCovers && level < map.OpenAirFromLevel) covered = true;

                    var cell = map.GetCell(x, z, level);
                    if (cell == null || cell.IsEmpty) continue; // nothing built here: does not close the column

                    cell.CoveredFromAbove = covered;

                    // Open: nothing above and no ceiling of its own. A floor under it does not matter for ITS light.
                    if (!covered && !cell.HasCeiling && !cell.IsSolidBlock)
                    {
                        map.SetSkyLightAt(x, z, level, DungeonMap.SkyLightLevel);
                        queue.Enqueue((x, z, level));
                    }

                    // Whatever lies below is in the shadow of this cell's ceiling, floor slab or block.
                    if (cell.HasCeiling || cell.HasFloor || cell.IsSolidBlock) covered = true;
                }
            }
        }

        /// <summary>
        /// Open air levels (a hybrid world's surface): a built cell next to an EMPTY cell, with no light blocking wall on
        /// that side, is lit from the outside through that opening (a door, a window, a gap).
        /// </summary>
        private static void SeedOpenAirNeighbours(DungeonMap map, Queue<(int X, int Z, int L)> queue)
        {
            if (map.OpenAirFromLevel >= map.Levels) return;

            const byte seed = DungeonMap.SkyLightLevel - Falloff;

            for (var level = Math.Max(0, map.OpenAirFromLevel); level < map.Levels; level++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var cell = map.GetCell(x, z, level);
                if (cell == null || cell.IsEmpty || cell.IsSolidBlock) continue;
                if (map.GetSkyLightAt(x, z, level) >= seed) continue;

                if (OpensTo(map, x + 1, z, level, cell.WallEast) || OpensTo(map, x - 1, z, level, cell.WallWest) ||
                    OpensTo(map, x, z + 1, level, cell.WallSouth) || OpensTo(map, x, z - 1, level, cell.WallNorth))
                {
                    map.SetSkyLightAt(x, z, level, seed);
                    queue.Enqueue((x, z, level));
                }
            }
        }

        /// <summary>
        /// Whether light comes in from the neighbour cell: it is empty (or beyond the map edge) and the wall is open.
        /// </summary>
        private static bool OpensTo(DungeonMap map, int nx, int nz, int level, WallType wall)
        {
            if (wall.BlocksLight()) return false;

            var neighbour = map.GetCell(nx, nz, level);
            return neighbour == null || neighbour.IsEmpty;
        }

        /// <summary>
        /// Breadth first spread with the barrier rules of the torch light (walls, ceiling, floor), but never into cells
        /// that are not built.
        /// </summary>
        private static void Spread(DungeonMap map, Queue<(int X, int Z, int L)> queue)
        {
            ReadOnlySpan<int> dx =  [
            1, -1, 0, 0, 0, 0];
            ReadOnlySpan<int> dz =  [
            0, 0, 1, -1, 0, 0];
            ReadOnlySpan<int> dl =  [
            0, 0, 0, 0, 1, -1];

            while (queue.Count > 0)
            {
                var (cx, cz, cl) = queue.Dequeue();
                var current = map.GetSkyLightAt(cx, cz, cl);
                if (current <= Falloff) continue;

                var source = map.GetCell(cx, cz, cl);

                for (var i = 0; i < 6; i++)
                {
                    var nx = cx + dx[i];
                    var nz = cz + dz[i];
                    var nl = cl + dl[i];
                    if (nx < 0 || nx >= map.Width || nz < 0 || nz >= map.Depth || nl < 0 || nl >= map.Levels) continue;

                    // Outgoing barriers.
                    if (source != null)
                    {
                        if (i == 0 && source.WallEast.BlocksLight()) continue;
                        if (i == 1 && source.WallWest.BlocksLight()) continue;
                        if (i == 2 && source.WallSouth.BlocksLight()) continue;
                        if (i == 3 && source.WallNorth.BlocksLight()) continue;
                        if (i == 4 && source.HasCeiling) continue;
                        if (i == 5 && source.HasFloor) continue;
                    }

                    var target = map.GetCell(nx, nz, nl);

                    // Light does not spread into rock or void.
                    if (target == null || target.IsEmpty || target.IsSolidBlock) continue;

                    // Incoming barriers.
                    if (i == 0 && target.WallWest.BlocksLight()) continue;
                    if (i == 1 && target.WallEast.BlocksLight()) continue;
                    if (i == 2 && target.WallNorth.BlocksLight()) continue;
                    if (i == 3 && target.WallSouth.BlocksLight()) continue;
                    if (i == 4 && target.HasFloor) continue;
                    if (i == 5 && target.HasCeiling) continue;

                    var spread = (byte)(current - Falloff);
                    if (map.GetSkyLightAt(nx, nz, nl) >= spread) continue;

                    map.SetSkyLightAt(nx, nz, nl, spread);
                    queue.Enqueue((nx, nz, nl));
                }
            }
        }
    }
}