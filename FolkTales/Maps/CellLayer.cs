/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        CellLayer.cs
 * PURPOSE:     Class representing a single decorative or functional layer within a map cell, such as water, grass, lava, fog, etc.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;
using Imaging.Objects;

namespace FolkTales.Maps
{
    /// <summary>
    /// Represents a decorative or functional layer within a map cell,
    /// such as water, grass, lava, fog, etc.
    /// </summary>
    public sealed class CellLayer
    {
        /// <summary>
        /// The texture identifier
        /// </summary>
        public int? TextureId;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; init; } = "Layer"; // e.g., "Water", "Grass"

        /// <summary>
        /// Height above the floor at which this layer sits.
        /// Example: Water surface = 0.2f if floor is 0.
        /// </summary>
        public float Height { get; init; }

        /// <summary>
        /// Simple solid color fallback if no texture is given.
        /// </summary>
        public Color? Color { get; init; }

        /// <summary>
        /// Optional mask texture for transparency or detail.
        /// </summary>
        public UnmanagedImageBuffer? Mask { get; set; }

        /// <summary>
        /// Whether this layer should alpha-blend with others.
        /// </summary>
        public bool AlphaBlend { get; init; } = true;
    }
}