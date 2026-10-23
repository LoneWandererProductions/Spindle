/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        TextureSet.cs
 * PURPOSE:     Named texture slots -> texture ids. One per map, saved as JSON. Replaces hardcoded ids.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.IO;
using System.Text.Json;
using FolkTales.Enums;

namespace FolkTales.Maps
{
    /// <summary>
    /// Maps a slot name to a texture id. Every map owns one (<c>map.Textures</c>).
    /// Ids are the usual ids: 10000+ = procedural catalogue, 1..9999 = loaded textures, &lt;= 0 = not set.
    /// A slot that is not set returns the caller's fallback, so the old defaults keep working.
    /// </summary>
    public sealed class TextureSet
    {
        /// <summary>Well-known slot names. Slots are plain strings, so a new one never breaks an old save file.</summary>
        public static class Slot
        {
            /// <summary>Default dungeon / structure floor texture.</summary>
            public const string Floor = "Floor";

            /// <summary>Default dungeon / structure wall texture.</summary>
            public const string Wall = "Wall";

            /// <summary>Default dungeon / structure ceiling texture.</summary>
            public const string Ceiling = "Ceiling";

            /// <summary>Liquid layer "Water" (CellLayer.Name is the slot).</summary>
            public const string Water = "Water";

            /// <summary>Liquid layer "Lava".</summary>
            public const string Lava = "Lava";

            /// <summary>Terrain splat layer 0 = <c>TerrainMap.Materials</c> value 0.</summary>
            public const string Terrain0 = "Terrain0";

            /// <summary>Terrain splat layer 1.</summary>
            public const string Terrain1 = "Terrain1";

            /// <summary>Terrain splat layer 2.</summary>
            public const string Terrain2 = "Terrain2";

            /// <summary>Terrain splat layer 3.</summary>
            public const string Terrain3 = "Terrain3";

            /// <summary>Basalt columns (causeway).</summary>
            public const string Basalt = "Basalt";
        }

        /// <summary>Prefix of the per scenery-type slots, e.g. <c>Scenery.SkeletalRemains</c> (the bones).</summary>
        public const string SceneryPrefix = "Scenery.";

        /// <summary>
        /// Template for NEW maps. Set it before you build a map; the map takes a copy in its constructor.
        /// Nothing else reads it, so maps never share state.
        /// </summary>
        public static TextureSet Template { get; set; } = new();

        /// <summary>Slot -> id. Public so an editor can bind to it and JSON can read and write it.</summary>
        public Dictionary<string, int> Ids { get; set; } = new();

        /// <summary>Bumped on every change; renderers can compare it to know when to rebake.</summary>
        public int Revision { get; private set; }

        /// <summary>Gets the slot for a scenery type.</summary>
        public static string SceneryKey(SceneryType type) => SceneryPrefix + type;

        /// <summary>Id of a slot, or <paramref name="fallback" /> when the slot is not set.</summary>
        public int Get(string slot, int fallback = -1) =>
            Ids.TryGetValue(slot, out var id) && id > 0 ? id : fallback;

        /// <summary>Sets a slot. An id of 0 or less clears it (back to the built-in default).</summary>
        public void Set(string slot, int id)
        {
            if (id <= 0) Ids.Remove(slot);
            else Ids[slot] = id;
            Revision++;
        }

        /// <summary>Primary texture id for a prop. Returns -1 when unset, then the factory default applies.</summary>
        public int ForScenery(SceneryType type) => Get(SceneryKey(type));

        /// <summary>
        /// The four terrain splat ids for <c>GlResourceManager.TerrainLayerTextureIds</c>.
        /// Unset slots fall back to the defaults the terrain used so far.
        /// </summary>
        public int[] TerrainLayers() => new[]
        {
            Get(Slot.Terrain0, 10023), Get(Slot.Terrain1, 10025),
            Get(Slot.Terrain2, 10026), Get(Slot.Terrain3, 10023)
        };

        /// <summary>Deep copy (own dictionary, revision 0).</summary>
        public TextureSet Clone() => new() { Ids = new Dictionary<string, int>(Ids) };

        /// <summary>Copies all slots of another set into this one.</summary>
        public void CopyFrom(TextureSet other)
        {
            Ids = new Dictionary<string, int>(other.Ids);
            Revision++;
        }

        // --- save / load: plain JSON; write it next to the map file or embed it in it ---

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>Serializes the set to JSON.</summary>
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        /// <summary>Reads a set from JSON; an empty or broken text gives an empty set.</summary>
        public static TextureSet FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<TextureSet>(json) ?? new TextureSet();
            }
            catch (JsonException)
            {
                return new TextureSet();
            }
        }

        /// <summary>Saves the set to a file.</summary>
        public void Save(string path) => File.WriteAllText(path, ToJson());

        /// <summary>Loads a set from a file; a missing file gives an empty set.</summary>
        public static TextureSet Load(string path) =>
            File.Exists(path) ? FromJson(File.ReadAllText(path)) : new TextureSet();
    }
}