/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Terrain
 * FILE:        TerrainRenderer.cs
 * PURPOSE:     Simple terrain renderer using heightmaps with distance shading.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using FolkTales;
using FolkTales.Enums;
using FolkTales.Interfaces;
using FolkTales.Maps;
using FolkTales.Structs;
using System.Drawing;
using System.Numerics;

namespace Terrain
{
    /// <summary>
    /// Renders a terrain map using a heightmap and simple lighting based on sun direction.
    /// </summary>
    public class TerrainRenderer
    {
        /// <summary>
        /// The map
        /// </summary>
        private readonly TerrainMap? _map;

        /// <summary>
        /// The rasterizer
        /// </summary>
        private readonly IRender? _rasterizer;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainRenderer"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="rasterizer">The rasterizer.</param>
        /// <param name="logger">The logger.</param>
        public TerrainRenderer(TerrainMap? map, IRender? rasterizer, ILogger? logger = null)
        {
            _map = map;
            _rasterizer = rasterizer;
            _logger = logger;
        }

        /// <summary>
        /// Renders the specified camera.
        /// </summary>
        /// <param name="camera">The camera.</param>
        /// <param name="clearColor">Color of the clear.</param>
        /// <param name="sunDirection">The sun direction.</param>
        /// <param name="actors">The actors.</param>
        /// <param name="textureId">The texture identifier.</param>
        /// <returns>The rendered bitmap.</returns>
        public Bitmap? Render(Camera3D camera, Color? clearColor, Vector3 sunDirection, List<PaperDoll>? actors = null,
            int? textureId = null)
        {
            if (_rasterizer == null) return null;

            // 1. Setup
            var activeClearColor = clearColor ?? Color.CornflowerBlue;
            _rasterizer.Clear(new ColorRgba(activeClearColor.R, activeClearColor.G, activeClearColor.B,
                activeClearColor.A));

            var aspect = (float)_rasterizer.Width / _rasterizer.Height;
            var vp = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspect);
            var sun = Vector3.Normalize(sunDirection);

            var drawList = new List<DrawCommand>();

            // 2. Terrain Loop
            for (var x = 0; x < _map.Width - 1; x++)
            {
                for (var z = 0; z < _map.Depth - 1; z++)
                {
                    var v0 = _map.GetWorldPosition(x, z);
                    var v1 = _map.GetWorldPosition(x + 1, z);
                    var v2 = _map.GetWorldPosition(x + 1, z + 1);
                    var v3 = _map.GetWorldPosition(x, z + 1);

                    var uv0 = new Vector2((float)x / _map.Width, (float)z / _map.Depth);
                    var uv1 = new Vector2((float)(x + 1) / _map.Width, (float)z / _map.Depth);
                    var uv2 = new Vector2((float)(x + 1) / _map.Width, (float)(z + 1) / _map.Depth);
                    var uv3 = new Vector2((float)x / _map.Width, (float)(z + 1) / _map.Depth);

                    EnqueueTriangle(v0, v2, v1, uv0, uv2, uv1, vp, camera.Position, sun, activeClearColor, textureId,
                        drawList);
                    EnqueueTriangle(v0, v3, v2, uv0, uv3, uv2, vp, camera.Position, sun, activeClearColor, textureId,
                        drawList);
                }
            }

            // 3. Actors
            if (actors != null)
            {
                foreach (var doll in actors)
                {
                    TryEnqueueDoll(vp, camera.Position, doll, drawList);
                }
            }

            // 4. Sort (Painter's Algorithm - Farthest to Nearest)
            drawList.Sort((a, b) => b.SortDepth.CompareTo(a.SortDepth));

            // 5. Execute
            foreach (var cmd in drawList)
            {
                ExecuteCommand(cmd);
            }

            return _rasterizer.GetFrame();
        }

        /// <summary>
        /// Enqueues the triangle.
        /// </summary>
        /// <param name="w0">The w0.</param>
        /// <param name="w1">The w1.</param>
        /// <param name="w2">The w2.</param>
        /// <param name="uv0">The uv0.</param>
        /// <param name="uv1">The uv1.</param>
        /// <param name="uv2">The uv2.</param>
        /// <param name="vp">The vp.</param>
        /// <param name="camPos">The cam position.</param>
        /// <param name="sun">The sun.</param>
        /// <param name="clearColor">Color of the clear.</param>
        /// <param name="tex">The tex.</param>
        /// <param name="list">The list.</param>
        private void EnqueueTriangle(Vector3 w0, Vector3 w1, Vector3 w2, Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Matrix4x4 vp, Vector3 camPos, Vector3 sun, Color clearColor, int? tex, List<DrawCommand> list)
        {
            var normal = Vector3.Normalize(Vector3.Cross(w1 - w0, w2 - w0));

            // Backface Culling (cull faces facing away from camera view vector)
            var viewDir = Vector3.Normalize(w0 - camPos);
            if (Vector3.Dot(normal, viewDir) >= 0) return;

            var c0 = Vector4.Transform(new Vector4(w0, 1f), vp);
            var c1 = Vector4.Transform(new Vector4(w1, 1f), vp);
            var c2 = Vector4.Transform(new Vector4(w2, 1f), vp);

            // Near-Plane Culling (Only cull if ALL vertices are behind camera near plane)
            if (c0.W < 0.01f && c1.W < 0.01f && c2.W < 0.01f) return;

            var s0 = ProjectToVec2(c0);
            var s1 = ProjectToVec2(c1);
            var s2 = ProjectToVec2(c2);

            // Calculate color per vertex with correct light and sky fog parameters
            var color0 = CalculateTerrainColor(w0, normal, sun, camPos, clearColor);
            var color1 = CalculateTerrainColor(w1, normal, sun, camPos, clearColor);
            var color2 = CalculateTerrainColor(w2, normal, sun, camPos, clearColor);

            var avgR = (byte)((color0.R + color1.R + color2.R) / 3);
            var avgG = (byte)((color0.G + color1.G + color2.G) / 3);
            var avgB = (byte)((color0.B + color1.B + color2.B) / 3);
            var finalColor = new ColorRgba(avgR, avgG, avgB, 255);

            // Use true Euclidean distance to camera for accurate Painter's algorithm sorting
            var dist0 = Vector3.Distance(w0, camPos);
            var dist1 = Vector3.Distance(w1, camPos);
            var dist2 = Vector3.Distance(w2, camPos);
            var averageSortDepth = (dist0 + dist1 + dist2) / 3f;

            list.Add(DrawCommand.CreateTriangle(s0, c0.W, uv0, s1, c1.W, uv1, s2, c2.W, uv2, tex, finalColor,
                averageSortDepth));
        }

        /// <summary>
        /// Tries the enqueue doll.
        /// </summary>
        /// <param name="vp">The vp.</param>
        /// <param name="camPos">The cam position.</param>
        /// <param name="doll">The doll.</param>
        /// <param name="list">The list.</param>
        private void TryEnqueueDoll(Matrix4x4 vp, Vector3 camPos, PaperDoll doll, List<DrawCommand> list)
        {
            var clip = Vector4.Transform(new Vector4(doll.Position, 1f), vp);
            if (clip.W <= 0.1f) return;

            var screenPos = ProjectToVec2(clip);
            var dist = Vector3.Distance(camPos, doll.Position);
            var scale = 1.0f / Math.Max(0.1f, dist);
            var pixelDiameter = (int)(doll.Radius * 800f * scale);

            list.Add(DrawCommand.CreateSprite(screenPos, clip.W, pixelDiameter, doll.Sprite, ColorRgba.Red));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="cmd">The command.</param>
        private void ExecuteCommand(DrawCommand cmd)
        {
            if (_rasterizer == null) return;

            if (cmd.Type == CommandType.Triangle)
            {
                if (cmd.TextureId.HasValue && _rasterizer.HasTexture(cmd.TextureId.Value))
                    _rasterizer.DrawTexturedTriangle(cmd.S0, cmd.Z0, cmd.Uv0, cmd.S1, cmd.Z1, cmd.Uv1, cmd.S2, cmd.Z2,
                        cmd.Uv2, cmd.TextureId.Value);
                else
                    _rasterizer.DrawSolidTriangle(cmd.S0, cmd.Z0, cmd.S1, cmd.Z1, cmd.S2, cmd.Z2, cmd.Fallback);
            }
            else
            {
                var r = cmd.PixelDiameter / 2;
                _rasterizer.DrawSprite(new Vec2(cmd.S0.X - r, cmd.S0.Y - r), cmd.Z0, cmd.TextureId ?? 0);
            }
        }

        /// <summary>
        /// Projects to vec2 with W safeguards to prevent division by zero or negative depth clipping errors.
        /// </summary>
        /// <param name="clip">The clip.</param>
        /// <returns>The projected 2D vector.</returns>
        private Vec2 ProjectToVec2(Vector4 clip)
        {
            var w = Math.Max(0.01f, clip.W);
            var ndcX = clip.X / w;
            var ndcY = clip.Y / w;
            var x = (int)((ndcX + 1f) * 0.5f * _rasterizer!.Width);
            var y = (int)((1f - ndcY) * 0.5f * _rasterizer.Height);
            return new Vec2(x, y);
        }

        /// <summary>
        /// Calculates the color of the terrain.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="sun">The sun.</param>
        /// <param name="cameraPos">The camera position.</param>
        /// <param name="skyColor">Color of the sky.</param>
        /// <returns>The calculated terrain color.</returns>
        private ColorRgba CalculateTerrainColor(Vector3 worldPos, Vector3 normal, Vector3 sun, Vector3 cameraPos,
            Color skyColor)
        {
            // 1. LIGHTING
            var lightDir = sun.Y < 0 ? -sun : sun;
            var dot = Math.Clamp(Vector3.Dot(normal, lightDir), 0f, 1f);
            var litFactor = 0.4f + 0.6f * dot;

            // 2. SMOOTH BIOMES
            var altitude = Math.Clamp(worldPos.Y / 80f, 0f, 1f);

            var deepGreen = new Vector3(30, 80, 40); // Lush base
            var mountainBrown = new Vector3(100, 85, 70); // Rocky mid
            var snowWhite = new Vector3(255, 250, 250); // Snowy peak

            var baseColor = altitude < 0.4f
                ? Vector3.Lerp(deepGreen, mountainBrown, altitude / 0.4f)
                : Vector3.Lerp(mountainBrown, snowWhite, (altitude - 0.4f) / 0.6f);

            // 3. DISTANCE FADE
            var dist = Vector3.Distance(worldPos, cameraPos);
            var fogFactor = Math.Clamp(dist / 1500f, 0f, 1f);
            var horizonColor = new Vector3(skyColor.R, skyColor.G, skyColor.B);

            var final = Vector3.Lerp(baseColor * litFactor, horizonColor, fogFactor);

            return new ColorRgba(
                (byte)Math.Clamp(final.X, 0, 255),
                (byte)Math.Clamp(final.Y, 0, 255),
                (byte)Math.Clamp(final.Z, 0, 255),
                255);
        }
    }
}