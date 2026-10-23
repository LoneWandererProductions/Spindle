/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Maps
 * FILE:        BlendedSceneryOverdrawGuard.cs
 * PURPOSE:     Build-time sanity check for alpha-blended scenery (seagrass, lilies, ...).
 *              Those props are double-sided and unsorted, so a dense bed makes every pixel blend dozens
 *              of layers once the camera is inside it. That is a fill-rate problem, invisible in
 *              triangle counts and very hard to debug (a 0.40 seagrass density cost ~20 FPS underwater).
 *              The check runs once while the map is built, never per frame, and stays silent unless
 *              a configuration crosses the limits below.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

using FolkTales.Enums;
using System.Diagnostics;
using System.Numerics;

namespace FolkTales.Maps
{
    /// <summary>
    /// Estimates how many alpha-blended quads a camera standing inside a prop cluster has to blend,
    /// and warns loudly when that number is high enough to cause frame drops.
    /// </summary>
    public static class BlendedSceneryOverdrawGuard
    {
        /// <summary>
        /// Radius in world units around a prop that counts as "visible from a camera standing there".
        /// </summary>
        public const float ReachRadius = 6.0f;

        /// <summary>
        /// Blended quads within <see cref="ReachRadius"/> above which a warning is logged.
        /// Seagrass is ~11 quads per plant. The default is a best guess from simulation of the valley pond
        /// (density 0.20 = fine, 0.40 = ~20 FPS lost): treat it as a starting point and calibrate it by
        /// reading the peak that <see cref="Analyze"/> writes to the debug output for a known-good and a
        /// known-bad configuration, then set this value between the two.
        /// </summary>
        public static int WarnQuadsInReach { get; set; } = 300;

        /// <summary>
        /// Blended quads within reach above which the message is escalated to a SEVERE warning.
        /// </summary>
        public static int SevereQuadsInReach { get; set; } = 450;

        /// <summary>
        /// Master switch, e.g. to silence the guard in unit tests that build absurd scenes on purpose.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Where warnings go. Defaults to <see cref="Trace.TraceWarning(string)"/>; replace it to
        /// capture messages in tests or to route them into your own log.
        /// </summary>
        public static Action<string> Sink { get; set; } = message => Trace.TraceWarning(message);

        /// <summary>
        /// Rough number of alpha-blended quads a single prop contributes (see BonsaiFactory.Bake*).
        /// Types that are not listed are opaque or sparse enough to be ignored by this check.
        /// </summary>
        private static int QuadsPerProp(SceneryType type) => type switch
        {
            SceneryType.Seagrass => 11, // 8..14 double-sided blades
            SceneryType.Lotus => 8,
            SceneryType.WaterLily => 4,
            SceneryType.ElevatedWaterLily => 4,
            SceneryType.TallGrass => 11,
            SceneryType.Flowers => 8,
            _ => 0
        };

        /// <summary>
        /// Result of an overdraw analysis.
        /// </summary>
        /// <param name="PeakQuadsInReach">The highest blended quad count found within reach of any prop.</param>
        /// <param name="PeakPosition">Where that peak is located.</param>
        /// <param name="BlendedProps">Number of props that were counted.</param>
        public readonly record struct Report(int PeakQuadsInReach, Vector3 PeakPosition, int BlendedProps);

        /// <summary>
        /// Analyzes the given placements and logs a warning if they exceed the limits.
        /// Safe to call with any input; never throws and never logs when everything is fine.
        /// </summary>
        /// <param name="context">Short label for the log, e.g. "WaterSceneryBuilder".</param>
        /// <param name="placements">Type and world position of every prop that is about to be committed.</param>
        /// <returns>The analysis result (also useful for tuning the thresholds).</returns>
        public static Report Analyze(string context, IReadOnlyList<(SceneryType Type, Vector3 Position)> placements)
        {
            if (!Enabled || placements.Count == 0)
            {
                return default;
            }

            // Bucket the blended props into a grid of ReachRadius-sized cells, so the neighbor search is
            // O(n * k) instead of O(n^2) even for very large maps.
            var grid = new Dictionary<(int, int), List<(Vector3 Pos, int Quads)>>();
            var blendedProps = 0;

            foreach (var (type, pos) in placements)
            {
                var quads = QuadsPerProp(type);
                if (quads <= 0)
                {
                    continue;
                }

                blendedProps++;
                var key = ((int)MathF.Floor(pos.X / ReachRadius), (int)MathF.Floor(pos.Z / ReachRadius));
                if (!grid.TryGetValue(key, out var bucket))
                {
                    bucket = new List<(Vector3, int)>();
                    grid[key] = bucket;
                }

                bucket.Add((pos, quads));
            }

            var peak = 0;
            var peakPos = Vector3.Zero;
            var reachSq = ReachRadius * ReachRadius;

            foreach (var (key, bucket) in grid)
            {
                foreach (var (center, _) in bucket)
                {
                    var sum = 0;

                    for (var dx = -1; dx <= 1; dx++)
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (!grid.TryGetValue((key.Item1 + dx, key.Item2 + dz), out var neighbors))
                        {
                            continue;
                        }

                        foreach (var (pos, quads) in neighbors)
                        {
                            var ox = pos.X - center.X;
                            var oz = pos.Z - center.Z;
                            if (ox * ox + oz * oz <= reachSq)
                            {
                                sum += quads;
                            }
                        }
                    }

                    if (sum > peak)
                    {
                        peak = sum;
                        peakPos = center;
                    }
                }
            }

            var report = new Report(peak, peakPos, blendedProps);

            // Always visible in the debugger output of Debug builds, compiled out of Release builds.
            Debug.WriteLine(
                $"[{context}] blended scenery: {blendedProps} props, peak {peak} quads within {ReachRadius}m at {peakPos}.");

            if (peak >= WarnQuadsInReach)
            {
                var severity = peak >= SevereQuadsInReach ? "SEVERE" : "WARNING";

                Sink(
                    $"[{context}] OVERDRAW {severity}: {peak} alpha-blended quads within {ReachRadius}m of ({peakPos.X:F1}, {peakPos.Z:F1}) " +
                    $"(limit {WarnQuadsInReach}). Expect big frame drops when the camera is inside this cluster " +
                    $"(typically underwater). Reduce the scatter density / radius, raise WithMinSpacing(), " +
                    $"or lower the blade count. Total blended props: {blendedProps}.");
            }

            return report;
        }
    }
}