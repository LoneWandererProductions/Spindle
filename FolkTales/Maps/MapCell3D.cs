/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapCell3D.cs
 * PURPOSE:     Represents a single 3D cell of a dungeon grid, with walls, floor, ceiling, textures and metadata.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Drawing;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Represents one cell of the dungeon map (stackable in X, Y, Z).
    /// Provides geometric bounds for walls, floors, ceilings, and supports texture/color metadata.
    /// </summary>
    public class MapCell3D
    {
        /// <summary>
        /// X coordinate of the cell in the grid.
        /// </summary>
        /// <value>
        /// The x.
        /// </value>
        public int X { get; }

        /// <summary>
        /// Y coordinate of the cell in the grid (Handles map depth dimension).
        /// </summary>
        /// <value>
        /// The y.
        /// </value>
        public int Y { get; }

        /// <summary>
        /// Z coordinate of the cell in the grid (Vertical tracking layers).
        /// </summary>
        /// <value>
        /// The z.
        /// </value>
        public int Z { get; }

        /// <summary>
        /// Creates a new <see cref="MapCell3D" />.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        public MapCell3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // --- Upgraded Multi-State Wall Architecture ---

        /// <summary>
        /// The structural layout archetype raised on the north side of the grid block.
        /// </summary>
        /// <value>
        /// The wall north.
        /// </value>
        public WallType WallNorth { get; set; } = WallType.None;

        /// <summary>
        /// The structural layout archetype raised on the south side of the grid block.
        /// </summary>
        /// <value>
        /// The wall south.
        /// </value>
        public WallType WallSouth { get; set; } = WallType.None;

        /// <summary>
        /// The structural layout archetype raised on the east side of the grid block.
        /// </summary>
        /// <value>
        /// The wall east.
        /// </value>
        public WallType WallEast { get; set; } = WallType.None;

        /// <summary>
        /// The structural layout archetype raised on the west side of the grid block.
        /// </summary>
        /// <value>
        /// The wall west.
        /// </value>
        public WallType WallWest { get; set; } = WallType.None;

        // --- Cell footprint (round chambers) ---

        /// <summary>
        /// Footprint of the room inside this cell. <see cref="CellShape.Round"/> draws a circular chamber with a mouth on
        /// every open edge (see <see cref="RoundCellGeometry"/>); the four edge walls keep their meaning.
        /// </summary>
        public CellShape Shape { get; set; } = CellShape.Square;

        /// <summary>
        /// Gets the wall type on one side of this cell.
        /// </summary>
        /// <param name="direction">The side.</param>
        /// <returns>The wall type stored on that side.</returns>
        public WallType GetWall(Direction direction) => direction switch
        {
            Direction.North => WallNorth,
            Direction.South => WallSouth,
            Direction.East => WallEast,
            Direction.West => WallWest,
            _ => WallType.None
        };

        /// <summary>
        /// Sets the wall type on one side of this cell.
        /// </summary>
        /// <param name="direction">The side.</param>
        /// <param name="wall">The new wall type.</param>
        public void SetWall(Direction direction, WallType wall)
        {
            switch (direction)
            {
                case Direction.North:
                    WallNorth = wall;
                    break;
                case Direction.South:
                    WallSouth = wall;
                    break;
                case Direction.East:
                    WallEast = wall;
                    break;
                case Direction.West:
                    WallWest = wall;
                    break;
            }
        }

        /// <summary>
        /// True when any side is a <see cref="WallType.Crawl"/> wall.
        /// </summary>
        public bool HasCrawlWall => WallNorth == WallType.Crawl || WallSouth == WallType.Crawl ||
                                    WallEast == WallType.Crawl || WallWest == WallType.Crawl;

        // --- Advanced Roof Configuration Fields ---

        /// <summary>
        /// Gets or sets the directional layout pitch orientation of the roof panel structures.
        /// </summary>
        /// <value>
        /// The roof style.
        /// </value>
        public RoofOrientation RoofStyle { get; set; } = RoofOrientation.None;

        /// <summary>
        /// Gets or sets the custom pitched roof slope angle modifier.
        /// </summary>
        /// <value>
        /// The roof angle degrees.
        /// </value>
        public float RoofAngleDegrees { get; set; } = 30f;

        /// <summary>
        /// Gets or sets a value indicating whether triangular attic walls seal empty sides.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [close gable sidewalls]; otherwise, <c>false</c>.
        /// </value>
        public bool CloseGableSidewalls { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this room cell bypasses angled roof curves internally.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has flat ceiling; otherwise, <c>false</c>.
        /// </value>
        public bool HasFlatCeiling { get; set; } = true;

        // --- Smart Legacy Boolean Adapters ---

        /// <summary>
        /// True if a wall exists on the north side of this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has wall north; otherwise, <c>false</c>.
        /// </value>
        public bool HasWallNorth
        {
            get => WallNorth.BlocksMovement();
            set => WallNorth = value ? WallType.Solid : WallType.None;
        }

        /// <summary>
        /// True if a wall exists on the south side of this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has wall south; otherwise, <c>false</c>.
        /// </value>
        public bool HasWallSouth
        {
            get => WallSouth.BlocksMovement();
            set => WallSouth = value ? WallType.Solid : WallType.None;
        }

        /// <summary>
        /// True if a wall exists on the east side of this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has wall east; otherwise, <c>false</c>.
        /// </value>
        public bool HasWallEast
        {
            get => WallEast.BlocksMovement();
            set => WallEast = value ? WallType.Solid : WallType.None;
        }

        /// <summary>
        /// True if a wall exists on the west side of this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has wall west; otherwise, <c>false</c>.
        /// </value>
        public bool HasWallWest
        {
            get => WallWest.BlocksMovement();
            set => WallWest = value ? WallType.Solid : WallType.None;
        }

        /// <summary>
        /// Gets a value indicating whether this instance contains any structural barrier side wall.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has any wall; otherwise, <c>false</c>.
        /// </value>
        public bool HasAnyWall => HasWallNorth || HasWallSouth || HasWallEast || HasWallWest;

        /// <summary>
        /// Gets a value indicating whether this cell is a solid opaque structural block that blocks light.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is solid block; otherwise, <c>false</c>.
        /// </value>
        public bool IsSolidBlock => WallNorth == WallType.Solid &&
                                    WallSouth == WallType.Solid &&
                                    WallEast == WallType.Solid &&
                                    WallWest == WallType.Solid;

        /// <summary>
        /// Gets a value indicating whether this cell block is entirely hollow vacuum space.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty => !HasAnyWall && !HasFloor && !HasCeiling;

        /// <summary>
        /// Thickness of walls (depth into the cell space).
        /// </summary>
        /// <value>
        /// The wall thickness.
        /// </value>
        public float WallThickness { get; set; } = MapDimensions.WallThicknessCells;

        /// <summary>
        /// Height of door and window openings, as a fraction of the cell's full floor-to-ceiling span.
        /// </summary>
        /// <value>
        /// The opening height ratio.
        /// </value>
        public float OpeningHeightRatio { get; set; } = MapDimensions.DefaultOpeningRatio;

        /// <summary>
        /// How the doors of this cell look (see <see cref="Enums.DoorStyle"/>). Only read where a wall is a door.
        /// </summary>
        public DoorStyle DoorStyle { get; set; } = DoorStyle.Wood;

        /// <summary>
        /// Optional color for walls if no texture is applied.
        /// </summary>
        /// <value>
        /// The color of the wall.
        /// </value>
        public Color? WallColor { get; set; }

        // --- Floor / Ceiling Space Options ---

        /// <summary>
        /// True if a walkable floor exists in this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has floor; otherwise, <c>false</c>.
        /// </value>
        public bool HasFloor { get; set; } = true;

        /// <summary>
        /// True if an overhead ceiling slab exists in this cell.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has ceiling; otherwise, <c>false</c>.
        /// </value>
        public bool HasCeiling { get; set; } = true;

        /// <summary>
        /// Gets whether something above this cell closes it off from the sky (the ceiling of the level above, a roof, the
        /// ground over an underground level). Not edited by hand: <see cref="SkyLightSolver"/> sets it, like light.
        /// </summary>
        public bool CoveredFromAbove { get; internal set; }

        /// <summary>
        /// Base height of this cell's level in cells (Z * LevelHeightCells). FloorHeight and CeilingHeight are offsets
        /// inside the level and never move the level itself (they used to, which displaced sunken floors above level 0).
        /// </summary>
        public float LevelBaseHeight => Z * MapDimensions.LevelHeightCells;

        /// <summary>
        /// Gets whether the cell is indoors: it has a ceiling of its own or is covered from above. Indoor surfaces are
        /// lit by the fixed indoor ambient and never by the sun or the weather. (A dungeon room has its ceiling on the
        /// level ABOVE its floor cells, so <see cref="HasCeiling"/> alone is false for the cells you walk in.)
        /// </summary>
        public bool IsIndoor => HasCeiling || CoveredFromAbove;

        /// <summary>
        /// Floor offset inside this cell (relative to vertical layer Z).
        /// </summary>
        /// <value>
        /// The height of the floor.
        /// </value>
        public float FloorHeight { get; set; } = 0f;

        /// <summary>
        /// Ceiling height dimension limit inside this cell block space.
        /// </summary>
        /// <value>
        /// The height of the ceiling.
        /// </value>
        public float CeilingHeight { get; set; } = MapDimensions.LevelHeightCells;

        /// <summary>
        /// Thickness of floor slab (solid volume below the walkable floor line).
        /// </summary>
        /// <value>
        /// The floor thickness.
        /// </value>
        public float FloorThickness { get; set; } = MapDimensions.DefaultFloorThickness;

        /// <summary>
        /// Optional floor color if no texture sheet is active.
        /// </summary>
        /// <value>
        /// The color of the floor.
        /// </value>
        public Color? FloorColor { get; set; }

        /// <summary>
        /// Optional overhead ceiling wash color tint profile options.
        /// </summary>
        /// <value>
        /// The color of the ceiling.
        /// </value>
        public Color? CeilingColor { get; set; }

        /// <summary>
        /// Thickness of ceiling slab (solid volume expanding above the interior roof line).
        /// </summary>
        /// <value>
        /// The ceiling thickness.
        /// </value>
        public float CeilingThickness { get; set; } = 0.2f;

        // --- Texture Slot ID Mappings ---

        /// <summary>
        /// Gets or sets the north wall texture identifier.
        /// </summary>
        /// <value>
        /// The north wall texture identifier.
        /// </value>
        public int? NorthWallTextureId { get; set; }

        /// <summary>
        /// Gets or sets the south wall texture identifier.
        /// </summary>
        /// <value>
        /// The south wall texture identifier.
        /// </value>
        public int? SouthWallTextureId { get; set; }

        /// <summary>
        /// Gets or sets the east wall texture identifier.
        /// </summary>
        /// <value>
        /// The east wall texture identifier.
        /// </value>
        public int? EastWallTextureId { get; set; }

        /// <summary>
        /// Gets or sets the west wall texture identifier.
        /// </summary>
        /// <value>
        /// The west wall texture identifier.
        /// </value>
        public int? WestWallTextureId { get; set; }

        /// <summary>
        /// Gets or sets the wall texture identifier.
        /// </summary>
        /// <value>
        /// The wall texture identifier.
        /// </value>
        public int? WallTextureId { get; set; }

        // --- Interior Wallpaper Override Slots ---

        /// <summary>
        /// Gets or sets the interior wall texture identifier.
        /// </summary>
        /// <value>
        /// The interior wall texture identifier.
        /// </value>
        public int? InteriorWallTextureId { get; set; }

        /// <summary>
        /// Gets or sets the north wall interior texture identifier.
        /// </summary>
        /// <value>
        /// The north wall interior texture identifier.
        /// </value>
        public int? NorthWallInteriorTextureId { get; set; }

        /// <summary>
        /// Gets or sets the south wall interior texture identifier.
        /// </summary>
        /// <value>
        /// The south wall interior texture identifier.
        /// </value>
        public int? SouthWallInteriorTextureId { get; set; }

        /// <summary>
        /// Gets or sets the east wall interior texture identifier.
        /// </summary>
        /// <value>
        /// The east wall interior texture identifier.
        /// </value>
        public int? EastWallInteriorTextureId { get; set; }

        /// <summary>
        /// Gets or sets the west wall interior texture identifier.
        /// </summary>
        /// <value>
        /// The west wall interior texture identifier.
        /// </value>
        public int? WestWallInteriorTextureId { get; set; }

        /// <summary>
        /// Gets or sets the ceiling texture identifier.
        /// </summary>
        /// <value>
        /// The ceiling texture identifier.
        /// </value>
        public int? CeilingTextureId { get; set; }

        /// <summary>
        /// Gets or sets the floor texture identifier.
        /// </summary>
        /// <value>
        /// The floor texture identifier.
        /// </value>
        public int? FloorTextureId { get; set; }

        /// <summary>
        /// Custom cell layer collection overlays. Initialized lazily (defaults to null) to save memory.
        /// </summary>
        public List<CellLayer>? Layers { get; set; }

        /// <summary>
        /// Inline unmanaged corners struct. Eliminates Vector3[] array object heap allocations!
        /// </summary>
        public CellCorners PrecomputedCorners;

        /// <summary>
        /// Precomputes structural corner vertices directly inside the inline struct.
        /// </summary>
        /// <param name="scale">The scale.</param>
        public void PrecomputeCorners(float scale)
        {
            var zOffset = LevelBaseHeight;
            var f = (FloorHeight + zOffset) * scale;
            var c = (CeilingHeight + zOffset) * scale;

            var x0 = X * scale;
            var x1 = (X + 1) * scale;
            var y0 = Y * scale;
            var y1 = (Y + 1) * scale;

            PrecomputedCorners.C0 = new Vector3(x0, f, y0);
            PrecomputedCorners.C1 = new Vector3(x1, f, y0);
            PrecomputedCorners.C2 = new Vector3(x0, f, y1);
            PrecomputedCorners.C3 = new Vector3(x1, f, y1);

            PrecomputedCorners.C4 = new Vector3(x0, c, y0);
            PrecomputedCorners.C5 = new Vector3(x1, c, y0);
            PrecomputedCorners.C6 = new Vector3(x0, c, y1);
            PrecomputedCorners.C7 = new Vector3(x1, c, y1);
        }

        /// <summary>
        /// Get the bounding box for a wall (if it exists).
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>Bounding box as a tuple of minimum and maximum Vector3 coordinates, or null if no wall exists in the specified direction.</returns>
        public (Vector3 min, Vector3 max)? GetWallBounds(Direction dir, float scale = 1.0f)
        {
            var zOffset = LevelBaseHeight;
            var f = (FloorHeight + zOffset) * scale;
            var c = (CeilingHeight + zOffset) * scale;

            var t = WallThickness * scale;

            var x0 = X * scale;
            var x1 = (X + 1) * scale;
            var y0 = Y * scale;
            var y1 = (Y + 1) * scale;

            switch (dir)
            {
                case Direction.North when HasWallNorth:
                    return (new Vector3(x0, f, y0), new Vector3(x1, c, y0 + t));

                case Direction.South when HasWallSouth:
                    return (new Vector3(x0, f, y1 - t), new Vector3(x1, c, y1));

                case Direction.West when HasWallWest:
                    return (new Vector3(x0, f, y0), new Vector3(x0 + t, c, y1));

                case Direction.East when HasWallEast:
                    return (new Vector3(x1 - t, f, y0), new Vector3(x1, c, y1));
            }

            return null;
        }

        /// <summary>
        /// Get the bounding box for the solid floor slab volume (if any).
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>Bounding box as a tuple of minimum and maximum Vector3 coordinates, or null if no floor exists.</returns>
        public (Vector3 min, Vector3 max)? GetFloorBounds(float scale = 1.0f)
        {
            if (!HasFloor || FloorThickness <= 0f) return null;

            var zOffset = LevelBaseHeight;
            var f = (FloorHeight + zOffset) * scale;

            var x0 = X * scale;
            var x1 = (X + 1) * scale;
            var y0 = Y * scale;
            var y1 = (Y + 1) * scale;

            return (new Vector3(x0, f - FloorThickness * scale, y0), new Vector3(x1, f, y1));
        }

        /// <summary>
        /// Get the bounding box for the solid ceiling slab volume (if any).
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>Bounding box as a tuple of minimum and maximum Vector3 coordinates, or null if no ceiling exists.</returns>
        public (Vector3 min, Vector3 max)? GetCeilingBounds(float scale = 1.0f)
        {
            if (!HasCeiling || CeilingThickness <= 0f) return null;

            var zOffset = LevelBaseHeight;
            var c = (CeilingHeight + zOffset) * scale;

            var x0 = X * scale;
            var x1 = (X + 1) * scale;
            var y0 = Y * scale;
            var y1 = (Y + 1) * scale;

            return (new Vector3(x0, c, y0), new Vector3(x1, c + CeilingThickness * scale, y1));
        }
    }
}