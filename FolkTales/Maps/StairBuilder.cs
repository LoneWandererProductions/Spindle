/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        StairBuilder.cs
 * PURPOSE:     Carves a level-high staircase into a DungeonMap: the flight cells, the walls of the stairwell, the hole
 *              in the floor above, the landing, and one StairFlight placement per flight cell.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Builds a staircase that really connects two levels. Works on the <see cref="DungeonMap"/> that holds the cells,
    /// which is the dungeon itself or the Structures layer of a hybrid world, so both use the same rule.
    /// </summary>
    public static class StairBuilder
    {
        /// <summary>
        /// Gets the placement seed that makes a <see cref="SceneryType.StairFlight"/> climb toward the given direction.
        /// Derived from the real yaw rotation, so it stays right if the seed-to-yaw table ever changes.
        /// </summary>
        /// <param name="direction">The compass direction of the climb (North = -Z, East = +X).</param>
        /// <returns>The seed, 0..3.</returns>
        public static int SeedFor(Direction direction)
        {
            var wanted = DirectionVector(direction);

            for (var seed = 0; seed < 4; seed++)
            {
                var yaw = SceneryType.StairFlight.GetRotationYaw(seed);
                var climb = Vector3.Transform(-Vector3.UnitZ, Matrix4x4.CreateRotationY(yaw));
                if (Vector2.Distance(new Vector2(climb.X, climb.Z), wanted) < 0.01f) return seed;
            }

            throw new InvalidOperationException("No seed turns a stair flight toward " + direction);
        }

        /// <summary>
        /// Carves the stairwell and reports every flight placement through <paramref name="addFlight"/>.
        /// </summary>
        /// <param name="map">The map that owns the cells.</param>
        /// <param name="gridX">Grid X of the bottom cell of the flight.</param>
        /// <param name="gridZ">Grid Z of the bottom cell of the flight.</param>
        /// <param name="level">The level the flight starts on; it ends on <c>level + 1</c>.</param>
        /// <param name="direction">The compass direction of the climb.</param>
        /// <param name="addFlight">Called once per flight cell with (world position, seed).</param>
        /// <returns><c>false</c>, and nothing is changed, if the flight does not fit into the map.</returns>
        public static bool Carve(DungeonMap map, int gridX, int gridZ, int level, Direction direction,
            Action<Vector3, int> addFlight)
        {
            var scale = map.DungeonCellScale;
            var cellsLong = StairLayout.CellsLong();
            var (dx, dz) = Step(direction);

            // The flight needs cellsLong cells on `level` plus the landing cell on `level + 1`.
            if (level < 0 || level + 1 >= map.Levels) return false;

            for (var k = 0; k <= cellsLong; k++)
            {
                if (map.GetCell(gridX + (dx * k), gridZ + (dz * k), k < cellsLong ? level : level + 1) == null)
                    return false;
            }

            var seed = SeedFor(direction);
            var left = Left(direction);
            var right = Opposite(left);
            var back = Opposite(direction);

            var wasOpen = false;
            for (var k = 0; k < cellsLong; k++)
            {
                var cx = gridX + (dx * k);
                var cz = gridZ + (dz * k);

                // Lower cell: floor to walk on up to the stair, solid side walls so it reads as a stairwell.
                var low = map.GetCell(cx, cz, level)!;
                low.HasFloor = true;
                SetWall(map, cx, cz, level, left, WallType.Solid);
                SetWall(map, cx, cz, level, right, WallType.Solid);

                // Upper cell: open where the slab would be closer than the headroom to the treads.
                var open = StairLayout.NeedsOpeningAbove(k, scale);
                var up = map.GetCell(cx, cz, level + 1)!;
                if (open)
                {
                    // The slab above the treads is gone, so the ceiling of the lower cell has to go too.
                    low.HasCeiling = false;
                    up.HasFloor = false;
                    SetWall(map, cx, cz, level + 1, left, WallType.Solid);
                    SetWall(map, cx, cz, level + 1, right, WallType.Solid);

                    // Nobody may step into the hole from the floor behind it.
                    if (!wasOpen) SetWall(map, cx, cz, level + 1, back, WallType.Solid);
                }

                wasOpen = open;

                var y = (level * map.LevelHeight) + (k * StairLayout.RisePerCell(scale));
                addFlight(new Vector3((cx + 0.5f) * scale, y, (cz + 0.5f) * scale), seed);
            }

            // Landing on the upper level, directly after the last flight cell.
            var landing = map.GetCell(gridX + (dx * cellsLong), gridZ + (dz * cellsLong), level + 1)!;
            landing.HasFloor = true;
            SetWall(map, gridX + (dx * cellsLong), gridZ + (dz * cellsLong), level + 1, back, WallType.None);

            return true;
        }

        /// <summary>
        /// Carves a 2x2 spiral staircase climbing from <paramref name="level" /> to <c>level + 1</c>.
        /// and reports scenery placements through <paramref name="addScenery" />.
        /// </summary>
        /// <param name="map">The target map.</param>
        /// <param name="gridX">Grid X of the bottom starting cell.</param>
        /// <param name="gridZ">Grid Z of the bottom starting cell.</param>
        /// <param name="level">The starting level.</param>
        /// <param name="addScenery">The add scenery.</param>
        /// <param name="withWalls">If true, encloses the 2x2 footprint in a walled shaft.</param>
        /// <param name="addCenterPillar">If true, places an AncientPillar as the central spindle.</param>
        /// <returns>
        /// True if carved successfully; false if space is out of bounds.
        /// </returns>
        public static bool CarveSpiral(
            DungeonMap map,
            int gridX, int gridZ,
            int level,
            Action<Vector3, int, SceneryType, float> addScenery,
            bool withWalls = true,
            bool addCenterPillar = true)
        {
            var scale = map.DungeonCellScale;
            var risePerCell = StairLayout.RisePerCell(scale);

            var steps = new (int x, int z, Direction dir)[]
            {
                (gridX, gridZ, Direction.North), // Step 0: Climb North
                (gridX, gridZ - 1, Direction.East), // Step 1: Climb East
                (gridX + 1, gridZ - 1, Direction.South), // Step 2: Climb South
                (gridX + 1, gridZ, Direction.West) // Step 3: Climb West
            };

            if (level < 0 || level + 1 >= map.Levels) return false;

            foreach (var (x, z, _) in steps)
            {
                if (map.GetCell(x, z, level) == null || map.GetCell(x, z, level + 1) == null)
                    return false;
            }

            // Interior walls on start level open
            SetWall(map, gridX, gridZ, level, Direction.North, WallType.None);
            SetWall(map, gridX, gridZ - 1, level, Direction.East, WallType.None);
            SetWall(map, gridX + 1, gridZ - 1, level, Direction.South, WallType.None);
            SetWall(map, gridX + 1, gridZ, level + 1, Direction.West, WallType.None);

            for (var k = 0; k < steps.Length; k++)
            {
                var (cx, cz, dir) = steps[k];
                var seed = SeedFor(dir);

                var low = map.GetCell(cx, cz, level)!;
                low.HasFloor = true;

                if (StairLayout.NeedsOpeningAbove(k, scale))
                {
                    var up = map.GetCell(cx, cz, level + 1)!;
                    low.HasCeiling = false;
                    up.HasFloor = false;
                }

                var y = (level * map.LevelHeight) + (k * risePerCell);
                var pos = new Vector3((cx + 0.5f) * scale, y, (cz + 0.5f) * scale);

                addScenery(pos, seed, SceneryType.StairFlight, scale);
            }

            var landing = map.GetCell(gridX, gridZ, level + 1)!;
            landing.HasFloor = true;

            if (withWalls)
            {
                for (var lvl = level; lvl <= level + 1; lvl++)
                {
                    SetWall(map, gridX, gridZ, lvl, Direction.West, WallType.Solid);
                    SetWall(map, gridX, gridZ - 1, lvl, Direction.West, WallType.Solid);

                    SetWall(map, gridX, gridZ - 1, lvl, Direction.North, WallType.Solid);
                    SetWall(map, gridX + 1, gridZ - 1, lvl, Direction.North, WallType.Solid);

                    SetWall(map, gridX + 1, gridZ - 1, lvl, Direction.East, WallType.Solid);
                    SetWall(map, gridX + 1, gridZ, lvl, Direction.East, WallType.Solid);

                    SetWall(map, gridX + 1, gridZ, lvl, Direction.South, WallType.Solid);
                    SetWall(map, gridX, gridZ, lvl, Direction.South, WallType.Solid);
                }

                SetWall(map, gridX, gridZ, level, Direction.South, WallType.None);
                SetWall(map, gridX, gridZ, level + 1, Direction.West, WallType.None);
            }

            if (addCenterPillar)
            {
                var pillarX = (gridX + 1.0f) * scale;
                var pillarZ = gridZ * scale;
                var pillarY = level * map.LevelHeight;

                addScenery(new Vector3(pillarX, pillarY, pillarZ), 0, SceneryType.AncientPillar, scale);
            }

            return true;
        }

        /// <summary>
        /// Direct overload for DungeonMap.
        /// </summary>
        public static bool CarveSpiral(
            DungeonMap map,
            int gridX, int gridZ,
            int level,
            bool withWalls = true,
            bool addCenterPillar = true) =>
            CarveSpiral(map, gridX, gridZ, level,
                (pos, seed, type, fitScale) => map.AddScenery(pos, seed, type, -1, -1, fitScale),
                withWalls, addCenterPillar);

        /// <summary>
        /// Sets one edge of a cell and the matching edge of its neighbour, so walls, collision and sight agree.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <param name="side">The side.</param>
        /// <param name="type">The type.</param>
        private static void SetWall(DungeonMap map, int x, int z, int level, Direction side, WallType type)
        {
            Apply(map.GetCell(x, z, level), side, type);

            var (dx, dz) = Step(side);
            Apply(map.GetCell(x + dx, z + dz, level), Opposite(side), type);
        }

        private static void Apply(MapCell3D? cell, Direction side, WallType type)
        {
            if (cell == null) return;

            switch (side)
            {
                case Direction.North:
                    cell.WallNorth = type;
                    break;
                case Direction.South:
                    cell.WallSouth = type;
                    break;
                case Direction.East:
                    cell.WallEast = type;
                    break;
                default:
                    cell.WallWest = type;
                    break;
            }
        }

        private static (int Dx, int Dz) Step(Direction d) => d switch
        {
            Direction.North => (0, -1),
            Direction.South => (0, 1),
            Direction.East => (1, 0),
            _ => (-1, 0)
        };

        private static Vector2 DirectionVector(Direction d)
        {
            var (x, z) = Step(d);
            return new Vector2(x, z);
        }

        private static Direction Opposite(Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            _ => Direction.East
        };

        /// <summary>
        /// Turns 90 degrees to the left when looking along <paramref name="d" /> from above (+Z is south).
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>The direction to the left of <paramref name="d" />.</returns>
        private static Direction Left(Direction d) => d switch
        {
            Direction.North => Direction.West,
            Direction.West => Direction.South,
            Direction.South => Direction.East,
            _ => Direction.North
        };
    }
}