/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapDimensions.cs
 * PURPOSE:     The one register for the dimensions every map, renderer, collision and viewport shares (cell size, storey height, door opening).
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// The central register of map dimensions. Nothing else may hard-code a cell size, a storey height or a door
    /// opening: dungeon, ortho dungeon, hybrid world, stairs, roofs, light, collision and the renderers all read
    /// these values, so changing one constant here changes every map and viewport together.
    /// <para>
    /// Heights are stored in CELLS (like <c>MapCell3D.CeilingHeight</c>) and turned into metres with the cell size,
    /// so a map built with another cell size keeps its proportions.
    /// </para>
    /// </summary>
    public static class MapDimensions
    {
        /// <summary>
        /// Edge length of one grid cell in metres, used by every map unless it is given its own cell size.
        /// </summary>
        public const float DefaultCellSize = 3.0f;

        /// <summary>
        /// Height of one storey (floor to the floor above) in CELLS. 1 gives cube shaped cells: 3 m high at the
        /// default cell size. This was 2.5 (7.5 m), far too tall next to a 1.7 m player and 2 m doors.
        /// </summary>
        public const float LevelHeightCells = 1.0f;

        /// <summary>
        /// How far (in metres) above the feet the floor lookup reaches: a sunken floor, a step or the top of a stair may
        /// lie slightly under the level boundary, so the level is derived from feet + this reach and then searched downwards.
        /// </summary>
        public const float FloorProbeReach = 0.5f;

        /// <summary>
        /// Deepest a pool bed may be sunk below its floor line, in METRES. Must stay below both the step-up limit of the
        /// player (0.45 m) and <see cref="FloorProbeReach"/>, otherwise the way out of the pool is lost.
        /// </summary>
        public const float MaxPoolDepthMetres = 0.4f;

        /// <summary>
        /// Default thickness of a floor slab in cells (the solid volume below the walkable floor line).
        /// </summary>
        public const float DefaultFloorThickness = 0.2f;

        /// <summary>
        /// Default wall thickness in cells.
        /// </summary>
        public const float WallThicknessCells = 0.1f;

        /// <summary>
        /// Height of a door opening as a fraction of the floor-to-ceiling span, as the builders set it for doorways
        /// (a 3 m storey gives a 2.25 m door).
        /// </summary>
        public const float DoorOpeningRatio = 0.75f;

        /// <summary>
        /// Height of any other opening (windows, plain cells) as a fraction of the floor-to-ceiling span.
        /// </summary>
        public const float DefaultOpeningRatio = 0.8f;

        /// <summary>
        /// Height in METRES of a crawl hole (<see cref="Enums.WallType.Crawl"/>). Absolute, because the crawling body is
        /// absolute (<see cref="PhysicsConstants.CrawlBodyHeight"/>); it is clamped to 90 % of the storey for tiny cells.
        /// </summary>
        public const float CrawlOpeningHeight = 0.95f;

        /// <summary>
        /// Width in METRES of a crawl hole. Clamped to 80 % of the cell edge for tiny cells.
        /// </summary>
        public const float CrawlOpeningWidth = 1.3f;

        /// <summary>
        /// Width in METRES of the mouth of a <see cref="Enums.CellShape.Round"/> cell where it opens to a neighbour.
        /// Clamped to 70 % of the cell edge.
        /// </summary>
        public const float RoundCellMouthWidth = 1.4f;

        /// <summary>
        /// Height of a walkable tunnel / mouth arch as a fraction of the storey height.
        /// </summary>
        public const float TunnelHeightRatio = 0.85f;

        /// <summary>
        /// Shape exponent of the arch of a walkable tunnel: 2 is an ellipse, higher is boxier (more headroom at the sides).
        /// </summary>
        public const float TunnelArchExponent = 2.6f;

        /// <summary>
        /// Shape exponent of the arch of a crawl hole / crawl tunnel (a plain half ellipse).
        /// </summary>
        public const float CrawlArchExponent = 2.0f;

        /// <summary>
        /// Number of columns an arch is split into when drawn (8 is smooth enough for a 1.4 m wide arch).
        /// </summary>
        public const int ArchColumns = 8;

        /// <summary>
        /// Number of straight segments per full circle of a round cell. 16 keeps a 3 m cell smooth and cheap (a quarter turn = 4 quads).
        /// </summary>
        public const int RoundCellSegments = 16;

        /// <summary>
        /// Largest height difference in metres between two neighbouring terrain samples that still counts as a
        /// walkable slope. A walking rule, so it does not follow the storey height.
        /// </summary>
        public const float MaxWalkableRise = 2.5f;

        /// <summary>
        /// Height of one storey in metres.
        /// </summary>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <returns>Storey height in metres.</returns>
        public static float LevelHeight(float cellSize) => LevelHeightCells * cellSize;

        /// <summary>
        /// World height of the surface layer of a hybrid world (level 1 floor) in metres.
        /// </summary>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <returns>World Y of the surface baseline.</returns>
        public static float SurfaceBaseline(float cellSize) => LevelHeight(cellSize);
    }
}