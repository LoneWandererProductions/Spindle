/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        PaperDoll.cs
 * PURPOSE:     Paper doll character representation for the 3D dungeon view.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales
{
    /// <summary>
    /// Represents a paper doll character.
    /// </summary>
    public class PaperDoll
    {
        /// <summary>
        /// Gets or sets the World position.
        /// </summary>
        /// <value>
        /// The World position.
        /// </value>
        public Vector3 Position { get; init; }

        /// <summary>
        /// Gets or sets the 2D sprite texture id.
        /// </summary>
        /// <value>
        /// The 2D sprite texture id.
        /// </value>
        public int Sprite { get; init; }

        /// <summary>
        /// Approximate world radius for frustum culling.
        /// Adjust if sprites are very wide/tall.
        /// </summary>
        public float Radius { get; init; } = 0.5f;
    }
}