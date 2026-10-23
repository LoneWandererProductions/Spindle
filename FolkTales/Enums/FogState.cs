/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        FogState.cs
 * PURPOSE:     States of fog-of-war for a grid cell in a tactical map.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// States of fog-of-war for a grid cell in a tactical map.
    /// </summary>
    public enum FogState : byte
    {
        Unexplored = 0, // Pitch black / geometry hidden
        Explored = 1, // Memory shroud (dimmed, no active actors visible)
        Visible = 2 // Fully lit in line-of-sight
    }
}