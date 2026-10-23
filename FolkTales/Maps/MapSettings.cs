/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapSettings.cs
 * PURPOSE:     Per-map start settings (fog, exploration, sight, time, weather, spawn). They used to be scattered over the
 *              viewports and MainWindow; now they live on the map, so a map file carries everything needed to play it.
 *              Every member is optional: null means "the viewport's default", so a map without settings behaves as before.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Where and how the player starts on a map.
    /// </summary>
    public sealed class MapSpawn
    {
        /// <summary>Gets or sets the world position of the feet.</summary>
        public Vector3 Position { get; set; }

        /// <summary>Gets or sets the view direction in radians (0 = north, 1.5708 = east). <c>null</c> = the viewport's own default.</summary>
        public float? Yaw { get; set; }

        /// <summary>Gets or sets the view pitch in radians (0 = eye level, negative = looking down). <c>null</c> = the viewport's own default.</summary>
        public float? Pitch { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapSpawn"/> class.
        /// </summary>
        public MapSpawn()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapSpawn"/> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="yaw">The yaw.</param>
        /// <param name="pitch">The pitch.</param>
        public MapSpawn(Vector3 position, float? yaw = null, float? pitch = null)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    /// <summary>
    /// The optional start settings of a map. Plain data, saved with the map.
    /// </summary>
    public sealed class MapSettings
    {
        /// <summary>
        /// Gets or sets the fog mode (exploration memory and/or line of sight). <c>null</c> = viewport default
        /// (dungeon: both, hybrid: exploration only).
        /// </summary>
        public FogMode? Fog { get; set; }

        /// <summary>
        /// Gets or sets the sight radius in cells. <c>null</c> = viewport default.
        /// </summary>
        public int? SightRadius { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the whole map counts as already discovered at the start
        /// (the cells are born in the "explored" state instead of "unexplored").
        /// </summary>
        public bool StartExplored { get; set; }

        /// <summary>
        /// Gets or sets the start hour of the day cycle (0 to 24). <c>null</c> = environment default.
        /// </summary>
        public float? TimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets the start weather. <c>null</c> = environment default (clear).
        /// </summary>
        public WeatherType? Weather { get; set; }

        /// <summary>
        /// Gets or sets the player start. <c>null</c> = the viewport's own default.
        /// </summary>
        public MapSpawn? Spawn { get; set; }

        /// <summary>
        /// Whether the exploration memory is on: the map's fog mode if it sets one, otherwise the viewport default.
        /// </summary>
        /// <param name="viewportDefault">What the viewport does without a setting.</param>
        /// <returns>True if explored cells are remembered (and unexplored ones stay dark).</returns>
        public bool ExplorationEnabled(bool viewportDefault) =>
            Fog is { } fog ? (fog & FogMode.Exploration) != 0 : viewportDefault;

        /// <summary>
        /// Whether line of sight is on: the map's fog mode if it sets one, otherwise the viewport default.
        /// </summary>
        /// <param name="viewportDefault">What the viewport does without a setting.</param>
        /// <returns>True if only what is in sight is shown at full brightness.</returns>
        public bool LineOfSightEnabled(bool viewportDefault) =>
            Fog is { } fog ? (fog & FogMode.LineOfSight) != 0 : viewportDefault;

        /// <summary>
        /// Creates a settings object from the optional constructor arguments of the maps.
        /// </summary>
        /// <param name="fogMode">The fog mode.</param>
        /// <param name="sightRadius">The sight radius.</param>
        /// <param name="startExplored">If set to <c>true</c> the map starts discovered.</param>
        /// <param name="timeOfDay">The start hour.</param>
        /// <param name="weather">The start weather.</param>
        /// <returns>The settings.</returns>
        public static MapSettings From(FogMode? fogMode, int? sightRadius, bool startExplored, float? timeOfDay,
            WeatherType? weather) => new()
        {
            Fog = fogMode,
            SightRadius = sightRadius,
            StartExplored = startExplored,
            TimeOfDay = timeOfDay,
            Weather = weather
        };

        /// <summary>
        /// Makes an independent copy.
        /// </summary>
        /// <returns>The copy.</returns>
        public MapSettings Clone() => new()
        {
            Fog = Fog,
            SightRadius = SightRadius,
            StartExplored = StartExplored,
            TimeOfDay = TimeOfDay,
            Weather = Weather,
            Spawn = Spawn == null ? null : new MapSpawn(Spawn.Position, Spawn.Yaw, Spawn.Pitch)
        };
    }
}