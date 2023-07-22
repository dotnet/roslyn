// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.Threading.ThreadingTools;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal class DiagnosticComputer
    {
        /// <summary>
        /// Cache of <see cref="CompilationWithAnalyzers"/> and a map from analyzer IDs to <see cref="DiagnosticAnalyzer"/>s
        /// for all analyzers for the last project to be analyzed.
        /// The <see cref="CompilationWithAnalyzers"/> instance is shared between all the following document analyses modes for the project:
        ///  1. Span-based analysis for active document (lightbulb)
        ///  2. Background analysis for active and open documents.
        ///  
        /// NOTE: We do not re-use this cache for project analysis as it leads to significant memory increase in the OOP process.
        /// Additionally, we only store the cache entry for the last project to be analyzed instead of maintaining a CWT keyed off
        /// each project in the solution, as the CWT does not seem to drop entries until ForceGC happens, leading to significant memory
        /// pressure when there are large number of open documents across different projects to be analyzed by background analysis.
        /// </summary>
        private static CompilationWithAnalyzersCacheEntry? s_compilationWithAnalyzersCache = null;

        /// <summary>
        /// Set of high priority diagnostic computation tasks which are currently executing.
        /// Any new high priority diagnostic request is added to this set before the core diagnostics
        /// compute call is performed, and removed from this list after the computation finishes.
        /// Any new normal priority diagnostic request first waits for all the high priority tasks in this set
        /// to complete, and moves ahead only after this list becomes empty.
        /// </summary>
        /// <remarks>
        /// Read/write access to this field is guarded by <see cref="s_gate"/>.
        /// </remarks>
        private static ImmutableHashSet<Task> s_highPriorityComputeTasks = ImmutableHashSet<Task>.Empty;

        /// <summary>
        /// Set of cancellation token sources for normal priority diagnostic computation tasks which are currently executing.
        /// For any new normal priority diagnostic request, a new cancellation token source is created and added to this set
        /// before the core diagnostics compute call is performed, and removed from this set after the computation finishes.
        /// Any new high priority diagnostic request first fires cancellation on all the cancellation token sources in this set
        /// to avoid resource contention between normal and high priority requests.
        /// Canceled normal priority diagnostic requests are re-attempted from scratch after all the high priority requests complete.
        /// </summary>
        /// <remarks>
        /// Read/write access to this field is guarded by <see cref="s_gate"/>.
        /// </remarks>
        private static ImmutableHashSet<CancellationTokenSource> s_normalPriorityCancellationTokenSources = ImmutableHashSet<CancellationTokenSource>.Empty;

        /// <summary>
        /// Static gate controlling access to following static fields:
        /// - <see cref="s_compilationWithAnalyzersCache"/>
        /// - <see cref="s_highPriorityComputeTasks"/>
        /// - <see cref="s_normalPriorityCancellationTokenSources"/>
        /// </summary>
        private static readonly object s_gate = new();

        /// <summary>
        /// Solution checksum for the diagnostic request.
        /// We use this checksum and the <see cref="ProjectId"/> of the diagnostic request as the key
        /// to the <see cref="s_compilationWithAnalyzersCache"/>.
        /// </summary>
        private readonly Checksum _solutionChecksum;

        private readonly TextDocument? _document;
        private readonly Project _project;
        private readonly IdeAnalyzerOptions _ideOptions;
        private readonly TextSpan? _span;
        private readonly AnalysisKind? _analysisKind;
        private readonly IPerformanceTrackerService? _performanceTracker;
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
        private readonly HostWorkspaceServices _hostWorkspaceServices;

        private DiagnosticComputer(
            TextDocument? document,
            Project project,
            Checksum solutionChecksum,
            IdeAnalyzerOptions ideOptions,
            TextSpan? span,
            AnalysisKind? analysisKind,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            HostWorkspaceServices hostWorkspaceServices)
        {
            _document = document;
            _project = project;
            _solutionChecksum = solutionChecksum;
            _ideOptions = ideOptions;
            _span = span;
            _analysisKind = analysisKind;
            _analyzerInfoCache = analyzerInfoCache;
            _hostWorkspaceServices = hostWorkspaceServices;
            _performanceTracker = project.Solution.Services.GetService<IPerformanceTrackerService>();
        }

        public static Task<SerializableDiagnosticAnalysisResults> GetDiagnosticsAsync(
            TextDocument? document,
            Project project,
            Checksum solutionChecksum,
            IdeAnalyzerOptions ideOptions,
            TextSpan? span,
            IEnumerable<string> analyzerIds,
            AnalysisKind? analysisKind,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            HostWorkspaceServices hostWorkspaceServices,
            bool isExplicit,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            // PERF: Due to the concept of InFlight solution snapshots in OOP process, we might have been
            //       handed a Project instance that does not match the Project instance corresponding to our
            //       cached CompilationWithAnalyzers instance, while the underlying Solution checksum matches
            //       for our cached entry and the incoming request.
            //       We detect this case upfront here and re-use the cached CompilationWithAnalyzers and Project
            //       instance for diagnostic computation, thus improving the performance of analyzer execution.
            //       This is an important performance optimization for lightbulb diagnostic computation.
            //       See https://github.com/dotnet/roslyn/issues/66968 for details.
            lock (s_gate)
            {
                if (s_compilationWithAnalyzersCache?.SolutionChecksum == solutionChecksum &&
                    s_compilationWithAnalyzersCache.Project.Id == project.Id &&
                    s_compilationWithAnalyzersCache.Project != project)
                {
                    project = s_compilationWithAnalyzersCache.Project;
                    if (document != null)
                        document = project.GetTextDocument(document.Id);
                }
            }

            // We execute explicit, user-invoked diagnostics requests with higher priority compared to implicit requests
            // from clients such as editor diagnostic tagger to show squiggles, background analysis to populate the error list, etc.
            var diagnosticsComputer = new DiagnosticComputer(document, project, solutionChecksum, ideOptions, span, analysisKind, analyzerInfoCache, hostWorkspaceServices);
            return isExplicit
                ? diagnosticsComputer.GetHighPriorityDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo, cancellationToken)
                : diagnosticsComputer.GetNormalPriorityDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo, cancellationToken);
        }

        private async Task<SerializableDiagnosticAnalysisResults> GetHighPriorityDiagnosticsAsync(
            IEnumerable<string> analyzerIds,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Step 1:
            //  - Create the core 'computeTask' for computing diagnostics.
            var computeTask = GetDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo, cancellationToken);

            // Step 2:
            //  - Add this computeTask to the set of currently executing high priority tasks.
            //    This set of high priority tasks is used in 'GetNormalPriorityDiagnosticsAsync'
            //    method to ensure that any new or cancelled normal priority task waits for all
            //    the executing high priority tasks before starting its execution.
            //  - Note that it is critical to do this step prior to Step 3 below to ensure that
            //    any canceled normal priority tasks in Step 3 do not resume execution prior to
            //    completion of this high priority computeTask. 
            lock (s_gate)
            {
                Debug.Assert(!s_highPriorityComputeTasks.Contains(computeTask));
                s_highPriorityComputeTasks = s_highPriorityComputeTasks.Add(computeTask);
            }

            try
            {
                // Step 3:
                //  - Force cancellation of all the executing normal priority tasks
                //    to minimize resource and CPU contention between normal priority tasks
                //    and the high priority computeTask in Step 4 below.
                CancelNormalPriorityTasks();

                // Step 4:
                //  - Execute the core 'computeTask' for diagnostic computation.
                return await computeTask.ConfigureAwait(false);
            }
            finally
            {
                // Step 5:
                //  - Remove the 'computeTask' from the set of current executing high priority tasks.
                lock (s_gate)
                {
                    Debug.Assert(s_highPriorityComputeTasks.Contains(computeTask));
                    s_highPriorityComputeTasks = s_highPriorityComputeTasks.Remove(computeTask);
                }
            }

            static void CancelNormalPriorityTasks()
            {
                ImmutableHashSet<CancellationTokenSource> cancellationTokenSources;
                lock (s_gate)
                {
                    cancellationTokenSources = s_normalPriorityCancellationTokenSources;
                }

                foreach (var cancellationTokenSource in cancellationTokenSources)
                {
                    try
                    {
                        cancellationTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // CancellationTokenSource might get disposed if the normal priority
                        // task completes while we were executing this foreach loop.
                        // Gracefully handle this case and ignore this exception.
                    }
                }
            }
        }

        private async Task<SerializableDiagnosticAnalysisResults> GetNormalPriorityDiagnosticsAsync(
            IEnumerable<string> analyzerIds,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Step 1:
                //  - Normal priority task must wait for all the executing high priority tasks to complete
                //    before beginning execution.
                await WaitForHighPriorityTasksAsync(cancellationToken).ConfigureAwait(false);

                // Step 2:
                //  - Create a custom 'cancellationTokenSource' associated with the current normal priority
                //    request and add it to the tracked set of normal priority cancellation token sources.
                //    This token source allows normal priority computeTasks to be cancelled when
                //    a subsequent high priority diagnostic request is received.
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (s_gate)
                {
                    s_normalPriorityCancellationTokenSources = s_normalPriorityCancellationTokenSources.Add(cancellationTokenSource);
                }

                try
                {
                    // Step 3:
                    //  - Execute the core compute task for diagnostic computation.
                    return await GetDiagnosticsAsync(analyzerIds, reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo,
                        cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationTokenSource.Token)
                {
                    // Step 4:
                    //  - Attempt to re-execute this cancelled normal priority task by running the loop again.
                    continue;
                }
                finally
                {
                    // Step 5:
                    //  - Remove the 'cancellationTokenSource' for completed or cancelled task.
                    //    For the case where the computeTask was cancelled, we will create a new
                    //    'cancellationTokenSource' for the retry.
                    lock (s_gate)
                    {
                        Debug.Assert(s_normalPriorityCancellationTokenSources.Contains(cancellationTokenSource));
                        s_normalPriorityCancellationTokenSources = s_normalPriorityCancellationTokenSources.Remove(cancellationTokenSource);
                    }
                }
            }

            static async Task WaitForHighPriorityTasksAsync(CancellationToken cancellationToken)
            {
                // We loop continuously until we have an empty high priority task queue.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ImmutableHashSet<Task> highPriorityTasksToAwait;
                    lock (s_gate)
                    {
                        highPriorityTasksToAwait = s_highPriorityComputeTasks;
                    }

                    if (highPriorityTasksToAwait.IsEmpty)
                    {
                        return;
                    }

                    // Wait for all the high priority tasks, ignoring all exceptions from it. Loop directly to avoid
                    // expensive allocations in Task.WhenAll.
                    foreach (var task in highPriorityTasksToAwait)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (task.IsCompleted)
                        {
                            // Make sure to yield so continuations of 'task' can make progress.
                            await Task.Yield().ConfigureAwait(false);
                        }
                        else
                        {
                            await task.WithCancellation(cancellationToken).NoThrowAwaitableInternal(false);
                        }
                    }
                }
            }
        }

        private async Task<SerializableDiagnosticAnalysisResults> GetDiagnosticsAsync(
            IEnumerable<string> analyzerIds,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var (compilationWithAnalyzers, analyzerToIdMap) = await GetOrCreateCompilationWithAnalyzersAsync(cancellationToken).ConfigureAwait(false);
            if (compilationWithAnalyzers == null)
            {
                return SerializableDiagnosticAnalysisResults.Empty;
            }

            var analyzers = GetAnalyzers(analyzerToIdMap, analyzerIds);
            if (analyzers.IsEmpty)
            {
                return SerializableDiagnosticAnalysisResults.Empty;
            }

            if (_document == null && analyzers.Length < compilationWithAnalyzers.Analyzers.Length)
            {
                // PERF: Generate a new CompilationWithAnalyzers with trimmed analyzers for non-document analysis case.
                compilationWithAnalyzers = compilationWithAnalyzers.Compilation.WithAnalyzers(analyzers, compilationWithAnalyzers.AnalysisOptions);
            }

            var skippedAnalyzersInfo = _project.GetSkippedAnalyzersInfo(_analyzerInfoCache);

            return await AnalyzeAsync(compilationWithAnalyzers, analyzerToIdMap, analyzers, skippedAnalyzersInfo,
                reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
        }

        private async Task<SerializableDiagnosticAnalysisResults> AnalyzeAsync(
            CompilationWithAnalyzers compilationWithAnalyzers,
            BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            SkippedHostAnalyzersInfo skippedAnalyzersInfo,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var documentAnalysisScope = _document != null
                ? new DocumentAnalysisScope(_document, _span, analyzers, _analysisKind!.Value)
                : null;

            var (analysisResult, additionalPragmaSuppressionDiagnostics) = await compilationWithAnalyzers.GetAnalysisResultAsync(
                documentAnalysisScope, _project, _analyzerInfoCache, cancellationToken).ConfigureAwait(false);

            if (logPerformanceInfo && _performanceTracker != null)
            {
                // Only log telemetry snapshot is we have an active telemetry session,
                // i.e. user has not opted out of reporting telemetry.
                var telemetryService = _hostWorkspaceServices.GetRequiredService<IWorkspaceTelemetryService>();
                if (telemetryService.HasActiveSession)
                {
                    // +1 to include project itself
                    var unitCount = 1;
                    if (documentAnalysisScope == null)
                        unitCount += _project.DocumentIds.Count;

                    _performanceTracker.AddSnapshot(analysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache), unitCount, forSpanAnalysis: _span.HasValue);
                }
            }

            var builderMap = await analysisResult.ToResultBuilderMapAsync(
                additionalPragmaSuppressionDiagnostics, documentAnalysisScope,
                _project, VersionStamp.Default, compilationWithAnalyzers.Compilation,
                analyzers, skippedAnalyzersInfo, reportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

            var telemetry = getTelemetryInfo
                ? GetTelemetryInfo(analysisResult, analyzers, analyzerToIdMap)
                : ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)>.Empty;

            return new SerializableDiagnosticAnalysisResults(Dehydrate(builderMap, analyzerToIdMap), telemetry);
        }

        private static ImmutableArray<(string analyzerId, SerializableDiagnosticMap diagnosticMap)> Dehydrate(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder> builderMap,
            BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
        {
            using var _ = ArrayBuilder<(string analyzerId, SerializableDiagnosticMap diagnosticMap)>.GetInstance(out var diagnostics);

            foreach (var (analyzer, analyzerResults) in builderMap)
            {
                var analyzerId = GetAnalyzerId(analyzerToIdMap, analyzer);

                diagnostics.Add((analyzerId,
                    new SerializableDiagnosticMap(
                        analyzerResults.SyntaxLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                        analyzerResults.SemanticLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                        analyzerResults.NonLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                        analyzerResults.Others)));
            }

            return diagnostics.ToImmutable();
        }

        private static ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)> GetTelemetryInfo(
            AnalysisResult analysisResult,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
        {
            Func<DiagnosticAnalyzer, bool> shouldInclude;
            if (analyzers.Length < analysisResult.AnalyzerTelemetryInfo.Count)
            {
                // Filter the telemetry info to the executed analyzers.
                using var _1 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var analyzersSet);
                analyzersSet.AddRange(analyzers);

                shouldInclude = analyzer => analyzersSet.Contains(analyzer);
            }
            else
            {
                shouldInclude = _ => true;
            }

            using var _2 = ArrayBuilder<(string analyzerId, AnalyzerTelemetryInfo)>.GetInstance(out var telemetryBuilder);
            foreach (var (analyzer, analyzerTelemetry) in analysisResult.AnalyzerTelemetryInfo)
            {
                if (shouldInclude(analyzer))
                {
                    var analyzerId = GetAnalyzerId(analyzerToIdMap, analyzer);
                    telemetryBuilder.Add((analyzerId, analyzerTelemetry));
                }
            }

            return telemetryBuilder.ToImmutable();
        }

        private static string GetAnalyzerId(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, DiagnosticAnalyzer analyzer)
        {
            var analyzerId = analyzerMap.GetKeyOrDefault(analyzer);
            Contract.ThrowIfNull(analyzerId);

            return analyzerId;
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<string> analyzerIds)
        {
            // TODO: this probably need to be cached as well in analyzer service?
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            foreach (var analyzerId in analyzerIds)
            {
                if (analyzerMap.TryGetValue(analyzerId, out var analyzer))
                {
                    builder.Add(analyzer);
                }
            }

            return builder.ToImmutable();
        }

        private async Task<(CompilationWithAnalyzers? compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)> GetOrCreateCompilationWithAnalyzersAsync(CancellationToken cancellationToken)
        {
            var cacheEntry = await GetOrCreateCacheEntryAsync().ConfigureAwait(false);
            return (cacheEntry.CompilationWithAnalyzers, cacheEntry.AnalyzerToIdMap);

            async Task<CompilationWithAnalyzersCacheEntry> GetOrCreateCacheEntryAsync()
            {
                if (_document == null)
                {
                    // Only use cache for document analysis.
                    return await CreateCompilationWithAnalyzersCacheEntryAsync(cancellationToken).ConfigureAwait(false);
                }

                lock (s_gate)
                {
                    if (s_compilationWithAnalyzersCache?.SolutionChecksum == _solutionChecksum &&
                        s_compilationWithAnalyzersCache.Project == _project)
                    {
                        return s_compilationWithAnalyzersCache;
                    }
                }

                var entry = await CreateCompilationWithAnalyzersCacheEntryAsync(cancellationToken).ConfigureAwait(false);

                lock (s_gate)
                {
                    s_compilationWithAnalyzersCache = entry;
                }

                return entry;
            }
        }

        private async Task<CompilationWithAnalyzersCacheEntry> CreateCompilationWithAnalyzersCacheEntryAsync(CancellationToken cancellationToken)
        {
            // We could consider creating a service so that we don't do this repeatedly if this shows up as perf cost
            using var pooledObject = SharedPools.Default<HashSet<object>>().GetPooledObject();
            using var pooledMap = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            var referenceSet = pooledObject.Object;
            var analyzerMapBuilder = pooledMap.Object;

            // This follows what we do in DiagnosticAnalyzerInfoCache.CheckAnalyzerReferenceIdentity
            using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var analyzerBuilder);
            foreach (var reference in _project.Solution.AnalyzerReferences.Concat(_project.AnalyzerReferences))
            {
                if (!referenceSet.Add(reference.Id))
                {
                    continue;
                }

                var analyzers = reference.GetAnalyzers(_project.Language);
                analyzerBuilder.AddRange(analyzers);
                analyzerMapBuilder.AppendAnalyzerMap(analyzers);
            }

            var compilationWithAnalyzers = analyzerBuilder.Count > 0
                ? await CreateCompilationWithAnalyzerAsync(analyzerBuilder.ToImmutable(), cancellationToken).ConfigureAwait(false)
                : null;
            var analyzerToIdMap = new BidirectionalMap<string, DiagnosticAnalyzer>(analyzerMapBuilder);

            return new CompilationWithAnalyzersCacheEntry(_solutionChecksum, _project, compilationWithAnalyzers, analyzerToIdMap);
        }

        private async Task<CompilationWithAnalyzers> CreateCompilationWithAnalyzerAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(!analyzers.IsEmpty);

            // Always run analyzers concurrently in OOP
            const bool concurrentAnalysis = true;

            // Get original compilation
            var compilation = await _project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Fork compilation with concurrent build. this is okay since WithAnalyzers will fork compilation
            // anyway to attach event queue. This should make compiling compilation concurrent and make things
            // faster
            compilation = compilation.WithOptions(compilation.Options.WithConcurrentBuild(concurrentAnalysis));

            // Run analyzers concurrently, with performance logging and reporting suppressed diagnostics.
            // This allows all client requests with or without performance data and/or suppressed diagnostics to be satisfied.
            // TODO: can we support analyzerExceptionFilter in remote host? 
            //       right now, host doesn't support watson, we might try to use new NonFatal watson API?
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                options: new WorkspaceAnalyzerOptions(_project.AnalyzerOptions, _ideOptions),
                onAnalyzerException: null,
                analyzerExceptionFilter: null,
                concurrentAnalysis: concurrentAnalysis,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: true);

            return compilation.WithAnalyzers(analyzers, analyzerOptions);
        }

        private sealed class CompilationWithAnalyzersCacheEntry
        {
            public Checksum SolutionChecksum { get; }
            public Project Project { get; }
            public CompilationWithAnalyzers? CompilationWithAnalyzers { get; }
            public BidirectionalMap<string, DiagnosticAnalyzer> AnalyzerToIdMap { get; }

            public CompilationWithAnalyzersCacheEntry(Checksum solutionChecksum, Project project, CompilationWithAnalyzers? compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
            {
                SolutionChecksum = solutionChecksum;
                Project = project;
                CompilationWithAnalyzers = compilationWithAnalyzers;
                AnalyzerToIdMap = analyzerToIdMap;
            }
        }
    }
}
