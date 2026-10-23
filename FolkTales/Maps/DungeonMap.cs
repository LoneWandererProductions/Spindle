/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        DungeonMap.cs
 * PURPOSE:     Describes a simple grid-based dungeon map with multiple levels. Each cell can hold information about its type (floor, wall, door, etc.) and properties.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Structs;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Simple grid-based dungeon map canvas layer tracking.
    /// </summary>
    public class DungeonMap
    {
        /// <summary>
        /// The map in the form of cells [Width, Height, Levels]
        /// </summary>
        private readonly MapCell3D?[,,] _cells;

        /// <summary>
        /// The lights
        /// </summary>
        private readonly LightGrid3D _lights;

        /// <summary>
        /// The fog of war visibility grid tracking [Width, Height, Levels]
        /// </summary>
        private readonly FogState[,,] _fogMap;

        /// <summary>
        /// Per cell: the visibility epoch in which the cell was last marked visible. Lets <see cref="MarkVisible"/> tell
        /// "already marked in this update" from "was visible in the previous one" without touching the fog twice.
        /// </summary>
        private readonly int[,,] _visibleStamp;

        /// <summary>
        /// Daylight that reaches a cell through the sky (0 = none, <see cref="SkyLightLevel"/> = open to the sky). Kept
        /// apart from the block light of torches because it is scaled by the time of day and the weather when it is
        /// used, see <see cref="GetEffectiveLightAt"/>.
        /// </summary>
        private readonly byte[,,] _skyLight;

        /// <summary>
        /// The <see cref="StructureRevision"/> the sky light was last computed for.
        /// </summary>
        private int _skyComputedRevision = -1;

        /// <summary>
        /// Current daylight in <see cref="SkyDaylightSteps"/> steps (0 = night).
        /// </summary>
        private int _daylightSteps;

        /// <summary>
        /// Current visibility epoch, bumped by <see cref="BeginVisibilityUpdate"/>. Starts at 1 so the default stamp 0
        /// never counts as "marked in this update".
        /// </summary>
        private int _visibleEpoch = 1;

        /// <summary>Gets the horizontal width footprint coordinate boundaries.</summary>
        public int Width { get; }

        /// <summary>Gets the secondary horizontal depth dimension boundary tracker.</summary>
        public int Depth { get; }

        /// <summary>Gets the vertical stacking layer height limits.</summary>
        public int Levels { get; }

        /// <summary>Gets or sets the height of the structural base cell walls.</summary>
        public float BaseHeight { get; set; } = MapDimensions.LevelHeightCells;

        /// <summary>
        /// Gets the world height of one level: <see cref="BaseHeight"/> times <see cref="DungeonCellScale"/>.
        /// This is the same spacing the dungeon builders use when they place each level.
        /// </summary>
        public float LevelHeight => BaseHeight * DungeonCellScale;

        /// <summary>
        /// Converts a FEET height (or scenery base height) to a valid level index of this map.
        /// This is the only level rule in the code base: collision, rendering, viewports and actors all call it.
        /// </summary>
        /// <param name="feetY">World Y of the feet. Never pass eye height.</param>
        /// <returns>A level index clamped to <c>0..Levels-1</c>.</returns>
        public int WorldToLevel(float feetY) =>
            Math.Clamp(MapConstants.LevelFromHeight(feetY, LevelHeight), 0, Levels - 1);

        /// <summary>Gets or sets the horizontal scale factor applied to each cell grid step step.</summary>
        public float DungeonCellScale { get; init; } = MapDimensions.DefaultCellSize;

        /// <summary>
        /// The master list tracking all active decorative environmental details layout metrics.
        /// </summary>
        private readonly List<SceneryPlacement> _scenery = new();

        /// <summary>
        /// Gets the active tracking collection of placed dungeon scenery components.
        /// </summary>
        public IEnumerable<SceneryPlacement> Scenery => _scenery;

        /// <summary>
        /// Gives the props that have no texture of their own the current texture of their type slot, see
        /// <see cref="SceneryRetexture"/>. Call it after replacing <see cref="Textures"/> on a loaded map.
        /// </summary>
        /// <returns>The number of props that changed.</returns>
        public int RetextureScenery()
        {
            var changed = SceneryRetexture.Apply(_scenery, _sceneryGrid, Textures);
            if (changed > 0) StructureRevision++;
            return changed;
        }

        /// <summary>
        /// The scenery grid dictionary bucket cache map tracker.
        /// </summary>
        private readonly Dictionary<(int, int), List<SceneryPlacement>> _sceneryGrid = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonMap" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="fogMode">The fog mode.</param>
        /// <param name="sightRadius">The sight radius.</param>
        /// <param name="startExplored">if set to <c>true</c> [start explored].</param>
        /// <param name="openAirFromLevel">The open air from level.</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <param name="weather">The weather.</param>
        public DungeonMap(int width, int depth, int levels, FogMode? fogMode = null, int? sightRadius = null,
            bool startExplored = false, int? openAirFromLevel = null, float? timeOfDay = null,
            WeatherType? weather = null)
            : this(width, depth, levels, MapSettings.From(fogMode, sightRadius, startExplored, timeOfDay, weather),
                openAirFromLevel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonMap"/> class from a settings object (also used when a map is loaded).
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="settings">The start settings; the map keeps this instance.</param>
        /// <param name="openAirFromLevel">From this level up an empty cell is open air. <c>null</c> = never (plain dungeon).</param>
        public DungeonMap(int width, int depth, int levels, MapSettings settings, int? openAirFromLevel = null)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (openAirFromLevel.HasValue) OpenAirFromLevel = openAirFromLevel.Value;

            Width = width;
            Depth = depth;
            Levels = levels;

            _cells = new MapCell3D?[width, depth, levels];
            _lights = new LightGrid3D(width, depth, levels);
            _fogMap = new FogState[width, depth, levels];
            _visibleStamp = new int[width, depth, levels];
            _skyLight = new byte[width, depth, levels];

            for (var x = 0; x < width; x++)
            for (var z = 0; z < depth; z++)
            for (var l = 0; l < levels; l++)
                _cells[x, z, l] = new MapCell3D(x, z, l);

            // Discovery: a map that starts explored is born with the memory shroud everywhere.
            if (settings.StartExplored) ExploreAll();
        }

        /// <summary>
        /// Gets the start settings of this map (fog, discovery, sight, time, weather, spawn).
        /// </summary>
        public MapSettings Settings { get; }

        /// <summary>
        /// Marks every cell as explored (memory shroud), as if the player had been everywhere.
        /// </summary>
        public void ExploreAll()
        {
            for (var x = 0; x < Width; x++)
            for (var z = 0; z < Depth; z++)
            for (var l = 0; l < Levels; l++)
                _fogMap[x, z, l] = FogState.Explored;

            FogVersion++;
        }

        /// <summary>
        /// Gets the precomputed light value byte sitting at explicit grid array coordinates.
        /// Defends against edge-case solver checks and out-of-bounds mountain sampling.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>
        /// The light value.
        /// </returns>
        public byte GetLightAt(int x, int z, int level) => _lights.Get(x, z, level);

        /// <summary>
        /// Injects a light value intensity byte directly into the target grid coordinate channel space.
        /// Safely ignores out-of-bounds writes from propagation algorithms.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <param name="light">The light value.</param>
        public void SetLightAt(int x, int z, int level, byte light)
        {
            _lights.Set(x, z, level, light);
            LightVersion++;
        }

        /// <summary>
        /// Counts every write to the light grid. Renderers compare it to notice a light change without re-hashing the
        /// light of every cell every frame.
        /// </summary>
        public int LightVersion { get; private set; }

        /// <summary>
        /// Sky light of a cell that is open to the sky.
        /// </summary>
        public const byte SkyLightLevel = 12;

        /// <summary>
        /// Number of daylight steps. Daylight is quantized to them so a chunk is rebaked a few times per sunrise, not
        /// every frame.
        /// </summary>
        public const int SkyDaylightSteps = 8;

        /// <summary>
        /// Gets or sets the first level whose EMPTY cells are open air (a hybrid world: the surface, level 1 and up). Empty
        /// cells below it are rock. Defaults to none: in a plain dungeon an empty cell is never sky.
        /// </summary>
        public int OpenAirFromLevel { get; set; } = int.MaxValue;

        /// <summary>
        /// Counts changes of the sky light or of the daylight that scales it. Renderers compare it to rebake only the
        /// chunks that sky light reaches.
        /// </summary>
        public int SkyVersion { get; private set; }

        /// <summary>
        /// Gets the current daylight step, 0 (night) to <see cref="SkyDaylightSteps"/> (clear noon).
        /// </summary>
        public int DaylightStep => _daylightSteps;

        /// <summary>
        /// Computes the sky light again when the structure changed since the last time (a ceiling was removed, a door
        /// opened). Cheap to call every frame: it returns at once while nothing changed.
        /// </summary>
        public void EnsureSkyLight()
        {
            if (_skyComputedRevision == StructureRevision) return;

            _skyComputedRevision = StructureRevision;
            SkyLightSolver.Recalculate(this);
            SkyVersion++;
        }

        /// <summary>
        /// Sets how much daylight there is (0 night, 1 clear noon), quantized to <see cref="SkyDaylightSteps"/> steps.
        /// </summary>
        /// <param name="daylight">Daylight 0 to 1.</param>
        public void SetDaylight(float daylight)
        {
            var steps = (int)MathF.Round(Math.Clamp(daylight, 0f, 1f) * SkyDaylightSteps);
            if (steps == _daylightSteps) return;

            _daylightSteps = steps;
            SkyVersion++;
        }

        /// <summary>
        /// Gets the sky light (0 to <see cref="SkyLightLevel"/>) of a cell, not yet scaled by the daylight.
        /// </summary>
        public byte GetSkyLightAt(int x, int z, int level)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level < 0 || level >= Levels) return 0;
            return _skyLight[x, z, level];
        }

        internal void SetSkyLightAt(int x, int z, int level, byte value) => _skyLight[x, z, level] = value;

        internal void ClearSkyLight() => Array.Clear(_skyLight);

        /// <summary>
        /// The light a cell really has right now: its block light (torches) or the sky light scaled by the current
        /// daylight, whichever is brighter. Empty cells of the open air levels (<see cref="OpenAirFromLevel"/>) count as
        /// open sky.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>Light 0 to 15.</returns>
        public byte GetEffectiveLightAt(int x, int z, int level)
        {
            var block = _lights.Get(x, z, level);
            if (_daylightSteps == 0) return block;

            var sky = GetSkyLightAt(x, z, level);

            if (sky == 0 && level >= OpenAirFromLevel && x >= 0 && x < Width && z >= 0 && z < Depth &&
                level < Levels && _cells[x, z, level] is not { IsEmpty: false })
            {
                sky = SkyLightLevel;
            }

            if (sky == 0) return block;

            var scaled = (byte)((sky * _daylightSteps + (SkyDaylightSteps / 2)) / SkyDaylightSteps);
            return scaled > block ? scaled : block;
        }

        /// <summary>
        /// How much sky a camera in this cell sees, 0 (closed room) to 1 (open air). Drives the background colour.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>0 to 1.</returns>
        public float GetSkyViewAt(int x, int z, int level)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level < 0 || level >= Levels) return 0f;

            if (_cells[x, z, level] is not { IsEmpty: false })
            {
                return level >= OpenAirFromLevel ? 1f : 0f;
            }

            return Math.Min(1f, _skyLight[x, z, level] / (float)SkyLightLevel);
        }

        /// <summary>
        /// Gets the Fog of War state sitting at explicit grid array coordinates.
        /// Safe against out-of-bounds queries.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>The fog state.</returns>
        public FogState GetFogAt(int x, int z, int level)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level < 0 || level >= Levels)
                return FogState.Unexplored;

            return _fogMap[x, z, level];
        }

        /// <summary>
        /// Directly sets the Fog of War state sitting at explicit grid array coordinates.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <param name="state">The fog state.</param>
        public void SetFogAt(int x, int z, int level, FogState state)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level < 0 || level >= Levels) return;

            if (_fogMap[x, z, level] == state) return;

            _fogMap[x, z, level] = state;
            FogVersion++;
        }

        /// <summary>
        /// Counts every change of the fog of war. Renderers fold it into their chunk context so they notice a change at
        /// once instead of whenever a far chunk happens to be re-checked.
        /// </summary>
        public int FogVersion { get; private set; }

        /// <summary>
        /// Cells marked visible in the current update (see <see cref="MarkVisible"/>).
        /// </summary>
        private List<(int X, int Z, int Level)> _visibleCells = new(512);

        /// <summary>
        /// Cells that were visible in the update before; <see cref="EndVisibilityUpdate"/> shrouds those that were not
        /// marked again.
        /// </summary>
        private List<(int X, int Z, int Level)> _previousVisibleCells = new(512);

        /// <summary>
        /// Counts every change of the structure that can alter what the player can see: doors, walls, props, stairs.
        /// Visibility updates compare it to skip a recompute when nothing relevant changed. Bump it through
        /// <see cref="NotifyStructureChanged"/>.
        /// </summary>
        public int StructureRevision { get; private set; }

        /// <summary>
        /// Texture ids of this map (floor, wall, ceiling, liquids, one slot per prop type). Starts as a copy of
        /// <see cref="TextureSet.Template" />. Replacing it rebakes the structure.
        /// </summary>
        public TextureSet Textures
        {
            get => _textures;
            set
            {
                _textures = value ?? new TextureSet();
                StructureRevision++;
            }
        }

        /// <summary>
        /// The textures backing field.
        /// </summary>
        private TextureSet _textures = TextureSet.Template.Clone();

        /// <summary>
        /// Call after anything changed at runtime that can block or open a line of sight (a door was toggled, a wall or
        /// prop was added or removed).
        /// </summary>
        public void NotifyStructureChanged() => StructureRevision++;

        /// <summary>
        /// Starts a visibility update. Nothing is shrouded yet: the cells that were visible stay visible until
        /// <see cref="EndVisibilityUpdate"/>, and only those that were not marked again in between become explored.
        /// A cell that stays visible therefore never changes state (and never bumps <see cref="FogVersion"/>), so a
        /// renderer only re-checks the chunks whose fog really changed. Follow it with <see cref="MarkVisible"/> for
        /// every cell the player can see now, then call <see cref="EndVisibilityUpdate"/>.
        /// </summary>
        public void BeginVisibilityUpdate()
        {
            _visibleEpoch++;
            (_previousVisibleCells, _visibleCells) = (_visibleCells, _previousVisibleCells);
            _visibleCells.Clear();
        }

        /// <summary>
        /// Finishes a visibility update: every cell that was visible in the previous update and was not marked again is
        /// shrouded (visible becomes explored), on every level.
        /// </summary>
        public void EndVisibilityUpdate()
        {
            for (var i = 0; i < _previousVisibleCells.Count; i++)
            {
                var (x, z, level) = _previousVisibleCells[i];

                if (_visibleStamp[x, z, level] == _visibleEpoch) continue;

                if (_fogMap[x, z, level] == FogState.Visible)
                {
                    _fogMap[x, z, level] = FogState.Explored;
                    FogVersion++;
                }
            }

            _previousVisibleCells.Clear();
        }

        /// <summary>
        /// Marks a cell as currently visible. Safe against out-of-bounds coordinates. Marking a cell that is already
        /// visible changes nothing.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        public void MarkVisible(int x, int z, int level)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level < 0 || level >= Levels) return;
            if (_visibleStamp[x, z, level] == _visibleEpoch) return;

            _visibleStamp[x, z, level] = _visibleEpoch;
            _visibleCells.Add((x, z, level));

            if (_fogMap[x, z, level] == FogState.Visible) return;

            _fogMap[x, z, level] = FogState.Visible;
            FogVersion++;
        }

        /// <summary>
        /// Gets the world height of the walkable floor under a position, on the level that contains
        /// <paramref name="worldY"/>. The one definition every dungeon caller (player, projectiles, actors) uses, so no
        /// viewport has to guess the level on its own.
        /// </summary>
        /// <param name="worldX">World x.</param>
        /// <param name="worldY">World y of a point on the level to query (feet or eyes).</param>
        /// <param name="worldZ">World z.</param>
        /// <returns>The floor height, or 0 when the cell has no floor.</returns>
        public float GetFloorHeight(float worldX, float worldY, float worldZ)
        {
            var gx = (int)MathF.Floor(worldX / DungeonCellScale);
            var gz = (int)MathF.Floor(worldZ / DungeonCellScale);
            return TryGetSupport(gx, gz, worldY, out _, out var floorY) ? floorY : 0f;
        }

        /// <summary>
        /// The level whose floor carries a body with the given feet height at this column: the highest floor that is not more
        /// than <see cref="MapDimensions.FloorProbeReach"/> above the feet. Unlike <see cref="WorldToLevel"/> this also finds a
        /// sunken floor that lies below its level boundary, so walls and light of the right level are used there.
        /// </summary>
        /// <param name="gridX">Grid column.</param>
        /// <param name="gridZ">Grid row.</param>
        /// <param name="feetY">World height of the feet.</param>
        /// <returns>The supporting level, or the level of the height when there is no floor at all.</returns>
        public int GetSupportLevel(int gridX, int gridZ, float feetY) =>
            TryGetSupport(gridX, gridZ, feetY, out var level, out _) ? level : WorldToLevel(feetY);

        /// <summary>
        /// Restores a previously saved scenery placement directly into the map collections
        /// without re-applying wall offsets or modifying seeds.
        /// </summary>
        public void RestoreScenery(SceneryPlacement placement)
        {
            _scenery.Add(placement);

            var cx = (int)MathF.Floor(placement.Position.X / DungeonCellScale);
            var cz = (int)MathF.Floor(placement.Position.Z / DungeonCellScale);
            var gridKey = (cx, cz);

            if (!_sceneryGrid.TryGetValue(gridKey, out var list))
            {
                list = new List<SceneryPlacement>();
                _sceneryGrid[gridKey] = list;
            }

            list.Add(placement);
            StructureRevision++;
        }

        /// <summary>
        /// Tries the get support.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="feetY">The feet y.</param>
        /// <param name="level">The level.</param>
        /// <param name="floorY">The floor y.</param>
        /// <returns>
        ///   <c>true</c> if [is support]; otherwise, <c>false</c>.
        /// </returns>
        private bool TryGetSupport(int gridX, int gridZ, float feetY, out int level, out float floorY)
        {
            // Searched from the top level down, not from a level guessed from the height: a sunken floor (pool) can lie
            // more than the reach below its own level boundary, and a stairwell has no floor in its own level at all.
            var reach = MapDimensions.FloorProbeReach;
            for (var l = Levels - 1; l >= 0; l--)
            {
                var cell = GetCell(gridX, gridZ, l);
                if (cell is not { HasFloor: true }) continue;

                var y = (cell.FloorHeight + cell.LevelBaseHeight) * DungeonCellScale;
                if (y > feetY + reach) continue;

                level = l;
                floorY = y;
                return true;
            }

            level = 0;
            floorY = 0f;
            return false;
        }

        /// <summary>
        /// Helper evaluating whether a given wall type blocks vision rays.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <returns>
        ///   <c>true</c> if [is vision blocking] [the specified wall]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsVisionBlocking(WallType wall) => wall.BlocksSight();

        /// <summary>
        /// LEGACY ray-sampling line of sight. The viewports use <c>DungeonVisibility</c> (GlRender.Core.Shared) instead,
        /// which traces exact grid lines and lets props block sight too.
        /// Updates the Line of Sight around an origin grid position up to a given radius using direction-aware raycasting.
        /// Transitions previously Visible tiles to Explored (shrouded), and raycasts outwards to reveal newly Visible tiles.
        /// </summary>
        /// <param name="originX">The origin grid X coordinate.</param>
        /// <param name="originZ">The origin grid Z coordinate.</param>
        /// <param name="level">The current map level.</param>
        /// <param name="sightRadius">The sight radius in tile units.</param>
        public void UpdateLineOfSight(int originX, int originZ, int level, int sightRadius = 8)
        {
            if (level < 0 || level >= Levels) return;

            FogVersion++;

            // Step 1: Transition current Visible tiles on this level to Explored memory shroud
            for (var x = 0; x < Width; x++)
            for (var z = 0; z < Depth; z++)
            {
                if (_fogMap[x, z, level] == FogState.Visible)
                {
                    _fogMap[x, z, level] = FogState.Explored;
                }
            }

            // Step 2: Direction-aware 360-degree raycasting
            const int rayCount = 120; // 3-degree resolution step for precise room corner coverage
            for (var i = 0; i < rayCount; i++)
            {
                var angle = i * (MathF.PI * 2f / rayCount);
                var dirX = MathF.Cos(angle);
                var dirZ = MathF.Sin(angle);

                var prevX = originX;
                var prevZ = originZ;

                for (float r = 0; r <= sightRadius; r += 0.25f)
                {
                    var cx = (int)MathF.Floor(originX + dirX * r + 0.5f);
                    var cz = (int)MathF.Floor(originZ + dirZ * r + 0.5f);

                    if (cx < 0 || cx >= Width || cz < 0 || cz >= Depth) break;

                    // If crossing tile boundaries, evaluate directional wall blocking between prev and curr cells
                    if (cx != prevX || cz != prevZ)
                    {
                        var prevCell = GetCell(prevX, prevZ, level);
                        var currCell = GetCell(cx, cz, level);

                        var isBlocked = false;

                        if (cx > prevX && (IsVisionBlocking(prevCell?.WallEast ?? WallType.None) ||
                                           IsVisionBlocking(currCell?.WallWest ?? WallType.None))) isBlocked = true;
                        if (cx < prevX && (IsVisionBlocking(prevCell?.WallWest ?? WallType.None) ||
                                           IsVisionBlocking(currCell?.WallEast ?? WallType.None))) isBlocked = true;
                        if (cz > prevZ && (IsVisionBlocking(prevCell?.WallSouth ?? WallType.None) ||
                                           IsVisionBlocking(currCell?.WallNorth ?? WallType.None))) isBlocked = true;
                        if (cz < prevZ && (IsVisionBlocking(prevCell?.WallNorth ?? WallType.None) ||
                                           IsVisionBlocking(currCell?.WallSouth ?? WallType.None))) isBlocked = true;

                        if (isBlocked)
                        {
                            // Reveal boundary cell so the wall face/door itself is rendered before stopping the ray
                            _fogMap[cx, cz, level] = FogState.Visible;
                            break;
                        }

                        prevX = cx;
                        prevZ = cz;
                    }

                    _fogMap[cx, cz, level] = FogState.Visible;
                }
            }
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        internal void Reset()
        {
            _lights.Reset();
            LightVersion++;
        }

        /// <summary>
        /// Fetches the cell state layout tracking data sitting at explicit matrix coordinates.
        /// </summary>
        public MapCell3D? GetCell(int x, int y, int z)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Depth || z < 0 || z >= Levels) return null;

            return _cells[x, y, z];
        }

        /// <summary>
        /// Securely references a single local layout grid cell bucket list to return matching scenery props.
        /// </summary>
        public IReadOnlyList<SceneryPlacement>? GetSceneryAtCell(int cx, int cz)
        {
            var key = (cx, cz);
            return _sceneryGrid.TryGetValue(key, out var list) ? list : null;
        }

        /// <summary>
        /// Builds a staircase that climbs from <paramref name="level"/> to <c>level + 1</c>: the flight cells, the
        /// stairwell walls, the hole in the floor above and the landing. See <see cref="StairBuilder"/>.
        /// </summary>
        /// <param name="gridX">Grid X of the bottom cell.</param>
        /// <param name="gridZ">Grid Z of the bottom cell.</param>
        /// <param name="level">The level the flight starts on.</param>
        /// <param name="direction">The compass direction of the climb.</param>
        /// <returns><c>false</c> if the flight (<see cref="StairLayout.CellsLong"/> cells plus a landing) does not fit.</returns>
        public bool AddStaircase(int gridX, int gridZ, int level, Direction direction)
        {
            var carved = StairBuilder.Carve(this, gridX, gridZ, level, direction,
                (pos, seed) => AddScenery(pos, seed, SceneryType.StairFlight, -1, -1, DungeonCellScale));
            NotifyStructureChanged();
            return carved;
        }

        /// <summary>
        /// Builds a staircase that climbs from <paramref name="level"/> to <c>level + 1</c>: the flight cells, the
        /// stairwell walls, the hole in the floor above and the landing. See <see cref="StairBuilder"/>.
        /// </summary>
        /// <param name="gridX">The grid x.</param>
        /// <param name="gridZ">The grid z.</param>
        /// <param name="level">The level.</param>
        /// <param name="withWalls">if set to <c>true</c> [with walls].</param>
        /// <param name="addCenterPillar">if set to <c>true</c> [add center pillar].</param>
        /// <returns><c>false</c> if the flight (<see cref="StairLayout.CellsLong"/> cells plus a landing) does not fit.</returns>
        public bool AddSpiralStaircase(int gridX, int gridZ, int level = 0, bool withWalls = true,
            bool addCenterPillar = true)
        {
            var carved = StairBuilder.CarveSpiral(this, gridX, gridZ, level, withWalls, addCenterPillar);
            NotifyStructureChanged();
            return carved;
        }

        /// <summary>
        /// Adds a generic scenery component to the world map tracking streams, offsetting wall-bound entities.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="type">The type.</param>
        /// <param name="primaryTexId">The primary tex identifier.</param>
        /// <param name="secondaryTexId">The secondary tex identifier.</param>
        /// <param name="fitScale">Cell size for cell-fitted types; 0 (default) takes this map's cell size for them and 1 for everything else.</param>
        public void AddScenery(Vector3 position, int seed, SceneryType type, int primaryTexId, int secondaryTexId = -1,
            float fitScale = 0f)
        {
            if (fitScale <= 0f) fitScale = type.IsCellFitted() ? DungeonCellScale : 1f;

            // No explicit texture: use this map's slot for the prop type (still -1 = factory default if unset).
            if (primaryTexId <= 0) primaryTexId = _textures.ForScenery(type);

            // Wall-mounted props are moved to their wall (shared with HybridWorldMap).
            var rotationSeed = WallMountRules.Apply(ref position, seed, type, DungeonCellScale);
            var placement =
                new SceneryPlacement(position, rotationSeed, type, primaryTexId, secondaryTexId, -1, fitScale);
            _scenery.Add(placement);

            // SPATIAL PROXIMITY INDEXING: Translate real world placement vectors back into integer tile indexes
            var cx = (int)MathF.Floor(position.X / DungeonCellScale);
            var cz = (int)MathF.Floor(position.Z / DungeonCellScale);
            var gridKey = (cx, cz);

            if (!_sceneryGrid.ContainsKey(gridKey))
            {
                _sceneryGrid[gridKey] = new List<SceneryPlacement>();
            }

            _sceneryGrid[gridKey].Add(placement);
            StructureRevision++;
        }
    }
}