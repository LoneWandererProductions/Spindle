/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        MapDiagnostics.cs
 * PURPOSE:     Optional debugger for any map (terrain, dungeon, hybrid world). It finds the interesting places itself
 *              (pools, stairwells, light) instead of knowing a map's coordinates, runs the MapAudit checks and writes
 *              everything to a Core.MemoryLog ILogger (for example InMemoryLogger.Instance) and to its own line list.
 *              Replaces the Skara-Brae-only SkaraBraeDiagnostics. Nothing here runs unless somebody calls Run.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using Core.MemoryLog;
using FolkTales.Enums;
using FolkTales.Exporting;

namespace FolkTales.Maps
{
    /// <summary>
    /// What a <see cref="MapDiagnostics"/> run does. Everything is on by default.
    /// </summary>
    public sealed class MapDiagnosticsOptions
    {
        /// <summary>Gets or sets a value indicating whether the engine health lines are written.</summary>
        public bool EngineChecks { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether the <see cref="MapAudit"/> checks run.</summary>
        public bool Audit { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether pools, stairwells, light and props are probed.</summary>
        public bool Probes { get; set; } = true;

        /// <summary>Gets or sets how many pools / stairwells are probed at most (the biggest are not preferred, the first found).</summary>
        public int MaxProbesPerKind { get; set; } = 8;

        /// <summary>Gets or sets the step of a rim sweep in cells.</summary>
        public float SweepStep { get; set; } = 0.05f;
    }

    /// <summary>
    /// What a custom probe gets to look at: exactly one of the three maps is set (a hybrid world also sets
    /// <see cref="Structures"/> to its structure layer).
    /// </summary>
    public sealed class MapProbeContext
    {
        /// <summary>Gets the hybrid world, if the map is one.</summary>
        public HybridWorldMap? Hybrid { get; init; }

        /// <summary>Gets the dungeon (for a hybrid world: its structure layer).</summary>
        public DungeonMap? Structures { get; init; }

        /// <summary>Gets the terrain (for a hybrid world: its surface terrain).</summary>
        public TerrainMap? Terrain { get; init; }
    }

    /// <summary>
    /// The optional map debugger.
    /// </summary>
    public sealed class MapDiagnostics
    {
        private readonly ILogger? _logger;
        private readonly List<string> _lines = new();
        private readonly List<(string name, Func<MapProbeContext, IEnumerable<string>> probe)> _custom = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MapDiagnostics"/> class.
        /// </summary>
        /// <param name="logger">Where the lines go as well (<c>null</c> = only collected in <see cref="Lines"/>).</param>
        /// <param name="options">What to run; <c>null</c> = everything.</param>
        public MapDiagnostics(ILogger? logger = null, MapDiagnosticsOptions? options = null)
        {
            _logger = logger;
            Options = options ?? new MapDiagnosticsOptions();
        }

        /// <summary>Gets the options.</summary>
        public MapDiagnosticsOptions Options { get; }

        /// <summary>Gets or sets a value indicating whether <c>Run</c> does anything. Lets a caller keep one instance and switch it.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Gets the lines written by the runs so far.</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Gets the number of warnings (failed audits, probes that found a hole) of all runs so far.</summary>
        public int Warnings { get; private set; }

        /// <summary>
        /// Registers a probe of your own, run after the built-in ones on every map.
        /// </summary>
        /// <param name="name">The name written in front of its lines.</param>
        /// <param name="probe">The probe: it returns the lines; a line that starts with "WARN" counts as a warning.</param>
        /// <returns>This instance.</returns>
        public MapDiagnostics AddProbe(string name, Func<MapProbeContext, IEnumerable<string>> probe)
        {
            _custom.Add((name, probe));
            return this;
        }

        /// <summary>
        /// Diagnoses a map that came out of a map file.
        /// </summary>
        /// <param name="map">The loaded map.</param>
        /// <returns>True if no warning was raised by this run.</returns>
        public bool Run(LoadedMap map) => map.Kind switch
        {
            MapKind.Terrain => Run(map.Terrain!, map.Name),
            MapKind.Dungeon => Run(map.Dungeon!, map.Name),
            _ => Run(map.Hybrid!, map.Name)
        };

        /// <summary>
        /// Diagnoses a dungeon map.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="name">A name for the header line.</param>
        /// <returns>True if no warning was raised by this run.</returns>
        public bool Run(DungeonMap map, string? name = null)
        {
            if (!Enabled) return true;
            var before = Warnings;

            Header("Dungeon", name, $"{map.Width}x{map.Depth}x{map.Levels}, cell {map.DungeonCellScale:0.0} m",
                map.Settings);
            if (Options.EngineChecks) EngineChecks();
            if (Options.Audit) Audit(notes => MapAudit.Run(map, notes));
            if (Options.Probes)
            {
                var grid = new Grid(map, null);
                Pools(grid);
                Stairwells(grid, null);
                LightSummary(grid, map.Levels);
                ScenerySummary(map.Scenery);
            }

            RunCustom(new MapProbeContext { Structures = map });
            return Warnings == before;
        }

        /// <summary>
        /// Diagnoses a hybrid world.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="name">A name for the header line.</param>
        /// <returns>True if no warning was raised by this run.</returns>
        public bool Run(HybridWorldMap map, string? name = null)
        {
            if (!Enabled) return true;
            var before = Warnings;

            Header("Hybrid", name,
                $"{map.Width}x{map.Depth}x{map.Levels}, cell {map.SharedScale:0.0} m, surface level {map.SurfaceLevel}",
                map.Settings);
            if (Options.EngineChecks) EngineChecks();
            if (Options.Audit) Audit(notes => MapAudit.Run(map, notes));
            if (Options.Probes)
            {
                var grid = new Grid(map.Structures, map);
                Pools(grid);
                WaterBodies(map);
                Stairwells(grid, map);
                LightSummary(grid, map.SurfaceLevel);
                TerrainSummary("surface", map.SurfaceTerrain);
                ScenerySummary(map.Scenery);
            }

            RunCustom(new MapProbeContext { Hybrid = map, Structures = map.Structures, Terrain = map.SurfaceTerrain });
            return Warnings == before;
        }

        /// <summary>
        /// Diagnoses a terrain map.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="name">A name for the header line.</param>
        /// <returns>True if no warning was raised by this run.</returns>
        public bool Run(TerrainMap map, string? name = null)
        {
            if (!Enabled) return true;
            var before = Warnings;

            Header("Terrain", name, $"{map.Width}x{map.Depth}, step {map.Scale:0.0} m", map.Settings);
            if (Options.EngineChecks) EngineChecks();
            if (Options.Probes)
            {
                TerrainSummary("terrain", map);
                ScenerySummary(map.Scenery);
            }

            RunCustom(new MapProbeContext { Terrain = map });
            return Warnings == before;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Built-in parts
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes the header: kind, name, size and the start settings.
        /// </summary>
        private void Header(string kind, string? name, string size, MapSettings settings)
        {
            Info($"Diagnostics {kind}{(string.IsNullOrEmpty(name) ? string.Empty : " '" + name + "'")}: {size}");
            Info("Settings: fog=" + (settings.Fog?.ToString() ?? "viewport default") +
                 $", sight={(settings.SightRadius?.ToString() ?? "viewport default")}" +
                 $", start explored={settings.StartExplored}" +
                 $", time={(settings.TimeOfDay?.ToString("0.0") ?? "default")}" +
                 $", weather={(settings.Weather?.ToString() ?? "default")}" +
                 (settings.Spawn == null
                     ? ", spawn=viewport default"
                     : $", spawn=({settings.Spawn.Position.X:0.0}, {settings.Spawn.Position.Y:0.0}, {settings.Spawn.Position.Z:0.0}) yaw {(settings.Spawn.Yaw?.ToString("0.00") ?? "default")}, pitch {(settings.Spawn.Pitch?.ToString("0.00") ?? "default")}"));
        }

        /// <summary>
        /// Values that tell whether the engine patch is fully active (these used to be Skara Brae only).
        /// </summary>
        private void EngineChecks()
        {
            var radius = SceneryType.LargeGableRoof.GetCollisionRadius();
            var opens = StairLayout.NeedsOpeningAbove(0, 4f);
            Check(radius == 0f, $"Engine check: roof collider radius = {radius:0.0} (patched: 0.0)");
            Check(opens, $"Engine check: stair cell 0 opens the floor above at scale 4 = {opens} (patched: True)");
            Info($"Engine check: floor probe reach = {MapDimensions.FloorProbeReach:0.0} m");
        }

        /// <summary>
        /// Runs a MapAudit run and forwards its lines (a failed check and its violations become warnings).
        /// </summary>
        private void Audit(Func<List<string>, bool> run)
        {
            var notes = new List<string>();
            run(notes);
            foreach (var line in notes)
            {
                if (line.StartsWith("Audit FAIL", StringComparison.Ordinal) ||
                    line.StartsWith("  ", StringComparison.Ordinal))
                    Warn(line);
                else
                    Info(line);
            }
        }

        /// <summary>
        /// Finds every liquid pool of the structure layer and sweeps its rim: the ground must not drop out.
        /// </summary>
        private void Pools(Grid g)
        {
            var seen = new HashSet<(int x, int z, int l)>();
            var probed = 0;

            for (var l = 0; l < g.Structures.Levels; l++)
            for (var x = 0; x < g.Structures.Width; x++)
            for (var z = 0; z < g.Structures.Depth; z++)
            {
                var liquid = LiquidOf(g.Structures.GetCell(x, z, l));
                if (liquid == null || !seen.Add((x, z, l))) continue;

                // Flood fill the pool (same liquid, same level, 4-neighbours) to get its bounds.
                var minX = x;
                var maxX = x;
                var minZ = z;
                var maxZ = z;
                var cells = 0;
                var stack = new Stack<(int x, int z)>();
                stack.Push((x, z));
                while (stack.Count > 0)
                {
                    var (cx, cz) = stack.Pop();
                    cells++;
                    minX = Math.Min(minX, cx);
                    maxX = Math.Max(maxX, cx);
                    minZ = Math.Min(minZ, cz);
                    maxZ = Math.Max(maxZ, cz);
                    foreach (var (nx, nz) in new[] { (cx + 1, cz), (cx - 1, cz), (cx, cz + 1), (cx, cz - 1) })
                    {
                        if (LiquidOf(g.Structures.GetCell(nx, nz, l)) != liquid || !seen.Add((nx, nz, l))) continue;
                        stack.Push((nx, nz));
                    }
                }

                if (probed++ >= Options.MaxProbesPerKind) continue;

                var feet = l * g.Structures.LevelHeight;
                var row = ((minZ + maxZ) / 2f) + 0.5f;
                Sweep($"{liquid} pool level {l} ({minX},{minZ})-({maxX},{maxZ}), {cells} cells, rim", g, row, minX - 2f,
                    maxX + 3f, feet, l);
            }

            if (probed > Options.MaxProbesPerKind)
                Info($"Probe pools: {probed - Options.MaxProbesPerKind} more not probed");
        }

        /// <summary>
        /// The terrain water bodies of a hybrid world: bed height and rim.
        /// </summary>
        private void WaterBodies(HybridWorldMap map)
        {
            if (map.SurfaceTerrain == null) return;

            var n = 0;
            foreach (var wb in map.SurfaceTerrain.WaterBodies)
            {
                if (n++ >= Options.MaxProbesPerKind) break;
                var row = ((wb.MinZ + wb.MaxZ) / 2f) + 0.5f;
                Sweep(
                    $"water body '{wb.Name}' (level {wb.WaterLevel:0.00} m, ({wb.MinX},{wb.MinZ})-({wb.MaxX},{wb.MaxZ})) rim",
                    new Grid(map.Structures, map), row, wb.MinX - 2f, wb.MaxX + 3f, map.SurfaceBaselineY);
            }
        }

        /// <summary>
        /// Finds the stairwells (a floored cell with a built but floorless cell right above it) and checks what lies below.
        /// </summary>
        private void Stairwells(Grid g, HybridWorldMap? hybrid)
        {
            var s = g.Scale;
            var lh = g.Structures.LevelHeight;
            var listed = 0;
            var total = 0;

            for (var l = 0; l + 1 < g.Structures.Levels; l++)
            for (var x = 0; x < g.Structures.Width; x++)
            for (var z = 0; z < g.Structures.Depth; z++)
            {
                var low = g.Structures.GetCell(x, z, l);
                var up = g.Structures.GetCell(x, z, l + 1);
                if (low is not { HasFloor: true } || up is not { IsEmpty: false, HasFloor: false }) continue;

                total++;
                if (listed >= Options.MaxProbesPerKind * 3) continue;
                listed++;

                var lowFloor = (low.FloorHeight + low.LevelBaseHeight) * s;
                var found = g.FloorAt((x + 0.5f) * s, (l + 1) * lh, (z + 0.5f) * s);
                var text =
                    $"Probe stairwell ({x},{z}) level {l + 1} -> {l}: ground below the hole = {found:0.00} m (floor below {lowFloor:0.00} m)";

                if (hybrid != null)
                {
                    var props = hybrid.GetSceneryAtCell(x, z);
                    var propText = props == null || props.Count == 0
                        ? "-"
                        : string.Join(",", props.Select(p => $"{p.Type}@{p.Position.Y:0.0}"));
                    text += $" | terrain punched={hybrid.SurfaceTerrain?.IsTileOccluded(x, z)}" +
                            $" | lower ceiling={low.HasCeiling} | light(lower)={hybrid.GetEffectiveLightAt(x, z, l)} | props: {propText}";
                }

                // Fell through the floor below: the stair would drop the player out of the level.
                if (found < lowFloor - 0.05f) Warn(text + "  <-- ground lies below the floor");
                else Info(text);
            }

            if (total == 0) Info("Probe stairwells: none found");
            else if (total > listed) Info($"Probe stairwells: {total} cells, {listed} listed");
        }

        /// <summary>
        /// Light range over the floors. A light grid that was never baked keeps its default of full brightness everywhere, so
        /// "every floor at full brightness" means the bake did not run (for example after loading a map without it), and
        /// "a floor without light" means the ambient fill is missing.
        /// </summary>
        private void LightSummary(Grid g, int belowLevel)
        {
            var floors = 0;
            var unlit = 0;
            byte min = byte.MaxValue;
            byte max = 0;

            for (var l = 0; l < Math.Min(g.Structures.Levels, belowLevel); l++)
            for (var x = 0; x < g.Structures.Width; x++)
            for (var z = 0; z < g.Structures.Depth; z++)
            {
                if (g.Structures.GetCell(x, z, l) is not { HasFloor: true }) continue;

                floors++;
                var light = g.Structures.GetLightAt(x, z, l);
                if (light == 0) unlit++;
                if (light < min) min = light;
                if (light > max) max = light;
            }

            if (floors == 0)
            {
                Info($"Probe light: no floors below level {belowLevel}");
                return;
            }

            var text =
                $"Probe light: {floors} floors below level {belowLevel}, darkest {min}, brightest {max}, without light {unlit}";
            if (unlit > 0) Warn(text + "  <-- floors without light: the ambient fill is missing");
            else if (min == MapConstants.MaxLightLevel)
                Warn(text + "  <-- every floor at full brightness: the light bake did not run?");
            else Info(text);
        }

        /// <summary>
        /// Height range, water bodies and holes of a terrain.
        /// </summary>
        private void TerrainSummary(string what, TerrainMap? terrain)
        {
            if (terrain == null) return;

            var min = float.MaxValue;
            var max = float.MinValue;
            for (var x = 0; x < terrain.Width; x++)
            for (var z = 0; z < terrain.Depth; z++)
            {
                var h = terrain[x, z];
                if (h < min) min = h;
                if (h > max) max = h;
            }

            Info(
                $"Probe {what} terrain: heights {min:0.00}..{max:0.00} m, water bodies {terrain.WaterBodies.Count()}, punched tiles {terrain.OccludedTiles.Count()}, props {terrain.Scenery.Count()}");
        }

        /// <summary>
        /// Prop count, light emitters and the most frequent types.
        /// </summary>
        private void ScenerySummary(IEnumerable<Structs.SceneryPlacement> scenery)
        {
            var list = scenery.ToList();
            var top = list.GroupBy(p => p.Type).OrderByDescending(grp => grp.Count()).Take(5)
                .Select(grp => $"{grp.Key} x{grp.Count()}");
            Info(
                $"Probe props: {list.Count} placed, {list.Count(p => p.LightEmission > 0)} emit light; most frequent: " +
                (list.Count == 0 ? "-" : string.Join(", ", top)));
        }

        /// <summary>
        /// Runs the probes registered with <see cref="AddProbe"/>.
        /// </summary>
        private void RunCustom(MapProbeContext context)
        {
            foreach (var (name, probe) in _custom)
            {
                try
                {
                    foreach (var line in probe(context))
                    {
                        if (line.StartsWith("WARN", StringComparison.Ordinal)) Warn($"[{name}] {line}");
                        else Info($"[{name}] {line}");
                    }
                }
                catch (Exception ex)
                {
                    Warn($"[{name}] probe failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Walks along a row and records the lowest and highest ground found; a hit at 0 m above the ground floor, on a
        /// cell that has a floor, is a hole.
        /// </summary>
        private void Sweep(string name, Grid g, float rowCell, float fromCell, float toCell, float feetY,
            int? level = null)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            var zeros = 0;
            var z = rowCell * g.Scale;

            for (var x = fromCell; x <= toCell; x += Options.SweepStep)
            {
                var ground = g.FloorAt(x * g.Scale, feetY, z);
                if (ground < min) min = ground;
                if (ground > max) max = ground;

                // On the lowest storey 0 m is a legitimate floor; above it, 0 m means the lookup found nothing. It only
                // counts where the cell really has a floor on this level: the rim reaches past the room into the wall.
                var floored = level is not { } lvl ||
                              g.Structures.GetCell((int)MathF.Floor(x), (int)MathF.Floor(rowCell), lvl) is
                                  { HasFloor: true };
                if (floored && feetY > MapDimensions.FloorProbeReach && ground <= 0.001f) zeros++;
            }

            var text = $"Probe {name}: ground {min:0.00}..{max:0.00} m, zero hits {zeros} (feet at {feetY:0.00} m)";
            if (zeros > 0) Warn(text + "  <-- the ground drops out");
            else Info(text);
        }

        /// <summary>
        /// The liquid layer name of a cell, if it has one.
        /// </summary>
        private static string? LiquidOf(MapCell3D? cell) =>
            cell is { HasFloor: true, Layers: { Count: > 0 } layers } ? layers[0].Name : null;

        private void Check(bool ok, string line)
        {
            if (ok) Info(line);
            else Warn(line + "  <-- engine patch not active?");
        }

        private void Info(string line)
        {
            _lines.Add(line);
            _logger?.LogInformation("{0}", line);
        }

        private void Warn(string line)
        {
            Warnings++;
            _lines.Add("WARN " + line);
            _logger?.LogWarning("{0}", line);
        }

        /// <summary>
        /// The floor lookup of whichever map is probed: a hybrid world includes the terrain, a dungeon only its cells.
        /// </summary>
        private readonly struct Grid
        {
            private readonly HybridWorldMap? _hybrid;

            public Grid(DungeonMap structures, HybridWorldMap? hybrid)
            {
                Structures = structures;
                _hybrid = hybrid;
            }

            public DungeonMap Structures { get; }

            public float Scale => Structures.DungeonCellScale;

            public float FloorAt(float worldX, float feetY, float worldZ) =>
                _hybrid?.GetFloorHeight(worldX, feetY, worldZ) ?? Structures.GetFloorHeight(worldX, feetY, worldZ);
        }
    }
}