/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        CommandType.cs
 * PURPOSE:     Enum for selecting render command types for the terrain viewport, either Triangle for the 3D mesh or PaperDoll for actors.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Selection of render command types for the terrain viewport, either Triangle for the 3D mesh or PaperDoll for actors.
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// The triangle
        /// </summary>
        Triangle,

        /// <summary>
        /// The paper doll
        /// </summary>
        PaperDoll
    }
}