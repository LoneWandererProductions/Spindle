/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        DoorStyle.cs
 * PURPOSE:     How a door wall state (WallType.DoorClosed / DoorOpen) of a map cell is built. Prison bars are NOT a door style: they are props placed by hand.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Look of the door a cell draws for <see cref="WallType.DoorClosed"/> / <see cref="WallType.DoorOpen"/>.
    /// Stored per cell (<c>MapCell3D.DoorStyle</c>) and set on both cells of a doorway by the builders.
    /// </summary>
    public enum DoorStyle
    {
        /// <summary>
        /// Plank door with iron straps and a ring handle (<c>DoorFactory.BakeWoodDoor</c>).
        /// </summary>
        Wood = 0
    }
}