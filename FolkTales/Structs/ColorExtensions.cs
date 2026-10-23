/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        ColorExtensions.cs
 * PURPOSE:     Converts System.Drawing.Color to our internal ColorRgba struct for the software rasterizer, with a default fallback color for null values.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;

namespace FolkTales.Structs
{
    /// <summary>
    /// Extensions for converting System.Drawing.Color to ColorRgba, with a default fallback for null values.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Converts to colorrgba.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns>New ColorRgba instance.</returns>
        public static ColorRgba ToColorRgba(this Color? c)
        {
            return c.HasValue
                ? new ColorRgba(c.Value.R, c.Value.G, c.Value.B, c.Value.A)
                : ColorRgba.DarkGray;
        }
    }
}