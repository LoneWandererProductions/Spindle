/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     GlRender.Core
 * FILE:        SceneryExtensions.cs
 * PURPOSE:     Exposes unified architectural 3D collision radius and height parameters for placed scenery decorations.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using FolkTales.Maps;
using FolkTales.Structs;
using System.Numerics;

namespace FolkTales
{
    /// <summary>
    /// Provides data-driven extension structures for the SceneryType enumeration layers.
    /// </summary>
    public static class SceneryExtensions
    {
        /// <summary>
        /// The authoring scale for scenery collision geometry. Scenery meshes are always baked by
        /// <c>GlRender.Scenery.SceneryHelper.BakeRawMesh</c> with a fixed factory scale of 1.0f, in real-world
        /// metres, regardless of the host map's grid cell size (a TerrainMap/HybridWorldMap/DungeonMap "scale"
        /// or "cellScale" is the size of one grid cell, not a scale factor for props placed on it).
        /// Every caller that bakes or draws scenery collision geometry (GetCollisionBoxes,
        /// GetCollisionCapsules, GetCollisionTriangles, GetCollisionGeometry, GetMaxCollisionReach,
        /// BakedSceneryCollider) MUST pass this constant rather than the map/cell scale, or the collider
        /// will not match the visible mesh. See GetCollisionRadius/GetCollisionHeight for the metre values
        /// this is meant to multiply.
        /// </summary>
        public const float PropScale = 1.0f;

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        private static float ToRad(float degrees) => degrees * (MathF.PI / 180.0f);

        /// <summary>
        /// Whether a type is sized from the host map's CELL (<see cref="SceneryPlacement.FitScale"/>) instead of being
        /// a fixed human-scale object in metres. Architecture that has to line up with the grid (stairs) is fitted;
        /// furniture is not, a barrel must stay a barrel whether the cell is 3 m or 10 m.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if mesh and collision are sized from the cell.</returns>
        public static bool IsCellFitted(this SceneryType type) => type == SceneryType.StairFlight;

        /// <summary>
        /// The scale that mesh bake and collision must use for a placement: the cell size for cell-fitted types,
        /// <see cref="PropScale"/> for everything else. Every caller that builds colliders goes through this.
        /// </summary>
        /// <param name="placement">The placement.</param>
        /// <returns>The scale to pass to the Get*Collision* functions.</returns>
        public static float ColliderScale(this SceneryPlacement placement) =>
            placement.Type.IsCellFitted() ? placement.FitScale : PropScale;

        /// <summary>
        /// Extends SceneryType to natively expose its physical collision footprint radius.
        /// </summary>
        /// <param name="type">The target scenery type choice.</param>
        /// <returns>The radius boundary in world units. A value of 0.0f indicates a fully passable asset.</returns>
        public static float GetCollisionRadius(this SceneryType type)
        {
            switch (type)
            {
                case SceneryType.Thimble:
                case SceneryType.Rose:
                case SceneryType.Iris:
                case SceneryType.Sunflower:
                case SceneryType.Delphinium:
                    return 0.0f; // Fully walkable / passable

                // Macro Outdoor Flora & Geology
                case SceneryType.MammothTree:
                    return 4.0f; // Massive trunk
                case SceneryType.Oak:
                    return 1.2f;
                case SceneryType.PineTree:
                    return 0.6f; // Solid trunk core line
                case SceneryType.DeadTree:
                    return 0.6f;
                case SceneryType.MushroomTree:
                    return 0.5f;
                case SceneryType.BonsaiTree:
                    return 0.4f;
                case SceneryType.Bamboo:
                    return 0.2f;
                case SceneryType.JaggedRock:
                    return 2.0f;
                // Ground Cover & Low Obstacles, player can pass through but may collide with the visual mesh
                case SceneryType.Bramble:
                    return 0.0f;
                case SceneryType.WaterLily:
                    return 0.0f;

                // Heavy Structural Elements
                case SceneryType.LargeGableRoof:
                    return 0.0f; // overhead: the walls are cells, a collider at the anchor is an invisible pillar
                case SceneryType.AncientPillar:
                    return 0.6f;
                case SceneryType.RuinedColumnIntact:
                    return 0.3f;
                case SceneryType.AncientArch:
                    return 0.4f;
                case SceneryType.CurvedWall:
                    return 0.5f;
                case SceneryType.Machinery:
                    return 0.3f;

                case SceneryType.Fountain:
                    return 0.55f;
                case SceneryType.Fireplace:
                    return 0.6f;
                case SceneryType.CrystalShard:
                    return 0.8f;
                case SceneryType.TownWell:
                    return 0.7f;
                case SceneryType.TrainingDummy:
                    return 0.4f;
                case SceneryType.WheelLaying:
                    return 1.0f;
                case SceneryType.WheelStanding:
                    return 0.3f;

                // Solid Medium Furniture Assets
                case SceneryType.ArmorRackFull:
                case SceneryType.ArmorRackEmpty:
                    return 0.3f;
                case SceneryType.Barrel:
                case SceneryType.BarrelClosed:
                case SceneryType.BarrelOpen:
                    return 0.37f;
                case SceneryType.RoundTable:
                    return 0.8f;

                // Small Micro Obstacles
                case SceneryType.RoundVase:
                    return 0.3f;
                case SceneryType.Campfire:
                    return 0.5f;
                case SceneryType.StandingTorch:
                    return 0.1f;
                case SceneryType.Lever:
                    return 0.1f;

                // Interception Boundaries & Barriers
                case SceneryType.DoorWoodClosed:
                    return 0.5f;

                // Non-Solid Visual Props / Open Doors (Completely Passable)
                case SceneryType.WallTorch:
                    return 0.0f;
                case SceneryType.IronRing:
                    return 0.0f;
                case SceneryType.HangingChain:
                    return 0.0f;
                case SceneryType.SkeletalRemains:
                    return 0.0f;
                case SceneryType.Mushrooms:
                    return 0.0f;
                case SceneryType.Flowers:
                    return 0.0f;
                case SceneryType.DoorWoodOpen:
                    return 0.0f;
                case SceneryType.DoorPrisonOpen:
                    return 0.0f;
                case SceneryType.DogBowlFull:
                case SceneryType.DogBowlEmpty:
                    return 0.0f;

                // Various themed objects
                case SceneryType.ChinesePagoda:
                    return 1.0f;
                case SceneryType.WindChime:
                    return 0.1f; // Mostly hanging, minimal floor collision
                case SceneryType.DreamCatcher:
                    return 0.1f; // Hangs on walls/ceilings
                default:
                    //Trace.WriteLine("Warning: SceneryType " + type + " does not have a defined collision radius. Defaulting to 0.0f.");
                    return 0;
            }
        }

        /// <summary>
        /// Extends SceneryType to natively expose its structural physical height profile limit.
        /// </summary>
        /// <param name="type">The target scenery type choice.</param>
        /// <returns>The vertical dimension in world units from the origin plane level.</returns>
        public static float GetCollisionHeight(this SceneryType type)
        {
            switch (type)
            {
                // Macro Outdoor Trees
                case SceneryType.MammothTree:
                    return 75.0f;
                case SceneryType.Oak:
                    return 5.0f;
                case SceneryType.PineTree:
                    return 9.0f;
                case SceneryType.DeadTree:
                    return 6.0f;
                case SceneryType.Bamboo:
                    return 5.0f;
                case SceneryType.MushroomTree:
                    return 5.5f;
                case SceneryType.BonsaiTree:
                    return 1.4f;
                case SceneryType.Bramble:
                    return 1.2f;
                case SceneryType.JaggedRock:
                    return 2.8f;
                case SceneryType.WaterLily:
                    return 0.0f;
                case SceneryType.Thimble:
                    return 1.2f;
                case SceneryType.Rose:
                    return 0.85f;
                case SceneryType.Iris:
                    return 0.95f;
                case SceneryType.Sunflower:
                    return 1.85f;
                case SceneryType.Delphinium:
                    return 1.45f;

                // Tall Architecture & Wall Attachments
                case SceneryType.LilacRuin:
                    return 2.8f; // Must match wallHeight in BonsaiFactory.BakeLilacRuins
                case SceneryType.LargeGableRoof:
                    return 4.5f;
                case SceneryType.AncientPillar:
                    return 5.3f;
                case SceneryType.RuinedColumnIntact:
                    return 2.35f;
                case SceneryType.RuinedColumnBroken:
                    return 1.1f;
                case SceneryType.CurvedWall:
                    return 3.0f;

                // Medium Industrial & Room Architecture
                case SceneryType.Fireplace:
                    return 2.0f;
                case SceneryType.StandingTorch:
                    return 1.7f;
                case SceneryType.ArmorRackEmpty:
                case SceneryType.ArmorRackFull:
                    return 1.3f;
                case SceneryType.CrystalShard:
                    return 4.0f;
                case SceneryType.Machinery:
                    return 0.85f;

                // Ground Furniture & Altars
                case SceneryType.Fountain:
                    return 0.35f;
                case SceneryType.Lever:
                    return 0.60f;
                case SceneryType.RoundVase:
                    return 0.6f;
                case SceneryType.Campfire:
                    return 0.40f;
                case SceneryType.Barrel:
                case SceneryType.BarrelClosed:
                case SceneryType.BarrelOpen:
                    return 0.85f;

                // Operational Standard Door Units
                case SceneryType.DoorWoodClosed:
                    return 2.6f;

                // Non-Solid Decorative Elements
                case SceneryType.WallTorch:
                    return 0.0f;
                case SceneryType.IronRing:
                    return 0.0f;
                case SceneryType.HangingChain:
                    return 0.0f;
                case SceneryType.SkeletalRemains:
                    return 0.0f;
                case SceneryType.Mushrooms:
                    return 0.0f;
                case SceneryType.Flowers:
                    return 0.0f;
                case SceneryType.DoorWoodOpen:
                    return 0.0f;
                case SceneryType.DoorPrisonOpen:
                    return 0.0f;


                // Various themed objects
                case SceneryType.ChinesePagoda:
                    return 4.5f;
                case SceneryType.WindChime:
                    return 2.0f;
                case SceneryType.DreamCatcher:
                    return 1.5f;
                case SceneryType.TownWell:
                    return 0.65f;
                case SceneryType.WheelLaying:
                    return 0.4f;
                case SceneryType.WheelStanding:
                    return 2.0f;
                case SceneryType.TrainingDummy:
                    return 1.8f;
                default:
                    //Trace.WriteLine("Warning: SceneryType " + type + " does not have a defined collision height. Defaulting to 0.0f.");
                    return 0;
            }
        }

        /// <summary>
        /// Converts the directional placement seed into a standard Y-axis rotation angle in radians.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>The Y-axis rotation angle in radians.</returns>
        public static float GetRotationYaw(this SceneryType type, int seed)
        {
            // Clamp the seed to 0-3 range just in case a random large seed passes through
            var directionIndex = seed % 4;

            return directionIndex switch
            {
                0 => 0.0f, // North (0 degrees)
                1 => MathF.PI, // South (180 degrees)
                2 => -MathF.PI / 2.0f, // West (-90 degrees)
                3 => MathF.PI / 2.0f, // East (90 degrees)
                _ => 0.0f
            };
        }

        /// <summary>
        /// Generates deterministic 3D collision capsules for complex scenery (roots, trunks, boughs, stairs, ladders, collapsed beams)
        /// or a default vertical capsule for standard props.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="position">The position.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>List of collision Capsules.</returns>
        public static List<CollisionCapsule> GetCollisionCapsules(this SceneryType type, Vector3 position, float scale,
            int seed)
        {
            var capsules = new List<CollisionCapsule>();

            switch (type)
            {
                case SceneryType.Oak:
                case SceneryType.PineTree:
                case SceneryType.DeadTree:
                case SceneryType.MammothTree:
                {
                    var r = type.GetCollisionRadius() * scale;
                    var h = type.GetCollisionHeight() *
                            scale; // Uses full tree height (e.g. 12m for Oak, 25m for MammothTree)

                    if (r > 0f && h > 0f)
                    {
                        var start = position;
                        var end = position + new Vector3(0f, h, 0f);
                        capsules.Add(new CollisionCapsule(start, end, r));
                    }

                    break;
                }

                // ── Smithy (Stump/Anvil + Water Quench Tub) ───────────────────
                case SceneryType.Smithy:
                {
                    var yaw = type.GetRotationYaw(seed);

                    // 1. Stump & Anvil (Right side vertical column)
                    var stumpOffset = RotateOffset(new Vector3(0.36f * scale, 0f, 0.05f * scale), yaw);
                    var stumpR = 0.22f * scale;
                    var stumpTotalH = 0.76f * scale; // 0.52m stump + 0.24m anvil
                    capsules.Add(new CollisionCapsule(
                        position + stumpOffset,
                        position + stumpOffset + new Vector3(0f, stumpTotalH, 0f),
                        stumpR));

                    // 2. Water Quench Tub (Front-center barrel)
                    var tubOffset = RotateOffset(new Vector3(-0.02f * scale, 0f, 0.32f * scale), yaw);
                    var tubR = 0.16f * scale;
                    var tubH = 0.38f * scale;
                    capsules.Add(new CollisionCapsule(
                        position + tubOffset,
                        position + tubOffset + new Vector3(0f, tubH, 0f),
                        tubR));

                    break;
                }

                // ── Jagged Rock ───────────────────────────────────────────────
                case SceneryType.JaggedRock:
                {
                    var yaw = type.GetRotationYaw(seed);

                    // 1. Main Central Dome (Taller & slightly wider to cap tall peaks like Seed 2)
                    var mainH = 2.60f * scale; // Increased from 1.80f to match tall seed peaks
                    var mainR = 1.00f * scale; // Increased from 0.85f to fill the central core
                    capsules.Add(new CollisionCapsule(
                        position,
                        position + new Vector3(0f, mainH, 0f),
                        mainR));

                    // 2. Right Base Lobe (+X outcrop, covers right side in Seed 0)
                    var rightOffset = RotateOffset(new Vector3(0.95f * scale, 0f, 0.15f * scale), yaw);
                    var lobeH = 1.05f * scale;
                    var lobeR = 0.60f * scale;
                    capsules.Add(new CollisionCapsule(
                        position + rightOffset,
                        position + rightOffset + new Vector3(0f, lobeH, 0f),
                        lobeR));

                    // 3. Left Base Lobe (-X outcrop, seals the open gap on the left in Seed 0)
                    lobeH = 1.25f * scale;
                    var leftOffset = RotateOffset(new Vector3(-0.95f * scale, 0f, -0.10f * scale), yaw);
                    capsules.Add(new CollisionCapsule(
                        position + leftOffset,
                        position + leftOffset + new Vector3(0f, lobeH, 0f),
                        lobeR));

                    break;
                }

                // ── Ruined Column Broken ──────────────────────────────────────────────────
                case SceneryType.RuinedColumnBroken:
                {
                    var stumpH = type.GetCollisionHeight() * scale; // 1.10f * scale
                    var stumpR = 0.30f * scale; // Covers plinth base and upright broken shaft

                    capsules.Add(new CollisionCapsule(
                        position,
                        position + new Vector3(0f, stumpH, 0f),
                        stumpR));

                    break;
                }

                // ── Machinery ──────────────────────────────────────────────────
                case SceneryType.Machinery:
                {
                    var baseTopY = 0.40f * scale; // Sits flush on top of the 0.4m base box
                    var totalHeight = type.GetCollisionHeight() * scale; // 0.85f * scale
                    var boilerRadius = 0.30f * scale;

                    var start = position + new Vector3(0f, baseTopY, 0f);
                    var end = position + new Vector3(0f, totalHeight, 0f);

                    capsules.Add(new CollisionCapsule(start, end, boilerRadius));
                    break;
                }

                // ── Round Dining Table ─────────────────────────────────────────
                case SceneryType.RoundTable:
                {
                    // Hardcode height to 0.8f so it cannot default to 0.0f
                    var totalHeight = 0.80f * scale;

                    // Wireframe table rim radius is ~0.6m
                    var topRadius = 0.60f * scale;
                    var postRadius = 0.12f * scale;

                    var slabThickness = 0.06f * scale;
                    var slabHalfThick = slabThickness * 0.5f;
                    var postHeight = totalHeight - slabThickness;

                    // 1. Central support pedestal: floor (Y = 0) up to underside of tabletop
                    capsules.Add(new CollisionCapsule(
                        position,
                        position + new Vector3(0f, postHeight, 0f),
                        postRadius));

                    // 2. Tabletop disc: 4 horizontal cross-capsules at Y = totalHeight - halfThickness
                    var discCenterY = totalHeight - slabHalfThick;
                    var discCenter = position + new Vector3(0f, discCenterY, 0f);
                    var discExtent = MathF.Max(0f, topRadius - slabHalfThick);
                    var diag = discExtent * 0.7071f; // cos(45°)

                    // Spoke 1: X-Axis (Left <-> Right)
                    capsules.Add(new CollisionCapsule(
                        discCenter + new Vector3(-discExtent, 0f, 0f),
                        discCenter + new Vector3(discExtent, 0f, 0f),
                        slabHalfThick));

                    // Spoke 2: Z-Axis (Forward <-> Backward)
                    capsules.Add(new CollisionCapsule(
                        discCenter + new Vector3(0f, 0f, -discExtent),
                        discCenter + new Vector3(0f, 0f, discExtent),
                        slabHalfThick));

                    // Spoke 3: Diagonal 1
                    capsules.Add(new CollisionCapsule(
                        discCenter + new Vector3(-diag, 0f, -diag),
                        discCenter + new Vector3(diag, 0f, diag),
                        slabHalfThick));

                    // Spoke 4: Diagonal 2
                    capsules.Add(new CollisionCapsule(
                        discCenter + new Vector3(-diag, 0f, diag),
                        discCenter + new Vector3(diag, 0f, -diag),
                        slabHalfThick));

                    break;
                }

                // ── TownWell ─────────────────────────────────────────
                case SceneryType.TownWell:
                {
                    var wellHeight = type.GetCollisionHeight() * scale; // 0.65f * scale
                    var wellRadius = type.GetCollisionRadius() * scale; // 0.70f * scale

                    // Stone Basin Cylinder (flush with ground)
                    capsules.Add(new CollisionCapsule(
                        position,
                        position + new Vector3(0f, wellHeight, 0f),
                        wellRadius));

                    break;
                }

                // ── Fountain ─────────────────────────────────────────
                case SceneryType.Fountain:
                {
                    var basinHeight = type.GetCollisionHeight() * scale; // 0.35f * scale
                    var basinRadius = type.GetCollisionRadius() * scale; // 0.55f * scale

                    // ─────────────────────────────────────────────────────────────
                    // 2 Main Body Capsules (Basin + Central Pillar)
                    // ─────────────────────────────────────────────────────────────

                    // 1. Lower Basin: Preserves the working green cylinder
                    capsules.Add(new CollisionCapsule(
                        position,
                        position + new Vector3(0f, basinHeight, 0f),
                        basinRadius));

                    // 2. Central Pillar & Spire: Extends from ground through to the top finial
                    var pillarHeight = 0.82f * scale; // Total visual height to top sphere
                    var pillarRadius = 0.08f * scale;

                    capsules.Add(new CollisionCapsule(
                        position + new Vector3(0f, pillarRadius, 0f),
                        position + new Vector3(0f, MathF.Max(pillarRadius, pillarHeight - pillarRadius), 0f),
                        pillarRadius));

                    // ─────────────────────────────────────────────────────────────
                    // 3 Capsules on Top (60° 3-Way Star for the Upper Basin)
                    // ─────────────────────────────────────────────────────────────
                    var bowlCenterY = 0.53f * scale; // Elevation of the upper bowl
                    var bowlRadius = 0.25f * scale; // Outer rim radius of the upper bowl
                    var bowlThickness = 0.07f * scale; // Vertical thickness
                    var bowlHalfThickness = bowlThickness * 0.5f;

                    var bowlExtent = MathF.Max(0f, bowlRadius - bowlHalfThickness);
                    var bowlCenter = position + new Vector3(0f, bowlCenterY, 0f);

                    // Angle offsets: cos(60°) = 0.5, sin(60°) ≈ 0.8660
                    var off60 = new Vector3(bowlExtent * 0.5f, 0f, bowlExtent * 0.8660f);
                    var off120 = new Vector3(-bowlExtent * 0.5f, 0f, bowlExtent * 0.8660f);

                    // 1. Spoke 1 (0° / X-Axis)
                    capsules.Add(new CollisionCapsule(
                        bowlCenter + new Vector3(-bowlExtent, 0f, 0f),
                        bowlCenter + new Vector3(bowlExtent, 0f, 0f),
                        bowlHalfThickness));

                    // 2. Spoke 2 (60°)
                    capsules.Add(new CollisionCapsule(
                        bowlCenter - off60,
                        bowlCenter + off60,
                        bowlHalfThickness));

                    // 3. Spoke 3 (120°)
                    capsules.Add(new CollisionCapsule(
                        bowlCenter - off120,
                        bowlCenter + off120,
                        bowlHalfThickness));

                    break;
                }

                // ── Mushroom Tree ─────────────────────────────────────────
                case SceneryType.MushroomTree:
                {
                    var totalHeight = type.GetCollisionHeight() * scale;
                    var trunkRadius = type.GetCollisionRadius() * scale;

                    // 1. Cap Radius: 3.2x is already a perfect fit in the top-down view
                    var capRadius = trunkRadius * 3.2f;
                    var capThickness = totalHeight * 0.10f; // Covers vertical depth of the dome
                    var capHalfThickness = capThickness * 0.5f;

                    // 2. Cap Height: Centers the disc directly inside the teal dome
                    var capCenterY = totalHeight * 1.02f;
                    var capBottomY = capCenterY - capHalfThickness;

                    // ─────────────────────────────────────────────────────────
                    // 1. Trunk (Vertical Capsule)
                    // ─────────────────────────────────────────────────────────
                    // Stalk hemisphere starts flush on the ground (Y=0)
                    // and extends up so its top hemisphere seamlessly meets the underside of the cap
                    var stalkBottom = position + new Vector3(0f, trunkRadius, 0f);
                    var stalkTop = position + new Vector3(0f, MathF.Max(trunkRadius, capBottomY - trunkRadius), 0f);

                    capsules.Add(new CollisionCapsule(stalkBottom, stalkTop, trunkRadius));

                    // ─────────────────────────────────────────────────────────
                    // 2. Cap (8-Way Rounded Disc)
                    // ─────────────────────────────────────────────────────────
                    var capExtent = MathF.Max(0f, capRadius - capHalfThickness);
                    var capCenter = position + new Vector3(0f, capCenterY, 0f);
                    var diag = capExtent * 0.7071f; // cos(45°)

                    // X-Axis (Left <-> Right)
                    capsules.Add(new CollisionCapsule(
                        capCenter + new Vector3(-capExtent, 0f, 0f),
                        capCenter + new Vector3(capExtent, 0f, 0f),
                        capHalfThickness));

                    // Z-Axis (Forward <-> Backward)
                    capsules.Add(new CollisionCapsule(
                        capCenter + new Vector3(0f, 0f, -capExtent),
                        capCenter + new Vector3(0f, 0f, capExtent),
                        capHalfThickness));

                    // Diagonal 1
                    capsules.Add(new CollisionCapsule(
                        capCenter + new Vector3(-diag, 0f, -diag),
                        capCenter + new Vector3(diag, 0f, diag),
                        capHalfThickness));

                    // Diagonal 2
                    capsules.Add(new CollisionCapsule(
                        capCenter + new Vector3(-diag, 0f, diag),
                        capCenter + new Vector3(diag, 0f, -diag),
                        capHalfThickness));

                    break;
                }

                case SceneryType.LilacRuin:
                {
                    var yaw = type.GetRotationYaw(seed);
                    var wallHeight = type.GetCollisionHeight() * scale;
                    var localCorner = new Vector3(-1.5f * scale, 0f, 1.5f * scale);

                    var beam1Start = position +
                                     RotateOffset(
                                         localCorner + new Vector3(0.7f * scale, wallHeight - 0.1f * scale,
                                             0.4f * scale), yaw);
                    var beam1End =
                        position + RotateOffset(new Vector3(2.5f * scale, -0.2f * scale, -0.5f * scale), yaw);
                    capsules.Add(new CollisionCapsule(beam1Start, beam1End, 0.15f * scale));

                    var beam2Start = position +
                                     RotateOffset(
                                         localCorner + new Vector3(0.4f * scale, (wallHeight * 0.8f) - 0.1f * scale,
                                             -1.0f * scale), yaw);
                    var beam2End = position + RotateOffset(new Vector3(1.0f * scale, -0.2f * scale, 2.0f * scale), yaw);
                    capsules.Add(new CollisionCapsule(beam2Start, beam2End, 0.12f * scale));

                    break;
                }

                // ── Default Fallback: Standard Vertical Capsule ────────────────
                // Covers Oak, Pine, DeadTree, MammothTree, Bonsai, Bamboo,
                // StandingTorch, Barrel, Campfire, Vases, Pillars, etc.
                default:
                {
                    // If the prop already has dedicated collision boxes (e.g. Table, Bed, Chest, Shelf), skip
                    if (type.GetCollisionBoxes(position, scale, seed).Count > 0)
                    {
                        break;
                    }

                    var r = type.GetCollisionRadius() * scale;
                    var h = type.GetCollisionHeight() * scale;

                    if (r > 0f && h > 0f)
                    {
                        var start = position;
                        var end = position + new Vector3(0f, h, 0f);
                        capsules.Add(new CollisionCapsule(start, end, r));
                    }

                    break;
                }
            }

            return capsules;
        }

        /// <summary>
        /// Generates oriented bounding boxes for scenery that's fundamentally flat architectural
        /// panels - wall runs, thresholds - where a box's sharp corners and uniform thickness
        /// are a closer match to the real shape than a capsule chain.
        /// </summary>
        /// <param name="type">The target scenery type.</param>
        /// <param name="position">World-space anchor position of the placement.</param>
        /// <param name="scale">World scale multiplier.</param>
        /// <param name="seed">Placement seed, used for rotation.</param>
        /// <returns>List of oriented boxes; empty if this type has no box-shaped collision.</returns>
        public static List<CollisionBox> GetCollisionBoxes(this SceneryType type, Vector3 position, float scale,
            int seed)
        {
            var boxes = new List<CollisionBox>();
            var yaw = type.GetRotationYaw(seed);

            switch (type)
            {
                // ── Torture Chair ────────────────────────────────────────────────
                case SceneryType.TortureChair:
                {
                    // 1. Seat Base (Lowered to 0.45f total height)
                    var seatHalfW = 0.3f * scale;
                    var seatHalfD = 0.3f * scale;
                    var seatHalfH = 0.3f * scale; // Total seat top sits at Y = 0.45f * scale
                    var seatCenter = position + RotateOffset(new Vector3(0f, seatHalfH, 0f), yaw);
                    boxes.Add(new CollisionBox(seatCenter, new Vector3(seatHalfW, seatHalfH, seatHalfD), yaw));

                    // 2. Backrest (Starts at seat top Y = 0.45f, extends up to Y = 1.45f)
                    var backHalfW = 0.3f * scale;
                    var backHalfD = 0.05f * scale;
                    var backHalfH = 0.5f * scale; // 1.0f total height

                    var seatTopY = seatHalfH * 2f; // 0.45f * scale
                    var backLocalOffset = new Vector3(0f, seatTopY + backHalfH, -seatHalfD + backHalfD);
                    boxes.Add(new CollisionBox(position + RotateOffset(backLocalOffset, yaw),
                        new Vector3(backHalfW, backHalfH, backHalfD), yaw));

                    // 3. Armrests (Slightly elevated above seat top level)
                    var armHalfW = 0.05f * scale;
                    var armHalfD = seatHalfD;
                    var armHalfH = 0.1f * scale; // 0.2f total height
                    var armCenterY = seatTopY + armHalfH; // Center sits at Y = 0.55f * scale

                    // Left Armrest (-X offset)
                    var leftArmLocal = new Vector3(-seatHalfW + armHalfW, armCenterY, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(leftArmLocal, yaw),
                        new Vector3(armHalfW, armHalfH, armHalfD), yaw));

                    // Right Armrest (+X offset)
                    var rightArmLocal = new Vector3(seatHalfW - armHalfW, armCenterY, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(rightArmLocal, yaw),
                        new Vector3(armHalfW, armHalfH, armHalfD), yaw));

                    break;
                }

                // ── Smithy (Main Forge Hearth & Smoke Hood) ───────────────────
                case SceneryType.Smithy:
                {
                    // Forge masonry hearth and chimney hood (box on -X, -Z)
                    var halfW = 0.34f * scale; // 0.68m total width
                    var halfD = 0.325f * scale; // 0.65m total depth
                    var totalHeight = 2.0f * scale; // Extends to chimney flue top
                    var halfH = totalHeight * 0.5f;

                    var localCenter = new Vector3(-0.32f * scale, halfH, -0.15f * scale);
                    var center = position + RotateOffset(localCenter, yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfW, halfH, halfD), yaw));

                    break;
                }

                // ── Machinery ──────────────────────────────────────────────────
                case SceneryType.Machinery:
                {
                    var halfW = 0.5f * scale;
                    var halfD = 0.5f * scale;
                    var halfH = 0.2f * scale;

                    var center = position + RotateOffset(new Vector3(0f, halfH, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfW, halfH, halfD), yaw));

                    break;
                }

                // ── Chair With Backrest ──────────────────────────────────────────
                case SceneryType.ChairWithBackrest:
                {
                    // 1. Seat Base
                    var seatHalfW = 0.2f * scale;
                    var seatHalfD = 0.25f * scale;
                    var seatHalfH = 0.25f * scale;
                    var seatCenter = position + RotateOffset(new Vector3(0f, seatHalfH, 0f), yaw);
                    boxes.Add(new CollisionBox(seatCenter, new Vector3(seatHalfW, seatHalfH, seatHalfD), yaw));

                    // 2. Backrest (Spanning across X, aligned to the -Z face)
                    var backHalfW = 0.2f * scale; // Width across X
                    var backHalfD = 0.05f * scale; // Thickness across Z
                    var backHalfH = 0.25f * scale; // Scaled down to match the mesh spindle height

                    var seatTopY = seatHalfH * 2f;
                    // Mirrored: sits at -Z instead of +Z
                    var backLocalOffset = new Vector3(0f, seatTopY + backHalfH, -seatHalfD + backHalfD);
                    boxes.Add(new CollisionBox(position + RotateOffset(backLocalOffset, yaw),
                        new Vector3(backHalfW, backHalfH, backHalfD), yaw));

                    break;
                }

                // ── Lilac Ruin ──────────────────────────────────────────
                case SceneryType.LilacRuin:
                {
                    var wallHeight = type.GetCollisionHeight() * scale;
                    var wallThick = MathF.Min(0.4f * scale, 0.5f);
                    var halfThick = wallThick * 0.5f;

                    // Shared corner anchor coordinate matching BonsaiFactory
                    var localCorner = new Vector3(-1.5f * scale, 0f, 1.5f * scale);
                    var corner = position + RotateOffset(localCorner, yaw);

                    // Corner Junction Box: fills the seam where the two sloped triangle runs join
                    boxes.Add(new CollisionBox(
                        Center: corner + new Vector3(0f, wallHeight * 0.5f, 0f),
                        HalfExtents: new Vector3(halfThick, wallHeight * 0.5f, halfThick),
                        Yaw: yaw));

                    break;
                }

                // ── TownWell ─────────────────────────────────────────
                case SceneryType.TownWell:
                {
                    var postHalfW = 0.05f * scale;
                    var postHalfD = 0.05f * scale;
                    var postBaseY = 0.65f * scale; // Sits on stone basin rim
                    var postTopY = 1.65f * scale; // Reaches underside of roof
                    var postHalfH = (postTopY - postBaseY) * 0.5f;
                    var postCenterY = postBaseY + postHalfH;
                    var postOffsetX = 0.48f * scale;

                    // Left Post (-X)
                    boxes.Add(new CollisionBox(
                        position + RotateOffset(new Vector3(-postOffsetX, postCenterY, 0f), yaw),
                        new Vector3(postHalfW, postHalfH, postHalfD),
                        yaw));

                    // Right Post (+X)
                    boxes.Add(new CollisionBox(
                        position + RotateOffset(new Vector3(postOffsetX, postCenterY, 0f), yaw),
                        new Vector3(postHalfW, postHalfH, postHalfD),
                        yaw));

                    break;
                }

                // ── Supply Crate ──────────────────────────────────────────
                case SceneryType.SupplyCrate:
                {
                    var h = 0.8f * scale;
                    var r = 0.4f * scale;

                    var center = position + new Vector3(0f, h * 0.5f, 0f);
                    boxes.Add(new CollisionBox(center, new Vector3(r, h * 0.5f, r), yaw));
                    break;
                }

                // ── One cell of a level-high staircase: a thin box per tread plus rail blockers ─────────
                // Treads are thin boxes, not solid blocks: the existing step-up snaps the feet onto a box top that is
                // within 0.45 m above them, and the space under the high treads stays walkable like in the mesh.
                // "scale" is the CELL SIZE here (see IsCellFitted / ColliderScale).
                case SceneryType.StairFlight:
                {
                    var steps = StairLayout.StepsPerCell(scale);
                    var tread = StairLayout.Tread(scale);
                    var width = StairLayout.Width(scale);
                    const float t = StairLayout.TreadThickness;
                    const float nose = StairLayout.Nosing;
                    const float railH = StairLayout.RailHeight;

                    for (var i = 0; i < steps; i++)
                    {
                        var front = StairLayout.StepFrontZ(i, scale);
                        var back = front - tread;
                        var top = StairLayout.StepTopY(i, scale);

                        // Tread slab
                        var treadCenter = new Vector3(0f, top - (t * 0.5f), (front + nose + back) * 0.5f);
                        boxes.Add(new CollisionBox(position + RotateOffset(treadCenter, yaw),
                            new Vector3(width * 0.5f, t * 0.5f, (tread + nose) * 0.5f), yaw));
                    }

                    // Rail blockers on both open sides, so nobody walks off the flight into the shaft.
                    // ONE tall box per side over the whole cell, NOT one per step: the solver treats any box whose top is
                    // less than 0.45 m above the feet as a step to climb, and the rail boxes of the lower steps are
                    // exactly that. A player pressed against the rail would be lifted onto the handrail.
                    const float railHalfX = 0.04f;
                    var railTop = StairLayout.RisePerCell(scale) + StairLayout.Riser(scale) + railH;
                    for (var side = -1; side <= 1; side += 2)
                    {
                        var railCenter = new Vector3(side * ((width * 0.5f) + railHalfX), railTop * 0.5f, 0f);
                        boxes.Add(new CollisionBox(position + RotateOffset(railCenter, yaw),
                            new Vector3(railHalfX, railTop * 0.5f, scale * 0.5f), yaw));
                    }

                    break;
                }

                // ── Stone Archway (Dual Pillars + Top Crossbeam) ───────────────
                case SceneryType.StoneArch:
                case SceneryType.AncientArch:
                {
                    var totalHeight = 5.0f * scale;
                    var spanWidth = 3.5f * scale;
                    var thickness = 1.0f * scale;
                    var pillarWidth = 0.8f * scale;

                    // 1. Left Pillar Box
                    var halfPillarW = pillarWidth * 0.5f;
                    var halfPillarH = (totalHeight - pillarWidth) * 0.5f;
                    var halfPillarD = thickness * 0.5f;

                    var leftLocalCenter = new Vector3((-spanWidth / 2f) + halfPillarW, halfPillarH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(leftLocalCenter, yaw),
                        new Vector3(halfPillarW, halfPillarH, halfPillarD), yaw));

                    // 2. Right Pillar Box
                    var rightLocalCenter = new Vector3((spanWidth / 2f) - halfPillarW, halfPillarH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(rightLocalCenter, yaw),
                        new Vector3(halfPillarW, halfPillarH, halfPillarD), yaw));

                    // 3. Top Capstone Crossbeam Box
                    var halfCapW = spanWidth * 0.5f;
                    var halfCapH = pillarWidth * 0.5f;
                    var halfCapD = (thickness + (0.1f * scale)) * 0.5f;

                    var capLocalCenter = new Vector3(0f, totalHeight - halfCapH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(capLocalCenter, yaw),
                        new Vector3(halfCapW, halfCapH, halfCapD), yaw));

                    break;
                }

                // ── Hexagonal Stone Pillar ─────────────────────────────────────
                case SceneryType.Pillar:
                {
                    var radius = 0.45f * scale;
                    var height = 5.0f * scale; // Average height matching procedural bounds
                    var halfHeight = height * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(radius, halfHeight, radius), yaw));
                    break;
                }

                // ── Wooden Sitting Stool ───────────────────────────────────────
                case SceneryType.Stool:
                {
                    var halfSize = 0.2f * scale;
                    var halfHeight = 0.25f * scale;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfSize, halfHeight, halfSize), yaw));
                    break;
                }

                // ── Wooden Dining Table ────────────────────────────────────────
                case SceneryType.TableCluttered:
                case SceneryType.Table:
                {
                    var halfWidth = 0.6f * scale; // 1.2f total length along X
                    var halfDepth = 0.4f * scale; // 0.8f total depth along Z
                    var totalHeight = 0.8f * scale; // 0.8f total height

                    // 1. Tabletop slab (top box)
                    var topHalfH = 0.04f * scale; // 0.08f slab thickness
                    var topCenterY = totalHeight - topHalfH;
                    var topCenter = position + RotateOffset(new Vector3(0f, topCenterY, 0f), yaw);
                    boxes.Add(new CollisionBox(topCenter, new Vector3(halfWidth, topHalfH, halfDepth), yaw));

                    // 2 & 3. Side leg assemblies (two side boxes)
                    var legHalfW = 0.08f * scale; // Thickness of the leg/stretcher frame
                    var legHalfH = (totalHeight - (topHalfH * 2f)) * 0.5f; // Sits flush from ground to under the slab
                    var legOffsetX = halfWidth - legHalfW;

                    // Left side support (-X)
                    var leftCenter = position + RotateOffset(new Vector3(-legOffsetX, legHalfH, 0f), yaw);
                    boxes.Add(new CollisionBox(leftCenter, new Vector3(legHalfW, legHalfH, halfDepth), yaw));

                    // Right side support (+X)
                    var rightCenter = position + RotateOffset(new Vector3(legOffsetX, legHalfH, 0f), yaw);
                    boxes.Add(new CollisionBox(rightCenter, new Vector3(legHalfW, legHalfH, halfDepth), yaw));

                    break;
                }

                // ── Bed ──────────────────────────────────────────────────────────
                case SceneryType.Bed:
                {
                    var halfWidth = 0.4f * scale; // 1.6f total width
                    var halfDepth = 0.8f * scale; // 1.0f total depth
                    var halfHeight = 0.2f * scale; // 0.8f total height

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── StrawBed ──────────────────────────────────────────────────────────
                case SceneryType.StrawBed:
                {
                    var halfWidth = 0.4f * scale; // 1.6f total width
                    var halfDepth = 0.8f * scale; // 1.0f total depth
                    var halfHeight = 0.1f * scale; // 0.8f total height

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Hedges ──────────────────────────────────────────────────────────
                case SceneryType.Hedges:
                {
                    // 1. Choose or derive the direction angle (yaw in radians)
                    // yaw Or manually: 0f (North), MathF.PI (South), etc. via type.GetRotationYaw(seed);

                    // 2. Define local half-extents (Half-Width X, Half-Height Y, Half-Depth Z)
                    var halfExtents = new Vector3(0.5f * scale, 0.4f * scale, 0.2f * scale);

                    // 3. Offset the center up by halfHeight so the bottom sits flush on Y = 0
                    //    (Y-only offsets aren't affected by Y-rotation, but RotateOffset keeps it consistent)
                    var localCenter = new Vector3(0f, halfExtents.Y, 0f);
                    var worldCenter = position + RotateOffset(localCenter, yaw);

                    // 4. Create the box with the world center, local extents, and yaw
                    boxes.Add(new CollisionBox(worldCenter, halfExtents, yaw));
                    break;
                }

                // ── Prison Bars ────────────────────────────────────────────────
                case SceneryType.PrisonBars:
                {
                    var halfWidth = 0.5f * scale;
                    var halfDepth = 0.03f * scale;
                    var totalHeight = 2f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Ladder ────────────────────────────────────────────────
                case SceneryType.Ladder:
                {
                    var halfWidth = 0.2f * scale;
                    var halfDepth = 0.03f * scale;
                    var totalHeight = 2f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── XCross ────────────────────────────────────────────────
                case SceneryType.XCross:
                {
                    var halfWidth = 0.6f * scale;
                    var halfDepth = 0.08f * scale;
                    var totalHeight = 1.8f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Notice Boad ────────────────────────────────────────────────
                case SceneryType.NoticeBoard:
                {
                    var halfWidth = 0.6f * scale;
                    var halfDepth = 0.03f * scale;
                    var totalHeight = 1.6f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Doors ────────────────────────────────────────────────
                case SceneryType.DoorWoodClosed:
                case SceneryType.DoorPrisonClosed:
                {
                    var halfWidth = 0.5f * scale;
                    var halfDepth = 0.03f * scale;
                    var totalHeight = 2f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Wheel Standing ────────────────────────────────────────────────
                case SceneryType.WheelStanding:
                {
                    var halfWidth = 0.8f * scale;
                    var halfDepth = 0.03f * scale;
                    var totalHeight = 2f * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Ritual Altar Bounding Box ────────────────────────────────
                case SceneryType.RitualAltar:
                {
                    var w = 1.4f * scale;
                    var h = 0.7f * scale;
                    var d = 0.8f * scale;

                    var halfW = w * 0.5f;
                    var halfH = h * 0.5f;
                    var halfD = d * 0.5f;

                    // Main sacrificial stone block collision (decor candles omitted)
                    var localCenter = new Vector3(0f, halfH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(localCenter, yaw),
                        new Vector3(halfW, halfH, halfD), yaw));

                    break;
                }

                // ── Bookcase / Shelf (Empty & Populated) ────────────────────────
                case SceneryType.ShelfEmpty:
                case SceneryType.ShelfBooks:
                {
                    var halfWidth = 0.5f * scale; // 1.2f total width
                    var halfDepth = 0.2f * scale; // 0.4f total depth
                    var halfHeight = 0.8f * scale; // 1.8f total height

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Weapon Rack (Empty & Full) ────────────────────────
                case SceneryType.WeaponRackEmpty:
                case SceneryType.WeaponRackFull:
                {
                    var halfWidth = 0.7f * scale;
                    var halfDepth = 0.1f * scale;
                    var halfHeight = 0.65f * scale;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Ruined Column Broken ───────────────────────────────────────────
                case SceneryType.RuinedColumnBroken:
                {
                    // 1. Fallen Column Shaft
                    //                      X              Y (Half-Height)     Z
                    var shaftHalf = new Vector3(0.1f * scale, 0.2f * scale, 0.29f * scale);
                    var shaftOffset = new Vector3(0.36f * scale, shaftHalf.Y, 0.14f * scale);

                    boxes.Add(CreateBoxDegrees(position, shaftOffset, shaftHalf, yaw, extraDegrees: 117));

                    // 2. Fallen Capital Block
                    //                    X              Y (Half-Height)     Z
                    var capHalf = new Vector3(0.24f * scale, 0.16f * scale, 0.20f * scale);
                    var capOffset = new Vector3(0.72f * scale, capHalf.Y, 0.28f * scale);

                    boxes.Add(CreateBoxDegrees(position, capOffset, capHalf, yaw, extraDegrees: 45.0f));

                    break;
                }

                // ── Treasure Chest (Closed) ──────────────────────────────
                case SceneryType.TreasureChestClosed:
                {
                    var halfWidth = 0.4f * scale;
                    var halfDepth = 0.25f * scale;
                    var halfHeight = halfDepth * scale;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Treasure Chest (Open) ──────────────────────────────
                case SceneryType.TreasureChestOpen:
                {
                    var halfWidth = 0.4f * scale;
                    var halfDepth = 0.30f * scale; // Expanded to cover open lid
                    var totalHeight = 0.65f * scale; // Lid stands slightly taller
                    var halfHeight = totalHeight * 0.5f;

                    // Shift center backward (-Z) by half of the added rear overhang
                    var center = position + RotateOffset(new Vector3(0f, halfHeight, -0.05f * scale), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Hanging Cages (Elevated Box: Passable underneath on ground level) ──
                case SceneryType.HangingCageClosed:
                case SceneryType.HangingCageOpen:
                {
                    var radius = 0.38f * scale;
                    var cageHeight = 1.4f * scale;
                    var cageBottomY = 0.8f * scale; // Suspended 0.8 units off the floor
                    var halfHeight = cageHeight * 0.5f;

                    // Box center starts at Y = 1.5s, bottom edge sits at Y = 0.8s
                    var center = position + RotateOffset(new Vector3(0f, cageBottomY + halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(radius, halfHeight, radius), yaw));
                    break;
                }

                // ── Stretching Rack (Low Table: Step-on / Climbable Height) ──────────────
                case SceneryType.StretchingRack:
                {
                    var halfWidth = 0.425f * scale; // Total width 0.85f
                    var halfLength = 1.0f * scale; // Total length 2.0f
                    var halfHeight = 0.225f * scale; // Total height 0.45f

                    // Rest flush on Y=0; height (0.45s) allows step-up mechanics to step onto it
                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfLength), yaw));
                    break;
                }

                // ── Crypt Sarcophagus (Open/Closed) ──────────────
                case SceneryType.CryptSarcophagusOpen:
                case SceneryType.CryptSarcophagusClosed:
                {
                    var halfWidth = 0.425f * scale; // Total width 0.85f
                    var halfLength = 0.9f * scale; // Total length 1.8f
                    var halfHeight = 0.350f * scale; // Total height 0.7f

                    // Rest flush on Y=0; height (0.45s) allows step-up mechanics to step onto it
                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfLength), yaw));
                    break;
                }

                // ── Wooden Horse ───────────────────────────────────────────────────────────
                case SceneryType.WoodenHorse:
                {
                    var halfWidth = 0.3f * scale; // Total width 0.6f
                    var halfLength = 0.65f * scale; // Total length 1.3f
                    var halfHeight = 0.55f * scale; // Total height 1.1f

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfLength), yaw));
                    break;
                }

                // ── Pillory ────────────────────────────────────────────────────────────────
                case SceneryType.PilloryClosed:
                case SceneryType.PilloryOpen:
                {
                    var halfWidth = 0.475f * scale; // Total width 0.95f
                    var halfDepth = 0.2f * scale; // Base footing depth 0.4f
                    var totalHeight = (type == SceneryType.PilloryOpen ? 1.9f : 1.7f) * scale;
                    var halfHeight = totalHeight * 0.5f;

                    var center = position + RotateOffset(new Vector3(0f, halfHeight, 0f), yaw);
                    boxes.Add(new CollisionBox(center, new Vector3(halfWidth, halfHeight, halfDepth), yaw));
                    break;
                }

                // ── Iron Maiden ───────────────────────────────────────────────────────────
                case SceneryType.IronMaiden:
                {
                    // Overall frame dimensions
                    var halfW = 0.4f * scale; // Half-width across X
                    var halfD = 0.3f * scale; // Half-depth across Z
                    var halfH = 0.7f * scale; // Half-height of the main walls
                    var wallT = 0.04f * scale; // Wall / door half-thickness

                    // 1. Back Wall (Blocks the back at -Z)
                    var backHalf = new Vector3(halfW, halfH, wallT);
                    var backOffset = new Vector3(0f, halfH, -halfD + wallT);
                    boxes.Add(new CollisionBox(position + RotateOffset(backOffset, yaw), backHalf, yaw));

                    // 2. Left Side / Door (Runs along -X, from back to front)
                    var sideDepth = halfD - wallT;
                    var sideHalf = new Vector3(wallT, halfH, sideDepth);
                    var leftOffset = new Vector3(-halfW + wallT, halfH, wallT);
                    boxes.Add(new CollisionBox(position + RotateOffset(leftOffset, yaw), sideHalf, yaw));

                    // 3. Right Side / Door (Runs along +X, from back to front)
                    var rightOffset = new Vector3(halfW - wallT, halfH, wallT);
                    boxes.Add(new CollisionBox(position + RotateOffset(rightOffset, yaw), sideHalf, yaw));

                    // 4. (Optional) Step-On Floor Slab
                    // Keep halfH <= 0.20f * scale (height <= 0.40m) so character step-up mechanics can climb onto it
                    var floorHalfH = 0.05f * scale;
                    var floorOffset = new Vector3(0f, floorHalfH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(floorOffset, yaw),
                        new Vector3(halfW - (wallT * 2f), floorHalfH, halfD - wallT), yaw));

                    // 5. Top Smaller Collision Box (Sits flush on top of the main body)
                    var mainTopY = halfH * 2f;
                    var topHalfW = 0.25f * scale; // Narrower width
                    var topHalfH = 0.20f * scale; // Half-height of top section (0.3m total)
                    var topHalfD = 0.25f * scale; // Narrower depth

                    var topOffset = new Vector3(0f, mainTopY + topHalfH, 0f);
                    boxes.Add(new CollisionBox(position + RotateOffset(topOffset, yaw),
                        new Vector3(topHalfW, topHalfH, topHalfD), yaw));

                    break;
                }
            }

            return boxes;
        }

        /// <summary>
        /// Gets the collision triangle.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="position">The position.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>List of oriented triangles; empty if this type has no box-shaped collision.</returns>
        public static List<CollisionTriangle> GetCollisionTriangles(this SceneryType type, Vector3 position,
            float scale,
            int seed)
        {
            var triangles = new List<CollisionTriangle>();

            switch (type)
            {
                // ── TownWell (A-frame gable roof) ──────────────────────
                case SceneryType.TownWell:
                {
                    var yaw = type.GetRotationYaw(seed);

                    // Abmessungen passend zu BakeTownWell (roofW: ~1.50, roofD: ~1.40)
                    var halfW = 0.75f * scale; // Halbe Breite (X-Richtung: First bis Traufe)
                    var halfD = 0.70f * scale; // Halbe Tiefe (Z-Richtung: Firstbalkenlänge)

                    var highY = 1.95f * scale; // Firsthöhe (Zentrum X = 0)
                    var lowY = 1.45f * scale; // Traufhöhe außen (X = ±halfW)

                    // ── 1. Linke Dachfläche (-X Seite: fällt von X=0 nach -halfW ab) ─
                    var lEavesBack = new Vector3(-halfW, lowY, -halfD);
                    var lEavesFront = new Vector3(-halfW, lowY, halfD);
                    var lRidgeFront = new Vector3(0f, highY, halfD);
                    var lRidgeBack = new Vector3(0f, highY, -halfD);

                    // Dreiecke mit Normalen nach außen/oben
                    triangles.Add(CreateTriangle(position, lEavesBack, lEavesFront, lRidgeFront, yaw));
                    triangles.Add(CreateTriangle(position, lEavesBack, lRidgeFront, lRidgeBack, yaw));

                    // ── 2. Rechte Dachfläche (+X Seite: fällt von X=0 nach +halfW ab) ─
                    var rEavesBack = new Vector3(halfW, lowY, -halfD);
                    var rEavesFront = new Vector3(halfW, lowY, halfD);
                    var rRidgeFront = new Vector3(0f, highY, halfD);
                    var rRidgeBack = new Vector3(0f, highY, -halfD);

                    // Dreiecke mit Normalen nach außen/oben
                    triangles.Add(CreateTriangle(position, rEavesBack, rRidgeBack, rRidgeFront, yaw));
                    triangles.Add(CreateTriangle(position, rEavesBack, rRidgeFront, rEavesFront, yaw));

                    break;
                }

                // ── Lilac Ruin ──────────────────────────────────────────
                case SceneryType.LilacRuin:
                {
                    var yaw = type.GetRotationYaw(seed);
                    var wallWidth = 4.0f * scale;
                    var wallHeight = type.GetCollisionHeight() * scale;
                    var wallBh = wallHeight * 0.8f;
                    var wallThick = MathF.Min(0.4f * scale, 0.5f);
                    var halfThick = wallThick * 0.5f;

                    var localCorner = new Vector3(-1.5f * scale, 0f, 1.5f * scale);

                    // ── Wall A (Slopes down along +X from the corner junction) ──────
                    // Starts at X = +halfThick flush against the corner box, slopes to Y = 0 at X = wallWidth
                    var aStartX = halfThick;
                    var aEndX = wallWidth;

                    var aTopNearBack = localCorner + new Vector3(aStartX, wallHeight, -halfThick);
                    var aTopNearFront = localCorner + new Vector3(aStartX, wallHeight, halfThick);
                    var aBotNearBack = localCorner + new Vector3(aStartX, 0f, -halfThick);
                    var aBotNearFront = localCorner + new Vector3(aStartX, 0f, halfThick);
                    var aBotFarBack = localCorner + new Vector3(aEndX, 0f, -halfThick);
                    var aBotFarFront = localCorner + new Vector3(aEndX, 0f, halfThick);

                    // Outer & Inner vertical faces
                    triangles.Add(CreateTriangle(position, aBotNearFront, aBotFarFront, aTopNearFront, yaw));
                    triangles.Add(CreateTriangle(position, aBotNearBack, aTopNearBack, aBotFarBack, yaw));

                    // Sloped top broken edge (two triangles forming the sloped ramp quad)
                    triangles.Add(CreateTriangle(position, aTopNearBack, aBotFarBack, aTopNearFront, yaw));
                    triangles.Add(CreateTriangle(position, aTopNearFront, aBotFarBack, aBotFarFront, yaw));

                    // ── Wall B (Slopes down along -Z from the corner junction) ──────
                    // Starts at Z = -halfThick flush against the corner box, slopes to Y = 0 at Z = -wallWidth
                    var bStartZ = -halfThick;
                    var bEndZ = -wallWidth;

                    var bTopNearLeft = localCorner + new Vector3(-halfThick, wallBh, bStartZ);
                    var bTopNearRight = localCorner + new Vector3(halfThick, wallBh, bStartZ);
                    var bBotNearLeft = localCorner + new Vector3(-halfThick, 0f, bStartZ);
                    var bBotNearRight = localCorner + new Vector3(halfThick, 0f, bStartZ);
                    var bBotFarLeft = localCorner + new Vector3(-halfThick, 0f, bEndZ);
                    var bBotFarRight = localCorner + new Vector3(halfThick, 0f, bEndZ);

                    // Outer & Inner vertical faces
                    triangles.Add(CreateTriangle(position, bBotNearLeft, bTopNearLeft, bBotFarLeft, yaw));
                    triangles.Add(CreateTriangle(position, bBotNearRight, bBotFarRight, bTopNearRight, yaw));

                    // Sloped top broken edge
                    triangles.Add(CreateTriangle(position, bTopNearLeft, bTopNearRight, bBotFarLeft, yaw));
                    triangles.Add(CreateTriangle(position, bTopNearRight, bBotFarRight, bBotFarLeft, yaw));

                    break;
                }

                default:
                    return triangles;
            }

            return triangles; // Gibt die befüllte Liste zurück statt einer leeren Instanz
        }

        /// <summary>
        /// Extends SceneryType to expose its maximum outer horizontal collision reach in scale units.
        /// Used by CollisionService to determine if an object exceeds the local cell search radius.
        /// </summary>
        /// <param name="type">The target scenery type.</param>
        /// <returns>The maximum radius or reach extent in cell units.</returns>
        public static float GetMaxCollisionReach(this SceneryType type)
        {
            return type switch
            {
                // MammothTree buttress roots can reach up to ~16-18 units out from origin
                SceneryType.MammothTree => 18.0f,
                SceneryType.LilacRuin => 4.0f,
                SceneryType.TownWell => 1.1f,
                SceneryType.JaggedRock => 1.8f,
                SceneryType.Smithy => 1.0f,
                _ => type.GetCollisionRadius()
            };
        }

        /// <summary>
        /// Extends SceneryType to natively expose the light intensity it emits into the voxel
        /// light grid, 0-15 (MapConstants.MaxLightLevel). Sits here rather than on SceneryPlacement,
        /// matching GetCollisionRadius / GetCollisionHeight / GetCollisionCapsules.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The light intensity emitted by the scenery type into the voxel light grid, 0-15 (MapConstants.MaxLightLevel).</returns>
        public static byte GetLightEmission(this SceneryType type) => type switch
        {
            SceneryType.CrystalShard => 12,
            SceneryType.Campfire => 14,
            SceneryType.Smithy => 14,
            SceneryType.Fireplace => 15,
            SceneryType.WallTorch => 14,
            SceneryType.StandingTorch => 13,
            SceneryType.HandheldCandle => 13,
            SceneryType.TableCluttered => 13,
            SceneryType.RitualAltar => 12,
            SceneryType.MushroomTree => 5,
            SceneryType.Mushrooms => 4, // ground clusters - subtler than MushroomTree's 5
            _ => 0
        };

        /// <summary>
        /// Constructs a CollisionBox using local offsets and an additional relative yaw angle in degrees.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="localOffset">The local offset.</param>
        /// <param name="halfExtents">The half extents.</param>
        /// <param name="yaw">The yaw.</param>
        /// <param name="extraDegrees">The extra degrees.</param>
        /// <returns>The constructed <see cref="CollisionBox"/>.</returns>
        private static CollisionBox CreateBoxDegrees(
            Vector3 position,
            Vector3 localOffset,
            Vector3 halfExtents,
            float yaw,
            float extraDegrees = 0.0f)
        {
            var boxYaw = yaw + ToRad(extraDegrees);
            var worldCenter = position + RotateOffset(localOffset, yaw);
            return new CollisionBox(worldCenter, halfExtents, boxYaw);
        }

        /// <summary>
        /// Constructs an oriented CollisionTriangle from local-space vertices, rotating them into world space.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="localA">The local a.</param>
        /// <param name="localB">The local b.</param>
        /// <param name="localC">The local c.</param>
        /// <param name="yaw">The yaw.</param>
        /// <returns>The constructed <see cref="CollisionTriangle"/>.</returns>
        private static CollisionTriangle CreateTriangle(
            Vector3 position,
            Vector3 localA,
            Vector3 localB,
            Vector3 localC,
            float yaw)
        {
            var a = position + RotateOffset(localA, yaw);
            var b = position + RotateOffset(localB, yaw);
            var c = position + RotateOffset(localC, yaw);

            var minY = MathF.Min(a.Y, MathF.Min(b.Y, c.Y));
            var maxY = MathF.Max(a.Y, MathF.Max(b.Y, c.Y));

            return new CollisionTriangle(a, b, c, yaw, minY, maxY);
        }

        /// <summary>
        /// Rotates a local 3D offset point by the Y-axis yaw angle in radians.
        /// </summary>
        /// <param name="localOffset">The local offset.</param>
        /// <param name="yaw">The yaw.</param>
        /// <returns>The rotated local offset.</returns>
        private static Vector3 RotateOffset(Vector3 localOffset, float yaw)
        {
            if (MathF.Abs(yaw) < 0.001f) return localOffset;

            return Vector3.Transform(localOffset, Matrix4x4.CreateRotationY(yaw));
        }
    }
}