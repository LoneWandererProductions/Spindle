/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        SceneryPlacement.cs
 * PURPOSE:     Position and type information for placing scenery objects in the dungeon, such as bonsai trees, jagged rocks, ancient pillars, and crystal shards.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;
using System.Text.Json.Serialization;

namespace FolkTales.Structs
{
    /// <summary>
    /// Lightweight pure-data structural description of a placed scenic asset.
    /// </summary>
    public readonly struct SceneryPlacement
    {
        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the seed.
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public int Seed { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public SceneryType Type { get; }

        /// <summary>
        /// Gets the tertiary texture identifier.
        /// </summary>
        /// <value>
        /// The tertiary texture identifier.
        /// </value>
        public int TertiaryTextureId { get; }

        /// <summary>
        /// Gets the secondary texture identifier.
        /// </summary>
        /// <value>
        /// The secondary texture identifier.
        /// </value>
        public int SecondaryTextureId { get; }

        /// <summary>
        /// Gets the primary texture identifier.
        /// </summary>
        /// <value>
        /// The primary texture identifier.
        /// </value>
        public int PrimaryTextureId { get; }

        /// <summary>
        /// Gets the size of the host map's grid cell in metres. Only cell-fitted types (see
        /// <c>SceneryExtensions.IsCellFitted</c>) read it: their mesh and collision are sized from the cell, everything
        /// else is human-scale and ignores it. Not part of equality, so a lookup placement still finds the stored one.
        /// </summary>
        public float FitScale { get; }

        /// <summary>
        /// Gets the direction seed.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="type">The type.</param>
        /// <param name="primaryTex">The primary texture identifier.</param>
        /// <param name="secondaryTex">The secondary texture identifier.</param>
        /// <param name="tertiaryTex">The tertiary texture identifier. optional</param>
        /// <param name="fitScale">Cell size in metres for cell-fitted types; 1 for everything else.</param>
        [JsonConstructor]
        public SceneryPlacement(Vector3 position, int seed, SceneryType type, int primaryTex, int secondaryTex,
            int tertiaryTex = -1, float fitScale = 1f)
        {
            FitScale = fitScale;
            Position = position;
            Seed = seed;
            Type = type;
            PrimaryTextureId = primaryTex;
            SecondaryTextureId = secondaryTex;
            TertiaryTextureId = tertiaryTex;
        }

        /// <summary>
        /// Evaluates spatial equality based strictly on physical geometry attributes (Position, Seed, Type).
        /// Texture metadata is intentionally excluded to allow collider cache sharing across textured variants.
        /// </summary>
        public bool Equals(SceneryPlacement other)
        {
            return Position.Equals(other.Position) &&
                   Seed == other.Seed &&
                   Type == other.Type;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is SceneryPlacement other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Position, Seed, Type);

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(SceneryPlacement left, SceneryPlacement right) => left.Equals(right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(SceneryPlacement left, SceneryPlacement right) => !left.Equals(right);
    }
}