/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        DungeonBuilder.cs
 * PURPOSE:     Fluent API builder for constructing multi-layer dungeons with native
 *              cell door archways, sunken liquid pools, and sealed roof headers.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Drawing;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Fluent API for building multi-level dungeons with automatic native door framing,
    /// sunken liquid pools, and interactive scenery props.
    /// </summary>
    public sealed class DungeonBuilder
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly DungeonMap? _map;

        /// <summary>
        /// The spawn position
        /// </summary>
        private Vector3 _spawnPosition = new(10.5f, 1.7f, 10.5f);

        /// <summary>
        /// Gets the calculated player spawn position vector.
        /// </summary>
        public Vector3 SpawnPosition => _spawnPosition;

        /// <summary>
        /// Initializes a new fluent builder instance.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="cellScale">The cell scale.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public static DungeonBuilder Create(int width = 20, int depth = 20, int levels = 2,
            float cellScale = MapDimensions.DefaultCellSize)
        {
            return new DungeonBuilder(width, depth, levels, cellScale);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonBuilder" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="cellScale">The cell scale.</param>
        private DungeonBuilder(int width, int depth, int levels, float cellScale)
        {
            _map = new DungeonMap(width, depth, levels) { DungeonCellScale = cellScale };
            ClearMap();
        }

        /// <summary>
        /// Sets the first level that is open air (empty cells and cells without ceiling get daylight from there up). Use it for
        /// a town or a courtyard on a level above 0.
        /// </summary>
        /// <param name="level">First open-air level.</param>
        public DungeonBuilder WithOpenAirFromLevel(int level)
        {
            if (_map != null) _map.OpenAirFromLevel = level;
            return this;
        }

        /// <summary>
        /// Creates a level-aware brush (rooms, streets, doors, pools, stairs, props on any level) on this builder's map.
        /// </summary>
        /// <param name="offsetX">Grid offset of the brush's local (0, 0).</param>
        /// <param name="offsetZ">Grid offset of the brush's local (0, 0).</param>
        public StructureBrush CreateBrush(int offsetX = 0, int offsetZ = 0)
        {
            var map = _map ?? throw new InvalidOperationException("The builder has no map.");
            return new StructureBrush(map, offsetX, offsetZ, map.OpenAirFromLevel,
                (pos, seed, type) => map.AddScenery(pos, seed, type, -1, -1),
                (x, z, level, dir) => map.AddStaircase(x, z, level, dir));
        }

        /// <summary>
        /// Sets the player starting spawn grid coordinate.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder SetSpawn(float gridX, float gridZ, int level = 0)
        {
            if (_map == null) return this;

            var scale = _map.DungeonCellScale;
            var levelOffsetY = level * _map.BaseHeight * scale;
            _spawnPosition = new Vector3(gridX * scale, levelOffsetY + 1.7f, gridZ * scale);
            return this;
        }

        /// <summary>
        /// Adds a multi-level room enclosure with Level 0 floors and Level 1 roof ceilings.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="wallColor">Color of the wall.</param>
        /// <param name="floorColor">Color of the floor.</param>
        /// <param name="ceilingColor">Color of the ceiling.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddRoom(int minX, int minZ, int maxX, int maxZ,
            Color wallColor, Color floorColor, Color? ceilingColor = null)
        {
            if (_map == null) return this;

            var roofColor = ceilingColor ?? Color.FromArgb(30, 30, 40);

            for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
            {
                // Level 0: Main Room Floor
                var cell0 = _map.GetCell(x, z, 0);
                if (cell0 != null)
                {
                    cell0.HasFloor = true;
                    cell0.HasCeiling = false; // Open upward into Level 1
                    cell0.FloorHeight = 0.0f;
                    cell0.FloorColor = floorColor;
                    cell0.WallColor = wallColor;

                    cell0.WallNorth = (z == minZ) ? WallType.Solid : WallType.None;
                    cell0.WallSouth = (z == maxZ) ? WallType.Solid : WallType.None;
                    cell0.WallWest = (x == minX) ? WallType.Solid : WallType.None;
                    cell0.WallEast = (x == maxX) ? WallType.Solid : WallType.None;

                    cell0.HasWallNorth = (z == minZ);
                    cell0.HasWallSouth = (z == maxZ);
                    cell0.HasWallWest = (x == minX);
                    cell0.HasWallEast = (x == maxX);
                }

                // Level 1: Roof Enclosure
                if (_map.Levels > 1)
                {
                    var cell1 = _map.GetCell(x, z, 1);
                    if (cell1 != null)
                    {
                        cell1.HasFloor = false; // Open downward into Level 0
                        cell1.HasCeiling = true; // Seals off weather/sky
                        cell1.CeilingColor = roofColor;
                        cell1.WallColor = wallColor;

                        cell1.WallNorth = (z == minZ) ? WallType.Solid : WallType.None;
                        cell1.WallSouth = (z == maxZ) ? WallType.Solid : WallType.None;
                        cell1.WallWest = (x == minX) ? WallType.Solid : WallType.None;
                        cell1.WallEast = (x == maxX) ? WallType.Solid : WallType.None;

                        cell1.HasWallNorth = (z == minZ);
                        cell1.HasWallSouth = (z == maxZ);
                        cell1.HasWallWest = (x == minX);
                        cell1.HasWallEast = (x == maxX);
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Adds a connecting corridor between two coordinates.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="wallColor">Color of the wall.</param>
        /// <param name="floorColor">Color of the floor.</param>
        /// <param name="ceilingColor">Color of the ceiling.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddCorridor(int minX, int minZ, int maxX, int maxZ,
            Color wallColor, Color floorColor, Color? ceilingColor = null)
        {
            return AddRoom(minX, minZ, maxX, maxZ, wallColor, floorColor, ceilingColor);
        }

        /// <summary>
        /// Places a native cell doorway between adjacent cells.
        /// Level 0 gets the door opening with stone arch header, and Level 1 retains a solid wall.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        /// <param name="style">How the door looks; stored on both cells of the doorway.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddDoor(int x1, int z1, int x2, int z2, bool isOpen = false,
            DoorStyle style = DoorStyle.Wood)
        {
            var wallState = isOpen ? WallType.DoorOpen : WallType.DoorClosed;

            if (_map == null) return this;

            var cell10 = _map.GetCell(x1, z1, 0);
            var cell20 = _map.GetCell(x2, z2, 0);
            var cell11 = _map.GetCell(x1, z1, 1);
            var cell21 = _map.GetCell(x2, z2, 1);

            if (cell10 != null)
            {
                cell10.OpeningHeightRatio = MapDimensions.DoorOpeningRatio;
                cell10.DoorStyle = style;
            }

            if (cell20 != null)
            {
                cell20.OpeningHeightRatio = MapDimensions.DoorOpeningRatio;
                cell20.DoorStyle = style;
            }

            if (x2 > x1) // Moving East
            {
                if (cell10 != null) cell10.WallEast = wallState;
                if (cell20 != null) cell20.WallWest = wallState;
                if (cell11 != null) cell11.WallEast = WallType.Solid;
                if (cell21 != null) cell21.WallWest = WallType.Solid;
            }
            else if (x2 < x1) // Moving West
            {
                if (cell10 != null) cell10.WallWest = wallState;
                if (cell20 != null) cell20.WallEast = wallState;
                if (cell11 != null) cell11.WallWest = WallType.Solid;
                if (cell21 != null) cell21.WallEast = WallType.Solid;
            }
            else if (z2 > z1) // Moving South
            {
                if (cell10 != null) cell10.WallSouth = wallState;
                if (cell20 != null) cell20.WallNorth = wallState;
                if (cell11 != null) cell11.WallSouth = WallType.Solid;
                if (cell21 != null) cell21.WallNorth = WallType.Solid;
            }
            else if (z2 < z1) // Moving North
            {
                if (cell10 != null) cell10.WallNorth = wallState;
                if (cell20 != null) cell20.WallSouth = wallState;
                if (cell11 != null) cell11.WallNorth = WallType.Solid;
                if (cell21 != null) cell21.WallSouth = WallType.Solid;
            }

            return this;
        }

        /// <summary>
        /// Places a native cell doorway between adjacent cells without hardcoded props.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        /// <param name="style">The style.</param>
        /// <returns>
        /// The current <see cref="DungeonBuilder" /> instance.
        /// </returns>
        public DungeonBuilder AddFramedDoor(int x1, int z1, int x2, int z2, bool isOpen = false,
            DoorStyle style = DoorStyle.Wood)
        {
            return AddDoor(x1, z1, x2, z2, isOpen, style);
        }

        /// <summary>
        /// Automatically snaps wall torches to all solid walls adjacent to the specified cell.
        /// Places 1 torch per adjacent wall automatically.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="DungeonBuilder" /> instance.</returns>
        public DungeonBuilder AddCellTorches(int x, int z, int level = 0)
        {
            var cell = _map?.GetCell(x, z, level);
            if (cell is not { HasFloor: true }) return this;

            var centerX = x + 0.5f;
            var centerZ = z + 0.5f;

            if (cell.WallNorth == WallType.Solid)
                AddProp(centerX, centerZ, SceneryType.WallTorch, seed: 0, level: level);
            if (cell.WallSouth == WallType.Solid)
                AddProp(centerX, centerZ, SceneryType.WallTorch, seed: 1, level: level);
            if (cell.WallWest == WallType.Solid)
                AddProp(centerX, centerZ, SceneryType.WallTorch, seed: 2, level: level);
            if (cell.WallEast == WallType.Solid)
                AddProp(centerX, centerZ, SceneryType.WallTorch, seed: 3, level: level);

            return this;
        }

        /// <summary>
        /// Opens a fully walkable passage between Level 0 cells.
        /// Keeps Level 1 walls SOLID above the passage to prevent sky/daylight leaks.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddPassage(int x1, int z1, int x2, int z2)
        {
            if (_map == null) return this;

            var cell10 = _map.GetCell(x1, z1, 0);
            var cell20 = _map.GetCell(x2, z2, 0);

            var cell11 = _map.GetCell(x1, z1, 1);
            var cell21 = _map.GetCell(x2, z2, 1);

            if (x2 > x1) // East
            {
                if (cell10 != null) cell10.WallEast = WallType.None;
                if (cell20 != null) cell20.WallWest = WallType.None;
                if (cell11 != null) cell11.WallEast = WallType.Solid;
                if (cell21 != null) cell21.WallWest = WallType.Solid;
            }
            else if (x2 < x1) // West
            {
                if (cell10 != null) cell10.WallWest = WallType.None;
                if (cell20 != null) cell20.WallEast = WallType.None;
                if (cell11 != null) cell11.WallWest = WallType.Solid;
                if (cell21 != null) cell21.WallEast = WallType.Solid;
            }
            else if (z2 > z1) // South
            {
                if (cell10 != null) cell10.WallSouth = WallType.None;
                if (cell20 != null) cell20.WallNorth = WallType.None;
                if (cell11 != null) cell11.WallSouth = WallType.Solid;
                if (cell21 != null) cell21.WallNorth = WallType.Solid;
            }
            else if (z2 < z1) // North
            {
                if (cell10 != null) cell10.WallNorth = WallType.None;
                if (cell20 != null) cell20.WallSouth = WallType.None;
                if (cell11 != null) cell11.WallNorth = WallType.Solid;
                if (cell21 != null) cell21.WallSouth = WallType.Solid;
            }

            return this;
        }

        /// <summary>
        /// Creates a sunken floor pit/basin (recessed floor) filled with liquid.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="liquidType">Type of the liquid.</param>
        /// <param name="customColor">Color of the custom.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="liquidFillRatio">The liquid fill ratio.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddSunkenPool(int minX, int minZ, int maxX, int maxZ,
            string liquidType, Color? customColor = null, float depth = -0.18f, float liquidFillRatio = 0.12f,
            int level = 0)
        {
            if (_map == null) return this;

            // A bed deeper than the player can climb out of is a trap (see MapAudit.StepTraps): same limit as StructureBrush.
            depth = MathF.Max(depth, -MapDimensions.MaxPoolDepthMetres / _map.DungeonCellScale);

            for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
            {
                var cell = _map.GetCell(x, z, level);
                if (cell == null) continue;

                cell.HasFloor = true;
                cell.FloorHeight = depth;
                cell.FloorColor = Color.FromArgb(35, 30, 30);

                cell.Layers ??= new List<CellLayer>();
                cell.Layers.Add(new CellLayer
                {
                    Name = liquidType, // "Water" or "Lava"
                    Height = liquidFillRatio,
                    Color = customColor,
                    TextureId = null,
                    AlphaBlend = true
                });
            }

            return this;
        }

        /// <summary>
        /// Builds a level-high staircase climbing from <paramref name="level"/> to <c>level + 1</c>.
        /// Carves the stairwell walls, flight cells, landing, and upper floor opening.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom cell.</param>
        /// <param name="gridZ">Grid Z of the bottom cell.</param>
        /// <param name="direction">The compass direction of the climb.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddStaircase(int gridX, int gridZ, Direction direction, int level = 0)
        {
            _map?.AddStaircase(gridX, gridZ, level, direction);
            return this;
        }

        /// <summary>
        /// Builds a 2x2 spiral staircase climbing from <paramref name="level" /> to <c>level + 1</c>.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom starting entry cell.</param>
        /// <param name="gridZ">Grid Z of the bottom starting entry cell.</param>
        /// <param name="level">The starting level index.</param>
        /// <param name="withWalls">If true, encloses the staircase in a walled tower shaft.</param>
        /// <param name="addCenterPillar">If true, places a central spindle pillar at the core.</param>
        /// <returns>
        /// The current <see cref="DungeonBuilder" /> instance.
        /// </returns>
        public DungeonBuilder AddSpiralStaircase(int gridX, int gridZ, int level = 0, bool withWalls = true,
            bool addCenterPillar = true)
        {
            _map?.AddSpiralStaircase(gridX, gridZ, level, withWalls, addCenterPillar);
            return this;
        }

        /// <summary>
        /// Puts any wall type on the edge between two adjacent cells, on BOTH cells, like <see cref="AddDoor"/> does.
        /// Level 0 gets <paramref name="wall"/>; a roof level above keeps a solid wall so the edge stays sealed.
        /// </summary>
        /// <param name="x1">Cell A x.</param>
        /// <param name="z1">Cell A z.</param>
        /// <param name="x2">Cell B x (next to A).</param>
        /// <param name="z2">Cell B z (next to A).</param>
        /// <param name="wall">The wall type for level 0.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddWall(int x1, int z1, int x2, int z2, WallType wall)
        {
            if (_map == null) return this;

            Direction fromA, fromB;
            if (x2 > x1)
            {
                fromA = Direction.East;
                fromB = Direction.West;
            }
            else if (x2 < x1)
            {
                fromA = Direction.West;
                fromB = Direction.East;
            }
            else if (z2 > z1)
            {
                fromA = Direction.South;
                fromB = Direction.North;
            }
            else if (z2 < z1)
            {
                fromA = Direction.North;
                fromB = Direction.South;
            }
            else return this;

            _map.GetCell(x1, z1, 0)?.SetWall(fromA, wall);
            _map.GetCell(x2, z2, 0)?.SetWall(fromB, wall);

            if (_map.Levels > 1)
            {
                const WallType upper = WallType.Solid;
                _map.GetCell(x1, z1, 1)?.SetWall(fromA, upper);
                _map.GetCell(x2, z2, 1)?.SetWall(fromB, upper);
            }

            return this;
        }

        /// <summary>
        /// A wall with a low hole between two adjacent cells: only a crawling body gets through (fox hole, cave mouth).
        /// </summary>
        /// <param name="x1">Cell A x.</param>
        /// <param name="z1">Cell A z.</param>
        /// <param name="x2">Cell B x.</param>
        /// <param name="z2">Cell B z.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddCrawlHole(int x1, int z1, int x2, int z2) => AddWall(x1, z1, x2, z2, WallType.Crawl);

        /// <summary>
        /// A single round cell (burrow, cave bubble): a closed chamber with floor and roof. Open it with
        /// <see cref="AddPassage"/> (walkable mouth), <see cref="AddCrawlHole"/> (crawl mouth) or <see cref="AddDoor"/>.
        /// </summary>
        /// <param name="x">Cell x.</param>
        /// <param name="z">Cell z.</param>
        /// <param name="wallColor">Wall colour.</param>
        /// <param name="floorColor">Floor colour.</param>
        /// <param name="ceilingColor">Ceiling colour.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddRoundCell(int x, int z, Color wallColor, Color floorColor, Color? ceilingColor = null)
        {
            if (_map == null) return this;

            AddRoom(x, z, x, z, wallColor, floorColor, ceilingColor);
            var cell = _map.GetCell(x, z, 0);
            if (cell != null) cell.Shape = CellShape.Round;
            return this;
        }

        /// <summary>
        /// A single tunnel cell: closed on all sides, with a rounded (arched) cross section once two opposite edges are opened
        /// with <see cref="AddPassage"/> (walkable) or <see cref="AddCrawlHole"/> (low crawl tunnel). Open edges on ONE axis only;
        /// bends and junctions are <see cref="AddRoundCell"/>.
        /// </summary>
        /// <param name="x">Cell x.</param>
        /// <param name="z">Cell z.</param>
        /// <param name="wallColor">Wall colour.</param>
        /// <param name="floorColor">Floor colour.</param>
        /// <param name="ceilingColor">Ceiling colour.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddTunnel(int x, int z, Color wallColor, Color floorColor, Color? ceilingColor = null)
        {
            if (_map == null) return this;

            AddRoom(x, z, x, z, wallColor, floorColor, ceilingColor);
            var cell = _map.GetCell(x, z, 0);
            if (cell != null) cell.Shape = CellShape.Tunnel;
            return this;
        }

        /// <summary>
        /// Places a decorative 3D scenery prop in the dungeon.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="type">The type.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonBuilder AddProp(float gridX, float gridZ, SceneryType type, int seed = 0, int level = 0)
        {
            if (_map == null) return this;

            var scale = _map.DungeonCellScale;
            var levelOffsetY = level * _map.BaseHeight * scale;
            var worldPos = new Vector3(gridX * scale, levelOffsetY, gridZ * scale);

            _map.AddScenery(worldPos, seed, type, -1, -1);
            return this;
        }

        /// <summary>
        /// Finalizes map construction and applies ambient lighting.
        /// </summary>
        /// <returns>The current <see cref="DungeonBuilder"/> instance.</returns>
        public DungeonMap? Build()
        {
            if (_map == null) return null;
            // Ambient floor light plus propagation from every light-emitting prop (shared with the map loader).
            DungeonLighting.Bake(_map);

            return _map;
        }

        /// <summary>
        /// Clears the map.
        /// </summary>
        private void ClearMap()
        {
            if (_map == null) return;

            for (var l = 0; l < _map.Levels; l++)
            for (var x = 0; x < _map.Width; x++)
            for (var z = 0; z < _map.Depth; z++)
            {
                var cell = _map.GetCell(x, z, l);
                if (cell == null) continue;

                cell.HasFloor = false;
                cell.HasCeiling = false;
                cell.WallNorth = cell.WallSouth = cell.WallEast = cell.WallWest = WallType.None;
                cell.HasWallNorth = cell.HasWallSouth = cell.HasWallEast = cell.HasWallWest = false;
            }
        }
    }
}