/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        TerrainWaterBodySaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;

namespace FolkTales.Exporting
{
    public sealed class TerrainWaterBodySaveData
    {
        public string Name { get; set; } = "Water";
        public float WaterLevel { get; set; }
        public int MinX { get; set; }
        public int MinZ { get; set; }
        public int MaxX { get; set; }
        public int MaxZ { get; set; }
        public Color Color { get; set; }
    }
}