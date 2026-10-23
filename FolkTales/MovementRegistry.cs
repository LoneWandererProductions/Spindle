/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales
 * FILE:        MovementRegistry.cs
 * PURPOSE:     Central register of the movement types (walk, run, crawl): speed, eye height, visibility.
 *              All four viewports read it, so one change here changes every viewport.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales
{
    /// <summary>
    /// The movement types of the player.
    /// </summary>
    public enum MovementMode
    {
        /// <summary>Normal walking.</summary>
        Walk = 0,

        /// <summary>Running (hold the run key): faster, easier to spot.</summary>
        Run = 1,

        /// <summary>Crawling / sneaking (toggle): slow, low, hard to spot, fits through crawl holes.</summary>
        Crawl = 2
    }

    /// <summary>
    /// Tuning of one movement type.
    /// </summary>
    /// <param name="Speed">Walking speed in world units per second.</param>
    /// <param name="EyeHeight">Eye height above the feet.</param>
    /// <param name="Visibility">Multiplier on how far enemies see and notice the player (1 = normal, 0.4 = hard to spot, above 1 = easier).</param>
    /// <param name="CanPassCrawlHoles">Whether the body fits through <see cref="Enums.WallType.Crawl"/> holes.</param>
    public sealed record MovementProfile(float Speed, float EyeHeight, float Visibility, bool CanPassCrawlHoles);

    /// <summary>
    /// Central register of all <see cref="MovementProfile"/>s. Change a value (at startup or at runtime) with
    /// <see cref="Set"/>; every viewport picks it up on the next frame.
    /// </summary>
    public static class MovementRegistry
    {
        /// <summary>Default walking speed (units per second).</summary>
        public const float DefaultWalkSpeed = 9.0f;

        /// <summary>Default running speed (units per second).</summary>
        public const float DefaultRunSpeed = 14.0f;

        /// <summary>Default crawling speed (units per second).</summary>
        public const float DefaultCrawlSpeed = 3.5f;

        /// <summary>
        /// The default profiles.
        /// </summary>
        private static readonly MovementProfile[] Defaults =
        {
            new(DefaultWalkSpeed, PhysicsConstants.StandingEyeHeight, 1.0f, false),
            new(DefaultRunSpeed, PhysicsConstants.StandingEyeHeight, 1.25f, false),
            new(DefaultCrawlSpeed, PhysicsConstants.CrawlEyeHeight, 0.4f, true)
        };

        /// <summary>
        /// The current profiles, indexed by <see cref="MovementMode"/>.
        /// </summary>
        private static readonly MovementProfile[] Profiles = (MovementProfile[])Defaults.Clone();

        /// <summary>
        /// Gets the profile of a movement type.
        /// </summary>
        /// <param name="mode">The movement type.</param>
        /// <returns>Its profile.</returns>
        public static MovementProfile Get(MovementMode mode) => Profiles[(int)mode];

        /// <summary>
        /// Replaces the profile of a movement type.
        /// </summary>
        /// <param name="mode">The movement type.</param>
        /// <param name="profile">The new profile.</param>
        /// <exception cref="ArgumentOutOfRangeException">Speed or eye height not above zero, or visibility negative.</exception>
        public static void Set(MovementMode mode, MovementProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            if (profile.Speed <= 0f) throw new ArgumentOutOfRangeException(nameof(profile), "Speed must be above 0.");
            if (profile.EyeHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(profile), "EyeHeight must be above 0.");
            if (profile.Visibility < 0f)
                throw new ArgumentOutOfRangeException(nameof(profile), "Visibility must not be negative.");

            Profiles[(int)mode] = profile;
        }

        /// <summary>
        /// Restores all profiles to their defaults.
        /// </summary>
        public static void Reset() => Array.Copy(Defaults, Profiles, Defaults.Length);
    }
}