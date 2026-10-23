/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        DungeonMapSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Maps;
using FolkTales.Structs;

namespace FolkTales.Exporting
{
    public sealed class DungeonMapSaveData
    {
        public int Version { get; set; } = 1;
        public int Width { get; set; }
        public int Depth { get; set; }
        public int Levels { get; set; }
        public float CellScale { get; set; } = MapDimensions.DefaultCellSize;
        public float BaseHeight { get; set; } = MapDimensions.LevelHeightCells;
        public int OpenAirFromLevel { get; set; } = int.MaxValue;

        /// <summary>Start settings: fog, discovery, sight, time, weather, spawn (optional).</summary>
        public MapSettings? Settings { get; set; }

        public TextureSet? Textures { get; set; }
        public List<SceneryPlacement> Scenery { get; set; } = new();
        public List<MapCellSaveData> ActiveCells { get; set; } = new();
    }
}