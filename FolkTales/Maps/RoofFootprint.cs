/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     GlRender.Core
 * FILE:        RoofFootprint.cs
 * PURPOSE:     Derives a roof's footprint, wall-top height and ridge rise from the cells underneath it.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */


namespace FolkTales.Maps
{
    /// <summary>
    /// The footprint of the building a roof sits on, derived from the map cells themselves instead of constants that
    /// have to match whatever the house builder happened to create. The roof is always as wide and deep as the connected
    /// ceilinged cells under it and starts exactly at the ceiling height of those cells.
    /// </summary>
    /// <param name="CenterX">World X of the footprint centre.</param>
    /// <param name="CenterZ">World Z of the footprint centre.</param>
    /// <param name="Width">Footprint extent along X in world units.</param>
    /// <param name="Depth">Footprint extent along Z in world units.</param>
    /// <param name="WallTopY">World height of the ceiling, which is where the roof starts.</param>
    /// <param name="Level">The level the footprint was found on.</param>
    public readonly record struct RoofFootprint(
        float CenterX,
        float CenterZ,
        float Width,
        float Depth,
        float WallTopY,
        int Level)

    {
        /// <summary>
        /// Ridge rise as a fraction of the full footprint width. 8/15 reproduces the original 6-cell house exactly
        /// (12.8 rise over a 24 wide footprint, roughly a 47 degree slope on each side).
        /// </summary>
        public const float RiseToWidth = 8f / 15f;

        /// <summary>
        /// The widest building (in cells, in either direction) that is searched for.
        /// </summary>
        public const int MaxSpanCells = 32;


        /// <summary>
        /// Gets the height of the ridge above the wall top.
        /// </summary>

        public float Rise => Width * RiseToWidth;


        /// <summary>
        /// World height of the ceiling of the given cell.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="scale">The cell scale.</param>
        /// <returns>The ceiling height in world units.</returns>
        public static float CeilingWorldY(MapCell3D cell, float scale)

        {
            var zOffset = cell.LevelBaseHeight;

            return (cell.CeilingHeight + zOffset) * scale;
        }


        /// <summary>
        /// Resolves the footprint of the building that contains the given anchor cell.
        /// </summary>
        /// <param name="getCell">Looks up a cell by grid x, grid z and level.</param>
        /// <param name="levels">Number of levels in the map (level 0 is the ground, never roofed).</param>
        /// <param name="gx">Grid x of the cell under the roof anchor.</param>
        /// <param name="gz">Grid z of the cell under the roof anchor.</param>
        /// <param name="scale">The cell scale.</param>
        /// <param name="footprint">The resolved footprint.</param>
        /// <returns><c>true</c> if a ceilinged cell was found under the anchor; otherwise <c>false</c>.</returns>
        public static bool TryResolve(
            Func<int, int, int, MapCell3D?> getCell,
            int levels,
            int gx,
            int gz,
            float scale,
            out RoofFootprint footprint)

        {
            footprint = default;


            // The roof belongs to the highest ceiling found above the anchor cell.

            var level = -1;

            var wallTopY = 0f;

            for (var lvl = 1; lvl < levels; lvl++)

            {
                var cell = getCell(gx, gz, lvl);

                if (cell is not { IsEmpty: false, HasCeiling: true }) continue;

                var ceilingY = CeilingWorldY(cell, scale);

                if (ceilingY <= wallTopY) continue;

                wallTopY = ceilingY;
                level = lvl;
            }


            if (level < 0) return false;

            // Flood fill over the connected ceilinged cells of that level to get the bounding box.

            var minX = gx;
            var maxX = gx;
            var minZ = gz;
            var maxZ = gz;

            var visited = new HashSet<(int X, int Z)> { (gx, gz) };
            var open = new Stack<(int X, int Z)>();

            open.Push((gx, gz));

            while (open.Count > 0)

            {
                var (x, z) = open.Pop();

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);


                TryVisit(x + 1, z);
                TryVisit(x - 1, z);
                TryVisit(x, z + 1);
                TryVisit(x, z - 1);
            }

            var cellsX = maxX - minX + 1;
            var cellsZ = maxZ - minZ + 1;

            footprint = new RoofFootprint(
                (minX + maxX + 1) * 0.5f * scale,
                (minZ + maxZ + 1) * 0.5f * scale,
                cellsX * scale,
                cellsZ * scale,
                wallTopY,
                level);

            return true;

            void TryVisit(int x, int z)
            {
                if (Math.Abs(x - gx) > MaxSpanCells || Math.Abs(z - gz) > MaxSpanCells) return;
                if (!visited.Add((x, z))) return;

                var cell = getCell(x, z, level);

                if (cell is { IsEmpty: false, HasCeiling: true }) open.Push((x, z));
            }
        }
    }
}