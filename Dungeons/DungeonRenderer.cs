/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Dungeons
 * FILE:        DungeonRenderer.cs
 * PURPOSE:     Software Renderer for testing, reference, and editor viewports.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using System.Drawing;
using System.Numerics;
using Core.MemoryLog;
using FolkTales;
using FolkTales.Enums;
using FolkTales.Interfaces;
using FolkTales.Maps;
using FolkTales.Structs;

namespace Dungeons
{
    /// <summary>
    /// Renderer responsible for drawing a dungeon map using CPU software rasterization.
    /// Builds quads from dungeon map cells, applies rasterization,
    /// and renders floors, walls, ceilings, layers, and paper dolls.
    /// </summary>
    public class DungeonRenderer
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly DungeonMap? _map;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger? _logger;

        /// <summary>
        /// Gets or sets the raster.
        /// </summary>
        /// <value>
        /// The raster.
        /// </value>
        public IRender? Raster { get; set; }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public RenderMode Mode { get; set; } = RenderMode.Solid;

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonRenderer"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="logger">The logger.</param>
        public DungeonRenderer(DungeonMap? map, ILogger? logger = null)
        {
            _map = map;
            _logger = logger;

            if (_map != null)
            {
                _logger?.LogInformation("DungeonRenderer initialized with map size {0}x{1}x{2}",
                    _map.Width, _map.Depth, _map.Levels);
            }
        }

        /// <summary>
        /// Renders the specified rast.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screen.</param>
        /// <param name="dolls">The dolls.</param>
        /// <returns>The rendered bitmap frame.</returns>
        public Bitmap? Render(IRender? rast, Camera3D camera, int screenWidth, int screenHeight,
            List<PaperDoll>? dolls = null)
        {
            if (rast == null || _map == null) return null;

            Raster = rast;
            var aspect = (float)screenWidth / screenHeight;
            var view = camera.GetViewMatrix();
            var proj = camera.GetProjectionMatrix(aspect);
            var vp = view * proj;

            Raster.Clear(ColorRgba.CornflowerBlue);

            // Fetch dynamic cell scale factor from map layout config
            var scale = _map.DungeonCellScale > 0f ? _map.DungeonCellScale : 1.0f;

            // 1. Draw dungeon cells (stacked Z-levels)
            for (var z = 0; z < _map.Levels; z++)
            {
                for (var x = 0; x < _map.Width; x++)
                {
                    for (var y = 0; y < _map.Depth; y++)
                    {
                        var cell = _map.GetCell(x, y, z);
                        if (cell == null || cell.IsEmpty) continue;

                        cell.PrecomputeCorners(scale);

                        var levelHeight = (cell.CeilingHeight - cell.FloorHeight) * scale;
                        var floor = (cell.FloorHeight * scale) + (z * levelHeight);
                        var ceil = (cell.CeilingHeight * scale) + (z * levelHeight);

                        var min = new Vector3(x * scale, floor, y * scale);
                        var max = new Vector3((x + 1) * scale, ceil, (y + 1) * scale);

                        if (!Culling.IsCellVisible(vp, min, max)) continue;

                        DrawCellScaled(Raster, camera, cell, vp, scale);
                        DrawCellLayersScaled(Raster, camera, cell, vp, scale);
                    }
                }
            }

            // 2. Draw paper dolls using Z-buffered sprite projection
            if (dolls is { Count: > 0 })
            {
                DrawPaperDolls(Raster, camera, dolls, vp);
            }

            return Raster.GetFrame();
        }

        /// <summary>
        /// Draws the cell scaled.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="scale">The scale.</param>
        private void DrawCellScaled(IRender rast, Camera3D camera, MapCell3D cell, Matrix4x4 vp, float scale)
        {
            foreach (var dir in Enum.GetValues<Direction>())
            {
                var wallBounds = cell.GetWallBounds(dir, scale);
                if (wallBounds.HasValue)
                {
                    var (min, max) = wallBounds.Value;
                    DrawBox(rast, camera, min, max, vp, cell.WallTextureId, ColorRgba.Gray);
                }
            }

            var floorBounds = cell.GetFloorBounds(scale);
            if (floorBounds.HasValue)
            {
                var (min, max) = floorBounds.Value;
                DrawBox(rast, camera, min, max, vp, cell.FloorTextureId, ColorRgba.DarkGray);
            }

            var ceilBounds = cell.GetCeilingBounds(scale);
            if (ceilBounds.HasValue)
            {
                var (min, max) = ceilBounds.Value;
                DrawBox(rast, camera, min, max, vp, cell.CeilingTextureId, ColorRgba.LightGray);
            }
        }

        /// <summary>
        /// Draws the cell layers scaled.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="scale">The scale.</param>
        private void DrawCellLayersScaled(IRender rast, Camera3D camera, MapCell3D cell, Matrix4x4 vp, float scale)
        {
            if (cell.Layers == null) return;
            if (cell?.Layers.Count == 0) return;

            var levelHeight = (cell.CeilingHeight - cell.FloorHeight) * scale;
            var zOffsetY = ZOffsetYCalculation(cell, scale);

            foreach (var layer in cell.Layers)
            {
                var y = (cell.FloorHeight * scale) + zOffsetY + (layer.Height * scale) + 0.01f;

                var v0 = new Vector3(cell.PrecomputedCorners[0].X, y, cell.PrecomputedCorners[0].Z);
                var v1 = new Vector3(cell.PrecomputedCorners[1].X, y, cell.PrecomputedCorners[1].Z);
                var v2 = new Vector3(cell.PrecomputedCorners[3].X, y, cell.PrecomputedCorners[3].Z);
                var v3 = new Vector3(cell.PrecomputedCorners[2].X, y, cell.PrecomputedCorners[2].Z);

                DrawQuad(rast, camera.Position, v0, v1, v2, v3, vp, null, layer.Color.ToColorRgba());
            }
        }

        /// <summary>
        /// zs the offset y calculation.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="scale">The scale.</param>
        private static float ZOffsetYCalculation(MapCell3D cell, float scale)
        {
            var levelHeight = (cell.CeilingHeight - cell.FloorHeight) * scale;
            return cell.Z * levelHeight;
        }

        /// <summary>
        /// Draws the box.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="textureId">The texture identifier.</param>
        /// <param name="fallbackColor">Color of the fallback.</param>
        private void DrawBox(IRender rast, Camera3D camera, Vector3 min, Vector3 max, Matrix4x4 vp, int? textureId,
            ColorRgba fallbackColor)
        {
            var c000 = new Vector3(min.X, min.Y, min.Z);
            var c100 = new Vector3(max.X, min.Y, min.Z);
            var c010 = new Vector3(min.X, min.Y, max.Z);
            var c110 = new Vector3(max.X, min.Y, max.Z);
            var c001 = new Vector3(min.X, max.Y, min.Z);
            var c101 = new Vector3(max.X, max.Y, min.Z);
            var c011 = new Vector3(min.X, max.Y, max.Z);
            var c111 = new Vector3(max.X, max.Y, max.Z);

            // Corrected face winding orders for outward-pointing normals
            DrawQuad(rast, camera.Position, c000, c001, c101, c100, vp, textureId, fallbackColor); // front
            DrawQuad(rast, camera.Position, c100, c101, c111, c110, vp, textureId, fallbackColor); // right
            DrawQuad(rast, camera.Position, c110, c111, c011, c010, vp, textureId, fallbackColor); // back
            DrawQuad(rast, camera.Position, c010, c011, c001, c000, vp, textureId, fallbackColor); // left
            DrawQuad(rast, camera.Position, c001, c011, c111, c101, vp, textureId, fallbackColor); // top
            DrawQuad(rast, camera.Position, c000, c100, c110, c010, vp, textureId, fallbackColor); // bottom
        }

        /// <summary>
        /// Draws the quad.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="cameraPos">The camera position.</param>
        /// <param name="v0">The v0.</param>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="v3">The v3.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="textureId">The texture identifier.</param>
        /// <param name="fallbackColor">Color of the fallback.</param>
        /// <returns>Draws the quad.</returns>
        private void DrawQuad(IRender rast, Vector3 cameraPos, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            Matrix4x4 vp, int? textureId, ColorRgba fallbackColor)
        {
            // Backface culling check
            var normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
            var viewDir = Vector3.Normalize(v0 - cameraPos);
            if (Vector3.Dot(normal, viewDir) >= 0) return;

            var c0 = Vector4.Transform(new Vector4(v0, 1f), vp);
            var c1 = Vector4.Transform(new Vector4(v1, 1f), vp);
            var c2 = Vector4.Transform(new Vector4(v2, 1f), vp);
            var c3 = Vector4.Transform(new Vector4(v3, 1f), vp);

            // Near-plane guard: Only cull if ALL vertices are behind camera near plane
            if (c0.W < 0.01f && c1.W < 0.01f && c2.W < 0.01f && c3.W < 0.01f) return;

            var s0 = Project(c0);
            var s1 = Project(c1);
            var s2 = Project(c2);
            var s3 = Project(c3);

            switch (Mode)
            {
                case RenderMode.Wireframe:
                    rast.DrawLine(s0, s1, ColorRgba.Black);
                    rast.DrawLine(s1, s2, ColorRgba.Black);
                    rast.DrawLine(s2, s3, ColorRgba.Black);
                    rast.DrawLine(s3, s0, ColorRgba.Black);
                    break;

                case RenderMode.Textured:
                    if (textureId.HasValue && rast.HasTexture(textureId.Value))
                    {
                        rast.DrawTexturedTriangle(s0, c0.W, new Vector2(0, 0), s1, c1.W, new Vector2(1, 0), s2, c2.W,
                            new Vector2(1, 1), textureId.Value);
                        rast.DrawTexturedTriangle(s0, c0.W, new Vector2(0, 0), s2, c2.W, new Vector2(1, 1), s3, c3.W,
                            new Vector2(0, 1), textureId.Value);
                    }
                    else
                    {
                        rast.DrawSolidTriangle(s0, c0.W, s1, c1.W, s2, c2.W, fallbackColor);
                        rast.DrawSolidTriangle(s0, c0.W, s2, c2.W, s3, c3.W, fallbackColor);
                    }

                    break;

                case RenderMode.Solid:
                    rast.DrawSolidTriangle(s0, c0.W, s1, c1.W, s2, c2.W, fallbackColor);
                    rast.DrawSolidTriangle(s0, c0.W, s2, c2.W, s3, c3.W, fallbackColor);
                    break;
            }
        }

        /// <summary>
        /// Draws the paper dolls.
        /// </summary>
        /// <param name="rast">The rast.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="dolls">The dolls.</param>
        /// <param name="vp">The vp.</param>
        private void DrawPaperDolls(IRender rast, Camera3D camera, List<PaperDoll> dolls, Matrix4x4 vp)
        {
            foreach (var doll in dolls)
            {
                var clipPos = Vector4.Transform(new Vector4(doll.Position, 1f), vp);
                if (clipPos.W <= 0.1f) continue;

                var ndcX = clipPos.X / clipPos.W;
                var ndcY = clipPos.Y / clipPos.W;
                var screenX = (int)((ndcX + 1f) * 0.5f * rast.Width);
                var screenY = (int)((1f - ndcY) * 0.5f * rast.Height);

                var dist = Vector3.Distance(camera.Position, doll.Position);
                var pixelSize = (int)(doll.Radius * 1000f / Math.Max(0.1f, dist));
                var halfSize = pixelSize / 2;

                rast.DrawSprite(new Vec2(screenX - halfSize, screenY - halfSize), clipPos.W, doll.Sprite);
            }
        }

        /// <summary>
        /// Projects the specified clip.
        /// </summary>
        /// <param name="clip">The clip.</param>
        /// <returns>Vector2 representing the projected screen coordinates.</returns>
        private Vec2 Project(Vector4 clip)
        {
            var w = Math.Max(0.01f, clip.W);
            var ndcX = clip.X / w;
            var ndcY = clip.Y / w;
            return new Vec2(
                (int)((ndcX + 1f) * 0.5f * Raster!.Width),
                (int)((1f - ndcY) * 0.5f * Raster.Height)
            );
        }
    }
}