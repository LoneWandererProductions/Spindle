/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        Camera3D.cs
 * PURPOSE:     Camera class for 3D dungeon view
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales
{
    /// <summary>
    ///     Represents a camera in 3D space for the dungeon view.
    /// </summary>
    public class Camera3D
    {
        /// <summary>
        /// Gets or sets the position.
        /// Player eye height
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public Vector3 Position { get; set; } = new(0, 1.7f, 0);


        /// <summary>
        /// Gets or sets the yaw.
        /// Horizontal rotation
        /// </summary>
        /// <value>
        /// The yaw.
        /// </value>
        public float Yaw { get; set; }

        /// <summary>
        /// Gets or sets the pitch.
        /// Vertical rotation
        /// </summary>
        /// <value>
        /// The pitch.
        /// </value>
        public float Pitch { get; set; }

        /// <summary>
        /// Gets or sets the fov.
        /// 60° is a common default for a first-person camera
        /// </summary>
        /// <value>
        /// The fov.
        /// </value>
        public float Fov { get; set; } = (float)(Math.PI / 3);


        /// <summary>
        /// Gets the view matrix.
        /// </summary>
        /// <returns>The view matrix.</returns>
        public Matrix4x4 GetViewMatrix()
        {
            // Build orientation
            var rotation = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, 0);

            // Basis vectors
            var forward = Vector3.TransformNormal(Vector3.UnitZ, rotation);
            var up = Vector3.TransformNormal(Vector3.UnitY, rotation);

            return Matrix4x4.CreateLookAt(Position, Position + forward, up);
        }

        /// <summary>
        /// Gets the projection matrix.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio.</param>
        /// <param name="near">The near plane distance.</param>
        /// <param name="far">The far plane distance.</param>
        /// <returns>The projection matrix.</returns>
        public Matrix4x4 GetProjectionMatrix(float aspectRatio, float near = 0.1f, float far = 100f)
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(Fov, aspectRatio, near, far);
        }
    }
}