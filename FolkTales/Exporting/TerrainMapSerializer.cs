/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        TerrainMapSerializer.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Text.Json;
using FolkTales.Maps;

namespace FolkTales.Exporting
{
    public static class TerrainMapSerializer
    {
        public static TerrainMapSaveData Export(TerrainMap terrain)
        {
            var w = terrain.Width;
            var d = terrain.Depth;
            var heightsFlat = new float[w * d];

            for (var z = 0; z < d; z++)
            for (var x = 0; x < w; x++)
            {
                heightsFlat[x + (z * w)] = terrain[x, z];
            }

            byte[]? materialsFlat = null;
            if (terrain.Materials != null)
            {
                materialsFlat = new byte[w * d];
                for (var z = 0; z < d; z++)
                for (var x = 0; x < w; x++)
                {
                    materialsFlat[x + (z * w)] = terrain.Materials[x, z];
                }
            }

            return new TerrainMapSaveData
            {
                Width = w,
                Depth = d,
                Scale = terrain.Scale,
                Settings = terrain.Settings.Clone(),
                Heights = heightsFlat,
                Materials = materialsFlat,
                Textures = terrain.Textures,
                OccludedTiles = terrain.OccludedTiles.Select(t => new TileCoordSaveData { X = t.X, Z = t.Z }).ToList(),
                WaterBodies = terrain.WaterBodies.Select(wb => new TerrainWaterBodySaveData
                {
                    Name = wb.Name,
                    WaterLevel = wb.WaterLevel,
                    MinX = wb.MinX,
                    MinZ = wb.MinZ,
                    MaxX = wb.MaxX,
                    MaxZ = wb.MaxZ,
                    Color = wb.Color
                }).ToList(),
                Scenery = terrain.Scenery.ToList()
            };
        }

        public static TerrainMap Import(TerrainMapSaveData data)
        {
            var terrain = new TerrainMap(data.Width, data.Depth, data.Scale,
                data.Settings?.Clone() ?? new MapSettings());
            ApplyData(terrain, data);
            return terrain;
        }

        /// <summary>
        /// Überträgt die gespeicherten Höhendaten in eine bestehende TerrainMap-Instanz
        /// (wichtig für die Verknüpfung in HybridWorldMap).
        /// </summary>
        public static void ApplyData(TerrainMap target, TerrainMapSaveData data)
        {
            var w = target.Width;
            var d = target.Depth;

            for (var z = 0; z < d; z++)
            for (var x = 0; x < w; x++)
            {
                target[x, z] = data.Heights[x + (z * w)];
            }

            if (data.Materials != null)
            {
                target.EnableMaterials();
                for (var z = 0; z < d; z++)
                for (var x = 0; x < w; x++)
                {
                    target.Materials![x, z] = data.Materials[x + (z * w)];
                }
            }

            if (data.Textures != null)
            {
                target.Textures = data.Textures;
            }

            foreach (var hole in data.OccludedTiles)
            {
                target.PunchHole(hole.X, hole.Z);
            }

            foreach (var wb in data.WaterBodies)
            {
                target.AddWaterBody(new TerrainWaterBody
                {
                    Name = wb.Name,
                    WaterLevel = wb.WaterLevel,
                    MinX = wb.MinX,
                    MinZ = wb.MinZ,
                    MaxX = wb.MaxX,
                    MaxZ = wb.MaxZ,
                    Color = wb.Color
                });
            }

            foreach (var prop in data.Scenery)
            {
                target.RestoreScenery(prop);
            }

            target.RecalculateMaxHeight();
            target.GenerateChunks();
            target.InitializeLightGrid();
        }

        public static string ToJson(TerrainMap terrain) =>
            JsonSerializer.Serialize(Export(terrain), MapJson.Options);

        public static TerrainMap FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<TerrainMapSaveData>(json, MapJson.Options)
                       ?? throw new InvalidOperationException("Failed to deserialize terrain map.");
            return Import(data);
        }
    }
}