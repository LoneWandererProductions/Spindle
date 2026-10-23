/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Structs
 * FILE:        CollisionBox.cs
 * PURPOSE:     Oriented bounding box collision primitive for flat, thin architectural
 *              geometry (wall panels, doors, thresholds) that a rounded capsule approximates
 *              poorly - sharp corners, a uniform thickness, and no rounded caps at the ends.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Numerics;

namespace FolkTales.Structs
{
    /// <summary>
    /// A yaw-only oriented bounding box: a world-space AABB rotated around the Y axis only, since
    /// nothing in this project's architectural scenery (walls, panels, thresholds) needs pitch or
    /// roll - everything stands upright and only varies in which way it faces.
    /// </summary>
    /// <param name="Center">World-space center of the box.</param>
    /// <param name="HalfExtents">Half-width (local X), half-height (Y), half-depth (local Z).
    /// Y is never affected by Yaw - the box doesn't tilt, only turns.</param>
    /// <param name="Yaw">Rotation around Y, in radians, in the same convention as
    /// <c>SceneryExtensions.GetRotationYaw</c> / <c>Matrix4x4.CreateRotationY</c>.</param>
    public readonly record struct CollisionBox(Vector3 Center, Vector3 HalfExtents, float Yaw);
}