/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        StairLayout.cs
 * PURPOSE:     The one definition of how a staircase is sized from the cell size. The mesh factory, the collision
 *              boxes and the map builder all read it, so the visible stairs, the walkable stairs and the hole in
 *              the floor above can never disagree.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// Sizes a straight staircase that climbs exactly one level, measured in CELLS.
    /// <para>
    /// A level is <c>MapDimensions.LevelHeightCells * cellSize</c> high, so a stair usually cannot fit into one cell:
    /// 3 m of rise in a 3 m cell would be a 45 degree ladder. The flight therefore runs over as many cells as the
    /// maximum slope needs, one <see cref="SceneryType.StairFlight"/> placement per cell, each rising the same amount.
    /// The level height is a fixed number of cells, so the result does not depend on the scale: with the register's
    /// 1 cell it is 2 cells long and every cell rises 0.5 cells (27 degrees) at scale 3, 5 or 10.
    /// </para>
    /// </summary>
    public static class StairLayout
    {
        /// <summary>
        /// Steepest slope a flight may have, in degrees. 35 is the upper end of a comfortable stair.
        /// </summary>
        public const float MaxSlopeDegrees = 35f;

        /// <summary>
        /// Target riser height in metres. Must stay below the player step-up limit (0.45 m).
        /// </summary>
        public const float TargetRiser = 0.19f;

        /// <summary>
        /// Free height the player needs above a tread (eye 1.7 m plus a margin).
        /// </summary>
        public const float Headroom = 2.0f;

        /// <summary>
        /// Stair width as a fraction of the cell size; the rest is landing / wall clearance.
        /// </summary>
        public const float WidthFraction = 0.6f;

        /// <summary>
        /// Widest a flight gets in metres, so a 10 m terrain cell does not produce a 6 m wide ladder-stair.
        /// </summary>
        public const float MaxWidth = 3.0f;

        /// <summary>
        /// Tread thickness in metres.
        /// </summary>
        public const float TreadThickness = 0.07f;

        /// <summary>
        /// How far a tread overhangs the riser below it, in metres.
        /// </summary>
        public const float Nosing = 0.03f;

        /// <summary>
        /// Handrail height above the tread, in metres.
        /// </summary>
        public const float RailHeight = 0.95f;

        /// <summary>
        /// Number of cells a flight has to span so that it does not exceed <see cref="MaxSlopeDegrees"/>.
        /// </summary>
        /// <param name="levelHeightInCells">Level height divided by cell size (<see cref="MapDimensions.LevelHeightCells"/> by default).</param>
        /// <returns>The cell count, at least 1.</returns>
        public static int CellsLong(float levelHeightInCells = MapDimensions.LevelHeightCells)
        {
            var tan = MathF.Tan(MaxSlopeDegrees * MathF.PI / 180f);
            return Math.Max(1, (int)MathF.Ceiling(levelHeightInCells / tan - 1e-4f));
        }

        /// <summary>
        /// Rise of ONE cell of the flight in metres.
        /// </summary>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <returns>Metres climbed per cell.</returns>
        public static float RisePerCell(float cellSize) =>
            MapDimensions.LevelHeightCells * cellSize / CellsLong();

        /// <summary>
        /// Number of steps in one cell. All steps of the flight have the same riser (about <see cref="TargetRiser"/>).
        /// </summary>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <returns>Steps per cell, at least 1.</returns>
        public static int StepsPerCell(float cellSize) =>
            Math.Max(1, (int)MathF.Round(RisePerCell(cellSize) / TargetRiser));

        /// <summary>
        /// Riser height in metres.
        /// </summary>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Riser height in metres.</returns>
        public static float Riser(float cellSize) => RisePerCell(cellSize) / StepsPerCell(cellSize);

        /// <summary>
        /// Tread depth in metres.
        /// </summary>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Tread depth in metres.</returns>
        public static float Tread(float cellSize) => cellSize / StepsPerCell(cellSize);

        /// <summary>
        /// Walkable width of the flight in metres.
        /// </summary>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Walkable width of the flight in metres.</returns>
        public static float Width(float cellSize) => MathF.Min(cellSize * WidthFraction, MaxWidth);

        /// <summary>
        /// Local Z of the riser face of step <paramref name="step" /> inside ONE cell. The local frame is: origin at the
        /// cell centre on the floor of the flight's cell, the flight climbs toward local -Z (the yaw of the placement
        /// then turns it to the wanted compass direction).
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Local Z of the riser face of the step inside the cell.</returns>
        public static float StepFrontZ(int step, float cellSize) => (cellSize * 0.5f) - (step * Tread(cellSize));

        /// <summary>Height of the tread surface of step <paramref name="step"/> above the cell's base.</summary>
        /// <param name="step">The step.</param>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Height of the tread surface of the step above the cell's base.</returns>
        public static float StepTopY(int step, float cellSize) => (step + 1) * Riser(cellSize);

        /// <summary>
        /// Slope in degrees.
        /// </summary>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>Slope in degrees.</returns>
        public static float SlopeDegrees(float cellSize) =>
            MathF.Atan2(RisePerCell(cellSize), cellSize) * 180f / MathF.PI;

        /// <summary>
        /// Whether the level above has to be open over flight cell <paramref name="index"/>: its slab would be
        /// closer than <see cref="Headroom"/> to the highest tread of that cell.
        /// </summary>
        /// <param name="index">Cell index along the flight, 0 = bottom.</param>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <returns><c>true</c> if the floor of the level above must be removed over this cell.</returns>
        public static bool NeedsOpeningAbove(int index, float cellSize)
        {
            // The slab of the floor above hangs below its walkable line, so it eats into the headroom as well.
            var highestTread = (index + 1) * RisePerCell(cellSize);
            var slab = MapDimensions.DefaultFloorThickness * cellSize;
            return highestTread + Headroom + slab > MapDimensions.LevelHeightCells * cellSize;
        }

        /// <summary>
        /// World height of the tread surface, measured along the climb axis.
        /// </summary>
        /// <param name="cellSize">Cell size in metres.</param>
        /// <param name="distanceFromStart">Distance in metres from the bottom of the flight (0 .. CellsLong * cellSize).</param>
        /// <returns>Height above the bottom floor of the flight.</returns>
        public static float TreadHeightAt(float cellSize, float distanceFromStart)
        {
            var step = Math.Clamp((int)MathF.Floor(distanceFromStart / Tread(cellSize)), 0,
                CellsLong() * StepsPerCell(cellSize) - 1);
            return (step + 1) * Riser(cellSize);
        }
    }
}