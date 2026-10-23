/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        BakedSceneryCollider.cs
 * PURPOSE:     Collection of Collision containers for complex collosion objects.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Structs
{
    /// <summary>
    /// Pre-calculated world-space colliders for a single scenery placement instance.
    /// </summary>
    public sealed class BakedSceneryCollider
    {
        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>
        /// The radius.
        /// </value>
        public float Radius { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public float Height { get; }

        /// <summary>
        /// Gets a value indicating whether custom box, capsule, or triangle geometry exists.
        /// Pre-calculated once at bake time to prevent LINQ / allocation checks on the hotpath.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has custom geometry; otherwise, <c>false</c>.
        /// </value>
        public bool HasCustomGeometry { get; }

        /// <summary>
        /// Gets the boxes.
        /// </summary>
        /// <value>
        /// The boxes.
        /// </value>
        public CollisionBox[] Boxes { get; }

        /// <summary>
        /// Gets the capsules.
        /// </summary>
        /// <value>
        /// The capsules.
        /// </value>
        public CollisionCapsule[] Capsules { get; }

        /// <summary>
        /// Gets the triangles.
        /// </summary>
        /// <value>
        /// The triangles.
        /// </value>
        public CollisionTriangle[] Triangles { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BakedSceneryCollider"/> class.
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <param name="scale">The scale.</param>
        public BakedSceneryCollider(SceneryPlacement prop, float scale)
        {
            // Cell-fitted types (stairs) are sized from the placement's cell, not from the generic prop scale.
            if (prop.Type.IsCellFitted()) scale = prop.FitScale;

            Radius = prop.Type.GetCollisionRadius() * scale;
            Height = prop.Type.GetCollisionHeight() * scale;
            Boxes = prop.Type.GetCollisionBoxes(prop.Position, scale, prop.Seed).ToArray();
            Capsules = prop.Type.GetCollisionCapsules(prop.Position, scale, prop.Seed).ToArray();
            Triangles = prop.Type.GetCollisionTriangles(prop.Position, scale, prop.Seed).ToArray();

            // Pre-calculated flag for zero-allocation check during physics updates
            HasCustomGeometry = Boxes.Length > 0 || Capsules.Length > 0 || Triangles.Length > 0;
        }
    }
}