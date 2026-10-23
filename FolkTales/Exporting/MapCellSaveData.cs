/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        MapCellSaveData.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Maps;
using System.Drawing;

namespace FolkTales.Exporting
{
    public sealed class MapCellSaveData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public WallType WallNorth { get; set; }
        public WallType WallSouth { get; set; }
        public WallType WallEast { get; set; }
        public WallType WallWest { get; set; }
        public CellShape Shape { get; set; } = CellShape.Square;

        public RoofOrientation RoofStyle { get; set; } = RoofOrientation.None;
        public float RoofAngleDegrees { get; set; } = 30f;
        public bool CloseGableSidewalls { get; set; } = true;
        public bool HasFlatCeiling { get; set; } = true;

        public float WallThickness { get; set; } = MapDimensions.WallThicknessCells;
        public float OpeningHeightRatio { get; set; } = MapDimensions.DefaultOpeningRatio;
        public DoorStyle DoorStyle { get; set; } = DoorStyle.Wood;
        public Color? WallColor { get; set; }

        public bool HasFloor { get; set; }
        public bool HasCeiling { get; set; }
        public float FloorHeight { get; set; }
        public float CeilingHeight { get; set; } = MapDimensions.LevelHeightCells;
        public float FloorThickness { get; set; } = MapDimensions.DefaultFloorThickness;
        public float CeilingThickness { get; set; } = 0.2f;
        public Color? FloorColor { get; set; }
        public Color? CeilingColor { get; set; }

        public int? WallTextureId { get; set; }
        public int? NorthWallTextureId { get; set; }
        public int? SouthWallTextureId { get; set; }
        public int? EastWallTextureId { get; set; }
        public int? WestWallTextureId { get; set; }
        public int? FloorTextureId { get; set; }
        public int? CeilingTextureId { get; set; }

        public List<CellLayerSaveData>? Layers { get; set; }
    }
}