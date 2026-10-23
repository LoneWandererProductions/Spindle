/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        CellShape.cs
 * PURPOSE:     Footprint of a cell's interior: square (default) or a round chamber (burrow, cave bubble, well room).
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Footprint of the room inside one <see cref="Maps.MapCell3D"/>. Walls (<see cref="WallType"/>) stay on the four
    /// edges; the shape only decides what is drawn and walked INSIDE the cell. Edges set to <see cref="WallType.None"/>,
    /// <see cref="WallType.DoorOpen"/> or <see cref="WallType.Crawl"/> become mouths in a round cell.
    /// </summary>
    public enum CellShape
    {
        /// <summary>
        /// The whole cell square is the room (default).
        /// </summary>
        Square,

        /// <summary>
        /// A circular chamber inscribed in the cell, with a straight tunnel from each open edge to the chamber.
        /// </summary>
        Round,

        /// <summary>
        /// A straight tunnel with a rounded (arched) cross section running between the two open edges of one axis.
        /// If either end is a <see cref="WallType.Crawl"/> wall the tunnel is the low, narrow crawl size.
        /// Needs open edges on ONE axis only (north and/or south, or east and/or west); bends and junctions use
        /// <see cref="Round"/> cells. With no usable axis the cell is drawn and walked as <see cref="Square"/>.
        /// </summary>
        Tunnel
    }
}