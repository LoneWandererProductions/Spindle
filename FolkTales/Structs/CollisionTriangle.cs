/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        CollisionTriangle.cs
 * PURPOSE:     Oriented bounding triangle collision primitive.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales.Structs
{
    /// <summary>
    /// Collision Triangle primitive.
    /// </summary>
    public struct CollisionTriangle
    {
        /// <summary>
        /// a
        /// </summary>
        public Vector3 A; // First vertex (floor level or base)

        /// <summary>
        /// The b
        /// </summary>
        public Vector3 B; // Second vertex

        /// <summary>
        /// The c
        /// </summary>
        public Vector3 C; // Third vertex

        /// <summary>
        /// The yaw
        /// </summary>
        public readonly float Yaw;

        /// <summary>
        /// The minimum y
        /// </summary>
        public readonly float MinY;

        /// <summary>
        /// The maximum y
        /// </summary>
        public readonly float MaxY;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollisionTriangle"/> struct.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="yaw">The yaw.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        public CollisionTriangle(Vector3 a, Vector3 b, Vector3 c, float yaw, float minY, float maxY)
        {
            A = a;
            B = b;
            C = c;
            Yaw = yaw;
            MinY = minY;
            MaxY = maxY;
        }
    }
}