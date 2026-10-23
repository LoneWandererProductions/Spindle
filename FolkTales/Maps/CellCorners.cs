/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        CellCorners.cs
 * PURPOSE:     Represents the 8 corners of a cell in a 3D grid, using unmanaged Vector3 fields to avoid heap allocations.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Unmanaged 8-corner container struct to eliminate managed Vector3[] array heap allocations per cell.
    /// </summary>
    public struct CellCorners
    {
        /// <summary>
        /// The Vector3 fields representing the 8 corners of a cell in a 3D grid.
        /// </summary>
        public Vector3 C0, C1, C2, C3, C4, C5, C6, C7;

        /// <summary>
        /// Sets the <see cref="Vector3"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Vector3"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="Vector3"/> at the specified index.</returns>
        /// <exception cref="System.IndexOutOfRangeException"></exception>
        public Vector3 this[int index]
        {
            readonly get => index switch
            {
                0 => C0,
                1 => C1,
                2 => C2,
                3 => C3,
                4 => C4,
                5 => C5,
                6 => C6,
                7 => C7,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (index)
                {
                    case 0:
                        C0 = value;
                        break;
                    case 1:
                        C1 = value;
                        break;
                    case 2:
                        C2 = value;
                        break;
                    case 3:
                        C3 = value;
                        break;
                    case 4:
                        C4 = value;
                        break;
                    case 5:
                        C5 = value;
                        break;
                    case 6:
                        C6 = value;
                        break;
                    case 7:
                        C7 = value;
                        break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}