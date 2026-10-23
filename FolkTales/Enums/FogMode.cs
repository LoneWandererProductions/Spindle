/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        FogMode.cs
 * PURPOSE:     Which of the two fog features a dungeon viewport has switched on. They are independent.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// The two fog features of a dungeon viewport. They are separate switches, not one package:
    /// <list type="bullet">
    /// <item><see cref="Exploration"/>: Dungeon Keeper style. Cells never seen are black; once seen they stay drawn.</item>
    /// <item><see cref="LineOfSight"/>: the field-of-view limit. Only cells in sight right now are lit, the rest is dimmed.</item>
    /// </list>
    /// </summary>
    [Flags]
    public enum FogMode
    {
        /// <summary>Everything is drawn lit, nothing is tracked.</summary>
        None = 0,

        /// <summary>Unexplored cells are black, explored cells stay fully visible (no dimming).</summary>
        Exploration = 1,

        /// <summary>Cells outside the current line of sight are dimmed, explored or not.</summary>
        LineOfSight = 2,

        /// <summary>Both: unexplored is black, explored but out of sight is dimmed, seen right now is lit.</summary>
        Both = Exploration | LineOfSight
    }
}