/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        LightGrid3D.cs
 * PURPOSE:     Shared 3D light grid for tracking luminance levels across a multi-level map.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */


namespace FolkTales.Maps
{
    /// <summary>
    /// LightGrid3D is a 3D grid that tracks light levels across multiple levels of a map. Each cell in the grid represents a specific (x, z, level) coordinate and holds a byte value indicating the light level at that position.
    /// The grid can be reset to a default state, and individual cells can be queried or updated as needed.
    /// </summary>
    public sealed class LightGrid3D
    {
        /// <summary>
        /// The grid
        /// </summary>
        private readonly byte[,,] _grid;

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public int Width { get; }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        /// <value>
        /// The depth.
        /// </value>
        public int Depth { get; }

        /// <summary>
        /// Gets the levels.
        /// </summary>
        /// <value>
        /// The levels.
        /// </value>
        public int Levels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightGrid3D"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="levels">The levels.</param>
        public LightGrid3D(int width, int depth, int levels)
        {
            Width = width;
            Depth = depth;
            Levels = levels;
            _grid = new byte[width, depth, levels];
            Reset();
        }

        /// <summary>
        /// Primes every cell to open-air sky luminance.
        /// </summary>
        public void Reset()
        {
            for (var x = 0; x < Width; x++)
            for (var z = 0; z < Depth; z++)
            for (var l = 0; l < Levels; l++)
                _grid[x, z, l] = MapConstants.MaxLightLevel;
        }

        /// <summary>
        /// Gets the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <returns>Light value at position.</returns>
        public byte Get(int x, int z, int level)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth || level >= Levels)
                return MapConstants.MaxLightLevel;
            if (level < 0)
                return MapConstants.MinLightLevel;

            return _grid[x, z, level];
        }

        /// <summary>
        /// Sets the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="z">The z.</param>
        /// <param name="level">The level.</param>
        /// <param name="value">The value.</param>
        public void Set(int x, int z, int level, byte value)
        {
            if (x >= 0 && x < Width && z >= 0 && z < Depth && level >= 0 && level < Levels)
                _grid[x, z, level] = value;
        }
    }
}