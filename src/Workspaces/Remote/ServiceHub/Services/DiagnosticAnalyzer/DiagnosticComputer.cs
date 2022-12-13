﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

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
        private static readonly WeakReference<CompilationWithAnalyzersCacheEntry?> s_compilationWithAnalyzersCache = new(null);
        private static readonly object s_gate = new();

        private readonly TextDocument? _document;
        private readonly Project _project;
        private readonly IdeAnalyzerOptions _ideOptions;
        private readonly TextSpan? _span;
        private readonly AnalysisKind? _analysisKind;
        private readonly IPerformanceTrackerService? _performanceTracker;
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
        private readonly HostWorkspaceServices _hostWorkspaceServices;

        public DiagnosticComputer(
            TextDocument? document,
            Project project,
            IdeAnalyzerOptions ideOptions,
            TextSpan? span,
            AnalysisKind? analysisKind,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            HostWorkspaceServices hostWorkspaceServices)
        {
            _document = document;
            _project = project;
            _ideOptions = ideOptions;
            _span = span;
            _analysisKind = analysisKind;
            _analyzerInfoCache = analyzerInfoCache;
            _hostWorkspaceServices = hostWorkspaceServices;
            _performanceTracker = project.Solution.Services.GetService<IPerformanceTrackerService>();
        }

        public async Task<SerializableDiagnosticAnalysisResults> GetDiagnosticsAsync(
            IEnumerable<string> analyzerIds,
            bool reportSuppressedDiagnostics,
            bool logPerformanceInfo,
            bool getTelemetryInfo,
            CancellationToken cancellationToken)
        {
            var (compilationWithAnalyzers, analyzerToIdMap) = await GetOrCreateCompilationWithAnalyzersAsync(cancellationToken).ConfigureAwait(false);

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

            try
            {
                return await AnalyzeAsync(compilationWithAnalyzers, analyzerToIdMap, analyzers, skippedAnalyzersInfo,
                    reportSuppressedDiagnostics, logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Do not re-use cached CompilationWithAnalyzers instance in presence of an exception, as the underlying analysis state might be corrupt.
                lock (s_gate)
                {
                    if (s_compilationWithAnalyzersCache.TryGetTarget(out var target) &&
                        target?.Project == _project)
                    {
                        s_compilationWithAnalyzersCache.SetTarget(null);
                    }
                }

                throw;
            }
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

        private async Task<(CompilationWithAnalyzers compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)> GetOrCreateCompilationWithAnalyzersAsync(CancellationToken cancellationToken)
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
                    if (s_compilationWithAnalyzersCache.TryGetTarget(out var target) &&
                        target?.Project == _project)
                    {
                        return target;
                    }
                }

                var entry = await CreateCompilationWithAnalyzersCacheEntryAsync(cancellationToken).ConfigureAwait(false);

                lock (s_gate)
                {
                    s_compilationWithAnalyzersCache.SetTarget(entry);
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

            var compilationWithAnalyzers = await CreateCompilationWithAnalyzerAsync(analyzerBuilder.ToImmutable(), cancellationToken).ConfigureAwait(false);
            var analyzerToIdMap = new BidirectionalMap<string, DiagnosticAnalyzer>(analyzerMapBuilder);

            return new CompilationWithAnalyzersCacheEntry(_project, compilationWithAnalyzers, analyzerToIdMap);
        }

        private async Task<CompilationWithAnalyzers> CreateCompilationWithAnalyzerAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
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
            public Project Project { get; }
            public CompilationWithAnalyzers CompilationWithAnalyzers { get; }
            public BidirectionalMap<string, DiagnosticAnalyzer> AnalyzerToIdMap { get; }

            public CompilationWithAnalyzersCacheEntry(Project project, CompilationWithAnalyzers compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
            {
                Project = project;
                CompilationWithAnalyzers = compilationWithAnalyzers;
                AnalyzerToIdMap = analyzerToIdMap;
            }
        }
    }
}
