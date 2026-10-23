/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        StructureBrush.cs
 * PURPOSE:     Level-aware building blocks for the cell grid (rooms, streets, doors, windows, pools, stairs, props).
 *              Shared by DungeonBuilder and HybridWorldBuilder (HybridWorldMap.Structures is a DungeonMap as well), so a map
 *              like a whole town is written once and works in the dungeon and in the hybrid viewport.
 */

using System.Drawing;
using System.Numerics;
using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Paints rooms, corridors, doors, pools, stairs and props into a <see cref="DungeonMap"/> on ANY level, with a grid
    /// offset so a block can be placed inside a bigger map. Coordinates are local to the offset.
    /// Create it with <c>DungeonBuilder.CreateBrush</c> or <c>HybridWorldBuilder.CreateBrush</c>, which wire the hooks.
    /// </summary>
    public sealed class StructureBrush
    {
        private readonly DungeonMap _map;
        private readonly int _offX;
        private readonly int _offZ;
        private readonly int _surfaceLevel;
        private readonly Action<Vector3, int, SceneryType> _addProp;
        private readonly Func<int, int, int, Direction, bool> _addStair;
        private readonly Action<int, int>? _onSurfaceFloor;

        /// <summary>
        /// Notes and warnings collected while painting (stairs that did not fit, resulting stair cells).
        /// </summary>
        public List<string> Notes { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StructureBrush"/> class.
        /// </summary>
        /// <param name="map">The cell grid.</param>
        /// <param name="offsetX">Grid offset of local (0, 0).</param>
        /// <param name="offsetZ">Grid offset of local (0, 0).</param>
        /// <param name="surfaceLevel">Level of the ground surface; floors on it call <paramref name="onSurfaceFloor"/>.</param>
        /// <param name="addProp">Adds a prop (world position, seed, type) to the owning map.</param>
        /// <param name="addStair">Carves a straight stair (global x, z, lower level, direction).</param>
        /// <param name="onSurfaceFloor">Called with global (x, z) for every floor painted on the surface level (hybrid: punch the terrain).</param>
        public StructureBrush(DungeonMap map, int offsetX, int offsetZ, int surfaceLevel,
            Action<Vector3, int, SceneryType> addProp, Func<int, int, int, Direction, bool> addStair,
            Action<int, int>? onSurfaceFloor = null)
        {
            _map = map;
            _offX = offsetX;
            _offZ = offsetZ;
            _surfaceLevel = surfaceLevel;
            _addProp = addProp;
            _addStair = addStair;
            _onSurfaceFloor = onSurfaceFloor;
        }

        /// <summary>
        /// Gets the cell at local coordinates, or null outside the map.
        /// </summary>
        public MapCell3D? Cell(int x, int z, int level) => _map.GetCell(x + _offX, z + _offZ, level);

        /// <summary>
        /// Empties the whole grid (no floors, ceilings, walls, layers) and resets the level heights.
        /// </summary>
        public void Clear()
        {
            for (var x = 0; x < _map.Width; x++)
            for (var z = 0; z < _map.Depth; z++)
            for (var l = 0; l < _map.Levels; l++)
            {
                var c = _map.GetCell(x, z, l);
                if (c == null) continue;

                c.HasFloor = false;
                c.HasCeiling = false;
                c.WallNorth = c.WallSouth = c.WallEast = c.WallWest = WallType.None;
                c.Layers = null;
                c.FloorHeight = 0f;
                c.CeilingHeight = MapDimensions.LevelHeightCells;
            }
        }

        /// <summary>
        /// Paints one floor cell. <paramref name="roofed"/> false leaves the sky open (streets, courtyards).
        /// </summary>
        public void Floor(int level, int x, int z, Color wall, Color floor, Color? ceiling, bool roofed)
        {
            var c = Cell(x, z, level);
            if (c == null) return;

            c.HasFloor = true;
            c.HasCeiling = roofed;
            c.FloorHeight = 0f;
            c.CeilingHeight = MapDimensions.LevelHeightCells;
            c.FloorColor = floor;
            c.WallColor = wall;
            c.HasFlatCeiling = true;
            if (ceiling.HasValue) c.CeilingColor = ceiling.Value;

            if (level == _surfaceLevel) _onSurfaceFloor?.Invoke(x + _offX, z + _offZ);
        }

        /// <summary>
        /// Paints a rectangular room (floor, optional ceiling, solid walls on its outline).
        /// </summary>
        public void Room(int level, int x0, int z0, int x1, int z1, Color wall, Color floor, bool roofed = true,
            Color? ceiling = null)
        {
            for (var x = x0; x <= x1; x++)
            for (var z = z0; z <= z1; z++)
                Floor(level, x, z, wall, floor, ceiling, roofed);

            for (var x = x0; x <= x1; x++)
            {
                Edge(level, x, z0, Direction.North, WallType.Solid);
                Edge(level, x, z1, Direction.South, WallType.Solid);
            }

            for (var z = z0; z <= z1; z++)
            {
                Edge(level, x0, z, Direction.West, WallType.Solid);
                Edge(level, x1, z, Direction.East, WallType.Solid);
            }
        }

        /// <summary>
        /// Turns every second solid outline wall of a room into a window (corners excluded).
        /// </summary>
        public void Windows(int level, int x0, int z0, int x1, int z1)
        {
            for (var x = x0 + 1; x < x1; x++)
            {
                if ((x - x0) % 2 != 1) continue;
                WindowIfSolid(level, x, z0, Direction.North);
                WindowIfSolid(level, x, z1, Direction.South);
            }

            for (var z = z0 + 1; z < z1; z++)
            {
                if ((z - z0) % 2 != 1) continue;
                WindowIfSolid(level, x0, z, Direction.West);
                WindowIfSolid(level, x1, z, Direction.East);
            }
        }

        /// <summary>
        /// Opens a wall edge completely (passage between two cells).
        /// </summary>
        public void Opening(int level, int x, int z, Direction d) => Edge(level, x, z, d, WallType.None);

        /// <summary>
        /// Puts a closed wooden door on a wall edge (both sides).
        /// </summary>
        public void Door(int level, int x, int z, Direction d)
        {
            Edge(level, x, z, d, WallType.DoorClosed);

            var (dx, dz) = Step(d);
            foreach (var c in new[] { Cell(x, z, level), Cell(x + dx, z + dz, level) })
            {
                if (c == null || !c.HasFloor) continue;
                c.OpeningHeightRatio = MapDimensions.DoorOpeningRatio;
                c.DoorStyle = DoorStyle.Wood;
            }
        }

        /// <summary>
        /// Sets a wall on one cell edge and, if the neighbour is a real cell with a floor, on its side as well. The outside
        /// (rock, terrain, map edge) is left untouched.
        /// </summary>
        public void Edge(int level, int x, int z, Direction d, WallType type)
        {
            var c = Cell(x, z, level);
            if (c == null) return;
            SetWall(c, d, type);

            var (dx, dz) = Step(d);
            var n = Cell(x + dx, z + dz, level);
            if (n is { HasFloor: true }) SetWall(n, Opposite(d), type);
        }

        /// <summary>
        /// Sinks a rectangle of cells into a liquid pool (a cell layer). Works on any level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="x0">The x0.</param>
        /// <param name="z0">The z0.</param>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="liquid">Layer name, e.g. "Water" or "Lava".</param>
        /// <param name="depth">Floor offset in cells (negative = sunken).</param>
        /// <param name="fill">Liquid height above the sunken floor in cells.</param>
        /// <param name="bed">Colour of the pool bed.</param>
        public void Pool(int level, int x0, int z0, int x1, int z1, string liquid, float depth, float fill,
            Color? bed = null)
        {
            // A pool bed deeper than the player can step out of (and deeper than the floor probe reaches) traps the walker
            // or, worse, makes the floor lookup lose the rim and drop him through the level. Clamp it in metres.
            depth = MathF.Max(depth, -MapDimensions.MaxPoolDepthMetres / _map.DungeonCellScale);
            for (var x = x0; x <= x1; x++)
            for (var z = z0; z <= z1; z++)
            {
                var c = Cell(x, z, level);
                if (c == null) continue;

                c.HasFloor = true;
                c.FloorHeight = depth;
                c.FloorColor = bed ?? Color.FromArgb(35, 30, 30);
                c.Layers ??= new List<CellLayer>();
                c.Layers.Add(new CellLayer { Name = liquid, Height = fill, TextureId = null, AlphaBlend = true });
            }
        }

        /// <summary>
        /// Hangs a wall torch on every solid wall of a cell.
        /// </summary>
        public void Torches(int level, int x, int z)
        {
            var c = Cell(x, z, level);
            if (c is not { HasFloor: true }) return;

            if (c.WallNorth == WallType.Solid) Prop(x + 0.5f, z + 0.5f, SceneryType.WallTorch, 0, level);
            if (c.WallSouth == WallType.Solid) Prop(x + 0.5f, z + 0.5f, SceneryType.WallTorch, 1, level);
            if (c.WallWest == WallType.Solid) Prop(x + 0.5f, z + 0.5f, SceneryType.WallTorch, 2, level);
            if (c.WallEast == WallType.Solid) Prop(x + 0.5f, z + 0.5f, SceneryType.WallTorch, 3, level);
        }

        /// <summary>
        /// Paints the floor cells under a stair flight on the lower level, so openings can be cut to it before it is carved.
        /// </summary>
        public void StairFootprint(int lowLevel, int x, int z, Direction d, Color wall, Color floor)
        {
            var (dx, dz) = Step(d);
            for (var k = 0; k < StairLayout.CellsLong(); k++)
                Floor(lowLevel, x + dx * k, z + dz * k, wall, floor, null, true);
        }

        /// <summary>
        /// Carves a straight stair from <paramref name="lowLevel"/> to the level above. Notes receive the result.
        /// </summary>
        public bool Stair(int lowLevel, int x, int z, Direction d, string name)
        {
            var ok = _addStair(x + _offX, z + _offZ, lowLevel, d);
            if (!ok)
            {
                Notes.Add($"PROBLEM: stair '{name}' does not fit (carve returned false).");
                return false;
            }

            var (dx, dz) = Step(d);
            var len = StairLayout.CellsLong();
            for (var k = 0; k < len; k++)
            {
                var low = Cell(x + dx * k, z + dz * k, lowLevel);
                var up = Cell(x + dx * k, z + dz * k, lowLevel + 1);
                Notes.Add(
                    $"  k{k}: lower floor={low?.HasFloor} ceiling={low?.HasCeiling} | upper floor={up?.HasFloor}");
            }

            var landing = Cell(x + dx * len, z + dz * len, lowLevel + 1);
            Notes.Add($"  landing: upper floor={landing?.HasFloor}");
            Notes.Add($"Stair OK: {name}");
            return true;
        }

        /// <summary>
        /// Adds a prop at local cell coordinates (fractions allowed) standing on the floor of the given level.
        /// </summary>
        public void Prop(float gx, float gz, SceneryType type, int seed, int level) =>
            _addProp(new Vector3((gx + _offX) * _map.DungeonCellScale, level * _map.LevelHeight,
                (gz + _offZ) * _map.DungeonCellScale), seed, type);

        private void WindowIfSolid(int level, int x, int z, Direction d)
        {
            var c = Cell(x, z, level);
            if (c == null || GetWall(c, d) != WallType.Solid) return;
            Edge(level, x, z, d, WallType.Window);
        }

        private static (int dx, int dz) Step(Direction d) => d switch
        {
            Direction.North => (0, -1),
            Direction.South => (0, 1),
            Direction.East => (1, 0),
            _ => (-1, 0)
        };

        private static Direction Opposite(Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            _ => Direction.East
        };

        private static WallType GetWall(MapCell3D c, Direction d) => d switch
        {
            Direction.North => c.WallNorth,
            Direction.South => c.WallSouth,
            Direction.East => c.WallEast,
            _ => c.WallWest
        };

        private static void SetWall(MapCell3D c, Direction d, WallType t)
        {
            switch (d)
            {
                case Direction.North:
                    c.WallNorth = t;
                    break;
                case Direction.South:
                    c.WallSouth = t;
                    break;
                case Direction.East:
                    c.WallEast = t;
                    break;
                default:
                    c.WallWest = t;
                    break;
            }
        }
    }
}