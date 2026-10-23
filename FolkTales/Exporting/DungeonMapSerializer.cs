/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        DungeonMapSerializer.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Maps;
using System.Text.Json;

namespace FolkTales.Exporting
{
    public static class DungeonMapSerializer
    {
        public static DungeonMapSaveData Export(DungeonMap map)
        {
            var data = new DungeonMapSaveData
            {
                Width = map.Width,
                Depth = map.Depth,
                Levels = map.Levels,
                CellScale = map.DungeonCellScale,
                BaseHeight = map.BaseHeight,
                OpenAirFromLevel = map.OpenAirFromLevel,
                Settings = map.Settings.Clone(),
                Textures = map.Textures
            };

            // 1. Scenery (the placements as they stand, wall offsets and seeds already applied)
            data.Scenery.AddRange(map.Scenery);

            // 2. Aktive Zellen filtern
            for (var l = 0; l < map.Levels; l++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var cell = map.GetCell(x, z, l);
                if (cell == null || cell.IsEmpty) continue;

                var cellData = new MapCellSaveData
                {
                    X = x,
                    Y = z,
                    Z = l,
                    WallNorth = cell.WallNorth,
                    WallSouth = cell.WallSouth,
                    WallEast = cell.WallEast,
                    WallWest = cell.WallWest,
                    Shape = cell.Shape,
                    RoofStyle = cell.RoofStyle,
                    RoofAngleDegrees = cell.RoofAngleDegrees,
                    CloseGableSidewalls = cell.CloseGableSidewalls,
                    HasFlatCeiling = cell.HasFlatCeiling,
                    WallThickness = cell.WallThickness,
                    OpeningHeightRatio = cell.OpeningHeightRatio,
                    DoorStyle = cell.DoorStyle,
                    WallColor = cell.WallColor,
                    HasFloor = cell.HasFloor,
                    HasCeiling = cell.HasCeiling,
                    FloorHeight = cell.FloorHeight,
                    CeilingHeight = cell.CeilingHeight,
                    FloorThickness = cell.FloorThickness,
                    CeilingThickness = cell.CeilingThickness,
                    FloorColor = cell.FloorColor,
                    CeilingColor = cell.CeilingColor,
                    WallTextureId = cell.WallTextureId,
                    NorthWallTextureId = cell.NorthWallTextureId,
                    SouthWallTextureId = cell.SouthWallTextureId,
                    EastWallTextureId = cell.EastWallTextureId,
                    WestWallTextureId = cell.WestWallTextureId,
                    FloorTextureId = cell.FloorTextureId,
                    CeilingTextureId = cell.CeilingTextureId
                };

                if (cell.Layers is { Count: > 0 })
                {
                    cellData.Layers = cell.Layers.Select(layer => new CellLayerSaveData
                    {
                        Name = layer.Name,
                        Height = layer.Height,
                        Color = layer.Color,
                        TextureId = layer.TextureId,
                        AlphaBlend = layer.AlphaBlend
                    }).ToList();
                }

                data.ActiveCells.Add(cellData);
            }

            return data;
        }

        /// <summary>
        /// Builds a map from its save data.
        /// </summary>
        /// <param name="data">The save data.</param>
        /// <param name="bakeLighting">If set to <c>true</c> the block light is baked after loading (a hybrid world bakes its
        /// own light and passes <c>false</c>).</param>
        /// <returns>The map.</returns>
        public static DungeonMap Import(DungeonMapSaveData data, bool bakeLighting = true)
        {
            var map = new DungeonMap(data.Width, data.Depth, data.Levels, data.Settings?.Clone() ?? new MapSettings(),
                data.OpenAirFromLevel)
            {
                DungeonCellScale = data.CellScale,
                BaseHeight = data.BaseHeight
            };

            if (data.Textures != null)
            {
                map.Textures = data.Textures;
            }

            // Alle Zellen zunächst leeren
            for (var l = 0; l < map.Levels; l++)
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
            {
                var c = map.GetCell(x, z, l);
                if (c == null) continue;
                c.HasFloor = false;
                c.HasCeiling = false;
                c.WallNorth = c.WallSouth = c.WallEast = c.WallWest = WallType.None;
            }

            // Aktive Zellen befüllen
            foreach (var d in data.ActiveCells)
            {
                var cell = map.GetCell(d.X, d.Y, d.Z);
                if (cell == null) continue;

                cell.WallNorth = d.WallNorth;
                cell.WallSouth = d.WallSouth;
                cell.WallEast = d.WallEast;
                cell.WallWest = d.WallWest;
                cell.Shape = d.Shape;
                cell.RoofStyle = d.RoofStyle;
                cell.RoofAngleDegrees = d.RoofAngleDegrees;
                cell.CloseGableSidewalls = d.CloseGableSidewalls;
                cell.HasFlatCeiling = d.HasFlatCeiling;
                cell.WallThickness = d.WallThickness;
                cell.OpeningHeightRatio = d.OpeningHeightRatio;
                cell.DoorStyle = d.DoorStyle;
                cell.WallColor = d.WallColor;
                cell.HasFloor = d.HasFloor;
                cell.HasCeiling = d.HasCeiling;
                cell.FloorHeight = d.FloorHeight;
                cell.CeilingHeight = d.CeilingHeight;
                cell.FloorThickness = d.FloorThickness;
                cell.CeilingThickness = d.CeilingThickness;
                cell.FloorColor = d.FloorColor;
                cell.CeilingColor = d.CeilingColor;
                cell.WallTextureId = d.WallTextureId;
                cell.NorthWallTextureId = d.NorthWallTextureId;
                cell.SouthWallTextureId = d.SouthWallTextureId;
                cell.EastWallTextureId = d.EastWallTextureId;
                cell.WestWallTextureId = d.WestWallTextureId;
                cell.FloorTextureId = d.FloorTextureId;
                cell.CeilingTextureId = d.CeilingTextureId;

                if (d.Layers is { Count: > 0 })
                {
                    cell.Layers = d.Layers.Select(l => new CellLayer
                    {
                        Name = l.Name,
                        Height = l.Height,
                        Color = l.Color,
                        TextureId = l.TextureId,
                        AlphaBlend = l.AlphaBlend
                    }).ToList();
                }
            }

            // Scenery exakt wiederherstellen (baut gleichzeitig _sceneryGrid auf, ohne Wandversatz/Seed neu anzuwenden)
            foreach (var s in data.Scenery)
            {
                map.RestoreScenery(s);
            }

            if (bakeLighting) DungeonLighting.Bake(map);

            return map;
        }

        public static string ToJson(DungeonMap map) =>
            JsonSerializer.Serialize(Export(map), MapJson.Options);

        public static DungeonMap FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<DungeonMapSaveData>(json, MapJson.Options)
                       ?? throw new InvalidOperationException("Failed to deserialize dungeon map.");
            return Import(data);
        }
    }
}