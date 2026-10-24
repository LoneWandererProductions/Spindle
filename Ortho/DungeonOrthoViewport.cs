/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Dungeons
 * FILE:        DungeonOrthoViewport.cs
 * PURPOSE:     Simplified orthographic top-down viewport for dungeons.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using FolkTales;
using FolkTales.Enums;
using FolkTales.Maps;
using System.Drawing;
using System.Numerics;

namespace Ortho
{
    /// <summary>
    /// Viewport manager handling orthographic camera updates, player movement, and frame rendering.
    /// </summary>
    public sealed class DungeonOrthoViewport
    {
        /// <summary>
        /// The orthographic renderer instance.
        /// </summary>
        private readonly DungeonOrthoRenderer _renderer;

        /// <summary>
        /// The software rasterizer instance.
        /// </summary>
        private readonly SoftRasterizer _rast;

        /// <summary>
        /// The dungeon map instance.
        /// </summary>
        private readonly DungeonMap? _map;

        /// <summary>
        /// Viewport width in pixels.
        /// </summary>
        private readonly int _width;

        /// <summary>
        /// Viewport height in pixels.
        /// </summary>
        private readonly int _height;

        /// <summary>
        /// The logger instance.
        /// </summary>
        private readonly ILogger? _logger;

        // State Tracking for the "Smart" Rasterizer
        private Vector3 _camPos;
        private Vector3 _target;
        private float _zoom;

        private Vector3 _lastCamPos;
        private Vector3 _lastTarget;
        private float _lastZoom;

        /// <summary>
        /// Initializes a new instance of the <see cref="DungeonOrthoViewport"/> class.
        /// </summary>
        /// <param name="map">The dungeon map.</param>
        /// <param name="rasterer">The software rasterizer.</param>
        /// <param name="width">The viewport width.</param>
        /// <param name="height">The viewport height.</param>
        /// <param name="logger">Optional logger instance.</param>
        public DungeonOrthoViewport(DungeonMap? map, SoftRasterizer rasterer, int width, int height,
            ILogger? logger = null)
        {
            _map = map;
            _rast = rasterer;
            _width = width;
            _height = height;
            _logger = logger;

            _renderer = new DungeonOrthoRenderer(map, rasterer);

            // Initialize default camera view
            _camPos = new Vector3(8f, 15f, 25f);
            _target = new Vector3(8f, 0f, 8f);
            _zoom = 25f;

            Player = new Actor { Position = new Vector3(1.5f, 0.5f, 1.5f) };
            Actors = new List<Actor>();

            _logger?.LogInformation("DungeonOrthoViewport initialized with camera at {0}, target at {1}, zoom {2}",
                _camPos, _target, _zoom);
        }

        /// <summary>
        /// Gets the player actor.
        /// </summary>
        /// <value>
        /// The player.
        /// </value>
        public Actor Player { get; }

        /// <summary>
        /// Gets the list of active actors in the viewport.
        /// </summary>
        /// <value>
        /// The actors list.
        /// </value>
        public List<Actor> Actors { get; init; }

        /// <summary>
        /// Gets the underlying orthographic renderer.
        /// </summary>
        /// <value>
        /// The renderer.
        /// </value>
        public DungeonOrthoRenderer Renderer => _renderer;

        /// <summary>
        /// Renders one frame of the orthographic viewport.
        /// </summary>
        /// <param name="key">Optional active game key input.</param>
        /// <returns>The rendered bitmap frame.</returns>
        public Bitmap? Render(GameKey? key = null)
        {
            if (key.HasValue) ApplyKeyMovement(key.Value);

            // 1. Check if camera or zoom changed since last frame
            var cameraMoved = _camPos != _lastCamPos || _target != _lastTarget || _zoom != _lastZoom;

            // 2. Toggle "Smart" Dirty Tile tracking
            if (cameraMoved)
            {
                _rast.DisableUpdates(); // Whole screen changed, just bulk upload
            }
            else
            {
                _rast.EnableUpdates(); // Static camera, only track moving actors/animations
            }

            var aspect = (float)_width / _height;

            var view = Matrix4x4.CreateLookAt(_camPos, _target, Vector3.UnitY);
            var proj = Matrix4x4.CreateOrthographic(_zoom * aspect, _zoom, 0.1f, 100f);
            var vp = view * proj;

            var pixelsPerUnit = _height / _zoom;

            var dolls = Actors.Select(a => new PaperDoll
            {
                Position = a.Position,
                Radius = 0.5f * pixelsPerUnit,
                Sprite = a.SpriteId
            }).ToList();

            // Pass both camera position and look-at target to the renderer
            var frame = _renderer.Render(_camPos, _target, vp, dolls);

            // 3. Cache current state for the next frame's comparison
            _lastCamPos = _camPos;
            _lastTarget = _target;
            _lastZoom = _zoom;

            return frame;
        }

        /// <summary>
        /// Applies the key movement to update player position and camera tracking.
        /// </summary>
        /// <param name="key">The game key pressed.</param>
        public void ApplyKeyMovement(GameKey key)
        {
            const float moveSpeed = 0.5f;
            const float lookSpeed = 0.5f;

            // WASD: Standard movement
            if (key == GameKey.W) Player.Position.Z -= moveSpeed;
            if (key == GameKey.S) Player.Position.Z += moveSpeed;
            if (key == GameKey.A) Player.Position.X -= moveSpeed;
            if (key == GameKey.D) Player.Position.X += moveSpeed;

            // Fix: Update camera target to track the player's position while preserving camera offset/pitch
            var camOffset = _camPos - _target;
            _target = new Vector3(Player.Position.X, 0f, Player.Position.Z);
            _camPos = _target + camOffset;

            // Up/Down: Look Angle (Pitch adjustment)
            // This shifts the camera altitude and target offset to "tilt" the view
            if (key == GameKey.Up)
            {
                _camPos.Y += lookSpeed;
                _target.Z -= lookSpeed;
            }

            if (key == GameKey.Down)
            {
                _camPos.Y = Math.Max(2f, _camPos.Y - lookSpeed); // Prevent going under floor
                _target.Z += lookSpeed;
            }

            // Optional: Zoom control with PageUp/PageDown
            if (key == GameKey.PageUp) _zoom = Math.Max(5f, _zoom - 1f);
            if (key == GameKey.PageDown) _zoom = Math.Min(100f, _zoom + 1f);
        }
    }
}