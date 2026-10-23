/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        HybridWorldMapSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Maps;
using FolkTales.Structs;

namespace FolkTales.Exporting
{
    public sealed class HybridWorldMapSaveData
    {
        public int Version { get; set; } = 1;
        public int Width { get; set; }
        public int Depth { get; set; }
        public int Levels { get; set; }
        public float SharedScale { get; set; } = MapDimensions.DefaultCellSize;
        public int SurfaceLevel { get; set; } = 1;

        /// <summary>Start settings: fog, discovery, sight, time, weather, spawn (optional).</summary>
        public MapSettings? Settings { get; set; }

        public TextureSet? Textures { get; set; }

        // Verbindung der Subsysteme
        public DungeonMapSaveData Structures { get; set; } = new();
        public TerrainMapSaveData? SurfaceTerrain { get; set; }
        public TerrainMapSaveData? SubterraneanTerrain { get; set; }

        public List<SceneryPlacement> Scenery { get; set; } = new();
    }
}