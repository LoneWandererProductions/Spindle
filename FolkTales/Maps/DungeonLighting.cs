/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        DungeonLighting.cs
 * PURPOSE:     The light bake of a dungeon map: ambient floor light plus propagation from every light-emitting prop.
 *              Shared by the builders and by the map loader, so a map loaded from a file is lit exactly like a built one.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using MemoryManager;
using MemoryManager.Core;

namespace FolkTales.Maps
{
    /// <summary>
    /// Bakes the block light of a <see cref="DungeonMap"/>.
    /// </summary>
    public static class DungeonLighting
    {
        /// <summary>
        /// The light every floor gets, so a floor exists even far from any light source.
        /// </summary>
        public const byte AmbientFloorLight = 3;

        /// <summary>
        /// Full bake: ambient fill on every floored cell, then the propagation from the props.
        /// </summary>
        /// <param name="map">The map.</param>
        public static void Bake(DungeonMap map)
        {
            for (var l = 0; l < map.Levels; l++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                if (map.GetCell(x, z, l) is { HasFloor: true }) map.SetLightAt(x, z, l, AmbientFloorLight);
            }

            PropagateFromProps(map);
        }

        /// <summary>
        /// Seeds and propagates light from every placed light-emitting prop.
        /// </summary>
        /// <param name="map">The map.</param>
        public static void PropagateFromProps(DungeonMap map)
        {
            using var arena = new MemoryArena(MemoryManagerConfig.CreateForFrameScratch(1 * 1024 * 1024));

            foreach (var scene in map.Scenery)
            {
                if (scene.LightEmission == 0) continue;

                var gx = (int)MathF.Floor(scene.Position.X / map.DungeonCellScale);
                var gz = (int)MathF.Floor(scene.Position.Z / map.DungeonCellScale);
                var gl = map.WorldToLevel(scene.Position.Y);

                if (scene.LightEmission > map.GetLightAt(gx, gz, gl)) map.SetLightAt(gx, gz, gl, scene.LightEmission);

                LightPropagationSolver.CalculateVoxelLighting(arena, map, gx, gz, gl);
            }
        }
    }
}