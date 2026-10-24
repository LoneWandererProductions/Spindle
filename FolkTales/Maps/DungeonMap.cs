/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        DungeonMap.cs
 * PURPOSE:     Describes a simple grid-based dungeon map with multiple levels. Each cell can hold information about its type (floor, wall, door, etc.) and properties.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Structs;

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
        /// The scenery grid dictionary bucket cache map tracker.
        /// </summary>
        private readonly Dictionary<(int, int), List<SceneryPlacement>> _sceneryGrid = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonMap" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        /// <param name="sightRadius">The sight radius.</param>
        /// <param name="startExplored">if set to <c>true</c> [start explored].</param>
        /// <param name="openAirFromLevel">The open air from level.</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <param name="weather">The weather.</param>
        public DungeonMap(int width, int depth, int levels, int? sightRadius = null,
            bool startExplored = false, int? openAirFromLevel = null, float? timeOfDay = null,
            WeatherType? weather = null)
            : this(width, depth, levels, MapSettings.From(sightRadius, startExplored, timeOfDay, weather),
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

            for (var x = 0; x < width; x++)
            for (var z = 0; z < depth; z++)
            for (var l = 0; l < levels; l++)
                _cells[x, z, l] = new MapCell3D(x, z, l);
        }

        /// <summary>
        /// Gets the start settings of this map (fog, discovery, sight, time, weather, spawn).
        /// </summary>
        public MapSettings Settings { get; }

        /// <summary>
        /// Gets or sets the first level whose EMPTY cells are open air (a hybrid world: the surface, level 1 and up). Empty
        /// cells below it are rock. Defaults to none: in a plain dungeon an empty cell is never sky.
        /// </summary>
        public int OpenAirFromLevel { get; set; } = int.MaxValue;

        /// <summary>
        /// Cells marked visible in the current update (see <see cref="MarkVisible"/>).
        /// </summary>
        private List<(int X, int Z, int Level)> _visibleCells = new(512);

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
    }
}