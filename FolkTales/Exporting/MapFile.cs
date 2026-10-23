/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        MapFile.cs
 * PURPOSE:     The map file: one JSON document per map with a small header (id, name, kind) and exactly one payload
 *              (terrain, dungeon or hybrid world). The start settings (fog, discovery, spawn ...) are part of the payload.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.IO;
using FolkTales.Maps;
using System.Text.Json;

namespace FolkTales.Exporting
{
    /// <summary>
    /// What kind of map a file holds.
    /// </summary>
    public enum MapKind
    {
        /// <summary>A heightmap landscape (<see cref="TerrainMap"/>).</summary>
        Terrain,

        /// <summary>A multi-level grid map (<see cref="DungeonMap"/>).</summary>
        Dungeon,

        /// <summary>Terrain plus structures (<see cref="HybridWorldMap"/>).</summary>
        Hybrid
    }

    /// <summary>
    /// The JSON document of a map. Exactly one of the three payloads is set, matching <see cref="Kind"/>.
    /// </summary>
    public sealed class MapFileData
    {
        /// <summary>Gets or sets the file format version.</summary>
        public int FormatVersion { get; set; } = MapFile.CurrentFormatVersion;

        /// <summary>Gets or sets the stable id (also the file name without extension).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets an optional description (shown when the map is loaded).</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets the kind of map.</summary>
        public MapKind Kind { get; set; }

        /// <summary>Gets or sets the terrain payload.</summary>
        public TerrainMapSaveData? Terrain { get; set; }

        /// <summary>Gets or sets the dungeon payload.</summary>
        public DungeonMapSaveData? Dungeon { get; set; }

        /// <summary>Gets or sets the hybrid world payload.</summary>
        public HybridWorldMapSaveData? Hybrid { get; set; }
    }

    /// <summary>
    /// A map read from a file: the header plus the one map that was in it.
    /// </summary>
    public sealed class LoadedMap
    {
        /// <summary>Gets the stable id.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Gets the display name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets the description.</summary>
        public string? Description { get; init; }

        /// <summary>Gets the kind of map.</summary>
        public MapKind Kind { get; init; }

        /// <summary>Gets the terrain (when <see cref="Kind"/> is Terrain).</summary>
        public TerrainMap? Terrain { get; init; }

        /// <summary>Gets the dungeon (when <see cref="Kind"/> is Dungeon).</summary>
        public DungeonMap? Dungeon { get; init; }

        /// <summary>Gets the hybrid world (when <see cref="Kind"/> is Hybrid).</summary>
        public HybridWorldMap? Hybrid { get; init; }

        /// <summary>
        /// Replaces the texture set of the loaded map and gives its props without a texture of their own the new slot
        /// textures (what a "rebuild with these ids" means for a map that comes from a file).
        /// </summary>
        /// <param name="textures">The texture set.</param>
        public void ApplyTextures(TextureSet textures)
        {
            switch (Kind)
            {
                case MapKind.Terrain:
                    Terrain!.Textures = textures;
                    Terrain.RetextureScenery();
                    break;
                case MapKind.Dungeon:
                    Dungeon!.Textures = textures;
                    Dungeon.RetextureScenery();
                    break;
                default:
                    Hybrid!.SetTextures(textures);
                    Hybrid.RetextureScenery();
                    break;
            }
        }

        /// <summary>
        /// Gets the start settings of whichever map this is.
        /// </summary>
        public MapSettings Settings => Kind switch
        {
            MapKind.Terrain => Terrain!.Settings,
            MapKind.Dungeon => Dungeon!.Settings,
            _ => Hybrid!.Settings
        };
    }

    /// <summary>
    /// Reads and writes map files.
    /// </summary>
    public static class MapFile
    {
        /// <summary>The format version this code writes and understands.</summary>
        public const int CurrentFormatVersion = 1;

        /// <summary>The file extension of a map file.</summary>
        public const string Extension = ".map.json";

        /// <summary>
        /// Wraps a terrain map.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The display name.</param>
        /// <param name="map">The map.</param>
        /// <param name="description">The description.</param>
        /// <returns>The document.</returns>
        public static MapFileData Describe(string id, string name, TerrainMap map, string? description = null) => new()
        {
            Id = id, Name = name, Description = description, Kind = MapKind.Terrain,
            Terrain = TerrainMapSerializer.Export(map)
        };

        /// <summary>
        /// Wraps a dungeon map.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The display name.</param>
        /// <param name="map">The map.</param>
        /// <param name="description">The description.</param>
        /// <returns>The document.</returns>
        public static MapFileData Describe(string id, string name, DungeonMap map, string? description = null) => new()
        {
            Id = id, Name = name, Description = description, Kind = MapKind.Dungeon,
            Dungeon = DungeonMapSerializer.Export(map)
        };

        /// <summary>
        /// Wraps a hybrid world map.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The display name.</param>
        /// <param name="map">The map.</param>
        /// <param name="description">The description.</param>
        /// <returns>The document.</returns>
        public static MapFileData Describe(string id, string name, HybridWorldMap map, string? description = null) =>
            new()
            {
                Id = id, Name = name, Description = description, Kind = MapKind.Hybrid,
                Hybrid = HybridWorldMapSerializer.Export(map)
            };

        /// <summary>
        /// Writes a document to a file (the folder is created if needed).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The document.</param>
        public static void Save(string path, MapFileData data)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            File.WriteAllText(path, ToJson(data));
        }

        /// <summary>
        /// Serializes a document.
        /// </summary>
        /// <param name="data">The document.</param>
        /// <returns>The JSON.</returns>
        public static string ToJson(MapFileData data) => JsonSerializer.Serialize(data, MapJson.Options);

        /// <summary>
        /// Reads a map file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The loaded map.</returns>
        public static LoadedMap Load(string path) => FromJson(File.ReadAllText(path));

        /// <summary>
        /// Builds a map from the JSON of a map file.
        /// </summary>
        /// <param name="json">The JSON.</param>
        /// <returns>The loaded map.</returns>
        /// <exception cref="InvalidDataException">The document is empty, from a newer format or lacks its payload.</exception>
        public static LoadedMap FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<MapFileData>(json, MapJson.Options)
                       ?? throw new InvalidDataException("The map file is empty.");

            if (data.FormatVersion > CurrentFormatVersion)
                throw new InvalidDataException(
                    $"Map '{data.Id}' has format {data.FormatVersion}, this build reads up to {CurrentFormatVersion}.");

            return data.Kind switch
            {
                MapKind.Terrain => new LoadedMap
                {
                    Id = data.Id, Name = data.Name, Description = data.Description, Kind = data.Kind,
                    Terrain = TerrainMapSerializer.Import(data.Terrain ?? throw Missing(data))
                },
                MapKind.Dungeon => new LoadedMap
                {
                    Id = data.Id, Name = data.Name, Description = data.Description, Kind = data.Kind,
                    Dungeon = DungeonMapSerializer.Import(data.Dungeon ?? throw Missing(data))
                },
                MapKind.Hybrid => new LoadedMap
                {
                    Id = data.Id, Name = data.Name, Description = data.Description, Kind = data.Kind,
                    Hybrid = HybridWorldMapSerializer.Import(data.Hybrid ?? throw Missing(data))
                },
                _ => throw new InvalidDataException($"Unknown map kind '{data.Kind}'.")
            };
        }

        private static InvalidDataException Missing(MapFileData data) =>
            new($"Map '{data.Id}' is of kind {data.Kind} but has no such payload.");
    }
}