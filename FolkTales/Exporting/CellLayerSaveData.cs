/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        CellLayerSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;

namespace FolkTales.Exporting
{
    public sealed class CellLayerSaveData
    {
        public string Name { get; set; } = "Layer";
        public float Height { get; set; }
        public Color? Color { get; set; }
        public int? TextureId { get; set; }
        public bool AlphaBlend { get; set; } = true;
    }
}