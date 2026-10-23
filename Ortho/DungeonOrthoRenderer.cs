/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Dungeons
 * FILE:        DungeonOrthoRenderer.cs
 * PURPOSE:     Top-down orthographic renderer for dungeon maps (with dolls, layers, textures).
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales;
using FolkTales.Enums;
using FolkTales.Interfaces;
using FolkTales.Maps;
using FolkTales.Structs;
using System.Drawing;
using System.Numerics;

namespace Ortho
{
    /// <summary>
    /// Orthographic renderer for dungeon maps, providing a top-down view with support for textures, wireframes, and sprite rendering.
    /// </summary>
    public class DungeonOrthoRenderer
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly DungeonMap? _map;

        /// <summary>
        /// The rasterizer
        /// </summary>
        private readonly IRender _rasterizer;

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public RenderMode Mode { get; set; } = RenderMode.Textured;

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonOrthoRenderer"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="rasterizer">The rasterizer.</param>
        public DungeonOrthoRenderer(DungeonMap? map, IRender rasterizer)
        {
            _map = map;
            _rasterizer = rasterizer;
        }

        /// <summary>
        /// Renders the orthographic top-down view.
        /// </summary>
        /// <param name="camPos">The cam position.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="dolls">The dolls.</param>
        /// <returns>Returns a Bitmap representing the rendered orthographic view.</returns>
        public Bitmap? Render(Vector3 camPos, Matrix4x4 vp, List<PaperDoll>? dolls = null)
        {
            _rasterizer.Clear(new ColorRgba(20, 15, 25, 255)); // Matches Dungeon dark void
            var drawList = new List<DrawCommand>();

            var scale = _map.DungeonCellScale > 0f ? _map.DungeonCellScale : 1.0f;

            for (var z = 0; z < _map.Levels; z++)
            {
                for (var x = 0; x < _map.Width; x++)
                {
                    for (var y = 0; y < _map.Depth; y++)
                    {
                        var cell = _map.GetCell(x, y, z);
                        if (cell == null || cell.IsEmpty) continue;

                        EnqueueCell(x, y, z, cell, vp, camPos, scale, drawList);
                    }
                }
            }

            if (dolls != null)
            {
                foreach (var doll in dolls)
                {
                    var clip = Vector4.Transform(new Vector4(doll.Position, 1f), vp);
                    var screenPos = ProjectToVec2(clip);

                    drawList.Add(DrawCommand.CreateSprite(
                        screenPos,
                        clip.Z,
                        (int)doll.Radius,
                        doll.Sprite,
                        ColorRgba.Red));
                }
            }

            // Painter's algorithm sort (farthest depth first)
            drawList.Sort((a, b) => b.SortDepth.CompareTo(a.SortDepth));
            foreach (var cmd in drawList) ExecuteCommand(cmd);

            return _rasterizer.GetFrame();
        }

        private void ExecuteCommand(DrawCommand cmd)
        {
            if (cmd.Type == CommandType.Triangle)
            {
                if (Mode == RenderMode.Wireframe)
                {
                    var wireColor = ColorRgba.Black;
                    _rasterizer.DrawLine(cmd.S0, cmd.S1, wireColor);
                    _rasterizer.DrawLine(cmd.S1, cmd.S2, wireColor);
                    _rasterizer.DrawLine(cmd.S2, cmd.S0, wireColor);
                }
                else
                {
                    var canDrawTexture = Mode == RenderMode.Textured &&
                                         cmd.TextureId.HasValue &&
                                         _rasterizer.HasTexture(cmd.TextureId.Value);

                    if (canDrawTexture && cmd.TextureId != null)
                    {
                        _rasterizer.DrawTexturedTriangle(
                            cmd.S0, cmd.Z0, cmd.Uv0,
                            cmd.S1, cmd.Z1, cmd.Uv1,
                            cmd.S2, cmd.Z2, cmd.Uv2,
                            cmd.TextureId.Value);
                    }
                    else
                    {
                        _rasterizer.DrawSolidTriangle(cmd.S0, cmd.Z0, cmd.S1, cmd.Z1, cmd.S2, cmd.Z2, cmd.Fallback);

                        var outlineColor = new ColorRgba(40, 40, 40, 255);
                        _rasterizer.DrawLine(cmd.S0, cmd.S1, outlineColor);
                        _rasterizer.DrawLine(cmd.S1, cmd.S2, outlineColor);
                        _rasterizer.DrawLine(cmd.S2, cmd.S0, outlineColor);
                    }
                }
            }
            else // Sprite Pass
            {
                var radius = cmd.PixelDiameter / 2;
                var topLeft = new Vec2(cmd.S0.X - radius, cmd.S0.Y - radius);

                if (cmd.TextureId.HasValue && _rasterizer.HasTexture(cmd.TextureId.Value))
                {
                    _rasterizer.DrawSprite(topLeft, cmd.Z0, cmd.TextureId.Value);
                }
                else
                {
                    _rasterizer.DrawFilledRect(topLeft.X, topLeft.Y, cmd.PixelDiameter, cmd.PixelDiameter,
                        ColorRgba.Red);
                }
            }
        }

        /// <summary>
        /// Enqueues the cell.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="camPos">The cam position.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="list">The list.</param>
        private void EnqueueCell(int x, int y, int z, MapCell3D cell, Matrix4x4 vp, Vector3 camPos, float scale,
            List<DrawCommand> list)
        {
            var levelHeight = (cell.CeilingHeight - cell.FloorHeight) * scale;
            var floor = (cell.FloorHeight * scale) + (z * levelHeight);
            var ceil = (cell.CeilingHeight * scale) + (z * levelHeight);

            var minX = x * scale;
            var maxX = (x + 1) * scale;
            var minZ = y * scale;
            var maxZ = (y + 1) * scale;

            Vector3[] c =
            {
                new(minX, floor, minZ), new(maxX, floor, minZ), new(maxX, floor, maxZ), new(minX, floor, maxZ),
                new(minX, ceil, minZ), new(maxX, ceil, minZ), new(maxX, ceil, maxZ), new(minX, ceil, maxZ)
            };

            var groundColor = new ColorRgba(180, 180, 180, 255);
            var sideColor = new ColorRgba(90, 90, 100, 255);

            // 1. Always render floors in top-down view
            if (cell.HasFloor)
                EnqueueQuad(c[0], c[1], c[2], c[3], vp, camPos, cell.FloorTextureId, groundColor, list);

            // 2. CEILINGS ARE CULLED IN TOP-DOWN ORTHO MODE (so you can see inside!)

            // 3. Render room walls
            if (cell.HasWallNorth) EnqueueQuad(c[0], c[4], c[5], c[1], vp, camPos, cell.WallTextureId, sideColor, list);
            if (cell.HasWallSouth) EnqueueQuad(c[2], c[6], c[7], c[3], vp, camPos, cell.WallTextureId, sideColor, list);
            if (cell.HasWallWest) EnqueueQuad(c[3], c[7], c[4], c[0], vp, camPos, cell.WallTextureId, sideColor, list);
            if (cell.HasWallEast) EnqueueQuad(c[1], c[5], c[6], c[2], vp, camPos, cell.WallTextureId, sideColor, list);
        }

        /// <summary>
        /// Enqueues the quad.
        /// </summary>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="v3">The v3.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="camPos">The cam position.</param>
        /// <param name="tex">The tex.</param>
        /// <param name="col">The col.</param>
        /// <param name="list">The list.</param>
        private void EnqueueQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Matrix4x4 vp, Vector3 camPos, int? tex,
            ColorRgba col, List<DrawCommand> list)
        {
            var normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
            var viewDir = Vector3.Normalize(v0 - camPos);
            if (Mode != RenderMode.Wireframe && Vector3.Dot(normal, viewDir) >= 0) return;

            EnqueueTriangle(v0, v1, v2, vp, tex, col, list);
            EnqueueTriangle(v0, v2, v3, vp, tex, col, list);
        }

        /// <summary>
        /// Enqueues the triangle.
        /// </summary>
        /// <param name="w0">The w0.</param>
        /// <param name="w1">The w1.</param>
        /// <param name="w2">The w2.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="tex">The tex.</param>
        /// <param name="col">The col.</param>
        /// <param name="list">The list.</param>
        private void EnqueueTriangle(Vector3 w0, Vector3 w1, Vector3 w2, Matrix4x4 vp, int? tex, ColorRgba col,
            List<DrawCommand> list)
        {
            var c0 = Vector4.Transform(new Vector4(w0, 1f), vp);
            var c1 = Vector4.Transform(new Vector4(w1, 1f), vp);
            var c2 = Vector4.Transform(new Vector4(w2, 1f), vp);

            var s0 = ProjectToVec2(c0);
            var s1 = ProjectToVec2(c1);
            var s2 = ProjectToVec2(c2);

            var z0 = c0.Z;
            var z1 = c1.Z;
            var z2 = c2.Z;

            list.Add(DrawCommand.CreateTriangle(
                s0, z0, new Vector2(0, 0),
                s1, z1, new Vector2(1, 0),
                s2, z2, new Vector2(1, 1),
                tex, col, (z0 + z1 + z2) / 3f));
        }

        /// <summary>
        /// Projects to vec2.
        /// </summary>
        /// <param name="ndc">The NDC.</param>
        /// <returns>Vector2 representing the projected screen coordinates.</returns>
        private Vec2 ProjectToVec2(Vector4 ndc)
        {
            var w = Math.Max(0.01f, ndc.W);
            var x = ndc.X / w;
            var y = ndc.Y / w;

            var screenX = (int)((x + 1.0f) * 0.5f * _rasterizer.Width);
            var screenY = (int)((1.0f - y) * 0.5f * _rasterizer.Height);

            return new Vec2(screenX, screenY);
        }
    }
}