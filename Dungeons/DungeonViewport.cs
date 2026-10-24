/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Dungeons
 * FILE:        DungeonViewport.cs
 * PURPOSE:     High-level wrapper that connects rendering, camera handling, player physics, and actor drawing.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales;
using System.Drawing;
using System.Numerics;
using System.Windows.Input;
using FolkTales.Interfaces;
using FolkTales.Enums;
using FolkTales.Maps;
using Core.MemoryLog;

namespace Dungeons
{
    /// <summary>
    /// Provides a viewport into a dungeon environment.
    /// Wraps <see cref="DungeonRenderer"/> to handle map rendering, camera movement,
    /// player physics, and optional actor rendering with culling.
    /// </summary>
    public sealed class DungeonViewport
    {
        /// <summary>Renderer responsible for drawing the dungeon map.</summary>
        public DungeonRenderer Renderer { get; set; }

        /// <summary>Persistent software rasterizer instance for Z-buffered drawing.</summary>
        private readonly IRender? _rast;

        /// <summary>The dungeon map to be visualized.</summary>
        private readonly DungeonMap _map;

        /// <summary>Viewport width in pixels.</summary>
        private readonly int _width;

        /// <summary>Viewport height in pixels.</summary>
        private readonly int _height;

        /// <summary>Logger for debug information, warnings, and diagnostics.</summary>
        private readonly ILogger? _logger;

        /// <summary>
        /// The textures
        /// </summary>
        private readonly Dictionary<int, Bitmap?>? _textures;

        // State tracking to determine if we should use bulk upload or dirty tiles

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
        /// Initializes a new instance of the <see cref="DungeonViewport"/> class.
        /// </summary>
        /// <param name="map">The dungeon map to render.</param>
        /// <param name="rasterer">The rasterizer used for drawing operations.</param>
        /// <param name="width">Viewport width in pixels.</param>
        /// <param name="height">Viewport height in pixels.</param>
        /// <param name="logger">Logger instance for runtime diagnostics.</param>
        /// <param name="textures">Optional dictionary of texture IDs mapped to <see cref="Bitmap"/> resources.</param>
        public DungeonViewport(
            DungeonMap map,
            IRender? rasterer,
            int width,
            int height,
            ILogger? logger,
            Dictionary<int, Bitmap?>? textures = null)
        {
            _map = map;
            Renderer = new DungeonRenderer(map, logger);
            _rast = rasterer;
            _width = width;
            _height = height;
            _logger = logger;
            _textures = textures;
            Renderer.Mode = RenderMode.Textured;

            Player = new Actor
            {
                Position = new Vector3(1.5f, 1.7f, 1.5f)
            };

            Camera = new Camera3D
            {
                Position = Player.Position
            };
        }

        /// <summary>
        /// The player actor controlled by input and simulated with basic physics.
        /// </summary>
        public Actor Player { get; }

        /// <summary>
        /// The camera used to render the scene, typically locked to the <see cref="Player"/> position.
        /// </summary>
        public Camera3D Camera { get; set; }

        /// <summary>
        /// Optional list of additional actors (NPCs, enemies, props) that can be rendered.
        /// These are culled against the camera view before drawing.
        /// </summary>
        public List<Actor>? Actors { get; set; }

        /// <summary>
        /// Renders a frame of the dungeon scene.
        /// Applies key input, updates the player’s physics state,
        /// syncs the camera, culls visible actors, and draws the world.
        /// </summary>
        /// <param name="eKey">Optional key input (movement or rotation).</param>
        /// <param name="deltaTime">Elapsed time step for physics updates (default: ~16 ms).</param>
        /// <returns>A rendered <see cref="Bitmap"/> image of the current frame.</returns>
        public Bitmap? Render(GameKey? eKey = null, float deltaTime = 0.016f)
        {
            // Apply input to player
            if (eKey.HasValue)
                ApplyKeyMovement(eKey.Value);

            // Physics update
            Player.Update(deltaTime);

            // Camera follows player
            Camera.Position = Player.Position;

            // Determine if camera moved to optimize the rasterizer upload path
            var cameraMoved = Camera.Position != _lastCamPos ||
                              Camera.Yaw != _lastYaw ||
                              Camera.Pitch != _lastPitch;

            if (cameraMoved)
            {
                _rast.DisableUpdates(); // Use high-speed SIMD bulk copy for full-screen changes
            }
            else
            {
                _rast.EnableUpdates(); // Only track and upload tiles changed by actors/animations
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
                        Radius = 0.5f // simple radius for frustum culling
                    })
                    .Where(d => Culling.IsSphereVisible(vp, d.Position, d.Radius))
                    .ToList();
            }

            // Render map + player-following camera + visible actors using persistent rasterizer
            var frame = Renderer.Render(_rast, Camera, _width, _height, dolls);

            // Cache camera state for the next frame
            _lastCamPos = Camera.Position;
            _lastYaw = Camera.Yaw;
            _lastPitch = Camera.Pitch;

            return frame;
        }

        /// <summary>
        /// Applies a single key movement to the player and updates camera orientation.
        /// Supports WASD movement, arrow key rotation, and jumping.
        /// </summary>
        /// <param name="key">The key input pressed by the user.</param>
        public void ApplyKeyMovement(GameKey key)
        {
            _logger?.LogDebug($"Key pressed: {key}");

            const float moveSpeed = 0.2f;
            const float rotSpeed = 0.05f;

            // Movement in world-space relative to camera yaw
            var forward = Vector3.Transform(new Vector3(0, 0, moveSpeed), Matrix4x4.CreateRotationY(Camera.Yaw));
            var right = Vector3.Transform(new Vector3(moveSpeed, 0, 0), Matrix4x4.CreateRotationY(Camera.Yaw));

            if (key == GameKey.W) Player.Position += forward;
            if (key == GameKey.S) Player.Position -= forward;
            if (key == GameKey.A) Player.Position -= right;
            if (key == GameKey.D) Player.Position += right;

            if (key == GameKey.Space) Player.Jump(); // jump handled by Actor

            // Camera rotation (Yaw and Pitch/Look Up-Down)
            if (key == GameKey.Left) Camera.Yaw -= rotSpeed;
            if (key == GameKey.Right) Camera.Yaw += rotSpeed;
            if (key == GameKey.Up) Camera.Pitch = MathF.Min(Camera.Pitch + rotSpeed, MathF.PI / 2);
            if (key == GameKey.Down) Camera.Pitch = MathF.Max(Camera.Pitch - rotSpeed, -MathF.PI / 2);
        }
    }
}