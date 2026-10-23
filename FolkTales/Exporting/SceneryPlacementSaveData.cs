/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        SceneryPlacementSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Exporting
{
    public sealed class SceneryPlacementSaveData
    {
        public Vector3 Position { get; set; }
        public int Seed { get; set; }
        public SceneryType Type { get; set; }
        public int PrimaryTexId { get; set; }
        public int SecondaryTexId { get; set; }
        public float FitScale { get; set; }
    }
}