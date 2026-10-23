/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        WallType.cs
 * PURPOSE:     Different type of walls
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Type of walls handles Doors/Portals
    /// </summary>
    public enum WallType
    {
        /// <summary>
        /// The none
        ///  Open space / street Alleys
        /// </summary>
        None,

        /// <summary>
        /// The solid
        ///  Standard concrete / stone brick walls
        /// </summary>
        Solid,

        /// <summary>
        /// The door closed
        /// Interactive barrier (Blocks movement and sight)
        /// </summary>
        DoorClosed,

        /// <summary>
        /// The door open
        /// Walkable portal frame (Allows passage)
        /// </summary>
        DoorOpen,

        /// <summary>
        /// The window
        /// </summary>
        Window,

        /// <summary>
        /// A wall with a low, narrow hole at the bottom (cave mouth, fox hole, burrow entrance).
        /// A standing body is blocked like by <see cref="Solid"/>; a crawling body fits through the hole.
        /// It also blocks light, and sight of anyone who is standing. Appended LAST so stored ordinals of the older values stay valid.
        /// </summary>
        Crawl
    }
}