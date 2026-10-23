/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        Direction.cs
 * PURPOSE:     Enum for cardinal directions (North, South, East, West) used for wall lookups and map navigation.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Cardinal directions for wall lookup.
    /// </summary>
    public enum Direction
    {
        /// <summary>North (negative Y direction).</summary>
        North,

        /// <summary>South (positive Y direction).</summary>
        South,

        /// <summary>East (positive X direction).</summary>
        East,

        /// <summary>West (negative X direction).</summary>
        West
    }
}