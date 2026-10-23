/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        LandSceneryBuilder.cs
 * PURPOSE:     Fluent builder for procedural placement of terrestrial foliage (grass, shrubs, flowers) with slope and altitude rules.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// A fluent level-design builder for scattering terrestrial foliage like high grass,
    /// shrubs, and flowers across dry terrain with slope and elevation constraints.
    /// </summary>
    public class LandSceneryBuilder
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
        /// The minimum slope angle
        /// </summary>
        private float _minSlopeAngle = 0f;

        /// <summary>
        /// The maximum slope angle
        ///  Max walkable/plantable angle in degrees
        /// </summary>
        private float _maxSlopeAngle = 35f;

        /// <summary>
        /// The minimum elevation
        /// </summary>
        private float _minElevation = float.MinValue;

        /// <summary>
        /// The maximum elevation
        /// </summary>
        private float _maxElevation = float.MaxValue;

        /// <summary>
        /// The minimum property distance
        /// </summary>
        private float _minPropDistance = 0.25f;

        /// <summary>
        /// The current seed
        /// </summary>
        private int _currentSeed = 42;

        /// <summary>
        ///Pending Placement Struct.
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
        /// Initializes a new instance of the <see cref="LandSceneryBuilder"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        public LandSceneryBuilder(TerrainMap? map)
        {
            _map = map;
        }

        // --- CONFIGURATION CHAINING ---

        /// <summary>
        /// Withes the slope range.
        /// </summary>
        /// <param name="minDegrees">The minimum degrees.</param>
        /// <param name="maxDegrees">The maximum degrees.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder WithSlopeRange(float minDegrees, float maxDegrees)
        {
            _minSlopeAngle = minDegrees;
            _maxSlopeAngle = maxDegrees;
            return this;
        }

        /// <summary>
        /// Withes the elevation range.
        /// </summary>
        /// <param name="minHeight">The minimum height.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder WithElevationRange(float minHeight, float maxHeight)
        {
            _minElevation = minHeight;
            _maxElevation = maxHeight;
            return this;
        }

        /// <summary>
        /// Withes the minimum spacing.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder WithMinSpacing(float distance)
        {
            _minPropDistance = distance;
            return this;
        }

        /// <summary>
        /// Withes the seed.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder WithSeed(int seed)
        {
            _currentSeed = seed;
            return this;
        }

        // --- PLACEMENT ROUTINES ---

        /// <summary>
        /// Places the exact.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="xzPos">The xz position.</param>
        /// <param name="primaryTex">The primary tex.</param>
        /// <param name="secondaryTex">The secondary tex.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder PlaceExact(
            SceneryType type,
            Vector2 xzPos,
            int primaryTex = -1,
            int secondaryTex = -1)
        {
            var worldX = xzPos.X;
            var worldZ = xzPos.Y;

            if (_map == null) return this;

            var groundY = _map.GetHeightAt(worldX, worldZ);

            if (groundY < _minElevation || groundY > _maxElevation) return this;

            var slope = CalculateSlopeAngle(worldX, worldZ);
            if (slope < _minSlopeAngle || slope > _maxSlopeAngle) return this;

            var finalPos = new Vector3(worldX, groundY, worldZ);

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
        /// Scatters the in area.
        /// </summary>
        /// <param name="minWorldPos">The minimum world position.</param>
        /// <param name="maxWorldPos">The maximum world position.</param>
        /// <param name="density">The density.</param>
        /// <param name="type">The type.</param>
        /// <param name="primaryTex">The primary tex.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder ScatterInArea(
            Vector2 minWorldPos,
            Vector2 maxWorldPos,
            float density = 0.5f,
            SceneryType type = SceneryType.TallGrass,
            int primaryTex = -1)
        {
            var rand = new Random(_currentSeed);

            var width = maxWorldPos.X - minWorldPos.X;
            var depth = maxWorldPos.Y - minWorldPos.Y;
            var totalArea = width * depth;

            var totalAttempts = (int)(totalArea * density * 0.5f);

            for (var i = 0; i < totalAttempts; i++)
            {
                var rx = minWorldPos.X + (float)rand.NextDouble() * width;
                var rz = minWorldPos.Y + (float)rand.NextDouble() * depth;

                PlaceExact(type, new Vector2(rx, rz), primaryTex);
            }

            return this;
        }

        /// <summary>
        /// Scatters the grass field.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="grassTex">The grass tex.</param>
        /// <param name="flowerTex">The flower tex.</param>
        /// <returns>Returns the current LandSceneryBuilder instance.</returns>
        public LandSceneryBuilder ScatterGrassField(
            Vector2 center,
            float radius,
            float density = 0.8f,
            int grassTex = -1,
            int flowerTex = -1)
        {
            var rand = new Random(_currentSeed);
            var circleArea = MathF.PI * radius * radius;
            var totalAttempts = (int)(circleArea * density * 0.6f);

            for (var i = 0; i < totalAttempts; i++)
            {
                var angle = (float)(rand.NextDouble() * Math.PI * 2.0);
                var r = (float)Math.Sqrt(rand.NextDouble()) * radius;
                var targetPos = center + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);

                // 85% Tall Grass, 15% Flowers
                var type = (rand.NextDouble() > 0.15) ? SceneryType.TallGrass : SceneryType.Flowers;
                var tex = (type == SceneryType.TallGrass) ? grassTex : flowerTex;

                PlaceExact(type, targetPos, tex);
            }

            return this;
        }

        // --- EXECUTION ---

        /// <summary>
        /// Commits this instance.
        /// </summary>
        public void Commit()
        {
            if (_map == null) return;

            foreach (var item in _queue)
            {
                _map?.AddScenery(
                    item.Position,
                    item.Seed,
                    item.Type,
                    item.PrimaryTex,
                    item.SecondaryTex);
            }

            _queue.Clear();
        }

        // --- INTERNAL HELPERS ---

        /// <summary>
        /// Calculates the slope angle.
        /// </summary>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns>Returns the slope angle in degrees.</returns>
        private float CalculateSlopeAngle(float worldX, float worldZ)
        {
            if (_map == null) return 0f;

            var delta = _map.Scale * 0.5f;

            var hL = _map.GetHeightAt(worldX - delta, worldZ);
            var hR = _map.GetHeightAt(worldX + delta, worldZ);
            var hD = _map.GetHeightAt(worldX, worldZ - delta);
            var hU = _map.GetHeightAt(worldX, worldZ + delta);

            var dhdx = (hR - hL) / (2.0f * delta);
            var dhdz = (hU - hD) / (2.0f * delta);

            var slopeGradient = MathF.Sqrt(dhdx * dhdx + dhdz * dhdz);
            return MathF.Atan(slopeGradient) * (180.0f / MathF.PI);
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
                    return true;
            }

            return false;
        }
    }
}