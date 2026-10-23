/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        WaterSceneryBuilder.cs
 * PURPOSE:     Helper tool for automatically populating water bodies with natural clusters of water foliage.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// A fluent level-design builder combining precise manual placement
    /// with procedural random scatter and density controls.
    /// </summary>
    public class WaterSceneryBuilder
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly TerrainMap? _map;

        /// <summary>
        /// The queue
        /// </summary>
        private readonly List<PendingPlacement> _queue = new();

        /// <summary>
        /// The active water body
        /// </summary>
        private TerrainWaterBody? _activeWaterBody;

        /// <summary>
        /// The minimum water depth
        /// </summary>
        private float _minWaterDepth = 0.15f;

        /// <summary>
        /// The maximum water depth
        /// </summary>
        private float _maxWaterDepth = 10.0f;

        /// <summary>
        /// The minimum property distance
        /// Minimum distance buffer to prevent clipping
        /// </summary>
        private float _minPropDistance = 0.35f;

        /// <summary>
        /// The current seed
        /// </summary>
        private int _currentSeed = 1337;

        /// <summary>
        /// Internal struct to hold pending placement data before committing to the map.
        /// </summary>
        private struct PendingPlacement
        {
            public SceneryType Type;
            public Vector3 Position;
            public int Seed;
            public int PrimaryTex;
            public int SecondaryTex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaterSceneryBuilder"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        public WaterSceneryBuilder(TerrainMap? map)
        {
            _map = map;
        }

        // --- CONFIGURATION CHAINING ---

        /// <summary>
        /// Binds a specific water body for surface level calculation and full-body scattering.
        /// </summary>
        /// <param name="water">The water.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder TargetWaterBody(TerrainWaterBody water)
        {
            _activeWaterBody = water;
            return this;
        }

        /// <summary>
        /// Enforces depth boundaries. Props will only spawn where water depth is within this range.
        /// </summary>
        /// <param name="minDepth">The minimum depth.</param>
        /// <param name="maxDepth">The maximum depth.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder WithDepthRange(float minDepth, float maxDepth)
        {
            _minWaterDepth = minDepth;
            _maxWaterDepth = maxDepth;
            return this;
        }

        /// <summary>
        /// Sets a minimum distance buffer between placed props to prevent overlapping meshes.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder WithMinSpacing(float distance)
        {
            _minPropDistance = distance;
            return this;
        }

        /// <summary>
        /// Sets the RNG seed for deterministic variation.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <returns>WaterScenery object.</returns>/returns>
        public WaterSceneryBuilder WithSeed(int seed)
        {
            _currentSeed = seed;
            return this;
        }

        // --- MANUAL & PRECISION PLACEMENT ---

        /// <summary>
        /// Manually places a single prop at exact X/Z coordinates.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="xzPos">The xz position.</param>
        /// <param name="overrideSurfaceY">The override surface y.</param>
        /// <param name="primaryTex">The primary tex.</param>
        /// <param name="secondaryTex">The secondary tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder PlaceExact(
            SceneryType type,
            Vector2 xzPos,
            float? overrideSurfaceY = null,
            int primaryTex = -1,
            int secondaryTex = -1)
        {
            if (_map == null) return this;

            var worldX = xzPos.X;
            var worldZ = xzPos.Y;
            var seabedY = _map.GetHeightAt(worldX, worldZ);

            var waterSurfaceY = overrideSurfaceY
                                ?? _activeWaterBody?.WaterLevel
                                ?? seabedY + 1.0f;

            var depth = waterSurfaceY - seabedY;
            if (depth < _minWaterDepth || depth > _maxWaterDepth)
            {
                return this;
            }

            // Root elevated lilies at the seabed so stems start at the bottom
            var isSeabedProp = type is SceneryType.Seagrass or SceneryType.ElevatedWaterLily;
            var finalY = isSeabedProp ? seabedY : waterSurfaceY;
            var finalPos = new Vector3(worldX, finalY, worldZ);

            if (!IsTooCloseToPending(finalPos))
            {
                _queue.Add(new PendingPlacement
                {
                    Type = type,
                    Position = finalPos,
                    Seed = _currentSeed++,
                    PrimaryTex = primaryTex,
                    SecondaryTex = secondaryTex
                });
            }

            return this;
        }

        /// <summary>
        /// Adds the lily pad.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="padTex">The pad tex.</param>
        /// <param name="flowerTex">The flower tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder AddLilyPad(Vector2 position, int padTex = -1, int flowerTex = -1) =>
            PlaceExact(SceneryType.WaterLily, position, primaryTex: padTex, secondaryTex: flowerTex);

        /// <summary>
        /// Adds the high lily pad.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="padTex">The pad tex.</param>
        /// <param name="flowerTex">The flower tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder AddHighLilyPad(Vector2 position, int padTex = -1, int flowerTex = -1) =>
            PlaceExact(SceneryType.ElevatedWaterLily, position, primaryTex: padTex, secondaryTex: flowerTex);

        /// <summary>
        /// Adds the lotus.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="petalTex">The petal tex.</param>
        /// <param name="stemTex">The stem tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder AddLotus(Vector2 position, int petalTex = -1, int stemTex = -1) =>
            PlaceExact(SceneryType.Lotus, position, primaryTex: petalTex, secondaryTex: stemTex);

        /// <summary>
        /// Adds the seagrass.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="bladeTex">The blade tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder AddSeagrass(Vector2 position, int bladeTex = -1) =>
            PlaceExact(SceneryType.Seagrass, position, primaryTex: bladeTex);

        /// <summary>
        /// Places the cluster.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="count">The count.</param>
        /// <param name="primaryTex">The primary tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder PlaceCluster(
            SceneryType type,
            Vector2 center,
            float radius,
            int count,
            int primaryTex = -1)
        {
            var rand = new Random(_currentSeed);

            for (var i = 0; i < count; i++)
            {
                var angle = (float)(rand.NextDouble() * Math.PI * 2.0);
                var dist = (float)Math.Sqrt(rand.NextDouble()) * radius;
                var offset = new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);

                PlaceExact(type, center + offset, primaryTex: primaryTex);
            }

            return this;
        }

        /// <summary>
        /// Places the line.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="stepCount">The step count.</param>
        /// <param name="jitterRadius">The jitter radius.</param>
        /// <param name="primaryTex">The primary tex.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder PlaceLine(
            SceneryType type,
            Vector2 start,
            Vector2 end,
            int stepCount,
            float jitterRadius = 0.2f,
            int primaryTex = -1)
        {
            var rand = new Random(_currentSeed);

            for (var i = 0; i <= stepCount; i++)
            {
                var t = (float)i / stepCount;
                var basePos = Vector2.Lerp(start, end, t);

                var jitter = new Vector2(
                    ((float)rand.NextDouble() - 0.5f) * jitterRadius * 2f,
                    ((float)rand.NextDouble() - 0.5f) * jitterRadius * 2f
                );

                PlaceExact(type, basePos + jitter, primaryTex: primaryTex);
            }

            return this;
        }

        // --- RANDOM PROCEDURAL SCATTER ROUTINES ---

        /// <summary>
        /// Scatters random foliage across the entire targeted water body using defined density and plant ratios.
        /// </summary>
        /// <param name="density">The density.</param>
        /// <param name="distribution">The distribution.</param>
        /// <returns>WaterScenery object.</returns>
        public WaterSceneryBuilder ScatterInWaterBody(
            float density = 0.3f,
            WaterPlantDistribution? distribution = null)
        {
            if (_activeWaterBody == null || _map == null) return this;

            var min = new Vector2(_activeWaterBody.MinX * _map.Scale, _activeWaterBody.MinZ * _map.Scale);
            var max = new Vector2(_activeWaterBody.MaxX * _map.Scale, _activeWaterBody.MaxZ * _map.Scale);

            return ScatterInArea(min, max, density, distribution);
        }

        /// <summary>
        /// Scatters random foliage within a specific rectangular world bounding box.
        /// </summary>
        public WaterSceneryBuilder ScatterInArea(
            Vector2 minWorldPos,
            Vector2 maxWorldPos,
            float density = 0.3f,
            WaterPlantDistribution? distribution = null)
        {
            var dist = distribution ?? new WaterPlantDistribution();
            var rand = new Random(_currentSeed);

            var areaWidth = maxWorldPos.X - minWorldPos.X;
            var areaDepth = maxWorldPos.Y - minWorldPos.Y;
            var totalArea = areaWidth * areaDepth;

            // Calculate total attempts based on area and density factor
            var totalAttempts = (int)(totalArea * density * 0.5f);

            for (var i = 0; i < totalAttempts; i++)
            {
                var rx = minWorldPos.X + (float)rand.NextDouble() * areaWidth;
                var rz = minWorldPos.Y + (float)rand.NextDouble() * areaDepth;
                var targetPos = new Vector2(rx, rz);

                var plantType = SelectRandomPlantType(rand, dist);
                PlaceExact(plantType, targetPos);
            }

            return this;
        }

        /// <summary>
        /// Scatters random foliage within a circular region around a center coordinate.
        /// </summary>
        public WaterSceneryBuilder ScatterInRadius(
            Vector2 center,
            float radius,
            float density = 0.3f,
            WaterPlantDistribution? distribution = null)
        {
            var dist = distribution ?? new WaterPlantDistribution();
            var rand = new Random(_currentSeed);

            var circleArea = MathF.PI * radius * radius;
            var totalAttempts = (int)(circleArea * density * 0.5f);

            for (var i = 0; i < totalAttempts; i++)
            {
                var angle = (float)(rand.NextDouble() * Math.PI * 2.0);
                var r = (float)Math.Sqrt(rand.NextDouble()) * radius;
                var targetPos = center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);

                var plantType = SelectRandomPlantType(rand, dist);
                PlaceExact(plantType, targetPos);
            }

            return this;
        }

        // --- EXECUTION & HELPER ROUTINES ---

        /// <summary>
        /// Commits all queued manual and randomized placements directly to the TerrainMap.
        /// </summary>
        public void Commit()
        {
            if (_map == null) return;

            // Build-time overdraw check: silent unless the queued props would be too dense to render smoothly.
            var placements = new List<(SceneryType Type, Vector3 Position)>(_queue.Count);
            foreach (var item in _queue)
            {
                placements.Add((item.Type, item.Position));
            }

            BlendedSceneryOverdrawGuard.Analyze(nameof(WaterSceneryBuilder), placements);

            foreach (var item in _queue)
            {
                _map.AddScenery(
                    item.Position,
                    item.Seed,
                    item.Type,
                    item.PrimaryTex,
                    item.SecondaryTex);
            }

            _queue.Clear();
        }

        /// <summary>
        /// Selects the random type of the plant.
        /// </summary>
        /// <param name="rand">The rand.</param>
        /// <param name="dist">The dist.</param>
        /// <returns>Random scenery type.</returns>
        private SceneryType SelectRandomPlantType(Random rand, WaterPlantDistribution dist)
        {
            var options = new (SceneryType Type, float Weight)[]
            {
                (SceneryType.WaterLily, dist.WaterLilyWeight),
                (SceneryType.ElevatedWaterLily,
                    0f), // Hardcode to 0f to prevent random scatter from placing tall stalk lilies
                (SceneryType.Lotus, dist.LotusWeight),
                (SceneryType.Seagrass, dist.SeagrassWeight),
            };

            var totalWeight = 0f;
            foreach (var option in options)
            {
                totalWeight += option.Weight;
            }

            if (totalWeight <= 0f) return SceneryType.WaterLily;

            var roll = (float)rand.NextDouble() * totalWeight;
            var cumulative = 0f;

            foreach (var option in options)
            {
                cumulative += option.Weight;
                if (roll < cumulative)
                    return option.Type;
            }

            return options[0].Type;
        }

        /// <summary>
        /// Determines whether [is too close to pending] [the specified position].
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <returns>
        ///   <c>true</c> if [is too close to pending] [the specified position]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsTooCloseToPending(Vector3 pos)
        {
            if (_minPropDistance <= 0f) return false;

            var distSq = _minPropDistance * _minPropDistance;
            for (var i = 0; i < _queue.Count; i++)
            {
                if (Vector3.DistanceSquared(_queue[i].Position, pos) < distSq)
                {
                    return true;
                }
            }

            return false;
        }
    }
}