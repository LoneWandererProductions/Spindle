/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        LightPropagationSolver.cs
 * PURPOSE:     High-performance 3D Voxel Light Propagation, 2D Terrain Lighting & Skylight Shadow Raycaster.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Runtime.CompilerServices;
using FolkTales.Enums;
using MemoryManager.Core;
using MemoryManager.Types;

namespace FolkTales.Maps
{
    /// <summary>
    /// Light propagation Solver with custom memory Manager
    /// </summary>
    public static class LightPropagationSolver
    {
        /// <summary>
        /// Lightweight 6-byte packed unmanaged Light node structure.
        /// </summary>
        private readonly struct LightNode
        {
            /// <summary>
            /// The x coordinate (X-world)
            /// </summary>
            public readonly short X;

            /// <summary>
            /// The y, Depth (Z-world)
            /// </summary>
            public readonly short Y;

            /// <summary>
            /// The z, Level/Layer profile
            /// </summary>
            public readonly short Z;

            /// <summary>
            /// Initializes a new instance of the <see cref="LightNode" /> struct.
            /// </summary>
            /// <param name="x">The x.</param>
            /// <param name="y">The y.</param>
            /// <param name="z">The z.</param>
            public LightNode(int x, int y, int z)
            {
                X = (short)x;
                Y = (short)y;
                Z = (short)z;
            }
        }

        /// <summary>
        /// Recalculates top-down sunlight cast across the world map.
        /// Rays travel straight down until blocked by a solid roof, floor, or ceiling.
        /// Uses unmanaged ArenaQueue for 0-byte GC heap allocation.
        /// </summary>
        /// <param name="arena">The arena.</param>
        /// <param name="map">The map.</param>
        /// <param name="daylightLumen">The daylight lumen.</param>
        /// <param name="ambientShadowLumen">The ambient shadow lumen.</param>
        public static void RecalculateSunlight(IMemoryAllocator? arena, HybridWorldMap? map, byte daylightLumen = 15,
            byte ambientShadowLumen = 3)
        {
            if (map == null) return;

            // 1. Rent an ArenaQueue backed 100% by unmanaged FastLane RAM
            using var queue = new ArenaQueue<LightNode>(arena, initialCapacity: 4096);

            // Cast sunlight vertically down from the highest level
            for (var x = 0; x < map.Width; x++)
            {
                for (var y = 0; y < map.Depth; y++)
                {
                    for (var lvl = map.Levels - 1; lvl >= 0; lvl--)
                    {
                        var cell = map.Structures.GetCell(x, y, lvl);

                        // If obstructed by a solid block, roof, or ceiling, stop direct sky raycast
                        if (cell is { IsEmpty: false } && (cell.IsSolidBlock || cell.HasCeiling || cell.HasFloor))
                        {
                            // Fill this cell AND all subterranean levels beneath it with ambient shadow light!
                            for (var subLvl = lvl; subLvl >= 0; subLvl--)
                            {
                                map.SetLightAt(x, y, subLvl, ambientShadowLumen);
                            }

                            break; // Sunlight ray stopped for this column
                        }

                        // Open sky cell receives full daylight
                        map.SetLightAt(x, y, lvl, daylightLumen);
                        queue.Enqueue(new LightNode(x, y, lvl));
                    }
                }
            }

            // 2. Spread sunlight horizontally into open doorways and windows
            PropagateLightQueueHybrid(map, queue);

            // 3. Re-inject & propagate placed torches/lanterns
            var surfaceBaselineY = map.SurfaceBaselineY;

            foreach (var prop in map.Scenery)
            {
                if (prop.LightEmission == 0) continue;

                var gx = map.WorldToGrid(prop.Position.X);
                var gz = map.WorldToGrid(prop.Position.Z);
                var glvl = map.WorldToLevel(prop.Position.Y);

                var targetLumen = Math.Min(MapConstants.MaxLightLevel, prop.LightEmission);
                map.SetLightAt(gx, gz, glvl, targetLumen);
                CalculateVoxelLighting(arena, map, gx, gz, glvl);

                var terrain = prop.Position.Y < surfaceBaselineY ? map.SubterraneanTerrain : map.SurfaceTerrain;
                var tgx = (int)MathF.Floor(prop.Position.X / terrain.Scale);
                var tgz = (int)MathF.Floor(prop.Position.Z / terrain.Scale);

                if (terrain.InBounds(tgx, tgz))
                {
                    if (targetLumen > terrain.GetLightAt(tgx, tgz))
                    {
                        terrain.SetLightAt(tgx, tgz, targetLumen);
                    }

                    CalculateTerrainLighting(arena, terrain, tgx, tgz);
                }
            }
        }

        /// <summary>
        /// Calculates and spreads point light (torches/lanterns) outward within a HybridWorldMap context.
        /// </summary>
        /// <param name="arena">The arena.</param>
        /// <param name="map">The map.</param>
        /// <param name="startX">The start x.</param>
        /// <param name="startY">The start y.</param>
        /// <param name="startZ">The start z.</param>
        public static void CalculateVoxelLighting(IMemoryAllocator? arena, HybridWorldMap? map, int startX, int startY,
            int startZ)
        {
            if (map == null) return;

            using var queue = new ArenaQueue<LightNode>(arena, initialCapacity: 512);
            queue.Enqueue(new LightNode(startX, startY, startZ));

            PropagateLightQueueHybrid(map, queue);
        }

        /// <summary>
        /// Calculates and spreads point light outward within a standalone DungeonMap context.
        /// </summary>
        /// <param name="arena">The arena.</param>
        /// <param name="map">The map.</param>
        /// <param name="startX">The start x.</param>
        /// <param name="startY">The start y.</param>
        /// <param name="startZ">The start z.</param>
        public static void CalculateVoxelLighting(IMemoryAllocator? arena, DungeonMap? map, int startX, int startY,
            int startZ)
        {
            if (map == null) return;

            using var queue = new ArenaQueue<LightNode>(arena, initialCapacity: 512);
            queue.Enqueue(new LightNode(startX, startY, startZ));

            PropagateLightQueueDungeon(map, queue);
        }

        /// <summary>
        /// Calculates and spreads 2D horizontal surface lighting across a standalone TerrainMap heightmap grid.
        /// </summary>
        /// <param name="arena">The arena.</param>
        /// <param name="map">The map.</param>
        /// <param name="gx">The gx.</param>
        /// <param name="gz">The gz.</param>
        public static void CalculateTerrainLighting(IMemoryAllocator? arena, TerrainMap? map, int gx, int gz)
        {
            if (map == null) return;

            using var queue = new ArenaQueue<LightNode>(arena, initialCapacity: 512);
            queue.Enqueue(new LightNode(gx, 0, gz));

            // Static stack-allocated direction offsets (Zero-GC Heap Allocation)
            ReadOnlySpan<int> dx =  [
            1, -1, 0, 0];
            ReadOnlySpan<int> dz =  [
            0, 0, 1, -1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentLight = map.GetLightAt(current.X, current.Z);

                if (currentLight <= 1) continue;

                for (var i = 0; i < 4; i++)
                {
                    var nx = current.X + dx[i];
                    var nz = current.Z + dz[i];

                    if (!map.InBounds(nx, nz)) continue;
                    if (map.IsTileOccluded(nx, nz)) continue;

                    var propagatedLight = (byte)(currentLight - 1);

                    if (map.GetLightAt(nx, nz) >= propagatedLight) continue;

                    map.SetLightAt(nx, nz, propagatedLight);
                    queue.Enqueue(new LightNode(nx, 0, nz));
                }
            }
        }

        // --- SHARED PROPAGATION LOGIC ---

        /// <summary>
        /// BFS propagation loop for HybridWorldMap context using unmanaged ArenaQueue.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="queue">The queue.</param>
        private static void PropagateLightQueueHybrid(HybridWorldMap map, ArenaQueue<LightNode> queue)
        {
            // Static stack-allocated direction offsets (Zero-GC Heap Allocation)
            ReadOnlySpan<int> dx =  [
            1, -1, 0, 0, 0, 0];
            ReadOnlySpan<int> dy =  [
            0, 0, 1, -1, 0, 0];
            ReadOnlySpan<int> dz =  [
            0, 0, 0, 0, 1, -1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentLight = map.GetLightAt(current.X, current.Y, current.Z);

                if (currentLight <= 1) continue;

                for (var i = 0; i < 6; i++)
                {
                    var nx = current.X + dx[i];
                    var ny = current.Y + dy[i];
                    var nz = current.Z + dz[i];

                    if (nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Depth || nz < 0 || nz >= map.Levels)
                        continue;

                    var sourceCell = map.Structures.GetCell(current.X, current.Y, current.Z);

                    // --- OUTGOING BARRIER CHECKS (Leaving Current Cell) ---
                    if (sourceCell != null)
                    {
                        if (i == 0 && IsOpaqueWall(sourceCell.WallEast)) continue; // +X
                        if (i == 1 && IsOpaqueWall(sourceCell.WallWest)) continue; // -X
                        if (i == 2 && IsOpaqueWall(sourceCell.WallSouth)) continue; // +Y
                        if (i == 3 && IsOpaqueWall(sourceCell.WallNorth)) continue; // -Y
                        if (i == 4 && sourceCell.HasCeiling) continue; // +Z
                        if (i == 5 && sourceCell.HasFloor) continue; // -Z
                    }

                    var targetCell = map.Structures.GetCell(nx, ny, nz);

                    // --- INCOMING BARRIER CHECKS (Entering Target Cell) ---
                    if (targetCell != null)
                    {
                        if (targetCell.IsSolidBlock) continue;

                        if (i == 0 && IsOpaqueWall(targetCell.WallWest)) continue;
                        if (i == 1 && IsOpaqueWall(targetCell.WallEast)) continue;
                        if (i == 2 && IsOpaqueWall(targetCell.WallNorth)) continue;
                        if (i == 3 && IsOpaqueWall(targetCell.WallSouth)) continue;
                        if (i == 4 && targetCell.HasFloor) continue;
                        if (i == 5 && targetCell.HasCeiling) continue;
                    }

                    var propagatedLight = (byte)(currentLight - 1);

                    // Stop if neighbor already has brighter or equal illumination
                    if (map.GetLightAt(nx, ny, nz) >= propagatedLight) continue;

                    map.SetLightAt(nx, ny, nz, propagatedLight);
                    queue.Enqueue(new LightNode(nx, ny, nz));
                }
            }
        }

        /// <summary>
        /// BFS propagation loop for standalone DungeonMap context using unmanaged ArenaQueue.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="queue">The queue.</param>
        private static void PropagateLightQueueDungeon(DungeonMap map, ArenaQueue<LightNode> queue)
        {
            // Static stack-allocated direction offsets (Zero-GC Heap Allocation)
            ReadOnlySpan<int> dx =  [
            1, -1, 0, 0, 0, 0];
            ReadOnlySpan<int> dy =  [
            0, 0, 1, -1, 0, 0];
            ReadOnlySpan<int> dz =  [
            0, 0, 0, 0, 1, -1];

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentLight = map.GetLightAt(current.X, current.Y, current.Z);

                if (currentLight <= 1) continue;

                for (var i = 0; i < 6; i++)
                {
                    var nx = current.X + dx[i];
                    var ny = current.Y + dy[i];
                    var nz = current.Z + dz[i];

                    if (nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Depth || nz < 0 || nz >= map.Levels)
                        continue;

                    var sourceCell = map.GetCell(current.X, current.Y, current.Z);

                    // --- OUTGOING BARRIER CHECKS ---
                    if (sourceCell != null)
                    {
                        if (i == 0 && IsOpaqueWall(sourceCell.WallEast)) continue;
                        if (i == 1 && IsOpaqueWall(sourceCell.WallWest)) continue;
                        if (i == 2 && IsOpaqueWall(sourceCell.WallSouth)) continue;
                        if (i == 3 && IsOpaqueWall(sourceCell.WallNorth)) continue;
                        if (i == 4 && sourceCell.HasCeiling) continue;
                        if (i == 5 && sourceCell.HasFloor) continue;
                    }

                    var targetCell = map.GetCell(nx, ny, nz);

                    // --- INCOMING BARRIER CHECKS ---
                    if (targetCell != null)
                    {
                        if (targetCell.IsSolidBlock) continue;
                        if (i == 0 && IsOpaqueWall(targetCell.WallWest)) continue;
                        if (i == 1 && IsOpaqueWall(targetCell.WallEast)) continue;
                        if (i == 2 && IsOpaqueWall(targetCell.WallNorth)) continue;
                        if (i == 3 && IsOpaqueWall(targetCell.WallSouth)) continue;
                        if (i == 4 && targetCell.HasFloor) continue;
                        if (i == 5 && targetCell.HasCeiling) continue;
                    }

                    var propagatedLight = (byte)(currentLight - 1);

                    if (map.GetLightAt(nx, ny, nz) >= propagatedLight) continue;

                    map.SetLightAt(nx, ny, nz, propagatedLight);
                    queue.Enqueue(new LightNode(nx, ny, nz));
                }
            }
        }

        /// <summary>
        /// Checks if a wall profile completely blocks light propagation.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <returns>
        ///   <c>true</c> if [is opaque wall] [the specified wall]; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOpaqueWall(WallType wall) => wall.BlocksLight();
    }
}