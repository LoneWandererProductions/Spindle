/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        WaterPlantDistribution.cs
 * PURPOSE:     Struct that handles the distribution of water plants for random scatter routines.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// Helper struct to define plant distribution ratios for random scatter routines.
    /// Values are normalized automatically.
    /// </summary>
    public struct WaterPlantDistribution
    {
        /// <summary>
        /// Gets or sets the water lily weight.
        /// </summary>
        /// <value>
        /// The water lily weight.
        /// </value>
        public float WaterLilyWeight { get; init; } = 0.5f;

        /// <summary>
        /// Gets or sets the high water lily weight.
        /// </summary>
        /// <value>
        /// The high water lily weight.
        /// </value>
        public float HighWaterLilyWeight { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the lotus weight.
        /// </summary>
        /// <value>
        /// The lotus weight.
        /// </value>
        public float LotusWeight { get; init; } = 0.2f;

        /// <summary>
        /// Gets or sets the seagrass weight.
        /// </summary>
        /// <value>
        /// The seagrass weight.
        /// </value>
        public float SeagrassWeight { get; init; } = 0.3f;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterPlantDistribution"/> struct.
        /// </summary>
        public WaterPlantDistribution()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterPlantDistribution" /> struct.
        /// </summary>
        /// <param name="lily">The lily.</param>
        /// <param name="lotus">The lotus.</param>
        /// <param name="grass">The grass.</param>
        /// <param name="highLily">The high lily.</param>
        public WaterPlantDistribution(float lily, float lotus, float grass, float highLily)
        {
            WaterLilyWeight = lily;
            HighWaterLilyWeight = highLily;
            LotusWeight = lotus;
            SeagrassWeight = grass;
        }
    }
}