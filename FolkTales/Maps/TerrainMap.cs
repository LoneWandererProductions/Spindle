/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        TerrainMap.cs
 * PURPOSE:     Represents a heightmap grid for terrain rendering, now supporting spatial chunking and prop indexing.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Structs;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Holds heightmap data and partitioned spatial chunks for terrain rendering.
    /// </summary>
    public class TerrainMap
    {
        /// <summary>
        /// The height values, indexed by [x, z].
        /// </summary>
        /// <value>
        /// The heights.
        /// </value>
        public float[,] Heights { get; }

        /// <summary>
        /// Optional material index (0..3) per tile, indexed by [x, z]. <c>null</c> = no splatting
        /// (single texture / height colours as before). Allocate with <see cref="EnableMaterials"/>.
        /// </summary>
        public byte[,]? Materials { get; private set; }

        /// <summary>
        /// Texture ids of this map. <c>Terrain0..3</c> are the splat layers that <see cref="Materials" /> (0..3)
        /// points at. Starts as a copy of <see cref="TextureSet.Template" />.
        /// </summary>
        public TextureSet Textures { get; set; } = TextureSet.Template.Clone();

        /// <summary>
        /// Gets the start settings of this map (fog, discovery, sight, time, weather, spawn).
        /// </summary>
        public MapSettings Settings { get; }

        /// <summary>
        /// Gets all currently occluded (punched) tile coordinates.
        /// </summary>
        /// <value>
        /// The occluded tiles.
        /// </value>
        public IReadOnlyCollection<(int X, int Z)> OccludedTiles => _occludedTiles;

        /// <summary>
        /// Allocates the material grid and fills every tile with <paramref name="defaultMaterial" />.
        /// </summary>
        /// <param name="defaultMaterial">Material index 0..3.</param>
        public void EnableMaterials(byte defaultMaterial = 0)
        {
            Materials = new byte[Width, Depth];
            if (defaultMaterial == 0) return;

            for (var x = 0; x < Width; x++)
            for (var z = 0; z < Depth; z++)
                Materials[x, z] = defaultMaterial;
        }

        /// <summary>
        /// Blend weights of grid vertex (vx, vz): the share of materials 0, 1 and 2 among the tiles within
        /// <paramref name="radius" /> tiles of the vertex (material 3 is the remainder, 1 - X - Y - Z).
        /// Neighbouring quads and chunks evaluate the same vertex, so the blend is seamless.
        /// Radius 1 fades over about one tile, 2 over about two.
        /// </summary>
        /// <param name="vx">The vx.</param>
        /// <param name="vz">The vz.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>The blend weights as a Vector3.</returns>
        public Vector3 GetSplatWeights(int vx, int vz, int radius = 1)
        {
            if (Materials == null) return Vector3.Zero;

            Span<float> w = stackalloc float[4];
            var total = 0f;
            for (var dx = -radius; dx < radius; dx++)
            {
                for (var dz = -radius; dz < radius; dz++)
                {
                    var tx = Math.Clamp(vx + dx, 0, Width - 1);
                    var tz = Math.Clamp(vz + dz, 0, Depth - 1);
                    w[Math.Min(Materials[tx, tz], (byte)3)] += 1f;
                    total += 1f;
                }
            }

            return new Vector3(w[0], w[1], w[2]) / total;
        }

        /// <summary>
        /// Width (X dimension) of the map in grid steps.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public int Width { get; }

        /// <summary>
        /// Depth (Z dimension) of the map in grid steps.
        /// </summary>
        /// <value>
        /// The depth.
        /// </value>
        public int Depth { get; }

        /// <summary>
        /// Meters per grid step. Controls spacing between samples.
        /// </summary>
        /// <value>
        /// The scale.
        /// </value>
        public float Scale { get; set; }

        /// <summary>
        /// Spatial partition array holding cached bounding box definitions for the map.
        /// </summary>
        /// <value>
        /// The chunks.
        /// </value>
        public TerrainChunk[,]? Chunks { get; private set; }

        /// <summary>
        /// Gets the width of the chunk.
        /// </summary>
        /// <value>
        /// The width of the chunk.
        /// </value>
        public int ChunkWidth { get; private set; }

        /// <summary>
        /// Gets the chunk depth.
        /// </summary>
        /// <value>
        /// The chunk depth.
        /// </value>
        public int ChunkDepth { get; private set; }

        /// <summary>
        /// Gets the size of the chunk.
        /// </summary>
        /// <value>
        /// The size of the chunk.
        /// </value>
        public int ChunkSize { get; private set; }

        /// <summary>
        /// Thread-safe tracking collection containing coordinate indices of completely cut-out terrain tiles.
        /// </summary>
        private readonly HashSet<(int x, int z)> _occludedTiles = new();

        /// <summary>
        /// The cached maximum height
        /// Property backing field to avoid intensive recalculation scans
        /// </summary>
        private float _cachedMaxHeight = 0f;

        /// <summary>
        /// Gets the maximum elevation value currently recorded in the heightmap.
        /// </summary>
        public float MaxHeight => _cachedMaxHeight;

        /// <summary>
        /// The master list tracking all active decorative environmental details layout metrics.
        /// </summary>
        private readonly List<SceneryPlacement> _scenery = new();

        /// <summary>
        /// Gets the active tracking collection of placed landscape scenery components.
        /// </summary>
        public IEnumerable<SceneryPlacement> Scenery => _scenery;

        /// <summary>
        /// The scenery grid dictionary bucket cache.
        /// </summary>
        private readonly Dictionary<(int, int), List<SceneryPlacement>> _sceneryGrid = new();

        /// <summary>
        /// Initializes a new instance of <see cref="TerrainMap" />.
        /// </summary>
        /// <param name="width">Number of samples along X.</param>
        /// <param name="depth">Number of samples along Z.</param>
        /// <param name="scale">Meters per grid step.</param>
        /// <param name="fogMode">The fog mode.</param>
        /// <param name="sightRadius">The sight radius.</param>
        /// <param name="startExplored">if set to <c>true</c> [start explored].</param>
        /// <param name="timeOfDay">The time of day.</param>
        /// <param name="weather">The weather.</param>
        public TerrainMap(int width, int depth, float scale = 10f, FogMode? fogMode = null, int? sightRadius = null,
            bool startExplored = false, float? timeOfDay = null, WeatherType? weather = null)
            : this(width, depth, scale, MapSettings.From(fogMode, sightRadius, startExplored, timeOfDay, weather))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainMap"/> class from a settings object (also used when a map is loaded).
        /// </summary>
        /// <param name="width">Number of samples along X.</param>
        /// <param name="depth">Number of samples along Z.</param>
        /// <param name="scale">Meters per grid step.</param>
        /// <param name="settings">The start settings; the map keeps this instance.</param>
        public TerrainMap(int width, int depth, float scale, MapSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (width <= 1) throw new ArgumentOutOfRangeException(nameof(width), "Width must be > 1.");
            if (depth <= 1) throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be > 1.");
            if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be > 0.");

            Width = width;
            Depth = depth;
            Scale = scale;
            Heights = new float[width, depth];
        }

        /// <summary>
        /// Slices the raw heightmap into a 2D grid of TerrainChunks and calculates their AABB.
        /// Call this once after loading or generating the heightmap data.
        /// Default chunk size is 16 tiles.
        /// </summary>
        /// <param name="chunkSize">Size of the chunk.</param>
        public void GenerateChunks(int chunkSize = 16)
        {
            // Guard clause: Fallback to 16 if chunkSize is uninitialized (0 or negative)
            if (chunkSize <= 0)
            {
                chunkSize = 16;
            }

            ChunkSize = chunkSize;
            ChunkWidth = (int)Math.Ceiling((double)Width / chunkSize);
            ChunkDepth = (int)Math.Ceiling((double)Depth / chunkSize);

            Chunks = new TerrainChunk[ChunkWidth, ChunkDepth];

            for (var cx = 0; cx < ChunkWidth; cx++)
            {
                for (var cz = 0; cz < ChunkDepth; cz++)
                {
                    var chunk = new TerrainChunk(new Vector2(cx, cz));

                    var startX = cx * chunkSize;
                    var startZ = cz * chunkSize;

                    // Fix off-by-one: Use Width/Depth directly so the upper bounds (e.g. index 63) are sampled
                    var endX = Math.Min(startX + chunkSize, Width);
                    var endZ = Math.Min(startZ + chunkSize, Depth);

                    for (var x = startX; x < endX; x++)
                    {
                        for (var z = startZ; z < endZ; z++)
                        {
                            chunk.UpdateBounds(x, z, Scale, Heights[x, z]);
                        }
                    }

                    Chunks[cx, cz] = chunk;
                }
            }

            RecalculateMaxHeight();
        }

        /// <summary>
        /// Yields scenery props strictly within a circular radius of a focal position.
        /// Bypasses distant map objects using spatial grid buckets.
        /// </summary>
        /// <param name="centerPos">The center position.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>Returns all Scenery objects in the defined Radius.</returns>
        public IEnumerable<SceneryPlacement> GetSceneryInRadius(Vector3 centerPos, float radius)
        {
            var minX = Math.Max(0, (int)MathF.Floor((centerPos.X - radius) / Scale));
            var maxX = Math.Min(Width - 1, (int)MathF.Floor((centerPos.X + radius) / Scale));
            var minZ = Math.Max(0, (int)MathF.Floor((centerPos.Z - radius) / Scale));
            var maxZ = Math.Min(Depth - 1, (int)MathF.Floor((centerPos.Z + radius) / Scale));

            var radiusSq = radius * radius;

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    if (!_sceneryGrid.TryGetValue((x, z), out var list)) continue;

                    for (var i = 0; i < list.Count; i++)
                    {
                        var prop = list[i];
                        if (Vector3.DistanceSquared(prop.Position, centerPos) <= radiusSq)
                        {
                            yield return prop;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Flags a specific landscape grid tile to be completely cut out of the rendering pipeline.
        /// </summary>
        /// <param name="x">The target grid tile X index.</param>
        /// <param name="z">The target grid tile Z index.</param>
        public void PunchHole(int x, int z)
        {
            _occludedTiles.Add((x, z));
        }

        /// <summary>
        /// Validates whether a specific heightmap grid tile has been flagged as hidden.
        /// </summary>
        /// <param name="x">The checked grid tile X index.</param>
        /// <param name="z">The checked grid tile Z index.</param>
        /// <returns>True if the tile is masked out; otherwise, false.</returns>
        public bool IsTileOccluded(int x, int z)
        {
            return _occludedTiles.Contains((x, z));
        }

        /// <summary>
        /// Safely fetches a terrain chunk at the specified chunk-grid coordinates.
        /// </summary>
        /// <param name="cx">The cx.</param>
        /// <param name="cz">The cz.</param>
        /// <returns>The terrain chunk at the specified coordinates, or <c>null</c> if out of bounds.</returns>
        public TerrainChunk? GetTerrainChunk(int cx, int cz)
        {
            if (Chunks == null) return null;
            if (cx < 0 || cx >= ChunkWidth || cz < 0 || cz >= ChunkDepth) return null;

            return Chunks[cx, cz];
        }

        /// <summary>
        /// Securely references a single local layout grid cell bucket list to return matching scenery props.
        /// </summary>
        /// <param name="cx">The cell x index.</param>
        /// <param name="cz">The cell z index.</param>
        /// <returns>A list of placed objects occupying this specific tile slot coordinates, or null if empty.</returns>
        public IReadOnlyList<SceneryPlacement>? GetSceneryAtCell(int cx, int cz)
        {
            var key = (cx, cz);
            return _sceneryGrid.TryGetValue(key, out var list) ? list : null;
        }

        /// <summary>
        /// Adds a generic scenery component to the world map tracking streams.
        /// </summary>
        /// <param name="position">The 3D coordinates representing base object placements.</param>
        /// <param name="seed">A tracking seed integer to maintain randomized procedural mesh uniformity.</param>
        /// <param name="type">The geometry profile choice index flag (Tree, Rock, Crystal, etc.).</param>
        /// <param name="primaryTexId">Primary face texture index sheet reference.</param>
        /// <param name="secondaryTexId">Secondary detail asset texture reference (Optional parameter: Defaults to -1 for single-texture objects like rocks).</param>
        public void AddScenery(Vector3 position, int seed, SceneryType type, int primaryTexId, int secondaryTexId = -1)
        {
            // Check if the asset is a wall-dependent prop
            if (type is SceneryType.WallTorch or SceneryType.Lever or SceneryType.PrisonBars)
            {
                // Calculate half-cell boundary width based on local landscape scale metrics
                // (minus a tiny padding buffer so it doesn't clip completely inside solid geometry planes)
                var wallOffsetDistance = (Scale * 0.5f) - 0.1f;

                var direction = seed % 4;
                switch (direction)
                {
                    case 0: // Facing North -> Push along -Z boundary line
                        position.Z -= wallOffsetDistance;
                        break;
                    case 1: // Facing South -> Push along +Z boundary line
                        position.Z += wallOffsetDistance;
                        break;
                    case 2: // Facing West -> Push along -X boundary line
                        position.X -= wallOffsetDistance;
                        break;
                    case 3: // Facing East -> Push along +X boundary line
                        position.X += wallOffsetDistance;
                        break;
                }
            }

            var placement = new SceneryPlacement(position, seed, type, primaryTexId, secondaryTexId);
            _scenery.Add(placement); // Keep this so your macro world rendering loop doesn't break

            // SPATIAL PROXIMITY INDEXING: Translate real world placement vectors into local grid cells
            var cx = (int)MathF.Floor(position.X / Scale);
            var cz = (int)MathF.Floor(position.Z / Scale);
            var gridKey = (cx, cz);

            if (!_sceneryGrid.ContainsKey(gridKey))
            {
                _sceneryGrid[gridKey] = new List<SceneryPlacement>();
            }

            _sceneryGrid[gridKey].Add(placement);
        }

        /// <summary>
        /// Gets or sets the height at grid coordinate (x, z).
        /// </summary>
        /// <value>
        /// The <see cref="float"/>.
        /// </value>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <returns>The height at the specified grid coordinates.</returns>
        public float this[int x, int z]
        {
            get => Heights[x, z];
            set
            {
                Heights[x, z] = value;
                if (value > _cachedMaxHeight) _cachedMaxHeight = value;
            }
        }

        /// <summary>
        /// Returns true if the given coordinates are inside the map bounds.
        /// </summary>
        /// <param name="x">The x coordinate in the grid.</param>
        /// <param name="z">The z coordinate in the grid.</param>
        /// <returns><c>true</c> if the coordinates are inside the map bounds; otherwise, <c>false</c>.</returns>
        public bool InBounds(int x, int z) =>
            x >= 0 && x < Width && z >= 0 && z < Depth;

        /// <summary>
        /// Resets all heights to a constant value.
        /// </summary>
        /// <param name="height">The height.</param>
        public void Clear(float height = 0f)
        {
            for (var x = 0; x < Width; x++)
            for (var z = 0; z < Depth; z++)
                Heights[x, z] = height;

            _cachedMaxHeight = height;
        }

        /// <summary>
        /// Calculates the exact terrain height at a given continuous world coordinate.
        /// Outside the grid (and in the last cell strip past the final vertex) the height of the nearest edge is
        /// returned; the surface is never extrapolated.
        /// </summary>
        /// <param name="worldX">The world x.</param>
        /// <param name="worldZ">The world z.</param>
        /// <returns>The height at the specified world coordinates.</returns>
        public float GetHeightAt(float worldX, float worldZ)
        {
            var gridX = worldX / Scale;
            var gridZ = worldZ / Scale;

            var x0 = (int)Math.Floor(gridX);
            var z0 = (int)Math.Floor(gridZ);

            if (x0 < 0) x0 = 0;
            if (x0 >= Width - 1) x0 = Width - 2;
            if (z0 < 0) z0 = 0;
            if (z0 >= Depth - 1) z0 = Depth - 2;

            // Clamp the position inside the cell too: only the cell INDEX used to be clamped, so queries beyond the
            // edge extrapolated the last triangle (e.g. -50 at (-50, -50) on a gently sloping map).
            var fx = Math.Clamp(gridX - x0, 0f, 1f);
            var fz = Math.Clamp(gridZ - z0, 0f, 1f);

            var h00 = Heights[x0, z0];
            var h10 = Heights[x0 + 1, z0];
            var h01 = Heights[x0, z0 + 1];
            var h11 = Heights[x0 + 1, z0 + 1];

            if (fx > fz)
            {
                return h00 + fx * (h10 - h00) + fz * (h11 - h10);
            }
            else
            {
                return h00 + fx * (h11 - h01) + fz * (h01 - h00);
            }
        }

        /// <summary>
        /// Converts a grid coordinate (x,z) into a world position (scaled, with height).
        /// </summary>
        /// <param name="x">The x coordinate in the grid.</param>
        /// <param name="z">The z coordinate in the grid.</param>
        /// <returns>The world position corresponding to the specified grid coordinates.</returns>
        public Vector3 GetWorldPosition(int x, int z) =>
            new(x * Scale, Heights[x, z], z * Scale);

        /// <summary>
        /// Recalculates MaxHeight from scratch.
        /// </summary>
        public float RecalculateMaxHeight()
        {
            var max = float.MinValue;
            for (var x = 0; x < Width; x++)
            {
                for (var z = 0; z < Depth; z++)
                {
                    if (Heights[x, z] > max) max = Heights[x, z];
                }
            }

            _cachedMaxHeight = max;
            return max;
        }
    }
}