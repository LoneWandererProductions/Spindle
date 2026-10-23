/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Exporting
 * FILE:        MapJson.cs
 * PURPOSE:     The one set of JSON options for all map files. System.Text.Json cannot round-trip Vector3 (public fields),
 *              System.Drawing.Color (no setters) and SceneryPlacement (constructor names differ from the properties) on its
 *              own, so the converters live here. Enums are written by name: a map file stays readable and survives a
 *              re-ordered enum.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Structs;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolkTales.Exporting
{
    /// <summary>
    /// Shared JSON configuration for every map serializer.
    /// </summary>
    public static class MapJson
    {
        /// <summary>
        /// The options used to read and write map files.
        /// </summary>
        public static readonly JsonSerializerOptions Options = Create();

        /// <summary>
        /// Builds the options (a fresh instance, so a caller may tweak its own copy).
        /// </summary>
        /// <returns>The options.</returns>
        public static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                NewLine = "\n", // the same bytes on every OS, so a map file diffs cleanly in version control
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new Vector3Converter());
            options.Converters.Add(new ColorConverter());
            options.Converters.Add(new SceneryPlacementConverter());
            return options;
        }

        /// <summary>
        /// Writes a Vector3 as <c>{"x":1,"y":2,"z":3}</c>.
        /// </summary>
        private sealed class Vector3Converter : JsonConverter<Vector3>
        {
            /// <inheritdoc />
            public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Vector3 expects an object.");

                float x = 0, y = 0, z = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString();
                    reader.Read();
                    var value = reader.GetSingle();
                    switch (name?.ToLowerInvariant())
                    {
                        case "x":
                            x = value;
                            break;
                        case "y":
                            y = value;
                            break;
                        case "z":
                            z = value;
                            break;
                    }
                }

                return new Vector3(x, y, z);
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);
                writer.WriteNumber("z", value.Z);
                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Writes a Color as <c>"#AARRGGBB"</c> (also reads <c>#RRGGBB</c>).
        /// </summary>
        private sealed class ColorConverter : JsonConverter<Color>
        {
            /// <inheritdoc />
            public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var text = reader.GetString() ?? throw new JsonException("Color expects a string.");
                text = text.TrimStart('#');
                if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                    throw new JsonException($"'{text}' is not a hex color.");

                if (text.Length <= 6) argb |= 0xFF000000u;
                return Color.FromArgb(unchecked((int)argb));
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
                writer.WriteStringValue("#" + ((uint)value.ToArgb()).ToString("X8", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a SceneryPlacement with its property names (the struct's constructor uses shorter parameter names).
        /// </summary>
        private sealed class SceneryPlacementConverter : JsonConverter<SceneryPlacement>
        {
            /// <inheritdoc />
            public override SceneryPlacement Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("SceneryPlacement expects an object.");

                var position = Vector3.Zero;
                var seed = 0;
                var type = default(SceneryType);
                int primary = -1, secondary = -1, tertiary = -1;
                var fitScale = 1f;

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString()?.ToLowerInvariant();
                    reader.Read();
                    switch (name)
                    {
                        case "position":
                            position = JsonSerializer.Deserialize<Vector3>(ref reader, options);
                            break;
                        case "seed":
                            seed = reader.GetInt32();
                            break;
                        case "type":
                            type = JsonSerializer.Deserialize<SceneryType>(ref reader, options);
                            break;
                        case "primarytextureid":
                            primary = reader.GetInt32();
                            break;
                        case "secondarytextureid":
                            secondary = reader.GetInt32();
                            break;
                        case "tertiarytextureid":
                            tertiary = reader.GetInt32();
                            break;
                        case "fitscale":
                            fitScale = reader.GetSingle();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                return new SceneryPlacement(position, seed, type, primary, secondary, tertiary, fitScale);
            }

            /// <inheritdoc />
            public override void Write(Utf8JsonWriter writer, SceneryPlacement value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("position");
                JsonSerializer.Serialize(writer, value.Position, options);
                writer.WriteNumber("seed", value.Seed);
                writer.WriteString("type", value.Type.ToString());
                writer.WriteNumber("primaryTextureId", value.PrimaryTextureId);
                writer.WriteNumber("secondaryTextureId", value.SecondaryTextureId);
                writer.WriteNumber("tertiaryTextureId", value.TertiaryTextureId);
                writer.WriteNumber("fitScale", value.FitScale);
                writer.WriteEndObject();
            }
        }
    }
}