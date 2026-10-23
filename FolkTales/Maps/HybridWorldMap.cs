/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        HybridWorldMap.cs
 * PURPOSE:     Central master container governing upward-stacking structural layers.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Structs;
using MemoryManager;
using MemoryManager.Core;
using System.Numerics;

namespace FolkTales.Maps
{
    public class HybridWorldMap
    {
        /// <summary>
        /// Dedicated scratch arena backing fast, zero-GC unmanaged lighting queue operations.
        /// </summary>
        private readonly MemoryArena? _lightingArena;

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public int Width { get; }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        /// <value>
        /// The depth.
        /// </value>
        public int Depth { get; }

        /// <summary>
        /// Gets the levels.
        /// </summary>
        /// <value>
        /// The levels.
        /// </value>
        public int Levels { get; }

        /// <summary>
        /// Gets the shared scale.
        /// </summary>
        /// <value>
        /// The shared scale.
        /// </value>
        public float SharedScale { get; }

        /// <summary>
        /// Gets the surface terrain.
        /// </summary>
        /// <value>
        /// The surface terrain.
        /// </value>
        public TerrainMap? SurfaceTerrain { get; }

        /// <summary>
        /// Gets the structures.
        /// </summary>
        /// <value>
        /// The structures.
        /// </value>
        public DungeonMap Structures { get; }

        /// <summary>
        /// The texture set shared by <see cref="Structures" /> and both terrains. To swap it, use
        /// <see cref="SetTextures" /> so all three parts point at the new instance.
        /// </summary>
        public TextureSet Textures { get; private set; } = new();

        /// <summary>
        /// Gets the start settings of this map (fog, discovery, sight, time, weather, spawn). Shared with the structures and
        /// both terrains.
        /// </summary>
        public MapSettings Settings { get; }

        /// <summary>
        /// Replaces the texture set of the whole map (structures and both terrains).
        /// </summary>
        /// <param name="textures">The new set.</param>
        public void SetTextures(TextureSet textures)
        {
            Textures = textures;
            Structures.Textures = textures;
            if (SurfaceTerrain != null) SurfaceTerrain.Textures = textures;
            if (SubterraneanTerrain != null) SubterraneanTerrain.Textures = textures;
        }

        /// <summary>
        /// Gets the subterranean terrain.
        /// </summary>
        /// <value>
        /// The subterranean terrain.
        /// </value>
        public TerrainMap? SubterraneanTerrain { get; }

        /// <summary>
        /// The scenery
        /// </summary>
        private readonly List<SceneryPlacement> _scenery = new();

        /// <summary>
        /// The scenery grid
        /// </summary>
        private readonly Dictionary<(int, int), List<SceneryPlacement>> _sceneryGrid = new();

        /// <summary>
        /// Gets the scenery.
        /// </summary>
        /// <value>
        /// The scenery.
        /// </value>
        public IEnumerable<SceneryPlacement> Scenery => _scenery;

        /// <summary>
        /// Gives the props that have no texture of their own the current texture of their type slot, see
        /// <see cref="SceneryRetexture"/>. Call it after replacing <see cref="Textures"/> on a loaded map.
        /// </summary>
        /// <returns>The number of props that changed.</returns>
        public int RetextureScenery()
        {
            var changed = SceneryRetexture.Apply(_scenery, _sceneryGrid, Textures);
            if (changed > 0) Structures.NotifyStructureChanged();
            return changed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HybridWorldMap" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="fogMode">The fog mode.</param>
        /// <param name="sightRadius">The sight radius.</param>
        /// <param name="startExplored">if set to <c>true</c> [start explored].</param>
        /// <param name="surfaceLevel">The surface level.</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <param name="weather">The weather.</param>
        public HybridWorldMap(int width, int depth, int levels, float scale = MapDimensions.DefaultCellSize,
            FogMode? fogMode = null, int? sightRadius = null, bool startExplored = false,
            int? surfaceLevel = null, float? timeOfDay = null, WeatherType? weather = null)
            : this(width, depth, levels, scale,
                MapSettings.From(fogMode, sightRadius, startExplored, timeOfDay, weather), surfaceLevel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HybridWorldMap"/> class from a settings object (also used when a map is loaded).
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="settings">The start settings; structures and both terrains share this instance.</param>
        /// <param name="surfaceLevel">Level of the ground surface (terrain + open air), see <see cref="SurfaceLevel"/>. <c>null</c> = 1.</param>
        public HybridWorldMap(int width, int depth, int levels, float scale, MapSettings settings,
            int? surfaceLevel = null)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Width = width;
            Depth = depth;
            Levels = levels;
            SharedScale = scale;

            // Initialize dedicated 8 MB scratchpad (100% lock-free FastLane bump allocator)
            _lightingArena = new MemoryArena(MemoryManagerConfig.CreateForFrameScratch(8 * 1024 * 1024));

            // From level 1 up an empty structure cell is open air: daylight (scaled by time and weather) reaches it.
            Structures = new DungeonMap(width, depth, levels, settings, 1)
            {
                DungeonCellScale = scale
            };
            if (surfaceLevel.HasValue) SurfaceLevel = surfaceLevel.Value;

            // Clean slate initialization
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    for (var lvl = 0; lvl < levels; lvl++)
                    {
                        var cell = Structures.GetCell(x, z, lvl);
                        if (cell != null)
                        {
                            cell.HasFloor = false;
                            cell.HasCeiling = false;
                            cell.HasWallNorth = cell.HasWallSouth = cell.HasWallEast = cell.HasWallWest = false;
                        }
                    }
                }
            }

            SurfaceTerrain = new TerrainMap(width, depth, scale, settings);
            SubterraneanTerrain = new TerrainMap(width, depth, scale, settings);

            // One texture set for the whole hybrid map: structures, surface and underground share the instance.
            Textures = Structures.Textures;
            SurfaceTerrain.Textures = Textures;
            SubterraneanTerrain.Textures = Textures;

            // Automatically compute initial skylight & roof shadows on map creation
            InitializeLightGrid();
        }

        /// <summary>
        /// Gets the baked light intensity value at explicit grid positions.
        /// Safely catches mountain elevations and defaults to open-air skylight.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>Light value at the specified grid position.</returns>
        public byte GetLightAt(int x, int z, int level) => Structures.GetLightAt(x, z, level);

        /// <summary>
        /// The light a cell has right now: block light or the sky light scaled by the daylight, see
        /// <see cref="DungeonMap.GetEffectiveLightAt"/>.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>Light 0 to 15.</returns>
        public byte GetEffectiveLightAt(int x, int z, int level) => Structures.GetEffectiveLightAt(x, z, level);

        /// <summary>
        /// Injects a light value intensity byte directly into the target grid slot if within bounds.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <param name="light">The light.</param>
        public void SetLightAt(int x, int z, int level, byte light) => Structures.SetLightAt(x, z, level, light);

        /// <summary>
        /// Initializes the light grid and triggers automatic skylight propagation.
        /// </summary>
        public void InitializeLightGrid(IMemoryAllocator? externalArena = null)
        {
            Structures.Reset();

            var arenaToUse = externalArena ?? _lightingArena;

            // The block light is a flat baseline (torches add to it). Daylight is NOT baked in any more: it lives in the
            // sky light of the structures, scaled every frame by the time of day and the weather, so a house is not lit
            // at night just because its door faces the open air.
            LightPropagationSolver.RecalculateSunlight(arenaToUse, this, daylightLumen: 3);

            // Reset bump memory pointers back to 0 for instant reuse
            _lightingArena?.TickFrame();
        }

        /// <summary>
        /// Gets the height of the floor.
        /// </summary>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldY">The world y.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns>Height of the floor.</returns>
        public float GetFloorHeight(float worldX, float worldY, float worldZ)
        {
            if (SurfaceTerrain == null) return 0f;

            var gridX = WorldToGrid(worldX);
            var gridZ = WorldToGrid(worldZ);

            if (!InBounds(gridX, gridZ)) return 0f;

            var terrainY = SurfaceTerrain.GetHeightAt(worldX, worldZ);
            if (TryGetSupport(gridX, gridZ, terrainY, worldY, out _, out var floorY)) return floorY;

            // MIDDLE CRUST FALLBACK: If no building block cell overrides the space, follow the terrain mesh!
            return terrainY;
        }

        /// <summary>
        /// The structure level whose floor carries a body with the given feet height at this column. Unlike
        /// <see cref="WorldToLevel"/> it also finds a sunken floor below its level boundary (pool) and the floor below a
        /// stairwell, so fog, visibility, walls and doors use the level the player really stands on.
        /// </summary>
        /// <param name="gridX">Grid column.</param>
        /// <param name="gridZ">Grid row.</param>
        /// <param name="feetY">World height of the feet.</param>
        /// <returns>The supporting level; the level of the height when the terrain carries the body.</returns>
        public int GetSupportLevel(int gridX, int gridZ, float feetY)
        {
            if (SurfaceTerrain == null) return Math.Clamp(WorldToLevel(feetY), 0, Levels - 1);

            if (InBounds(gridX, gridZ))
            {
                var terrainY = SurfaceTerrain.GetHeightAt((gridX + 0.5f) * SharedScale, (gridZ + 0.5f) * SharedScale);
                if (TryGetSupport(gridX, gridZ, terrainY, feetY, out var level, out _)) return level;

                // The terrain itself carries the body (no floor does): that is the surface level, however deep the
                // ground lies below the level line. A pond bed is sunk below it, and WorldToLevel would call the
                // bathing player "underground" (level below the surface) and switch the whole world to the cellar view.
                // Only a body really below the ground (inside a cave under it) takes the level of its height.
                // Judged against the LOWEST ground of the cell, not its middle: on the slope of a pond the middle lies more
                // than the probe reach above the deepest spot of the same cell.
                if (feetY >= LowestTerrainInCell(gridX, gridZ, terrainY) - MapDimensions.FloorProbeReach)
                    return Math.Clamp(Math.Max(_surfaceLevel, WorldToLevel(feetY)), 0, Levels - 1);
            }

            return Math.Clamp(WorldToLevel(feetY), 0, Levels - 1);
        }

        /// <summary>
        /// Lowest terrain height at the middle and the four corners of a cell.
        /// </summary>
        private float LowestTerrainInCell(int gridX, int gridZ, float centreY)
        {
            var low = centreY;
            const float inset = 0.02f;
            for (var i = 0; i < 4; i++)
            {
                var fx = gridX + ((i & 1) == 0 ? inset : 1f - inset);
                var fz = gridZ + ((i & 2) == 0 ? inset : 1f - inset);
                low = MathF.Min(low, SurfaceTerrain!.GetHeightAt(fx * SharedScale, fz * SharedScale));
            }

            return low;
        }

        /// <summary>
        /// Tries the get support.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="terrainY">The terrain y.</param>
        /// <param name="feetY">The feet y.</param>
        /// <param name="level">The level.</param>
        /// <param name="floorY">The floor y.</param>
        /// <returns><c>true</c> if support is found; otherwise, <c>false</c>.</returns>
        private bool TryGetSupport(int gridX, int gridZ, float terrainY, float feetY, out int level, out float floorY)
        {
            if (SurfaceTerrain == null)
            {
                level = 0;
                floorY = 0f;
                return false;
            }

            var reach = MapDimensions.FloorProbeReach;

            // A floor buried under the terrain only counts when the way down is open: the terrain tile is punched (stairwell,
            // pit) or the body is already below the ground. Otherwise walking over a cellar would drop you into it.
            var wayDownOpen = SurfaceTerrain.IsTileOccluded(gridX, gridZ) || feetY < terrainY - reach;

            // Searched from the top level down, not from a level guessed from the height (sunken floors, stair tops, holes).
            for (var l = Levels - 1; l >= 0; l--)
            {
                var cell = Structures.GetCell(gridX, gridZ, l);
                if (cell is not { IsEmpty: false, HasFloor: true }) continue;

                var y = (cell.FloorHeight + cell.LevelBaseHeight) * SharedScale;
                if (y > feetY + reach) continue;
                if (y < terrainY - reach && !wayDownOpen) continue;

                level = l;
                floorY = y;
                return true;
            }

            level = 0;
            floorY = 0f;
            return false;
        }

        /// <summary>
        /// Finds the open-air water above a world position.
        /// </summary>
        /// <param name="worldX">World x.</param>
        /// <param name="worldY">World y.</param>
        /// <param name="worldZ">World z.</param>
        /// <param name="surfaceY">The height of the water surface.</param>
        /// <returns><c>true</c> if water is drawn above that position.</returns>
        public bool TryGetWaterSurface(float worldX, float worldY, float worldZ, out float surfaceY)
        {
            surfaceY = 0f;

            if (SurfaceTerrain == null || IsIndoors(worldX, worldY, worldZ)) return false;

            return SurfaceTerrain.TryGetWaterSurface(worldX, worldZ, out surfaceY);
        }

        /// <summary>
        /// Determines whether the specified world position is indoors.
        /// </summary>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldY">The world y.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns><c>true</c> if indoors; otherwise, <c>false</c>.</returns>
        public bool IsIndoors(float worldX, float worldY, float worldZ)
        {
            var gridX = WorldToGrid(worldX);
            var gridZ = WorldToGrid(worldZ);
            var gridY = WorldToLevel(worldY);

            var cell = Structures.GetCell(gridX, gridZ, Math.Clamp(gridY, 0, Levels - 1));
            return cell is { IsEmpty: false };
        }

        /// <summary>
        /// Determines whether the specified world position is walkable.
        /// </summary>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldY">The world y.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns><c>true</c> if walkable; otherwise, <c>false</c>.</returns>
        public bool IsWalkable(float worldX, float worldY, float worldZ)
        {
            var gridX = WorldToGrid(worldX);
            var gridZ = WorldToGrid(worldZ);
            var gridY = WorldToLevel(worldY);

            if (!InBounds(gridX, gridZ)) return false;

            var cell = Structures.GetCell(gridX, gridZ, Math.Clamp(gridY, 0, Levels - 1));

            if (cell is { IsEmpty: false })
            {
                return cell.HasFloor;
            }

            return IsSlopeWalkable(SurfaceTerrain, worldX, worldZ);
        }

        /// <summary>
        /// Determines whether a slope on terrain is walkable.
        /// </summary>
        /// <param name="terrain">The terrain.</param>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns>
        ///   <c>true</c> if [is slope walkable] [the specified terrain]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsSlopeWalkable(TerrainMap? terrain, float worldX, float worldZ)
        {
            var currentHeight = terrain.GetHeightAt(worldX, worldZ);
            var forwardHeight = terrain.GetHeightAt(worldX + 0.5f, worldZ + 0.5f);
            return Math.Abs(forwardHeight - currentHeight) <= MapDimensions.MaxWalkableRise;
        }

        /// <summary>
        /// Level of the ground surface (terrain + open air). 1 = one dungeon level below (the default); 2 = two dungeon levels
        /// fit under the terrain, and so on. Set it before building: it moves the open-air boundary of the structure layer.
        /// </summary>
        /// <value>
        /// The surface level.
        /// </value>
        public int SurfaceLevel
        {
            get => _surfaceLevel;
            set
            {
                _surfaceLevel = Math.Clamp(value, 1, Math.Max(1, Levels - 1));
                Structures.OpenAirFromLevel = _surfaceLevel;
            }
        }

        private int _surfaceLevel = 1;

        /// <summary>
        /// Height of the surface baseline in metres (SurfaceLevel storeys).
        /// </summary>
        public float SurfaceBaselineY => _surfaceLevel * MapDimensions.LevelHeight(SharedScale);

        /// <summary>
        /// Height below which a prop counts as underground. NOT the baseline itself: the beds of ponds are sunk below it, and
        /// their lilies, reeds and stones are surface props. The renderer and <see cref="MapAudit" /> both use this.
        /// </summary>
        public float SubterraneanLimitY => SurfaceBaselineY - (0.5f * MapDimensions.LevelHeight(SharedScale));

        /// <summary>
        /// Adds scenery to the map.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="type">The type.</param>
        /// <param name="primaryTexId">The primary tex identifier.</param>
        /// <param name="secondaryTexId">The secondary tex identifier.</param>
        /// <param name="fitScale">Cell size for cell-fitted types.</param>
        public void AddScenery(Vector3 position, int seed, SceneryType type, int primaryTexId = -1,
            int secondaryTexId = -1, float fitScale = 0f)
        {
            if (fitScale <= 0f) fitScale = type.IsCellFitted() ? SharedScale : 1f;

            // No explicit texture: use this map's slot for the prop type (still -1 = factory default if unset).
            if (primaryTexId <= 0) primaryTexId = Textures.ForScenery(type);

            // Wall-mounted props are moved to their wall (same rule as DungeonMap).
            var rotationSeed = WallMountRules.Apply(ref position, seed, type, SharedScale);
            var placement =
                new SceneryPlacement(position, rotationSeed, type, primaryTexId, secondaryTexId, -1, fitScale);
            _scenery.Add(placement);

            var cx = WorldToGrid(position.X);
            var cz = WorldToGrid(position.Z);
            if (!_sceneryGrid.ContainsKey((cx, cz))) _sceneryGrid[(cx, cz)] = new();
            _sceneryGrid[(cx, cz)].Add(placement);
            Structures.NotifyStructureChanged();
        }

        /// <summary>
        /// Restores an existing scenery placement directly into the map collections
        /// without recalculating wall offsets or modifying seeds.
        /// </summary>
        /// <param name="placement">The scenery placement to restore.</param>
        public void RestoreScenery(SceneryPlacement placement)
        {
            _scenery.Add(placement);

            var cx = WorldToGrid(placement.Position.X);
            var cz = WorldToGrid(placement.Position.Z);
            if (!_sceneryGrid.TryGetValue((cx, cz), out var list))
            {
                list = new List<SceneryPlacement>();
                _sceneryGrid[(cx, cz)] = list;
            }

            list.Add(placement);
            Structures.NotifyStructureChanged();
        }

        /// <summary>
        /// Builds a straight staircase between two structure levels.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom cell.</param>
        /// <param name="gridZ">Grid Z of the bottom cell.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <param name="direction">The compass direction of the climb.</param>
        /// <returns><c>false</c> if the flight does not fit.</returns>
        public bool AddStaircase(int gridX, int gridZ, int level, Direction direction)
        {
            var carved = StairBuilder.Carve(Structures, gridX, gridZ, level, direction,
                (pos, seed) => AddScenery(pos, seed, SceneryType.StairFlight, -1, -1, SharedScale));
            Structures.NotifyStructureChanged();
            return carved;
        }

        /// <summary>
        /// Builds a 2x2 spiral staircase climbing from <paramref name="level"/> to <c>level + 1</c>.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom starting cell.</param>
        /// <param name="gridZ">Grid Z of the bottom starting cell.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <param name="withWalls">If true, encloses the 2x2 footprint in a walled shaft.</param>
        /// <param name="addCenterPillar">If true, places an AncientPillar as the central spindle.</param>
        /// <returns><c>false</c> if the spiral does not fit.</returns>
        public bool AddSpiralStaircase(int gridX, int gridZ, int level = 0, bool withWalls = true,
            bool addCenterPillar = true)
        {
            var carved = StairBuilder.CarveSpiral(Structures, gridX, gridZ, level,
                (pos, seed, type, fitScale) => AddScenery(pos, seed, type, -1, -1, fitScale),
                withWalls, addCenterPillar);
            Structures.NotifyStructureChanged();
            return carved;
        }

        /// <summary>
        /// Gets the scenery at cell.
        /// </summary>
        /// <param name="cx">The cx.</param>
        /// <param name="cz">The cz.</param>
        /// <returns>A list of Scenery props at the target cell.</returns>
        public IReadOnlyList<SceneryPlacement>? GetSceneryAtCell(int cx, int cz) =>
            _sceneryGrid.TryGetValue((cx, cz), out var list) ? list : null;

        /// <summary>
        /// Yields scenery props strictly within a circular radius of the focal position.
        /// </summary>
        /// <param name="centerPos">The center position.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>A list of Scenery props within the specified radius.</returns>
        public IEnumerable<SceneryPlacement> GetSceneryInRadius(Vector3 centerPos, float radius)
        {
            var minX = Math.Max(0, WorldToGrid(centerPos.X - radius));
            var maxX = Math.Min(Width - 1, WorldToGrid(centerPos.X + radius));
            var minZ = Math.Max(0, WorldToGrid(centerPos.Z - radius));
            var maxZ = Math.Min(Depth - 1, WorldToGrid(centerPos.Z + radius));

            var radiusSq = radius * radius;

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    if (!_sceneryGrid.TryGetValue((x, z), out var list)) continue;

                    for (var i = 0; i < list.Count; i++)
                    {
                        var prop = list[i];
                        if (Vector3.DistanceSquared(prop.Position, centerPos) <= radiusSq)
                        {
                            yield return prop;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts world X/Z coordinate to grid index.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <returns>The corresponding grid index.</returns>
        public int WorldToGrid(float worldPos) => (int)MathF.Floor(worldPos / SharedScale);

        /// <summary>
        /// Converts world Y height to level index.
        /// </summary>
        /// <param name="worldY">The world y.</param>
        /// <returns>The corresponding level index.</returns>
        public int WorldToLevel(float worldY) =>
            MapConstants.LevelFromHeight(worldY, MapDimensions.LevelHeight(SharedScale));

        /// <summary>
        /// Checks if grid coordinates are in bounds.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <returns><c>true</c> if the grid coordinates are within bounds; otherwise, <c>false</c>.</returns>
        public bool InBounds(int gridX, int gridZ) => gridX >= 0 && gridX < Width && gridZ >= 0 && gridZ < Depth;
    }
}