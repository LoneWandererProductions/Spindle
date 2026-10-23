/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        WallMountRules.cs
 * PURPOSE:     Placement rules for props that hang on a cell wall, shared by DungeonMap and HybridWorldMap.
 */

using System.Numerics;
using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Wall-mounted props (torches, levers, prison bars) are placed at the cell centre by the callers; the seed picks the wall
    /// (0 north, 1 south, 2 west, 3 east). This moves them to eye level against that wall.
    /// </summary>
    public static class WallMountRules
    {
        /// <summary>Returns true for prop types that hang on a wall.</summary>
        public static bool IsWallMounted(SceneryType type) =>
            type is SceneryType.WallTorch or SceneryType.Lever or SceneryType.PrisonBars;

        /// <summary>
        /// Moves a wall-mounted prop to its wall and returns the rotation seed to store (torches are turned by 180 degrees so
        /// they face into the room). Other props are left untouched.
        /// </summary>
        public static int Apply(ref Vector3 position, int seed, SceneryType type, float cellScale)
        {
            if (IsWallMounted(type))
            {
                var offset = (cellScale * 0.5f) - 0.1f;

                // Raise to eye level
                position.Y += 1.5f;

                switch (seed % 4)
                {
                    case 0:
                        position.Z -= offset;
                        break; // North wall (-Z)
                    case 1:
                        position.Z += offset;
                        break; // South wall (+Z)
                    case 2:
                        position.X -= offset;
                        break; // West wall (-X)
                    case 3:
                        position.X += offset;
                        break; // East wall (+X)
                }
            }

            return type is SceneryType.WallTorch ? seed + 2 : seed;
        }
    }
}