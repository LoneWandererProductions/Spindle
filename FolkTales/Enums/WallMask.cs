/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        WallMask.cs
 * PURPOSE:     Values for wall presence in a grid-based environment, allowing for bitwise combination of wall directions (North, South, West, East) and an 'All' value representing walls in all directions.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// WallMask is an enumeration that represents the presence of walls in a grid-based environment.
    /// Each value corresponds to a specific wall direction (North, South, West, East) and can be combined using bitwise operations to represent multiple walls.
    /// The 'All' value represents the presence of walls in all four directions.
    /// </summary>
    [Flags]
    public enum WallMask : byte
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        North = 1 << 0,

        South = 1 << 1,

        /// <summary>
        /// The west
        /// </summary>
        West = 1 << 2,

        /// <summary>
        /// The east
        /// </summary>
        East = 1 << 3,

        All = North | South | West | East
    }
}