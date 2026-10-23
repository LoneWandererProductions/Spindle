/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        CollisionCapsule.cs
 * PURPOSE:     Represents a 3D line-segment capsule primitive for complex physics.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales.Structs
{
    /// <summary>
    /// Defines a 3D capsule collision primitive (line segment with an outer radius).
    /// </summary>
    public readonly struct CollisionCapsule
    {
        /// <summary>
        /// Gets the start.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public Vector3 Start { get; }

        /// <summary>
        /// Gets the end.
        /// </summary>
        /// <value>
        /// The end.
        /// </value>
        public Vector3 End { get; }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>
        /// The radius.
        /// </value>
        public float Radius { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollisionCapsule"/> struct.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="radius">The radius.</param>
        public CollisionCapsule(Vector3 start, Vector3 end, float radius)
        {
            Start = start;
            End = end;
            Radius = radius;
        }
    }
}