/*
 * COPYRIGHT:   See LICENSE.txt in the top level directory
 * PROJECT:     Spindle.Viewer
 * FILE:        MainWindow.xaml.cs
 * PURPOSE:     Minimal WPF host for the pure software-rendered viewports (terrain, dungeon, ortho).
 *              Derived from the Grimoire test bed, with the OpenGL pane removed.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using Dungeons;
using FolkTales;
using FolkTales.Enums;
using FolkTales.Maps;
using Ortho;
using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Terrain;

namespace Spindle.Viewer
{
    /// <summary>
    ///     Hosts one software viewport at a time and pumps it from the WPF render loop.
    /// </summary>
    public sealed partial class MainWindow
    {
        /// <summary>
        ///     Fixed render resolution; the rasterizer buffer is created once per mode switch.
        /// </summary>
        private const int RenderWidth = 800;

        private const int RenderHeight = 600;

        private readonly ILogger? _logger;
        private readonly HashSet<Key> _pressedKeys = new();

        private string? _active;
        private SoftRasterizer? _raster;

        private TerrainViewport? _terrainViewport;
        private DungeonViewport? _dungeonViewport;
        private DungeonOrthoViewport? _orthoViewport;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _logger = InMemoryLogger.Instance;
            CompositionTarget.Rendering += GameLoop;
            LogDiagnostics("Spindle.Viewer started. Pick a mode, then use WASD / arrow keys.");
        }

        /// <summary>
        ///     Applies held keys and renders one frame of the active viewport.
        /// </summary>
        private void GameLoop(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_active)) return;

            Bitmap? bmp = null;

            switch (_active)
            {
                case "Terrain" when _terrainViewport != null:
                    foreach (var key in _pressedKeys) _terrainViewport.ApplyKeyMovement(key);
                    bmp = _terrainViewport.Render();
                    break;

                case "Dungeon Solid" or "Dungeon Wireframe" when _dungeonViewport != null:
                    foreach (var key in _pressedKeys) _dungeonViewport.ApplyKeyMovement(key);
                    bmp = _dungeonViewport.Render();
                    break;

                case "Dungeon Ortho" when _orthoViewport != null:
                    foreach (var key in _pressedKeys) _orthoViewport.ApplyKeyMovement(key);
                    bmp = _orthoViewport.Render();
                    break;
            }

            if (bmp != null) ImageView.Bitmap = bmp;
        }

        private void ComboBoxRender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxRender.SelectedItem is not ComboBoxItem item) return;

            _active = item.Content.ToString();
            ReleaseViewports();

            try
            {
                switch (_active)
                {
                    case "Terrain":
                        InitiateTerrain();
                        break;
                    case "Dungeon Solid":
                        InitiateDungeon(RenderMode.Solid);
                        break;
                    case "Dungeon Wireframe":
                        InitiateDungeon(RenderMode.Wireframe);
                        break;
                    case "Dungeon Ortho":
                        InitiateOrtho();
                        break;
                }

                LogDiagnostics($"Switched to: {_active}");
            }
            catch (Exception ex)
            {
                _active = null;
                MessageBox.Show($"Could not start '{item.Content}': {ex.Message}");
            }

            Focus();
        }

        private void InitiateTerrain()
        {
            var map = new TerrainMap(50, 50, 10f);
            BuildHills(map);

            _raster = new SoftRasterizer(RenderWidth, RenderHeight, null, _logger);
            _terrainViewport = new TerrainViewport(map, _raster, RenderWidth, RenderHeight, _logger,
                Vector3.Normalize(new Vector3(-0.5f, -1f, -0.3f)));
            _terrainViewport.Player.Position = new Vector3(200, 60, 200);
        }

        private void InitiateDungeon(RenderMode mode)
        {
            _raster = new SoftRasterizer(RenderWidth, RenderHeight, null, _logger);
            _dungeonViewport = new DungeonViewport(CreateRoomMap(), _raster, RenderWidth, RenderHeight, _logger);
            _dungeonViewport.Renderer.Mode = mode;
            _dungeonViewport.Player.Position = new Vector3(1.5f, 1.0f, 1.5f);
        }

        private void InitiateOrtho()
        {
            _raster = new SoftRasterizer(RenderWidth, RenderHeight, null, _logger);
            _orthoViewport = new DungeonOrthoViewport(CreateRoomMap(), _raster, RenderWidth, RenderHeight, _logger);
            _orthoViewport.Player.Position = new Vector3(1.5f, 0.5f, 1.5f);
        }

        /// <summary>
        ///     Frees the previous viewport's rasterizer before a new mode is created.
        /// </summary>
        private void ReleaseViewports()
        {
            // TerrainViewport.Dispose() already disposes the rasterizer it was given,
            // so only dispose the rasterizer directly when no terrain viewport owns it.
            if (_terrainViewport != null) _terrainViewport.Dispose();
            else _raster?.Dispose();

            _terrainViewport = null;
            _dungeonViewport = null;
            _orthoViewport = null;
            _raster = null;
        }

        /// <summary>
        ///     A single walled 16x16 room, enough to see the renderer working.
        /// </summary>
        private static DungeonMap CreateRoomMap()
        {
            var map = new DungeonMap(16, 16, 3);
            for (var x = 0; x < 16; x++)
            for (var y = 0; y < 16; y++)
            {
                var cell = map.GetCell(x, y, 0);
                cell.HasFloor = true;
                cell.HasCeiling = true;
                cell.HasWallWest = x == 0;
                cell.HasWallEast = x == 15;
                cell.HasWallNorth = y == 0;
                cell.HasWallSouth = y == 15;
            }

            return map;
        }

        private static void BuildHills(TerrainMap map)
        {
            void AddHill(int cx, int cz, float peak, int radius)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                for (var z = cz - radius; z <= cz + radius; z++)
                {
                    if (x < 0 || z < 0 || x >= map.Width || z >= map.Depth) continue;

                    float dx = x - cx;
                    float dz = z - cz;
                    var dist = MathF.Sqrt(dx * dx + dz * dz);
                    if (dist <= radius) map[x, z] += peak * MathF.Pow(1f - dist / radius, 2f);
                }
            }

            AddHill(10, 10, 10f, 8);
            AddHill(35, 35, 18f, 12);
            AddHill(25, 15, -6f, 6);

            var rand = new Random(42);
            for (var x = 0; x < map.Width; x++)
            for (var z = 0; z < map.Depth; z++)
                map[x, z] = (map[x, z] + (float)(rand.NextDouble() - 0.5)) * 5f;

            map.RecalculateMaxHeight();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) => _pressedKeys.Add(e.Key);

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e) => _pressedKeys.Remove(e.Key);

        private void LogDiagnostics(string text)
        {
            TxtDiagnostics.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\n");
            TxtDiagnostics.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= GameLoop;
            ReleaseViewports();
            _logger?.DumpToFile("logs.txt", false, LogLevel.Information);
        }
    }
}