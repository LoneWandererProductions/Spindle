/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        FogRules.cs
 * PURPOSE:     The one place that decides how a cell looks for a given FogMode.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Turns the stored fog of a cell (what the player has seen: <see cref="FogState"/>) into what is shown
    /// (what the active <see cref="FogMode"/> makes of it). Renderers, smooth shading, scenery and actors all ask
    /// here, so they cannot disagree about it.
    /// </summary>
    public static class FogRules
    {
        /// <summary>
        /// Brightness of a cell that is shown but out of sight.
        /// </summary>
        public const float ShroudBrightness = 0.35f;

        /// <summary>
        /// Resolves a stored fog state for a mode.
        /// <list type="bullet">
        /// <item>None: always <see cref="FogState.Visible"/>.</item>
        /// <item>Exploration only: unexplored stays hidden; explored and visible are both fully shown.</item>
        /// <item>LineOfSight only: only visible is lit; everything else is dimmed (nothing is hidden for good).</item>
        /// <item>Both: the stored state as it is.</item>
        /// </list>
        /// </summary>
        /// <param name="stored">What the visibility pass stored for the cell.</param>
        /// <param name="mode">The active fog mode.</param>
        /// <returns>What to show.</returns>
        public static FogState Resolve(FogState stored, FogMode mode)
        {
            if (mode == FogMode.None) return FogState.Visible;

            var exploration = (mode & FogMode.Exploration) != 0;
            var lineOfSight = (mode & FogMode.LineOfSight) != 0;

            switch (stored)
            {
                case FogState.Visible:
                    return FogState.Visible;

                case FogState.Explored:
                    return lineOfSight ? FogState.Explored : FogState.Visible;

                default:
                    // Never seen: black with exploration, otherwise just "not in sight" (dimmed).
                    return exploration ? FogState.Unexplored : FogState.Explored;
            }
        }

        /// <summary>
        /// Brightness multiplier of a resolved state.
        /// </summary>
        /// <param name="shown">A state returned by <see cref="Resolve"/>.</param>
        /// <returns>1 for lit, <see cref="ShroudBrightness"/> for dimmed.</returns>
        public static float Brightness(FogState shown) => shown == FogState.Explored ? ShroudBrightness : 1f;

        /// <summary>
        /// Reads a cell of the map and resolves it in one step.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="x">Grid x.</param>
        /// <param name="z">Grid z.</param>
        /// <param name="level">Level.</param>
        /// <param name="mode">The active fog mode.</param>
        /// <returns>What to show for that cell.</returns>
        public static FogState ShownAt(DungeonMap map, int x, int z, int level, FogMode mode) =>
            mode == FogMode.None ? FogState.Visible : Resolve(map.GetFogAt(x, z, level), mode);
    }
}