/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapAudit.cs
 * PURPOSE:     Self checks for whole maps. Each check returns the violations it found (empty = fine), so the same code runs
 *              as a unit test, as a load-time check in the Tales diagnostics box and from any tool.
 *              Every check exists because the bug it finds was real: see the remark on each method.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Plain-data checks over a built map. They do not render anything and need no window.
    /// </summary>
    public static class MapAudit
    {
        private const int DefaultMaxReports = 20;

        private static readonly (int dx, int dz, string name)[] Directions =
        {
            (0, -1, "N"), (0, 1, "S"), (1, 0, "E"), (-1, 0, "W")
        };

        /// <summary>
        /// Runs every check that applies to a hybrid world and appends one line per check (and per violation) to
        /// <paramref name="notes" />.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="notes">The lines to append to.</param>
        /// <returns>True if nothing was found.</returns>
        public static bool Run(HybridWorldMap map, List<string> notes)
        {
            var ok = Report(notes, "step traps", StepTraps(map.Structures));
            ok &= Report(notes, "open edges into the void", VoidEdges(map.Structures, map.SurfaceLevel));
            ok &= Report(notes, "support level on open terrain", TerrainSupportLevels(map));
            ok &= Report(notes, "props filed under the wrong layer", MisfiledScenery(map));
            return ok;
        }

        /// <summary>
        /// Runs every check that applies to a plain dungeon map.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="notes">The lines to append to.</param>
        /// <returns>True if nothing was found.</returns>
        public static bool Run(DungeonMap map, List<string> notes)
        {
            var ok = Report(notes, "step traps", StepTraps(map));
            ok &= Report(notes, "open edges into the void", VoidEdges(map));
            return ok;
        }

        /// <summary>
        /// Formats one check.
        /// </summary>
        /// <param name="notes">The notes.</param>
        /// <param name="name">The name of the check.</param>
        /// <param name="violations">The violations.</param>
        /// <returns>True if there were none.</returns>
        public static bool Report(List<string> notes, string name, List<string> violations)
        {
            if (violations.Count == 0)
            {
                notes.Add($"Audit OK: {name}");
                return true;
            }

            notes.Add($"Audit FAIL: {name} ({violations.Count})");
            foreach (var v in violations) notes.Add($"  {v}");
            return false;
        }

        /// <summary>
        /// A floor you can walk down onto but not back up from. Walking from the lower cell, the higher neighbour's floor must
        /// be found by the floor lookup, which only reaches <see cref="MapDimensions.FloorProbeReach" /> above the feet.
        /// Found as: the cistern pool was sunk 0.54 m, the rim was out of reach and the player fell through the level.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="maxReports">The maximum number of lines.</param>
        /// <returns>The violations.</returns>
        public static List<string> StepTraps(DungeonMap map, int maxReports = DefaultMaxReports)
        {
            var found = new List<string>();
            var s = map.DungeonCellScale;

            for (var l = 0; l < map.Levels; l++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var c = map.GetCell(x, z, l);
                if (c is not { HasFloor: true }) continue;

                foreach (var d in Directions)
                {
                    var n = map.GetCell(x + d.dx, z + d.dz, l);
                    if (n is not { HasFloor: true }) continue;

                    var yc = (c.FloorHeight + c.LevelBaseHeight) * s;
                    var yn = (n.FloorHeight + n.LevelBaseHeight) * s;
                    if (yn <= yc + 0.001f) continue;

                    // Compared directly instead of asking the floor lookup: on the lowest level a miss returns 0, which
                    // is often the very height of the rim, and the trap would stay invisible.
                    if (yn - yc <= MapDimensions.FloorProbeReach) continue;

                    found.Add(
                        $"level {l} ({x},{z}) -> ({x + d.dx},{z + d.dz}): the rim floor {yn:0.00} m lies {yn - yc:0.00} m " +
                        $"above {yc:0.00} m, out of the {MapDimensions.FloorProbeReach:0.0} m reach of the floor lookup");
                    if (found.Count >= maxReports) return found;
                }
            }

            return found;
        }

        /// <summary>
        /// A floored cell with an open side to a place without a cell. Allowed: the top end of a stair, where the cell above
        /// has no floor (the stairwell). Anything else shows the void, or, in a hybrid world, whatever lies behind it.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="belowLevel">Only levels below this one are checked (the surface level of a hybrid world is open to
        /// the terrain by design). Default: all.</param>
        /// <param name="maxReports">The maximum number of lines.</param>
        /// <returns>The violations.</returns>
        public static List<string> VoidEdges(DungeonMap map, int belowLevel = int.MaxValue,
            int maxReports = DefaultMaxReports)
        {
            var found = new List<string>();

            for (var l = 0; l < Math.Min(map.Levels, belowLevel); l++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var c = map.GetCell(x, z, l);
                if (c is not { HasFloor: true }) continue;

                foreach (var d in Directions)
                {
                    var wall = d.name switch
                    {
                        "N" => c.WallNorth,
                        "S" => c.WallSouth,
                        "E" => c.WallEast,
                        _ => c.WallWest
                    };
                    if (wall != WallType.None) continue;

                    var n = map.GetCell(x + d.dx, z + d.dz, l);
                    if (n is { IsEmpty: false }) continue;

                    var above = l + 1 < map.Levels ? map.GetCell(x, z, l + 1) : null;
                    if (above is { HasFloor: false }) continue; // the top of a stair

                    found.Add($"level {l} ({x},{z}) {d.name}: open side without a cell behind it");
                    if (found.Count >= maxReports) return found;
                }
            }

            return found;
        }

        /// <summary>
        /// On open terrain (no structure at all) a body standing on the ground must be on the surface level as long as the
        /// ground does not rise into the next level band, however deep: a pond bed lies below the level line. Higher ground
        /// (a mountain above the structure grid) takes the level of its height, never less than the surface level and never
        /// more than the top level: that is the rule that keeps the level steady when a body steps from a tower floor
        /// out onto the hill beside it. Found as: stepping into a pond switched the whole world to the cellar view; and
        /// again on the pond rims, because the middle of the cell was compared instead of its lowest ground.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="maxReports">The maximum number of lines.</param>
        /// <returns>The violations.</returns>
        public static List<string> TerrainSupportLevels(HybridWorldMap map, int maxReports = DefaultMaxReports)
        {
            var found = new List<string>();
            if (map.SurfaceTerrain == null) return found;

            var s = map.SharedScale;
            var structures = map.Structures;

            for (var gx = 0; gx < map.Width; gx++)
            for (var gz = 0; gz < map.Depth; gz++)
            {
                var built = false;
                for (var l = 0; l < structures.Levels && !built; l++)
                {
                    built = structures.GetCell(gx, gz, l) is { IsEmpty: false };
                }

                if (built) continue;

                var wrong = 0;
                var firstFeet = 0f;
                for (var i = 0; i <= 4; i++)
                for (var j = 0; j <= 4; j++)
                {
                    var wx = (gx + (i / 4f * 0.98f) + 0.01f) * s;
                    var wz = (gz + (j / 4f * 0.98f) + 0.01f) * s;
                    var feet = map.SurfaceTerrain.GetHeightAt(wx, wz); // standing on the ground at this exact spot
                    // Never below the surface level (a pond bed), otherwise the level of the height (a mountain).
                    var expected = Math.Clamp(Math.Max(map.SurfaceLevel, map.WorldToLevel(feet)), 0, map.Levels - 1);
                    if (map.GetSupportLevel(gx, gz, feet) == expected) continue;

                    if (wrong++ == 0) firstFeet = feet;
                }

                if (wrong == 0) continue;

                found.Add(
                    $"cell ({gx},{gz}): {wrong} of 25 ground spots are not on the expected level (e.g. feet {firstFeet:0.00} m)");
                if (found.Count >= maxReports) return found;
            }

            return found;
        }

        /// <summary>
        /// A prop is drawn with the underground when its height is below <see cref="HybridWorldMap.SubterraneanLimitY" />, with
        /// the surface otherwise. That must agree with the level of the place it stands on (the one the player gets there),
        /// or it is drawn where it does not belong. Found as: pond lilies (y = 7.55, limit was 8.0) showed up in the cellars.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="maxReports">The maximum number of lines.</param>
        /// <param name="limitY">The height limit to test; default <see cref="HybridWorldMap.SubterraneanLimitY" />. Only for
        /// tests that show a wrong limit is caught.</param>
        /// <returns>The violations.</returns>
        public static List<string> MisfiledScenery(HybridWorldMap map, int maxReports = DefaultMaxReports,
            float? limitY = null)
        {
            var found = new List<string>();
            var s = map.SharedScale;
            var limit = limitY ?? map.SubterraneanLimitY;

            foreach (var p in map.Scenery)
            {
                // A stair joins two levels and is drawn with both.
                if (p.Type == SceneryType.StairFlight) continue;

                var gx = map.WorldToGrid(p.Position.X);
                var gz = map.WorldToGrid(p.Position.Z);
                if (!map.InBounds(gx, gz)) continue;

                // Over a stairwell nothing carries the prop (a roof stands over the hole); the floor far below is not its place.
                if (map.Structures.GetCell(gx, gz, map.WorldToLevel(p.Position.Y)) is
                    { IsEmpty: false, HasFloor: false }) continue;

                var undergroundByHeight = p.Position.Y < limit;
                var undergroundByPlace = map.GetSupportLevel(gx, gz, p.Position.Y) < map.SurfaceLevel;
                if (undergroundByHeight == undergroundByPlace) continue;

                found.Add($"{p.Type} at ({p.Position.X / s:0.0},{p.Position.Z / s:0.0}) y={p.Position.Y:0.00}: " +
                          $"filed {(undergroundByHeight ? "underground" : "on the surface")} but stands " +
                          $"{(undergroundByPlace ? "underground" : "on the surface")}");
                if (found.Count >= maxReports) return found;
            }

            return found;
        }
    }
}