/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        RenderMode.cs
 * PURPOSE:     Different modes for rendering the dungeon renderer.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    ///     Rendering modes for the dungeon.
    /// </summary>
    public enum RenderMode
    {
        /// <summary>
        /// The wireframe render mode.
        /// </summary>
        Wireframe,

        /// <summary>
        /// The solid render mode.
        /// </summary>
        Solid,

        /// <summary>
        /// The textured render mode.
        /// </summary>
        Textured
    }
}