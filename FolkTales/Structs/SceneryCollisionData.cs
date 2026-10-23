/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        SceneryCollisionData.cs
 * PURPOSE:     Unified container for all collision geometry associated with a scenery object.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Structs
{
    /// <summary>
    /// Unified container for all collision geometry associated with a scenery object.
    /// </summary>
    public struct SceneryCollisionData
    {
        /// <summary>
        /// The capsules
        /// </summary>
        public List<CollisionCapsule> Capsules;

        /// <summary>
        /// The boxes
        /// </summary>
        public List<CollisionBox> Boxes;

        /// <summary>
        /// The triangles
        /// </summary>
        public List<CollisionTriangle> Triangles;

        /// <summary>
        /// Quick check to see if the asset is completely passable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has collision; otherwise, <c>false</c>.
        /// </value>
        public readonly bool HasCollision =>
            Capsules != null && Capsules.Count > 0 ||
            Boxes != null && Boxes.Count > 0 ||
            Triangles != null && Triangles.Count > 0;
    }
}