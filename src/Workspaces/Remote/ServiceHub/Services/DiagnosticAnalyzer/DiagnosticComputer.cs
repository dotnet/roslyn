// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

internal sealed partial class DiagnosticComputer
{
    /// <summary>
    /// Cache of <see cref="CompilationWithAnalyzers"/> and a map from analyzer IDs to <see cref="DiagnosticAnalyzer"/>s
    /// for all analyzers for the last project to be analyzed. The <see cref="CompilationWithAnalyzers"/> instance is
    /// shared between all the following document analyses modes for the project:
    /// <list type="number">
    /// <item>Span-based analysis for active document (lightbulb)</item>
    /// <item>Background analysis for active and open documents.</item>
    /// </list>
    /// NOTE: We do not re-use this cache for project analysis as it leads to significant memory increase in the OOP
    /// process. Additionally, we only store the cache entry for the last project to be analyzed instead of maintaining
    /// a CWT keyed off each project in the solution, as the CWT does not seem to drop entries until ForceGC happens,
    /// leading to significant memory pressure when there are large number of open documents across different projects
    /// to be analyzed by background analysis.
    /// </summary>
    private static CompilationWithAnalyzersCacheEntry? s_compilationWithAnalyzersCache = null;

    /// <summary>
    /// Static gate controlling access to following static fields:
    /// - <see cref="s_compilationWithAnalyzersCache"/>
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
    private readonly TextSpan? _span;
    private readonly AnalysisKind? _analysisKind;
    private readonly IPerformanceTrackerService? _performanceTracker;
    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
    private readonly HostWorkspaceServices _hostWorkspaceServices;

    private DiagnosticComputer(
        TextDocument? document,
        Project project,
        Checksum solutionChecksum,
        TextSpan? span,
        AnalysisKind? analysisKind,
        DiagnosticAnalyzerInfoCache analyzerInfoCache,
        HostWorkspaceServices hostWorkspaceServices)
    {
        _document = document;
        _project = project;
        _solutionChecksum = solutionChecksum;
        _span = span;
        _analysisKind = analysisKind;
        _analyzerInfoCache = analyzerInfoCache;
        _hostWorkspaceServices = hostWorkspaceServices;
        _performanceTracker = project.Solution.Services.GetService<IPerformanceTrackerService>();
    }

    public static Task<DiagnosticAnalysisResults> GetDiagnosticsAsync(
        TextDocument? document,
        Project project,
        Checksum solutionChecksum,
        TextSpan? span,
        ImmutableArray<string> analyzerIds,
        AnalysisKind? analysisKind,
        DiagnosticAnalyzerInfoCache analyzerInfoCache,
        HostWorkspaceServices hostWorkspaceServices,
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
        var diagnosticsComputer = new DiagnosticComputer(document, project, solutionChecksum, span, analysisKind, analyzerInfoCache, hostWorkspaceServices);
        return diagnosticsComputer.GetDiagnosticsAsync(
            analyzerIds, logPerformanceInfo, getTelemetryInfo, cancellationToken);
    }

    private async Task<DiagnosticAnalysisResults> GetDiagnosticsAsync(
        ImmutableArray<string> analyzerIds,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var (compilationWithAnalyzers, analyzerToIdMap) = await GetOrCreateCompilationWithAnalyzersAsync(cancellationToken).ConfigureAwait(false);
        if (compilationWithAnalyzers == null)
            return DiagnosticAnalysisResults.Empty;

        var analyzers = GetAnalyzers(analyzerToIdMap, analyzerIds);
        if (analyzerIds.IsEmpty)
            return DiagnosticAnalysisResults.Empty;

        if (_document == null)
        {
            if (analyzers.Length < compilationWithAnalyzers.Analyzers.Length)
            {
                Contract.ThrowIfFalse(analyzers.Length > 0 || compilationWithAnalyzers is not null);

                // PERF: Generate a new CompilationWithAnalyzers with trimmed analyzers for non-document analysis case.
                compilationWithAnalyzers = compilationWithAnalyzers.Compilation!.WithAnalyzers(analyzers, compilationWithAnalyzers.AnalysisOptions);
            }
        }

        var skippedAnalyzersInfo = _project.Solution.SolutionState.Analyzers.GetSkippedAnalyzersInfo(
            _project.State, _analyzerInfoCache);

        return await AnalyzeAsync(compilationWithAnalyzers, analyzerToIdMap, analyzers, skippedAnalyzersInfo,
            logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DiagnosticAnalysisResults> AnalyzeAsync(
        CompilationWithAnalyzers compilationWithAnalyzers,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        SkippedHostAnalyzersInfo skippedAnalyzersInfo,
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

                var performanceInfo = analysisResult?.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache) ?? [];
                _performanceTracker.AddSnapshot(performanceInfo, unitCount, forSpanAnalysis: _span.HasValue);
            }
        }

        var builderMap = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>.Empty;
        if (analysisResult is not null)
        {
            builderMap = builderMap.AddRange(await analysisResult.ToResultBuilderMapAsync(
                additionalPragmaSuppressionDiagnostics, documentAnalysisScope,
                _project, analyzers, skippedAnalyzersInfo, cancellationToken).ConfigureAwait(false));
        }

        var telemetry = getTelemetryInfo
            ? GetTelemetryInfo(analysisResult, analyzers, analyzerToIdMap)
            : [];

        return new DiagnosticAnalysisResults(Dehydrate(builderMap, analyzerToIdMap), telemetry);
    }

    private static ImmutableArray<(string analyzerId, DiagnosticMap diagnosticMap)> Dehydrate(
        ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder> builderMap,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
    {
        var diagnostics = new FixedSizeArrayBuilder<(string analyzerId, DiagnosticMap diagnosticMap)>(builderMap.Count);

        foreach (var (analyzer, analyzerResults) in builderMap)
        {
            var analyzerId = GetAnalyzerId(analyzerToIdMap, analyzer);

            diagnostics.Add((analyzerId,
                new DiagnosticMap(
                    analyzerResults.SyntaxLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                    analyzerResults.SemanticLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                    analyzerResults.NonLocals.SelectAsArray(entry => (entry.Key, entry.Value)),
                    analyzerResults.Others)));
        }

        return diagnostics.MoveToImmutable();
    }

    private static ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)> GetTelemetryInfo(
        AnalysisResult? analysisResult,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
    {
        Func<DiagnosticAnalyzer, bool> shouldInclude;
        if (analyzers.Length < (analysisResult?.AnalyzerTelemetryInfo.Count ?? 0))
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
        if (analysisResult is not null)
        {
            foreach (var (analyzer, analyzerTelemetry) in analysisResult.AnalyzerTelemetryInfo)
            {
                if (shouldInclude(analyzer))
                {
                    var analyzerId = GetAnalyzerId(analyzerToIdMap, analyzer);
                    telemetryBuilder.Add((analyzerId, analyzerTelemetry));
                }
            }
        }

        return telemetryBuilder.ToImmutableAndClear();
    }

    private static string GetAnalyzerId(
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap,
        DiagnosticAnalyzer analyzer)
    {
        var analyzerId = analyzerMap.GetKeyOrDefault(analyzer);
        Contract.ThrowIfNull(analyzerId);

        return analyzerId;
    }

    private static ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap,
        ImmutableArray<string> analyzerIds)
    {
        // TODO: this probably need to be cached as well in analyzer service?
        var builder = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();

        foreach (var analyzerId in analyzerIds)
        {
            if (analyzerMap.TryGetValue(analyzerId, out var analyzer))
            {
                builder.Add(analyzer);
            }
        }

        builder.RemoveDuplicates();
        return builder.ToImmutableAndFree();
    }

    private async Task<(CompilationWithAnalyzers? compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)> GetOrCreateCompilationWithAnalyzersAsync(CancellationToken cancellationToken)
    {
        var cacheEntry = await GetOrCreateCacheEntryAsync().ConfigureAwait(false);
        return (cacheEntry.CompilationWithAnalyzers, cacheEntry.AnalyzerToIdMap);//, cacheEntry.HostAnalyzerToIdMap);

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
        using var pooledMapAnalyzerMap = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
        var referenceSet = pooledObject.Object;
        var analyzerMapBuilder = pooledMapAnalyzerMap.Object;

        // This follows what we do in DiagnosticAnalyzerInfoCache.CheckAnalyzerReferenceIdentity
        using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var analyzerBuilder);
        foreach (var reference in _project.Solution.AnalyzerReferences)
        {
            if (!referenceSet.Add(reference.Id))
            {
                continue;
            }

            var analyzers = reference.GetAnalyzers(_project.Language);

            analyzerBuilder.AddRange(analyzers);
            analyzerMapBuilder.AppendAnalyzerMap(analyzers);
        }

        // Clear the set -- we want these two loops to be independent
        referenceSet.Clear();

        foreach (var reference in _project.AnalyzerReferences)
        {
            if (!referenceSet.Add(reference.Id))
            {
                continue;
            }

            var analyzers = reference.GetAnalyzers(_project.Language);

            // We do not want to run SDK 'features' analyzers.  We always defer to what is in VS for that.
            if (ShouldRedirectAnalyzers(_project, reference))
            {
                continue;
            }

            analyzerBuilder.AddRange(analyzers);
            analyzerMapBuilder.AppendAnalyzerMap(analyzers);
        }

        analyzerBuilder.RemoveDuplicates();

        var compilationWithAnalyzers = analyzerBuilder.Count > 0
            ? await CreateCompilationWithAnalyzerAsync(analyzerBuilder.ToImmutable(), cancellationToken).ConfigureAwait(false)
            : null;
        var analyzerToIdMap = new BidirectionalMap<string, DiagnosticAnalyzer>(analyzerMapBuilder);

        return new CompilationWithAnalyzersCacheEntry(_solutionChecksum, _project, compilationWithAnalyzers, analyzerToIdMap);//, hostAnalyzerToIdMap);

        static bool ShouldRedirectAnalyzers(Project project, AnalyzerReference reference)
        {
            // When replacing SDK CodeStyle analyzers we should redirect Features analyzers
            // so they are treated as project analyzers.
            return project.State.HasSdkCodeStyleAnalyzers && reference.IsFeaturesAnalyzer();
        }
    }

    private async Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzerAsync(
        ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
    {
        if (analyzers.IsEmpty)
            return null;

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
        return compilation.WithAnalyzers(
            analyzers,
            AnalyzerOptionsUtilities.Combine(_project.AnalyzerOptions, _project.HostAnalyzerOptions));
    }

    private sealed class CompilationWithAnalyzersCacheEntry
    {
        public Checksum SolutionChecksum { get; }
        public Project Project { get; }
        public CompilationWithAnalyzers? CompilationWithAnalyzers { get; }
        public BidirectionalMap<string, DiagnosticAnalyzer> AnalyzerToIdMap { get; }

        public CompilationWithAnalyzersCacheEntry(
            Checksum solutionChecksum,
            Project project,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
        {
            SolutionChecksum = solutionChecksum;
            Project = project;
            CompilationWithAnalyzers = compilationWithAnalyzers;
            AnalyzerToIdMap = analyzerToIdMap;
        }
    }
}
