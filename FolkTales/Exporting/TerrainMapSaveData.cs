/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        TerrainMapSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Maps;
using FolkTales.Structs;

namespace FolkTales.Exporting
{
    public sealed class TerrainMapSaveData
    {
        public int Version { get; set; } = 1;
        public int Width { get; set; }
        public int Depth { get; set; }
        public float Scale { get; set; } = 10f;

        // 1D-flaches Höhenfeld (Index: x + z * Width)
        public float[] Heights { get; set; } = Array.Empty<float>();

        // Optionales Splatting-Material-Grid (Index: x + z * Width)
        public byte[]? Materials { get; set; }

        /// <summary>Start settings: fog, discovery, sight, time, weather, spawn (optional).</summary>
        public MapSettings? Settings { get; set; }

        public TextureSet? Textures { get; set; }
        public List<TileCoordSaveData> OccludedTiles { get; set; } = new();
        public List<TerrainWaterBodySaveData> WaterBodies { get; set; } = new();
        public List<SceneryPlacement> Scenery { get; set; } = new();
    }
}