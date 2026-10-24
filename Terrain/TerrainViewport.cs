/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Terrain
 * FILE:        TerrainViewport.cs
 * PURPOSE:     Viewport for a simple 3D terrain renderer with camera and player handling.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using FolkTales;
using FolkTales.Enums;
using FolkTales.Interfaces;
using FolkTales.Maps;
using System.Drawing;
using System.Numerics;

namespace Terrain
{
    /// <inheritdoc/>
    /// <summary>
    /// Wrapper for TerrainRenderer that handles camera movement, player physics, and optional actor rendering.
    /// </summary>
    public sealed class TerrainViewport : IDisposable
    {
        /// <summary>
        /// The renderer
        /// </summary>
        private readonly TerrainRenderer _renderer;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger? _logger;

        /// <summary>
        /// The sun direction
        /// </summary>
        private readonly Vector3 _sunDirection;

        /// <summary>
        /// The rast
        /// </summary>
        private readonly IRender? _rast;

        /// <summary>
        /// The map
        /// </summary>
        private readonly TerrainMap? _map;

        /// <summary>
        /// The rasterizer
        /// </summary>
        private readonly SoftRasterizer? _rasterizer;

        /// <summary>
        /// The width
        /// </summary>
        private readonly int _width;

        /// <summary>
        /// The height
        /// </summary>
        private readonly int _height;

        // --- State tracking to determine if we should use bulk upload or dirty tiles ---

        /// <summary>
        /// The last cam position
        /// </summary>
        private Vector3 _lastCamPos;

        /// <summary>
        /// The last yaw
        /// </summary>
        private float _lastYaw;

        /// <summary>
        /// The last pitch
        /// </summary>
        private float _lastPitch;

        /// <summary>
        /// The tolerance
        /// </summary>
        private const double Tolerance = 0.0001;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainViewport" /> class.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="raster">The raster.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="sunDirection">The sun direction.</param>
        public TerrainViewport(
            TerrainMap? map,
            IRender? raster,
            int width,
            int height,
            ILogger? logger,
            Vector3 sunDirection)
        {
            _map = map;
            _rast = raster;
            _renderer = new TerrainRenderer(map, raster, logger);
            _logger = logger;
            _sunDirection = sunDirection;
            _width = width;
            _height = height;

            // Cast to SoftRasterizer to access Enable/Disable updates if applicable
            _rasterizer = raster as SoftRasterizer;

            Player = new Actor
            {
                Position = new Vector3(5f, 10f, 5f), // start above terrain
            };

            Camera = new Camera3D
            {
                Position = new Vector3(width / 2f, 20f, -30f),
                Yaw = 0f,
                Pitch = -0.6f
            };
        }

        /// <summary>
        /// Gets the player.
        /// </summary>
        /// <value>
        /// The player.
        /// </value>
        public Actor Player { get; }

        /// <summary>
        /// Gets or sets the camera.
        /// </summary>
        /// <value>
        /// The camera.
        /// </value>
        public Camera3D Camera { get; set; }

        /// <summary>
        /// Optional list of NPCs, enemies, or other actors to render.
        /// </summary>
        public List<Actor>? Actors { get; set; }

        /// <summary>
        /// Render a frame. Applies key input, updates player, and optionally renders actors.
        /// </summary>
        /// <param name="eKey">The e key.</param>
        /// <param name="deltaTime">The delta time.</param>
        /// <returns></returns>
        public Bitmap? Render(GameKey? eKey = null, float deltaTime = 0.016f)
        {
            // Apply input to player
            if (eKey.HasValue)
                ApplyKeyMovement(eKey.Value);

            // Physics update (player jump/gravity etc.)
            Player.Update(deltaTime);

            // Camera follows player
            Camera.Position = Player.Position;

            // 1. Check if camera moved to optimize the rasterizer upload path

            var cameraMoved = Camera.Position != _lastCamPos ||
                              Math.Abs(Camera.Yaw - _lastYaw) > Tolerance ||
                              Camera.Pitch != _lastPitch;

            if (cameraMoved)
            {
                _rasterizer?.DisableUpdates(); // Bulk SIMD copy for moving scenes
            }
            else
            {
                _rasterizer?.EnableUpdates(); // Tile tracking for stationary camera
            }

            // Cull visible actors if any
            List<PaperDoll>? dolls = null;
            if (Actors is { Count: > 0 })
            {
                _logger?.LogDebug($"Culling {Actors.Count} actors.");

                var aspect = (float)_width / _height;
                var vp = Camera.GetViewMatrix() * Camera.GetProjectionMatrix(aspect);

                dolls = Actors.Select(a => new PaperDoll
                    {
                        Position = a.Position,
                        Radius = 0.5f
                    })
                    .Where(d => Culling.IsSphereVisible(vp, d.Position, d.Radius))
                    .ToList();
            }

            // Render terrain + camera + visible actors
            var frame = _renderer.Render(Camera, Color.CornflowerBlue, _sunDirection, dolls);

            // 2. Update state tracking for next frame
            _lastCamPos = Camera.Position;
            _lastYaw = Camera.Yaw;
            _lastPitch = Camera.Pitch;

            return frame;
        }

        /// <summary>
        /// Applies the key movement.
        /// </summary>
        /// <param name="key">The key.</param>
        public void ApplyKeyMovement(GameKey key)
        {
            _logger?.LogDebug($"Key pressed: {key}");

            if (_map == null) return;

            var moveSpeed = _map.Width * 0.1f;
            const float rotSpeed = 0.4f; // slightly faster rotation

            // Movement in world-space relative to camera yaw
            var forward = Vector3.Transform(new Vector3(0, 0, moveSpeed), Matrix4x4.CreateRotationY(Camera.Yaw));
            var right = Vector3.Transform(new Vector3(moveSpeed, 0, 0), Matrix4x4.CreateRotationY(Camera.Yaw));

            if (key == GameKey.W) Player.Position += forward;
            if (key == GameKey.S) Player.Position -= forward;
            if (key == GameKey.A) Player.Position -= right;
            if (key == GameKey.D) Player.Position += right;

            if (key == GameKey.Space) Player.Jump();

            // Camera rotation (Yaw and Pitch/Look Up-Down)
            if (key == GameKey.Left) Camera.Yaw -= rotSpeed;
            if (key == GameKey.Right) Camera.Yaw += rotSpeed;
            if (key == GameKey.Up) Camera.Pitch = MathF.Min(Camera.Pitch + rotSpeed, MathF.PI / 2);
            if (key == GameKey.Down) Camera.Pitch = MathF.Max(Camera.Pitch - rotSpeed, -MathF.PI / 2);
        }

        /// <inheritdoc/>
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            _rasterizer?.Dispose();
        }
    }
}