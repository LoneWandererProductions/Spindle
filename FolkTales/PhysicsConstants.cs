/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        PhysicsConstants.cs
 * PURPOSE:     The single source of truth for gravity, jumping and body size. Every viewport,
 *              actor, projectile and physics body reads these so all four viewports feel the same.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales
{
    /// <summary>
    /// Shared physics tuning. Change a value here and it changes everywhere.
    /// </summary>
    public static class PhysicsConstants
    {
        /// <summary>
        /// Gravity magnitude in world units per second squared (applied downwards).
        /// </summary>
        public const float Gravity = 22.0f;

        /// <summary>
        /// Upward speed of a standing jump. With <see cref="Gravity"/> = 22 this gives a jump about 1.28 units high
        /// (the same height the old 5.0 / 9.8 pair produced).
        /// </summary>
        public const float JumpImpulse = 7.5f;

        /// <summary>
        /// Collision radius of the player body. Used for walls AND scenery in every viewport.
        /// </summary>
        public const float PlayerRadius = 0.35f;

        /// <summary>
        /// Eye height above the feet while standing.
        /// </summary>
        public const float StandingEyeHeight = 1.7f;

        /// <summary>
        /// Eye height above the feet while crawling.
        /// </summary>
        public const float CrawlEyeHeight = 0.6f;

        /// <summary>
        /// Body height while crawling. Must be below <see cref="Maps.MapDimensions.CrawlOpeningHeight"/>, or nobody fits through a crawl hole.
        /// </summary>
        public const float CrawlBodyHeight = 0.8f;
    }
}