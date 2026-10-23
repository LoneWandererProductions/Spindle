/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        CausewayBuilder.cs
 * PURPOSE:     Generates basalt column formations on local terrain and handles shoreline transitions.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Drawing;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Generates Giant's Causeway basalt formations by deforming local terrain elevation into
    /// blocky hexagonal stepped terraces and snapping dense hexagonal basalt column props.
    /// </summary>
    public class CausewayBuilder
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly TerrainMap? _map;

        /// <summary>
        /// Default basalt rock face color used when no texture sheet ID is provided.
        /// </summary>
        public static readonly Color DefaultBasaltColor = Color.FromArgb(255, 48, 52, 58);

        /// <summary>
        /// Initializes a new instance of the <see cref="CausewayBuilder"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        public CausewayBuilder(TerrainMap? map)
        {
            _map = map;
        }

        /// <summary>
        /// Builds an expansive hexagonal basalt column cluster formation anchored to local ground height.
        /// </summary>
        /// <param name="centerWorld">Center point of the causeway in world coordinates.</param>
        /// <param name="radius">Total radius of the formation in meters (e.g. 28m).</param>
        /// <param name="columnWidth">Diameter of each hexagon column (e.g. 1.1m).</param>
        /// <param name="peakHeightOffset">Height added above terrain at the center peak (e.g. 6.0m).</param>
        /// <param name="waterLevel">Water level at shoreline transition.</param>
        /// <param name="basaltTexId">Primary texture sheet ID for basalt rock faces (-1 for color fallback).</param>
        public void BuildCauseway(
            Vector2 centerWorld,
            float radius = 28.0f,
            float columnWidth = 1.1f,
            float peakHeightOffset = 6.0f,
            float waterLevel = 12.0f,
            int basaltTexId = -1)
        {
            if (_map == null) return;

            var rand = new Random(1337);

            // Axial hex grid spacing formulas
            var xSpacing = columnWidth * 0.866025f; // sqrt(3)/2
            var zSpacing = columnWidth * 0.75f;

            var rings = (int)MathF.Ceiling(radius / columnWidth);

            // Resolve texture or fallback color index
            var activeTexId = basaltTexId >= 0
                ? basaltTexId
                : _map.Textures.Get(TextureSet.Slot.Basalt, MapConstants.DefaultBasaltTexId);

            for (var q = -rings; q <= rings; q++)
            {
                for (var r = -rings; r <= rings; r++)
                {
                    // Axial hex to continuous 2D world coordinates
                    var worldX = centerWorld.X + (q * xSpacing + (r & 1) * (xSpacing * 0.5f));
                    var worldZ = centerWorld.Y + (r * zSpacing);

                    var distFromCenter = Vector2.Distance(centerWorld, new Vector2(worldX, worldZ));
                    if (distFromCenter > radius) continue;

                    // Query existing terrain height at column center
                    var localGroundY = _map.GetHeightAt(worldX, worldZ);

                    // Radial step falloff from center peak down to base shoreline
                    var falloff = MathF.Pow(1.0f - (distFromCenter / radius), 1.2f);

                    // Calculate column target top height anchored ON TOP of local ground
                    var targetCapY = MathF.Max(localGroundY + (peakHeightOffset * falloff), waterLevel - 0.5f);

                    // Quantize column cap into 1.2m vertical stepped tiers
                    var quantStep = 1.2f;
                    var steppedCapY = MathF.Floor(targetCapY / quantStep) * quantStep;
                    var stepJitter = ((float)rand.NextDouble() - 0.5f) * 0.25f;
                    var finalColumnCapY = steppedCapY + stepJitter;

                    // 1. Sculpt underlying heightmap cell so terrain bed matches column cap seat
                    var gridX = (int)MathF.Floor(worldX / _map.Scale);
                    var gridZ = (int)MathF.Floor(worldZ / _map.Scale);

                    if (_map.InBounds(gridX, gridZ))
                    {
                        _map[gridX, gridZ] = MathF.Max(_map[gridX, gridZ], finalColumnCapY - 0.2f);
                    }

                    // 2. Add basalt column prop to spatial scenery registry
                    var pos = new Vector3(worldX, finalColumnCapY, worldZ);

                    _map.AddScenery(
                        position: pos,
                        seed: rand.Next(),
                        type: SceneryType.BasaltColumn,
                        primaryTexId: activeTexId);
                }
            }

            _map.RecalculateMaxHeight();
            if (_map.ChunkSize > 0)
            {
                _map.GenerateChunks(_map.ChunkSize);
            }
        }
    }
}