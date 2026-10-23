/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        SceneryRetexture.cs
 * PURPOSE:     Re-resolves the textures of props after a map was loaded. A prop without an explicit texture takes the slot of
 *              its type when it is placed; a map loaded from a file has those ids baked, so changing a slot needs this.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Structs;

namespace FolkTales.Maps
{
    /// <summary>
    /// Applies the scenery slots of a <see cref="TextureSet"/> to props that have no texture of their own.
    /// </summary>
    internal static class SceneryRetexture
    {
        /// <summary>
        /// Gives every prop without a primary texture the texture of its type slot (props with their own texture keep it).
        /// </summary>
        /// <param name="scenery">The prop list of the map.</param>
        /// <param name="grid">The spatial index of the same props.</param>
        /// <param name="textures">The texture set to take the slots from.</param>
        /// <returns>The number of props that changed.</returns>
        public static int Apply(List<SceneryPlacement> scenery, Dictionary<(int, int), List<SceneryPlacement>> grid,
            TextureSet textures)
        {
            var replaced = new Dictionary<SceneryPlacement, SceneryPlacement>();

            for (var i = 0; i < scenery.Count; i++)
            {
                var p = scenery[i];
                if (p.PrimaryTextureId > 0) continue;

                var id = textures.ForScenery(p.Type);
                if (id <= 0) continue;

                var changed = new SceneryPlacement(p.Position, p.Seed, p.Type, id, p.SecondaryTextureId,
                    p.TertiaryTextureId, p.FitScale);
                scenery[i] = changed;
                replaced[p] = changed;
            }

            if (replaced.Count == 0) return 0;

            foreach (var cell in grid.Values)
            {
                for (var j = 0; j < cell.Count; j++)
                {
                    if (replaced.TryGetValue(cell[j], out var changed)) cell[j] = changed;
                }
            }

            return replaced.Count;
        }
    }
}