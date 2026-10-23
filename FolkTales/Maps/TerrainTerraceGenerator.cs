/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        TerrainTerraceGenerator.cs
 * PURPOSE:     Quantizes continuous terrain into terraced steps enclosed by raised retaining dikes.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Transforms continuous slope areas into terraced rice field steps enclosed by raised earthen
    /// retaining bunds (borders), constructing matching fluid pools and rice crops for each tier.
    /// </summary>
    public static class TerrainTerraceGenerator
    {
        /// <summary>
        /// Default foliage plant texture used when no rice texture ID is provided (-1).
        /// </summary>
        public static readonly Color DefaultPaddyMudColor = Color.FromArgb(170, 35, 125, 80);

        /// <summary>
        /// Deforms a rectangular grid area into terraced steps with raised perimeter bunds,
        /// creates enclosed fluid bodies for each step, and populates them with rice crops.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minZ">The minimum z.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxZ">The maximum z.</param>
        /// <param name="stepHeight">Height of the step.</param>
        /// <param name="waterDepth">The water depth.</param>
        /// <param name="riceTexId">The rice tex identifier.</param>
        public static void BuildTerracedRiceFields(
            TerrainMap? map,
            int minX, int minZ,
            int maxX, int maxZ,
            float stepHeight = 1.2f,
            float waterDepth = 0.35f,
            int riceTexId = -1)
        {
            if (map == null) return;

            var rangeZ = maxZ - minZ + 1;
            var rangeX = maxX - minX + 1;
            const int tierDepth = 3; // Grid tiles per step tier

            var startHeight = map.InBounds(minX, minZ) ? map[minX, minZ] : 12.0f;

            var stepAlongZ = rangeZ >= rangeX;
            var numTiers = stepAlongZ
                ? (int)MathF.Ceiling((float)rangeZ / tierDepth)
                : (int)MathF.Ceiling((float)rangeX / tierDepth);

            for (var tier = 0; tier < numTiers; tier++)
            {
                var tMinX = stepAlongZ ? minX : minX + tier * tierDepth;
                var tMaxX = stepAlongZ ? maxX : Math.Min(minX + (tier + 1) * tierDepth - 1, maxX);
                var tMinZ = stepAlongZ ? minZ + tier * tierDepth : minZ;
                var tMaxZ = stepAlongZ ? Math.Min(minZ + (tier + 1) * tierDepth - 1, maxZ) : maxZ;

                // Ensure tier spans at least 2 tiles so inner basin exists
                if (tMaxX - tMinX < 2 || tMaxZ - tMinZ < 2) continue;

                // Tier elevation profile
                var tierWaterLevel = startHeight + tier * stepHeight;
                var bedHeight = tierWaterLevel - waterDepth; // Sunken paddy basin floor
                var borderHeight = tierWaterLevel + 0.35f; // Raised retaining bund (border)

                // 1. Sculpt heightmap: Raised retaining dike outer ring + sunken interior paddy basin
                for (var x = tMinX; x <= tMaxX; x++)
                {
                    for (var z = tMinZ; z <= tMaxZ; z++)
                    {
                        if (!map.InBounds(x, z)) continue;

                        var isOuterBorder = (x == tMinX || x == tMaxX || z == tMinZ || z == tMaxZ);

                        if (isOuterBorder)
                        {
                            // Raised earthen border dike retaining wall
                            map[x, z] = borderHeight;
                        }
                        else
                        {
                            // Sunken paddy basin floor
                            map[x, z] = bedHeight;
                        }
                    }
                }

                // 2. Add water body enclosed strictly within the inner basin (tMinX..tMaxX-1)
                var terraceWater = new TerrainWaterBody
                {
                    Name = $"Terrace Paddy Tier {tier}",
                    WaterLevel = tierWaterLevel,
                    MinX = tMinX,
                    MinZ = tMinZ,
                    MaxX = tMaxX - 1,
                    MaxZ = tMaxZ - 1,
                    Color = DefaultPaddyMudColor
                };

                map.AddWaterBody(terraceWater);

                // 3. Scatter rice stalk clusters strictly inside the sunken paddy basin floor
                var waterBuilder = new WaterSceneryBuilder(map);
                waterBuilder.TargetWaterBody(terraceWater)
                    .WithDepthRange(0.05f, 0.60f)
                    .WithMinSpacing(0.5f);

                // World bounds inset by 1.0 grid tile from the retaining dike walls
                var minWorld = new Vector2((tMinX + 1.0f) * map.Scale, (tMinZ + 1.0f) * map.Scale);
                var maxWorld = new Vector2((tMaxX - 1.0f) * map.Scale, (tMaxZ - 1.0f) * map.Scale);

                waterBuilder.ScatterInArea(
                    minWorld,
                    maxWorld,
                    density: 0.40f,
                    distribution: new WaterPlantDistribution
                    {
                        WaterLilyWeight = 0.0f,
                        LotusWeight = 0.10f,
                        SeagrassWeight = 0.90f // Represents rice stalk clusters
                    });

                waterBuilder.Commit();
            }

            map.RecalculateMaxHeight();
            if (map.ChunkSize > 0)
            {
                map.GenerateChunks(map.ChunkSize);
            }
        }
    }
}