/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapConstants.cs
 * PURPOSE:     Some Consts for the Map System.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// Shared Constants for the Map System.
    /// </summary>
    public static class MapConstants
    {
        /// <summary>
        /// Small upward tolerance added to a FEET height before it is turned into a level, so a body standing
        /// exactly on a floor (or a hair below it through float noise) is never counted as the level beneath.
        /// </summary>
        public const float LevelFeetTolerance = 0.05f;

        /// <summary>
        /// The one definition of "which level is this height on".
        /// Every map, collision, renderer and viewport goes through this, so they cannot disagree.
        /// </summary>
        /// <param name="feetY">World Y of the FEET (or of a scenery base). Never pass eye height.</param>
        /// <param name="levelHeight">World height of one level (<c>BaseHeight * scale</c>).</param>
        /// <returns>The unclamped level index.</returns>
        public static int LevelFromHeight(float feetY, float levelHeight) =>
            (int)MathF.Floor((feetY + LevelFeetTolerance) / levelHeight);
    }
}