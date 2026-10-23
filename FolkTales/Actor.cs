/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        Actor.cs
 * PURPOSE:     Actor class handling NPC positioning, physics, rendering, and pathfinding profiles.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales
{
    public class Actor
    {
        /// <summary>
        /// Gets or sets the unique actor identifier.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets or sets the pathfinding physical dimensions and traversal limits.
        /// </summary>
        public AgentProfile Profile { get; init; }

        /// <summary>
        /// Gets or init-sets the radius, updating the underlying AgentProfile.
        /// </summary>
        public float Radius
        {
            get => Profile.Radius;
            init => Profile = Profile with { Radius = value };
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is resting on the ground.
        /// </summary>
        public bool IsOnGround { get; set; } = true;

        /// <summary>
        /// Gets or sets the 3D world position vector.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Gets or sets the vertical velocity for gravity and jumping dynamics.
        /// </summary>
        public float VerticalVelocity { get; set; }

        /// <summary>
        /// Gets or sets the sprite sheet asset identifier.
        /// </summary>
        public int SpriteId { get; init; }

        /// <summary>
        /// Gets or sets the Y-axis facing rotation in degrees.
        /// </summary>
        public float RotationY { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is target in sight.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is target in sight; otherwise, <c>false</c>.
        /// </value>
        public bool IsTargetInSight { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// </summary>
        public Actor(AgentProfile? profile = null)
        {
            Profile = profile ?? new AgentProfile();
        }

        /// <summary>
        /// Updates vertical gravity physics and ground landing detection.
        /// </summary>
        public void Update(float deltaTime, float groundHeight = 1.7f)
        {
            if (IsOnGround) return;

            // Apply gravity
            VerticalVelocity -= PhysicsConstants.Gravity * deltaTime;
            Position.Y += VerticalVelocity * deltaTime;

            // Resolve ground collision landing
            if (!(Position.Y <= groundHeight)) return;

            Position.Y = groundHeight;
            IsOnGround = true;
            VerticalVelocity = 0f;
        }

        /// <summary>
        /// Triggers an upward jump impulse if grounded.
        /// </summary>
        public void Jump()
        {
            if (!IsOnGround) return;

            VerticalVelocity = PhysicsConstants.JumpImpulse;
            IsOnGround = false;
        }
    }
}