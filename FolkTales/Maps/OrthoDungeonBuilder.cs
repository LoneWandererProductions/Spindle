/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        OrthoDungeonBuilder.cs
 * PURPOSE:     Fluent API builder for constructing ceilingless dungeon layouts specifically
 *              optimized for orthographic and top-down (Dungeon Keeper style) viewports.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Drawing;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Dedicated fluent API builder for orthographic and top-down tactical dungeons.
    /// Ensures all cells strictly maintain <c>HasCeiling = false</c> so overhead cameras
    /// have an unobstructed view of room interiors.
    /// </summary>
    public sealed class OrthoDungeonBuilder
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
        /// Initializes a new orthographic fluent builder instance.
        /// Defaults to a single level for pure top-down visibility.
        /// </summary>
        /// <param name="width">The width in grid cells.</param>
        /// <param name="depth">The depth in grid cells.</param>
        /// <param name="levels">The height levels (defaults to 1 for orthographic viewports).</param>
        /// <param name="cellScale">The spatial cell scale factor.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public static OrthoDungeonBuilder Create(int width = 20, int depth = 20, int levels = 1,
            float cellScale = MapDimensions.DefaultCellSize)
        {
            return new OrthoDungeonBuilder(width, depth, levels, cellScale);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrthoDungeonBuilder" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="cellScale">The cell scale.</param>
        private OrthoDungeonBuilder(int width, int depth, int levels, float cellScale)
        {
            _map = new DungeonMap(width, depth, levels) { DungeonCellScale = cellScale };
            ClearMap();
        }

        /// <summary>
        /// Sets the player starting spawn grid coordinate.
        /// </summary>
        /// <param name="gridX">The grid X coordinate.</param>
        /// <param name="gridZ">The grid Z coordinate.</param>
        /// <param name="level">The target map level.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder SetSpawn(float gridX, float gridZ, int level = 0)
        {
            var scale = _map.DungeonCellScale;
            var levelOffsetY = level * _map.BaseHeight * scale;
            _spawnPosition = new Vector3(gridX * scale, levelOffsetY + 1.7f, gridZ * scale);
            return this;
        }

        /// <summary>
        /// Adds a room enclosure without overhead roof layers or ceilings.
        /// Keeps interior floors and perimeter walls intact while preserving 100% top-down visibility.
        /// </summary>
        /// <param name="minX">Minimum X grid boundary.</param>
        /// <param name="minZ">Minimum Z grid boundary.</param>
        /// <param name="maxX">Maximum X grid boundary.</param>
        /// <param name="maxZ">Maximum Z grid boundary.</param>
        /// <param name="wallColor">Color of room perimeter walls.</param>
        /// <param name="floorColor">Color of floor tiles.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder AddRoom(int minX, int minZ, int maxX, int maxZ, Color wallColor, Color floorColor)
        {
            for (var l = 0; l < _map.Levels; l++)
            for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
            {
                var cell = _map.GetCell(x, z, l);
                if (cell == null) continue;

                // Level 0 gets full walkable floor tracking; upper levels remain open shafts
                cell.HasFloor = (l == 0);
                cell.HasCeiling = false; // Strictly disabled for orthographic cameras
                cell.FloorHeight = 0.0f;
                cell.FloorColor = floorColor;
                cell.WallColor = wallColor;

                // Establish solid perimeter walls surrounding room boundaries
                cell.HasWallNorth = (z == minZ);
                cell.WallNorth = cell.HasWallNorth ? WallType.Solid : WallType.None;
                cell.HasWallSouth = (z == maxZ);
                cell.WallSouth = cell.HasWallSouth ? WallType.Solid : WallType.None;
                cell.HasWallWest = (x == minX);
                cell.WallWest = cell.HasWallWest ? WallType.Solid : WallType.None;
                cell.HasWallEast = (x == maxX);
                cell.WallEast = cell.HasWallEast ? WallType.Solid : WallType.None;
            }

            return this;
        }

        /// <summary>
        /// Adds an open-air connecting corridor between two grid coordinates.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="wallColor">Color of the wall.</param>
        /// <param name="floorColor">Color of the floor.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder AddCorridor(int minX, int minZ, int maxX, int maxZ, Color wallColor,
            Color floorColor) =>
            AddRoom(minX, minZ, maxX, maxZ, wallColor, floorColor);

        /// <summary>
        /// Places a cell doorway without adding solid upper-level wall caps that obstruct top-down cameras.
        /// </summary>
        /// <param name="x1">Source X coordinate.</param>
        /// <param name="z1">Source Z coordinate.</param>
        /// <param name="x2">Destination X coordinate.</param>
        /// <param name="z2">Destination Z coordinate.</param>
        /// <param name="isOpen">Initial door state.</param>
        /// <param name="style">The style.</param>
        /// <returns>
        /// The current <see cref="OrthoDungeonBuilder" /> instance.
        /// </returns>
        public OrthoDungeonBuilder AddDoor(int x1, int z1, int x2, int z2, bool isOpen = false,
            DoorStyle style = DoorStyle.Wood)
        {
            SetWallBetween(x1, z1, x2, z2, isOpen ? WallType.DoorOpen : WallType.DoorClosed,
                MapDimensions.DoorOpeningRatio);

            foreach (var (x, z) in new[] { (x1, z1), (x2, z2) })
            {
                var cell = _map?.GetCell(x, z, 0);
                if (cell != null) cell.DoorStyle = style;
            }

            return this;
        }

        /// <summary>
        /// Places a doorway flanked by illuminated wall torches so portals are easily distinguishable from orthographic angles.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        /// <param name="style">The style.</param>
        /// <returns>
        /// The current <see cref="OrthoDungeonBuilder" /> instance.
        /// </returns>
        public OrthoDungeonBuilder AddFramedDoor(int x1, int z1, int x2, int z2, bool isOpen = false,
            DoorStyle style = DoorStyle.Wood)
        {
            AddDoor(x1, z1, x2, z2, isOpen, style);

            if (x1 == x2) // North/South Threshold
            {
                AddProp(x1 - 0.8f, z1 + 0.5f, SceneryType.WallTorch, seed: 0);
                AddProp(x1 + 0.8f, z1 + 0.5f, SceneryType.WallTorch, seed: 0);
            }
            else // East/West Threshold
            {
                AddProp(x1 + 0.5f, z1 - 0.8f, SceneryType.WallTorch, seed: 2);
                AddProp(x1 + 0.5f, z1 + 0.8f, SceneryType.WallTorch, seed: 2);
            }

            return this;
        }

        /// <summary>
        /// Opens an unobstructed, walkable passage between adjacent cells.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder AddPassage(int x1, int z1, int x2, int z2)
        {
            SetWallBetween(x1, z1, x2, z2, WallType.None);
            return this;
        }

        /// <summary>
        /// Recesses floor tiles to create a liquid pit (Water or Lava).
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
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder AddSunkenPool(int minX, int minZ, int maxX, int maxZ,
            string liquidType, Color? customColor = null, float depth = -0.18f, float liquidFillRatio = 0.12f,
            int level = 0)
        {
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
                    Name = liquidType,
                    Height = liquidFillRatio,
                    Color = customColor,
                    TextureId = null,
                    AlphaBlend = true
                });
            }

            return this;
        }

        /// <summary>
        /// Places an interactive or decorative 3D scenery object in the dungeon.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="type">The type.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="OrthoDungeonBuilder"/> instance.</returns>
        public OrthoDungeonBuilder AddProp(float gridX, float gridZ, SceneryType type, int seed = 0, int level = 0)
        {
            var scale = _map.DungeonCellScale;
            var levelOffsetY = level * _map.BaseHeight * scale;
            var worldPos = new Vector3(gridX * scale, levelOffsetY, gridZ * scale);

            _map.AddScenery(worldPos, seed, type, -1, -1);
            return this;
        }

        /// <summary>
        /// Finalizes construction, guarantees 100% ceilingless states across all cells,
        /// applies low ambient lighting, and propagates light emission from placed props.
        /// </summary>
        /// <returns>
        /// A fully built, top-down optimized <see cref="DungeonMap" /> instance.
        /// </returns>
        public DungeonMap? Build()
        {
            // 1. Set dark ambient fill (level 3) and explicitly strip all ceiling caps across all levels
            for (var l = 0; l < _map.Levels; l++)
            for (var x = 0; x < _map.Width; x++)
            for (var z = 0; z < _map.Depth; z++)
            {
                var cell = _map.GetCell(x, z, l);
                if (cell == null) continue;

                cell.HasCeiling = false; // Guaranteed ceilingless sweep for top-down orthographic views

                if (cell.HasFloor)
                {
                    _map.SetLightAt(x, z, l, 3); // Low ambient floor brightness
                }
            }

            // 2. Seed and propagate dynamic light from placed light-emitting scenery props
            DungeonLighting.PropagateFromProps(_map);

            return _map;
        }

        /// <summary>
        /// Updates boundary wall states across levels between two adjacent grid coordinates.
        /// </summary>
        private void SetWallBetween(int x1, int z1, int x2, int z2, WallType wallState, float openingRatio = 1.0f)
        {
            for (var l = 0; l < _map.Levels; l++)
            {
                var cell1 = _map.GetCell(x1, z1, l);
                var cell2 = _map.GetCell(x2, z2, l);

                if (cell1 != null) cell1.OpeningHeightRatio = openingRatio;
                if (cell2 != null) cell2.OpeningHeightRatio = openingRatio;

                if (x2 > x1)
                {
                    if (cell1 != null) cell1.WallEast = wallState;
                    if (cell2 != null) cell2.WallWest = wallState;
                }
                else if (x2 < x1)
                {
                    if (cell1 != null) cell1.WallWest = wallState;
                    if (cell2 != null) cell2.WallEast = wallState;
                }
                else if (z2 > z1)
                {
                    if (cell1 != null) cell1.WallSouth = wallState;
                    if (cell2 != null) cell2.WallNorth = wallState;
                }
                else if (z2 < z1)
                {
                    if (cell1 != null) cell1.WallNorth = wallState;
                    if (cell2 != null) cell2.WallSouth = wallState;
                }
            }
        }

        /// <summary>
        /// Resets all cell states and clears structural tracking.
        /// </summary>
        private void ClearMap()
        {
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