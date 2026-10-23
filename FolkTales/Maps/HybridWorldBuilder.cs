/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        HybridWorldBuilder.cs
 * PURPOSE:     Fluent API builder for constructing multi-layer hybrid worlds with surface 
 *              towns, windows, underground sewers, doors, staircases, and scenery props.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;
using System.Numerics;
using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Fluent API for building multi-layer hybrid worlds with surface towns,
    /// window-adorned buildings, underground sewers, staircases, and interactive props.
    /// </summary>
    public sealed class HybridWorldBuilder
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly HybridWorldMap _map;

        /// <summary>
        /// The spawn position
        /// </summary>
        private Vector3 _spawnPosition = new(10f, 10f, 10f);

        /// <summary>
        /// Gets the calculated player spawn position vector.
        /// </summary>
        public Vector3 SpawnPosition => _spawnPosition;

        /// <summary>
        /// Creates the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="sharedScale">The shared scale.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public static HybridWorldBuilder Create(int width = 40, int depth = 40, int levels = 2,
            float sharedScale = MapDimensions.DefaultCellSize)
        {
            return new HybridWorldBuilder(width, depth, levels, sharedScale);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HybridWorldBuilder"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="sharedScale">The shared scale.</param>
        private HybridWorldBuilder(int width, int depth, int levels, float sharedScale)
        {
            _map = new HybridWorldMap(width, depth, levels, sharedScale);
            InitializeBaseTerrain(10.0f, 0.0f);
        }

        /// <summary>
        /// Sets the spawn.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="heightY">The height y.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder SetSpawn(float gridX, float gridZ, float heightY = 10.0f)
        {
            var scale = _map.SharedScale;
            _spawnPosition = new Vector3(gridX * scale, heightY, gridZ * scale);
            return this;
        }

        /// <summary>
        /// Flattens the terrain.
        /// </summary>
        /// <param name="surfaceHeight">Height of the surface.</param>
        /// <param name="subterraneanHeight">Height of the subterranean.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder FlattenTerrain(float surfaceHeight = 10.0f, float subterraneanHeight = 0.0f)
        {
            InitializeBaseTerrain(surfaceHeight, subterraneanHeight);
            return this;
        }

        /// <summary>
        /// Creates a level-aware brush (rooms, streets, doors, pools, stairs, props on any level) on the structure layer. Floors
        /// on the surface level punch the terrain automatically. Call <see cref="WithSurfaceLevel"/> first.
        /// </summary>
        /// <param name="offsetX">Grid offset of the brush's local (0, 0).</param>
        /// <param name="offsetZ">Grid offset of the brush's local (0, 0).</param>
        public StructureBrush CreateBrush(int offsetX = 0, int offsetZ = 0)
        {
            var map = _map;
            return new StructureBrush(map.Structures, offsetX, offsetZ, map.SurfaceLevel,
                (pos, seed, type) => map.AddScenery(pos, seed, type, -1, -1),
                (x, z, level, dir) => map.AddStaircase(x, z, level, dir),
                (x, z) => map.SurfaceTerrain?.PunchHole(x, z));
        }

        /// <summary>
        /// Moves the surface up so that <paramref name="surfaceLevel"/> dungeon levels fit below the terrain (default builder:
        /// 1). The terrain is flattened to the new baseline; call it before adding terrain features.
        /// </summary>
        /// <param name="surfaceLevel">Level of the ground surface (2 = two dungeon levels below).</param>
        /// <param name="subterraneanHeight">Height of the underground terrain.</param>
        public HybridWorldBuilder WithSurfaceLevel(int surfaceLevel, float subterraneanHeight = 0.0f)
        {
            _map.SurfaceLevel = surfaceLevel;
            InitializeBaseTerrain(_map.SurfaceBaselineY, subterraneanHeight);
            return this;
        }

        /// <summary>
        /// Carves a pond into the surface terrain, registers its water body and floats water lilies on it. The plants go through
        /// the map's own scenery list because the terrain's scenery is not drawn by the hybrid renderer.
        /// </summary>
        /// <param name="minX">First tile column.</param>
        /// <param name="minZ">First tile row.</param>
        /// <param name="maxX">Last tile column.</param>
        /// <param name="maxZ">Last tile row.</param>
        /// <param name="depth">Depth of the basin below the surrounding ground in metres.</param>
        /// <param name="lilies">Number of lilies (-1 = by size).</param>
        public HybridWorldBuilder AddPond(int minX, int minZ, int maxX, int maxZ, float depth = 1.8f, int lilies = -1)
        {
            var terrain = _map.SurfaceTerrain!;
            var scale = _map.SharedScale;
            var baseY = terrain.GetHeightAt(((minX + maxX + 1) * 0.5f) * scale, ((minZ + maxZ + 1) * 0.5f) * scale);
            var waterLevel = baseY - 0.45f;

            for (var x = minX - 1; x <= maxX + 2; x++)
            for (var z = minZ - 1; z <= maxZ + 2; z++)
            {
                if (!terrain.InBounds(x, z)) continue;

                var inner = x > minX && x <= maxX && z > minZ && z <= maxZ;
                var onRect = x >= minX && x <= maxX + 1 && z >= minZ && z <= maxZ + 1;
                var target = inner ? baseY - depth : onRect ? baseY - 0.25f : baseY - 0.15f;
                terrain[x, z] = MathF.Min(terrain[x, z], target);
            }

            terrain.AddWaterBody(new TerrainWaterBody
            {
                Name = "Pond",
                WaterLevel = waterLevel,
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ,
                Color = Color.FromArgb(160, 30, 110, 220)
            });

            var rnd = new Random((minX * 31) + minZ);
            var count = lilies >= 0 ? lilies : Math.Max(3, (maxX - minX + 1) * (maxZ - minZ + 1) / 2);
            for (var i = 0; i < count; i++)
            {
                var wx = (minX + 0.8f + (rnd.NextSingle() * (maxX - minX + 0.4f))) * scale;
                var wz = (minZ + 0.8f + (rnd.NextSingle() * (maxZ - minZ + 0.4f))) * scale;
                var type = i % 3 == 0 ? SceneryType.Lotus : SceneryType.WaterLily;
                _map.AddScenery(new Vector3(wx, waterLevel, wz), rnd.Next(1, 99999), type, -1);
            }

            return this;
        }

        /// <summary>
        /// Adds the house block.
        /// </summary>
        /// <param name="startX">The start x.</param>
        /// <param name="startZ">The start z.</param>
        /// <param name="blockWidth">Width of the block.</param>
        /// <param name="blockDepth">The block depth.</param>
        /// <param name="wallColor">Color of the wall.</param>
        /// <param name="floorColor">Color of the floor.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddHouseBlock(int startX, int startZ, int blockWidth, int blockDepth,
            Color? wallColor = null, Color? floorColor = null, int level = 1)
        {
            if (_map == null) return this;

            var brickColor = wallColor ?? Color.FromArgb(145, 125, 115);
            var woodColor = floorColor ?? Color.FromArgb(75, 60, 50);

            for (var x = startX; x < startX + blockWidth; x++)
            {
                for (var z = startZ; z < startZ + blockDepth; z++)
                {
                    if (!_map.InBounds(x, z)) continue;

                    _map.SurfaceTerrain?.PunchHole(x, z);

                    var cell = _map.Structures.GetCell(x, z, level);
                    if (cell == null) continue;

                    cell.HasFloor = true;
                    cell.HasCeiling = true;
                    cell.FloorHeight = 0.0f;
                    cell.CeilingHeight = MapDimensions.LevelHeightCells;
                    cell.FloorColor = woodColor;
                    cell.WallColor = brickColor;
                    cell.HasFlatCeiling = true;

                    // Volumetric outer walls
                    cell.WallWest = (x == startX) ? WallType.Solid : WallType.None;
                    cell.WallEast = (x == startX + blockWidth - 1) ? WallType.Solid : WallType.None;
                    cell.WallNorth = (z == startZ) ? WallType.Solid : WallType.None;
                    cell.WallSouth = (z == startZ + blockDepth - 1) ? WallType.Solid : WallType.None;

                    // Windows
                    var relX = x - startX;
                    var relZ = z - startZ;

                    if (cell.WallWest == WallType.Solid && relZ % 2 == 1 && relZ != blockDepth - 1)
                        cell.WallWest = WallType.Window;

                    if (cell.WallEast == WallType.Solid && relZ % 2 == 1 && relZ != blockDepth - 1)
                        cell.WallEast = WallType.Window;

                    if (cell.WallNorth == WallType.Solid && relX % 2 == 1)
                        cell.WallNorth = WallType.Window;

                    if (cell.WallSouth == WallType.Solid && relX % 2 == 1 && relX != blockWidth / 2)
                        cell.WallSouth = WallType.Window;

                    // Front door
                    if (z == startZ + blockDepth - 1 && x == startX + (blockWidth / 2))
                    {
                        cell.WallSouth = WallType.DoorClosed;
                    }
                }
            }

            // Drop continuous roof
            var centerX = startX + (blockWidth * 0.5f);
            var centerZ = startZ + (blockDepth * 0.5f);
            var roofWorldPos = new Vector3(centerX, MapDimensions.LevelHeightCells, centerZ) * _map.SharedScale;
            _map.AddScenery(roofWorldPos, startX * 7 + startZ, SceneryType.LargeGableRoof, 4, -1);

            return this;
        }

        /// <summary>
        /// Adds the sewer corridor.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="wallColor">Color of the wall.</param>
        /// <param name="floorColor">Color of the floor.</param>
        /// <param name="level">The level.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddSewerCorridor(int minX, int minZ, int maxX, int maxZ,
            Color? wallColor = null, Color? floorColor = null, int level = 0)
        {
            var brick = wallColor ?? Color.FromArgb(50, 55, 52);
            var floor = floorColor ?? Color.FromArgb(30, 35, 32);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    if (!_map.InBounds(x, z)) continue;

                    var cell = _map.Structures.GetCell(x, z, level);
                    if (cell == null) continue;

                    cell.HasFloor = true;
                    cell.HasCeiling = true;
                    cell.FloorHeight = 0.0f;
                    cell.CeilingHeight = MapDimensions.LevelHeightCells;
                    cell.FloorColor = floor;
                    cell.WallColor = brick;

                    cell.WallNorth = (z == minZ) ? WallType.Solid : WallType.None;
                    cell.WallSouth = (z == maxZ) ? WallType.Solid : WallType.None;
                    cell.WallWest = (x == minX) ? WallType.Solid : WallType.None;
                    cell.WallEast = (x == maxX) ? WallType.Solid : WallType.None;

                    cell.Layers ??= new List<CellLayer>();
                    cell.Layers.Add(new CellLayer
                        { Name = "Water", Height = 0.15f, TextureId = _map.Textures.Get(TextureSet.Slot.Water, 2) });
                }
            }

            return this;
        }

        /// <summary>
        /// Builds a straight level-high staircase climbing from <paramref name="level"/> to <c>level + 1</c>.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom cell.</param>
        /// <param name="gridZ">Grid Z of the bottom cell.</param>
        /// <param name="direction">The compass direction of the climb.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddStaircase(int gridX, int gridZ, Direction direction, int level = 0)
        {
            _map.AddStaircase(gridX, gridZ, level, direction);
            return this;
        }

        /// <summary>
        /// Builds a 2x2 spiral staircase climbing from <paramref name="level"/> to <c>level + 1</c>.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom starting entry cell.</param>
        /// <param name="gridZ">Grid Z of the bottom starting entry cell.</param>
        /// <param name="withWalls">If true, encloses the staircase in a walled shaft.</param>
        /// <param name="addCenterPillar">If true, adds a central pillar spindle.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddSpiralStaircase(int gridX, int gridZ, int level = 0, bool withWalls = true,
            bool addCenterPillar = true)
        {
            _map.AddSpiralStaircase(gridX, gridZ, level, withWalls, addCenterPillar);
            return this;
        }

        /// <summary>
        /// Adds the door.
        /// </summary>
        /// <param name="x1">The x1.</param>
        /// <param name="z1">The z1.</param>
        /// <param name="x2">The x2.</param>
        /// <param name="z2">The z2.</param>
        /// <param name="level">The level.</param>
        /// <param name="isOpen">if set to <c>true</c> [is open].</param>
        /// <param name="style">How the door looks; stored on both cells of the doorway.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddDoor(int x1, int z1, int x2, int z2, int level = 1, bool isOpen = false,
            DoorStyle style = DoorStyle.Wood)
        {
            var wallState = isOpen ? WallType.DoorOpen : WallType.DoorClosed;

            var cell1 = _map.Structures.GetCell(x1, z1, level);
            var cell2 = _map.Structures.GetCell(x2, z2, level);

            if (cell1 != null) cell1.DoorStyle = style;
            if (cell2 != null) cell2.DoorStyle = style;

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

            return this;
        }

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="heightY">The height y.</param>
        /// <param name="type">The type.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>The current <see cref="HybridWorldBuilder"/> instance.</returns>
        public HybridWorldBuilder AddProp(float gridX, float gridZ, float heightY, SceneryType type, int seed = 0)
        {
            var scale = _map.SharedScale;
            var worldPos = new Vector3(gridX * scale, heightY, gridZ * scale);
            _map.AddScenery(worldPos, seed, type, -1, -1);
            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns>Initialize the Map mostly light.</returns>
        public HybridWorldMap Build()
        {
            _map.InitializeLightGrid();
            return _map;
        }

        /// <summary>
        /// Initializes the base terrain.
        /// </summary>
        /// <param name="surfaceHeight">Height of the surface.</param>
        /// <param name="subterraneanHeight">Height of the subterranean.</param>
        private void InitializeBaseTerrain(float surfaceHeight, float subterraneanHeight)
        {
            if (_map == null) return;

            for (var x = 0; x < _map.Width; x++)
            {
                for (var z = 0; z < _map.Depth; z++)
                {
                    if (_map.SurfaceTerrain != null) _map.SurfaceTerrain[x, z] = surfaceHeight;
                    if (_map.SubterraneanTerrain != null) _map.SubterraneanTerrain[x, z] = subterraneanHeight;
                }
            }
        }
    }
}