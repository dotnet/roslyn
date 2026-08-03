// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Implemented by server compilers that can produce telemetry for a build request. The request
    /// handler collects these events after a compilation completes and returns them to the client in
    /// the <see cref="CompletedBuildResponse"/>. The build task then forwards each event to the host
    /// via <c>IBuildEngine5.LogTelemetry</c>.
    /// </summary>
    /// <remarks>
    /// This is intentionally generic: the transport (protocol + task) has no knowledge of any
    /// particular event, so new server-side telemetry can be added by producing additional
    /// <see cref="BuildTelemetryEvent"/> instances without changing the protocol or the task.
    /// </remarks>
    internal interface ICompilerServerTelemetryProvider
    {
        /// <summary>
        /// Returns the telemetry events collected for the current request. Returns an empty list
        /// when there is nothing to report.
        /// </summary>
        IReadOnlyList<BuildTelemetryEvent> GetTelemetryEvents();
    }

    /// <summary>
    /// Outcome of the compilation cache lookup for a single request.
    /// </summary>
    internal enum CompilationCacheStatus
    {
        /// <summary>The cache did not run for this request (disabled or not applicable).</summary>
        None,

        /// <summary>A cached result was found and restored.</summary>
        Hit,

        /// <summary>No cached result was found; a normal compilation was performed.</summary>
        Miss,
    }

    /// <summary>
    /// Outcome of an attempt to store a compilation result in the cache.
    /// </summary>
    internal enum CompilationCacheStoreResult
    {
        /// <summary>No store was attempted (for example, on a cache hit).</summary>
        None,

        /// <summary>The result was stored successfully.</summary>
        Stored,

        /// <summary>Another writer was already populating the entry.</summary>
        SkippedRace,

        /// <summary>The entry already existed when the store was attempted.</summary>
        SkippedExists,

        /// <summary>The store attempt failed.</summary>
        Failed,
    }

    /// <summary>
    /// Accumulates compilation-cache statistics for a single request and converts them into a
    /// generic <see cref="BuildTelemetryEvent"/>. This is the first contributor to the compiler
    /// server telemetry channel; additional contributors can be added independently.
    /// </summary>
    internal sealed class CompilationCacheTelemetry
    {
        /// <summary>
        /// The telemetry event name reported by the task. Host telemetry pipelines prefix this
        /// (Visual Studio: <c>vs/</c>, dotnet CLI: <c>dotnet/cli/msbuild/</c>).
        /// </summary>
        internal const string EventName = "roslyn/compilercache";

        public CompilationCacheStatus Status { get; set; }
        public CompilationCacheStoreResult StoreResult { get; set; }
        public long KeyComputeMilliseconds { get; set; }
        public long RestoreMilliseconds { get; set; }
        public long StoreMilliseconds { get; set; }

        /// <summary>
        /// Wall-clock time spent compiling and emitting on a cache miss. This is the time a
        /// corresponding cache hit would have saved, so it is the key signal for evaluating the
        /// cache. Zero on a hit (no compilation ran) and on a miss whose compilation failed (the
        /// result is never stored).
        /// </summary>
        public long CompileMilliseconds { get; set; }

        private Stopwatch? _compileStopwatch;

        /// <summary>
        /// Starts measuring compilation time. Called when a cache miss is detected, immediately
        /// before the normal compilation runs.
        /// </summary>
        public void StartCompileTimer() => _compileStopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Records the elapsed compilation time. Called once the compilation has completed
        /// successfully. No-op if the timer was never started.
        /// </summary>
        public void StopCompileTimer()
        {
            if (_compileStopwatch is not null)
            {
                CompileMilliseconds = _compileStopwatch.ElapsedMilliseconds;
                _compileStopwatch = null;
            }
        }

        /// <summary>
        /// True when the cache actually ran for this request and there is something to report.
        /// </summary>
        public bool HasData => Status != CompilationCacheStatus.None;

        public BuildTelemetryEvent ToTelemetryEvent(string language)
        {
            var properties = new Dictionary<string, string>(7)
            {
                ["cachestatus"] = Status switch
                {
                    CompilationCacheStatus.Hit => "hit",
                    CompilationCacheStatus.Miss => "miss",
                    _ => "none",
                },
                ["storeresult"] = StoreResult switch
                {
                    CompilationCacheStoreResult.Stored => "stored",
                    CompilationCacheStoreResult.SkippedRace => "skippedrace",
                    CompilationCacheStoreResult.SkippedExists => "skippedexists",
                    CompilationCacheStoreResult.Failed => "failed",
                    _ => "none",
                },
                ["language"] = language,
                ["keycomputems"] = KeyComputeMilliseconds.ToString(CultureInfo.InvariantCulture),
                ["restorems"] = RestoreMilliseconds.ToString(CultureInfo.InvariantCulture),
                ["storems"] = StoreMilliseconds.ToString(CultureInfo.InvariantCulture),
                ["compilems"] = CompileMilliseconds.ToString(CultureInfo.InvariantCulture),
            };

            return new BuildTelemetryEvent(EventName, properties);
        }
    }
}
