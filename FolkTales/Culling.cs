/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales;
 * FILE:        Culling.cs
 * PURPOSE:     Simple utility class for frustum and backface culling of triangles, cells, points, and spheres in the terrain viewport.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales
{
    /// <summary>
    /// Lightweight frustum and backface culling helper
    /// </summary>
    public static class Culling
    {
        /// <summary>
        /// Checks if a triangle is visible given a view-projection matrix and optional backface culling.
        /// </summary>
        /// <param name="v0">Triangle vertex 0 (world or view space)</param>
        /// <param name="v1">Triangle vertex 1</param>
        /// <param name="v2">Triangle vertex 2</param>
        /// <param name="vp">Optional view-projection matrix for frustum culling</param>
        /// <param name="enableBackfaceCulling">Enable backface culling</param>
        /// <returns>True if triangle should be rendered</returns>
        public static bool IsTriangleVisible(Vector3 v0, Vector3 v1, Vector3 v2, Matrix4x4? vp = null,
            bool enableBackfaceCulling = true)
        {
            // Frustum culling
            if (vp.HasValue)
            {
                var min = Vector3.Min(Vector3.Min(v0, v1), v2);
                var max = Vector3.Max(Vector3.Max(v0, v1), v2);
                if (!IsCellVisible(vp.Value, min, max))
                    return false;
            }

            // Backface culling
            if (enableBackfaceCulling)
            {
                var normal = Vector3.Cross(v1 - v0, v2 - v0);
                if (Vector3.Dot(normal, v0) >= 0f) // assumes camera at origin in view space
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a cell (axis-aligned cube) is at least partially inside the view frustum.
        /// </summary>
        /// <param name="vp">The vp.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns>
        ///   <c>true</c> if [is cell visible] [the specified vp]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCellVisible(Matrix4x4 vp, Vector3 min, Vector3 max)
        {
            // 8 corners of the cube
            var corners = new[]
            {
                new Vector3(min.X, min.Y, min.Z),
                new Vector3(max.X, min.Y, min.Z),
                new Vector3(min.X, max.Y, min.Z),
                new Vector3(max.X, max.Y, min.Z),
                new Vector3(min.X, min.Y, max.Z),
                new Vector3(max.X, min.Y, max.Z),
                new Vector3(min.X, max.Y, max.Z),
                new Vector3(max.X, max.Y, max.Z)
            };

            foreach (var corner in corners)
            {
                var clip = Vector3.Transform(corner, vp);

                // simple frustum check in clip space
                if (clip.Z > 0 && clip.X >= -clip.Z && clip.X <= clip.Z &&
                    clip.Y >= -clip.Z && clip.Y <= clip.Z)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a point is visible in the frustum.
        /// </summary>
        /// <param name="vp">The vp.</param>
        /// <param name="point">The point.</param>
        /// <returns>
        ///   <c>true</c> if [is point visible] [the specified vp]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPointVisible(Matrix4x4 vp, Vector3 point)
        {
            var clip = Vector3.Transform(point, vp);
            return clip.Z > 0 &&
                   clip.X >= -clip.Z && clip.X <= clip.Z &&
                   clip.Y >= -clip.Z && clip.Y <= clip.Z;
        }

        /// <summary>
        /// Checks if a sphere is at least partially visible.
        /// </summary>
        /// <param name="vp">The vp.</param>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>
        ///   <c>true</c> if [is sphere visible] [the specified vp]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSphereVisible(Matrix4x4 vp, Vector3 center, float radius)
        {
            var clip = Vector3.Transform(center, vp);

            // simple AABB-like approximation in clip space
            return clip.Z - radius <= clip.Z &&
                   clip.X + radius >= -clip.Z && clip.X - radius <= clip.Z &&
                   clip.Y + radius >= -clip.Z && clip.Y - radius <= clip.Z;
        }
    }
}