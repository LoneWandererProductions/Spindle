/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        HybridWorldMapSerializer.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Text.Json;
using FolkTales.Maps;

namespace FolkTales.Exporting
{
    public static class HybridWorldMapSerializer
    {
        public static HybridWorldMapSaveData Export(HybridWorldMap map)
        {
            return new HybridWorldMapSaveData
            {
                Width = map.Width,
                Depth = map.Depth,
                Levels = map.Levels,
                SharedScale = map.SharedScale,
                SurfaceLevel = map.SurfaceLevel,
                Settings = map.Settings.Clone(),
                Textures = map.Textures,
                Structures = DungeonMapSerializer.Export(map.Structures),
                SurfaceTerrain = map.SurfaceTerrain != null ? TerrainMapSerializer.Export(map.SurfaceTerrain) : null,
                SubterraneanTerrain = map.SubterraneanTerrain != null
                    ? TerrainMapSerializer.Export(map.SubterraneanTerrain)
                    : null,
                Scenery = map.Scenery.ToList()
            };
        }

        public static HybridWorldMap Import(HybridWorldMapSaveData data)
        {
            var map = new HybridWorldMap(data.Width, data.Depth, data.Levels, data.SharedScale,
                data.Settings?.Clone() ?? new MapSettings(), data.SurfaceLevel);

            // 1. Texturset einhängen
            if (data.Textures != null)
            {
                map.SetTextures(data.Textures);
            }

            // 2. Terrains über den TerrainMapSerializer füllen
            if (data.SurfaceTerrain != null && map.SurfaceTerrain != null)
            {
                TerrainMapSerializer.ApplyData(map.SurfaceTerrain, data.SurfaceTerrain);
            }

            if (data.SubterraneanTerrain != null && map.SubterraneanTerrain != null)
            {
                TerrainMapSerializer.ApplyData(map.SubterraneanTerrain, data.SubterraneanTerrain);
            }

            // 3. Strukturen über den DungeonMapSerializer füllen
            if (data.Structures != null)
            {
                var importedDungeon = DungeonMapSerializer.Import(data.Structures, bakeLighting: false);
                CopyDungeonCells(importedDungeon, map.Structures);
            }

            // 4. Eigene Hybrid-Scenery wiederherstellen
            foreach (var prop in data.Scenery)
            {
                map.RestoreScenery(prop);
            }

            // 5. Gesamtes Lichtgitter neu berechnen
            map.InitializeLightGrid();

            return map;
        }

        private static void CopyDungeonCells(DungeonMap source, DungeonMap target)
        {
            for (var l = 0; l < target.Levels; l++)
            for (var x = 0; x < target.Width; x++)
            for (var z = 0; z < target.Depth; z++)
            {
                var sCell = source.GetCell(x, z, l);
                var tCell = target.GetCell(x, z, l);
                if (sCell == null || tCell == null) continue;

                tCell.WallNorth = sCell.WallNorth;
                tCell.WallSouth = sCell.WallSouth;
                tCell.WallEast = sCell.WallEast;
                tCell.WallWest = sCell.WallWest;
                tCell.Shape = sCell.Shape;
                tCell.RoofStyle = sCell.RoofStyle;
                tCell.RoofAngleDegrees = sCell.RoofAngleDegrees;
                tCell.CloseGableSidewalls = sCell.CloseGableSidewalls;
                tCell.HasFlatCeiling = sCell.HasFlatCeiling;
                tCell.WallThickness = sCell.WallThickness;
                tCell.OpeningHeightRatio = sCell.OpeningHeightRatio;
                tCell.DoorStyle = sCell.DoorStyle;
                tCell.WallColor = sCell.WallColor;
                tCell.HasFloor = sCell.HasFloor;
                tCell.HasCeiling = sCell.HasCeiling;
                tCell.FloorHeight = sCell.FloorHeight;
                tCell.CeilingHeight = sCell.CeilingHeight;
                tCell.FloorThickness = sCell.FloorThickness;
                tCell.CeilingThickness = sCell.CeilingThickness;
                tCell.FloorColor = sCell.FloorColor;
                tCell.CeilingColor = sCell.CeilingColor;
                tCell.WallTextureId = sCell.WallTextureId;
                tCell.NorthWallTextureId = sCell.NorthWallTextureId;
                tCell.SouthWallTextureId = sCell.SouthWallTextureId;
                tCell.EastWallTextureId = sCell.EastWallTextureId;
                tCell.WestWallTextureId = sCell.WestWallTextureId;
                tCell.FloorTextureId = sCell.FloorTextureId;
                tCell.CeilingTextureId = sCell.CeilingTextureId;
                tCell.Layers = sCell.Layers;
            }
        }

        public static string ToJson(HybridWorldMap map) =>
            JsonSerializer.Serialize(Export(map), MapJson.Options);

        public static HybridWorldMap FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<HybridWorldMapSaveData>(json, MapJson.Options)
                       ?? throw new InvalidOperationException("Failed to deserialize hybrid world map.");
            return Import(data);
        }
    }
}