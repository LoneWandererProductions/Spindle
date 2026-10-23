/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        CrawlHole.cs
 * PURPOSE:     Size and position of the low hole in a WallType.Crawl wall. One definition for the renderer (what is drawn)
 *              and the collision (what a crawling body can use), so the hole you see is the hole you fit through.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Maps
{
    /// <summary>
    /// Geometry of a crawl hole. The hole is centred on the cell edge; widths and heights are absolute metres
    /// (<see cref="MapDimensions.CrawlOpeningWidth"/>, <see cref="MapDimensions.CrawlOpeningHeight"/>), clamped for tiny cells.
    /// </summary>
    public static class CrawlHole
    {
        /// <summary>Half the width of the hole in metres, for a cell of edge length <paramref name="scale"/>.</summary>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <returns>Half width in metres.</returns>
        public static float HalfWidth(float scale) =>
            MathF.Min(MapDimensions.CrawlOpeningWidth, 0.8f * scale) * 0.5f;

        /// <summary>Height of the hole above the floor in metres.</summary>
        /// <param name="storeyHeight">Floor to ceiling in metres.</param>
        /// <returns>Hole height in metres.</returns>
        public static float Height(float storeyHeight) =>
            MathF.Min(MapDimensions.CrawlOpeningHeight, 0.9f * storeyHeight);

        /// <summary>
        /// Can a body of <paramref name="radius"/> at lateral position <paramref name="lateral"/> go through the hole of an
        /// edge whose middle is at <paramref name="edgeMiddle"/>? (Lateral = the coordinate ALONG the edge.)
        /// </summary>
        /// <param name="lateral">World coordinate along the edge (X for north/south edges, Z for east/west edges).</param>
        /// <param name="edgeMiddle">World coordinate of the middle of that edge.</param>
        /// <param name="scale">Cell edge length in metres.</param>
        /// <param name="radius">Body radius.</param>
        /// <returns><c>true</c> if the body fits.</returns>
        public static bool Fits(float lateral, float edgeMiddle, float scale, float radius) =>
            MathF.Abs(lateral - edgeMiddle) <= HalfWidth(scale) - radius;
    }
}