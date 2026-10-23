/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        TerrainWaterBody.cs
 * PURPOSE:     Represents a water body for terrain.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// Describes an outdoor fluid body (pond, lake, river, or hot spring) on the terrain.
    /// </summary>
    public sealed class TerrainWaterBody
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; init; } = "Pond";

        /// <summary>
        /// Gets the water level.
        /// </summary>
        /// <value>
        /// The water level.
        /// </value>
        public float WaterLevel { get; init; }

        /// <summary>
        /// Gets the minimum x.
        /// </summary>
        /// <value>
        /// The minimum x.
        /// </value>
        public int MinX { get; init; }

        /// <summary>
        /// Gets the minimum z.
        /// </summary>
        /// <value>
        /// The minimum z.
        /// </value>
        public int MinZ { get; init; }

        /// <summary>
        /// Gets the maximum x.
        /// </summary>
        /// <value>
        /// The maximum x.
        /// </value>
        public int MaxX { get; init; }

        /// <summary>
        /// Gets the maximum z.
        /// </summary>
        /// <value>
        /// The maximum z.
        /// </value>
        public int MaxZ { get; init; }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public System.Drawing.Color Color { get; init; } = System.Drawing.Color.FromArgb(170, 30, 110, 220);

        /// <summary>
        /// Gets or sets the texture identifier.
        /// </summary>
        /// <value>
        /// The texture identifier.
        /// </value>
        public int? TextureId { get; set; } = null;

        /// <summary>
        /// Gets the depth (world units) at which the water is about 95% opaque. Smaller = murkier water.
        /// Only used by untextured water.
        /// </summary>
        public float DeepDepth { get; init; } = 4f;

        /// <summary>
        /// Gets the opacity (0-255) of very shallow water, just off the shore. Plants and ground show through here.
        /// Only used by untextured water.
        /// </summary>
        public int ShallowAlpha { get; init; } = 40;

        /// <summary>
        /// Gets the opacity (0-255) of deep water. Untextured water takes its RGB from <see cref="Color"/>;
        /// its alpha now comes from <see cref="ShallowAlpha"/> / <see cref="DeepAlpha"/> and the depth.
        /// </summary>
        public int DeepAlpha { get; init; } = 170;
    }
}