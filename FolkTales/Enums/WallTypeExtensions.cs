/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        WallTypeExtensions.cs
 * PURPOSE:     The ONE table of what every wall type stops (body, shot, sight, light). Collision, projectiles, sight,
 *              light propagation and the pathfinder all read it, so a rule cannot drift between them.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// What a <see cref="WallType"/> blocks. "Crawling" is the stance of the body or observer that asks: a crawling body
    /// fits through <see cref="WallType.Crawl"/> holes and sees through them.
    /// </summary>
    public static class WallTypeExtensions
    {
        /// <summary>
        /// Does this wall stop a walking (or, with <paramref name="crawling" />, crawling) body?
        /// Crawl walls are the only type whose answer depends on the stance - and even for a crawler only the hole is open, see
        /// <see cref="HasCrawlHole" /> and the span check in the collision service.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <param name="crawling">if set to <c>true</c> [crawling].</param>
        /// <returns><c>true</c> if the wall blocks movement; otherwise, <c>false</c>.</returns>
        public static bool BlocksMovement(this WallType wall, bool crawling = false) => wall switch
        {
            WallType.Solid or WallType.DoorClosed or WallType.Window => true,
            WallType.Crawl => !crawling,
            _ => false
        };

        /// <summary>
        /// Anything that stops a body of that stance stops a shot fired from that stance (glass included).
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <param name="crawling">if set to <c>true</c> [crawling].</param>
        /// <returns><c>true</c> if the wall blocks a projectile; otherwise, <c>false</c>.</returns>
        public static bool BlocksProjectile(this WallType wall, bool crawling = false) =>
            wall.BlocksMovement(crawling);

        /// <summary>
        /// Walls and closed doors stop sight, windows do not. A crawl wall stops the sight of a standing observer (the wall
        /// is above the hole) but a crawling observer looks through the hole.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <param name="crawling">if set to <c>true</c> [crawling].</param>
        /// <returns><c>true</c> if the wall blocks sight; otherwise, <c>false</c>.</returns>
        public static bool BlocksSight(this WallType wall, bool crawling = false) => wall switch
        {
            WallType.Solid or WallType.DoorClosed => true,
            WallType.Crawl => !crawling,
            _ => false
        };

        /// <summary>
        /// Opaque for light. Crawl holes are small and low, so they keep the dark: a burrow stays dark unless it has its
        /// own torch. (Light has no stance, so there is no crawling flag.)
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <returns>Opaque for light.</returns>
        public static bool BlocksLight(this WallType wall) =>
            wall is WallType.Solid or WallType.DoorClosed or WallType.Crawl;

        /// <summary>
        /// True for walls that carry a low hole a crawler can use.
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <returns>
        ///   <c>true</c> if [has crawl hole] [the specified wall]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasCrawlHole(this WallType wall) => wall == WallType.Crawl;

        /// <summary>
        /// True when the edge is a mouth of a round cell for that stance: the ring of the chamber is open there.
        /// (None, an open door, and a crawl hole for a crawler.)
        /// </summary>
        /// <param name="wall">The wall.</param>
        /// <param name="crawling">if set to <c>true</c> [crawling].</param>
        /// <returns>
        ///   <c>true</c> if [is open for] [the specified wall]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOpenFor(this WallType wall, bool crawling) => !wall.BlocksMovement(crawling);
    }
}