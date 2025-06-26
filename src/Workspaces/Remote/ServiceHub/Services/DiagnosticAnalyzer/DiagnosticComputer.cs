// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

internal sealed class DiagnosticComputer
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

    public static Task<SerializableDiagnosticAnalysisResults> GetDiagnosticsAsync(
        TextDocument? document,
        Project project,
        Checksum solutionChecksum,
        TextSpan? span,
        ImmutableArray<string> projectAnalyzerIds,
        ImmutableArray<string> hostAnalyzerIds,
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
            projectAnalyzerIds, hostAnalyzerIds, logPerformanceInfo, getTelemetryInfo, cancellationToken);
    }

    private async Task<SerializableDiagnosticAnalysisResults> GetDiagnosticsAsync(
        ImmutableArray<string> projectAnalyzerIds,
        ImmutableArray<string> hostAnalyzerIds,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var (compilationWithAnalyzers, analyzerToIdMap) = await GetOrCreateCompilationWithAnalyzersAsync(cancellationToken).ConfigureAwait(false);
        if (compilationWithAnalyzers == null)
        {
            return SerializableDiagnosticAnalysisResults.Empty;
        }

        var (projectAnalyzers, hostAnalyzers) = GetAnalyzers(analyzerToIdMap, projectAnalyzerIds, hostAnalyzerIds);
        if (projectAnalyzers.IsEmpty && hostAnalyzers.IsEmpty)
        {
            return SerializableDiagnosticAnalysisResults.Empty;
        }

        if (_document == null)
        {
            if (projectAnalyzers.Length < compilationWithAnalyzers.ProjectAnalyzers.Length)
            {
                Contract.ThrowIfFalse(projectAnalyzers.Length > 0 || compilationWithAnalyzers.HostCompilationWithAnalyzers is not null);

                // PERF: Generate a new CompilationWithAnalyzers with trimmed analyzers for non-document analysis case.
                compilationWithAnalyzers = new CompilationWithAnalyzersPair(
                    projectAnalyzers.Any() ? compilationWithAnalyzers.ProjectCompilation!.WithAnalyzers(projectAnalyzers, compilationWithAnalyzers.ProjectCompilationWithAnalyzers!.AnalysisOptions) : null,
                    compilationWithAnalyzers.HostCompilationWithAnalyzers);
            }

            if (hostAnalyzers.Length < compilationWithAnalyzers.HostAnalyzers.Length)
            {
                Contract.ThrowIfFalse(hostAnalyzers.Length > 0 || compilationWithAnalyzers.ProjectCompilationWithAnalyzers is not null);

                // PERF: Generate a new CompilationWithAnalyzers with trimmed analyzers for non-document analysis case.
                compilationWithAnalyzers = new CompilationWithAnalyzersPair(
                    compilationWithAnalyzers.ProjectCompilationWithAnalyzers,
                    hostAnalyzers.Any() ? compilationWithAnalyzers.HostCompilation!.WithAnalyzers(hostAnalyzers, compilationWithAnalyzers.HostCompilationWithAnalyzers!.AnalysisOptions) : null);
            }
        }

        var skippedAnalyzersInfo = _project.Solution.SolutionState.Analyzers.GetSkippedAnalyzersInfo(
            _project.State, _analyzerInfoCache);

        return await AnalyzeAsync(compilationWithAnalyzers, analyzerToIdMap, projectAnalyzers, hostAnalyzers, skippedAnalyzersInfo,
            logPerformanceInfo, getTelemetryInfo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SerializableDiagnosticAnalysisResults> AnalyzeAsync(
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap,
        ImmutableArray<DiagnosticAnalyzer> projectAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> hostAnalyzers,
        SkippedHostAnalyzersInfo skippedAnalyzersInfo,
        bool logPerformanceInfo,
        bool getTelemetryInfo,
        CancellationToken cancellationToken)
    {
        var documentAnalysisScope = _document != null
            ? new DocumentAnalysisScope(_document, _span, projectAnalyzers, hostAnalyzers, _analysisKind!.Value)
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

                var performanceInfo = analysisResult?.MergedAnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache) ?? [];
                _performanceTracker.AddSnapshot(performanceInfo, unitCount, forSpanAnalysis: _span.HasValue);
            }
        }

        var builderMap = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>.Empty;
        if (analysisResult is not null)
        {
            builderMap = builderMap.AddRange(await analysisResult.ToResultBuilderMapAsync(
                additionalPragmaSuppressionDiagnostics, documentAnalysisScope,
                _project, projectAnalyzers, hostAnalyzers, skippedAnalyzersInfo, cancellationToken).ConfigureAwait(false));
        }

        var telemetry = getTelemetryInfo
            ? GetTelemetryInfo(analysisResult, projectAnalyzers, hostAnalyzers, analyzerToIdMap)
            : [];

        return new SerializableDiagnosticAnalysisResults(Dehydrate(builderMap, analyzerToIdMap), telemetry);
    }

    private static ImmutableArray<(string analyzerId, SerializableDiagnosticMap diagnosticMap)> Dehydrate(
        ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder> builderMap,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
    {
        var diagnostics = new FixedSizeArrayBuilder<(string analyzerId, SerializableDiagnosticMap diagnosticMap)>(builderMap.Count);

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

        return diagnostics.MoveToImmutable();
    }

    private static ImmutableArray<(string analyzerId, AnalyzerTelemetryInfo)> GetTelemetryInfo(
        AnalysisResultPair? analysisResult,
        ImmutableArray<DiagnosticAnalyzer> projectAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> hostAnalyzers,
        BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
    {
        Func<DiagnosticAnalyzer, bool> shouldInclude;
        if (projectAnalyzers.Length < (analysisResult?.ProjectAnalysisResult?.AnalyzerTelemetryInfo.Count ?? 0)
            || hostAnalyzers.Length < (analysisResult?.HostAnalysisResult?.AnalyzerTelemetryInfo.Count ?? 0))
        {
            // Filter the telemetry info to the executed analyzers.
            using var _1 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var analyzersSet);
            analyzersSet.AddRange(projectAnalyzers);
            analyzersSet.AddRange(hostAnalyzers);

            shouldInclude = analyzer => analyzersSet.Contains(analyzer);
        }
        else
        {
            shouldInclude = _ => true;
        }

        using var _2 = ArrayBuilder<(string analyzerId, AnalyzerTelemetryInfo)>.GetInstance(out var telemetryBuilder);
        if (analysisResult is not null)
        {
            foreach (var (analyzer, analyzerTelemetry) in analysisResult.MergedAnalyzerTelemetryInfo)
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

    private static string GetAnalyzerId(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, DiagnosticAnalyzer analyzer)
    {
        var analyzerId = analyzerMap.GetKeyOrDefault(analyzer);
        Contract.ThrowIfNull(analyzerId);

        return analyzerId;
    }

    private static (ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers) GetAnalyzers(BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap, ImmutableArray<string> projectAnalyzerIds, ImmutableArray<string> hostAnalyzerIds)
    {
        // TODO: this probably need to be cached as well in analyzer service?
        var projectBuilder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
        var hostBuilder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

        foreach (var analyzerId in projectAnalyzerIds)
        {
            if (analyzerMap.TryGetValue(analyzerId, out var analyzer))
            {
                projectBuilder.Add(analyzer);
            }
        }

        foreach (var analyzerId in hostAnalyzerIds)
        {
            if (analyzerMap.TryGetValue(analyzerId, out var analyzer))
            {
                hostBuilder.Add(analyzer);
            }
        }

        var projectAnalyzers = projectBuilder.ToImmutableAndClear();

        if (hostAnalyzerIds.Any())
        {
            // If any host analyzers are active, make sure to also include any project diagnostic suppressors
            var projectSuppressors = projectAnalyzers.WhereAsArray(static a => a is DiagnosticSuppressor);
            // Make sure to remove any project suppressors already in the host analyzer array so we don't end up with
            // duplicates.
            hostBuilder.RemoveRange(projectSuppressors);
            hostBuilder.AddRange(projectSuppressors);
        }

        return (projectAnalyzers, hostBuilder.ToImmutableAndClear());
    }

    private async Task<(CompilationWithAnalyzersPair? compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)> GetOrCreateCompilationWithAnalyzersAsync(CancellationToken cancellationToken)
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
        using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var projectAnalyzerBuilder);
        using var _2 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var hostAnalyzerBuilder);
        foreach (var reference in _project.Solution.AnalyzerReferences)
        {
            if (!referenceSet.Add(reference.Id))
            {
                continue;
            }

            var analyzers = reference.GetAnalyzers(_project.Language);

            // At times some Host analyzers should be treated as project analyzers and
            // not be given access to the Host fallback options. In particular when we
            // replace SDK CodeStyle analyzers with the Features analyzers.
            if (ShouldRedirectAnalyzers(_project, reference))
            {
                projectAnalyzerBuilder.AddRange(analyzers);
            }
            else
            {
                hostAnalyzerBuilder.AddRange(analyzers);
            }

            analyzerMapBuilder.AppendAnalyzerMap(analyzers);
        }

        // Evaluate project analyzers after host analyzers to ensure duplicates in analyzerMapBuilder are
        // overwritten with project analyzers if/when applicable.
        foreach (var reference in _project.AnalyzerReferences)
        {
            if (!referenceSet.Add(reference.Id))
            {
                continue;
            }

            var analyzers = reference.GetAnalyzers(_project.Language);
            projectAnalyzerBuilder.AddRange(analyzers);

            var projectSuppressors = analyzers.WhereAsArray(static a => a is DiagnosticSuppressor);
            // Make sure to remove any project suppressors already in the host analyzer array so we don't end up with
            // duplicates.
            hostAnalyzerBuilder.RemoveRange(projectSuppressors);
            hostAnalyzerBuilder.AddRange(projectSuppressors);

            analyzerMapBuilder.AppendAnalyzerMap(analyzers);
        }

        var compilationWithAnalyzers = projectAnalyzerBuilder.Count > 0 || hostAnalyzerBuilder.Count > 0
            ? await CreateCompilationWithAnalyzerAsync(projectAnalyzerBuilder.ToImmutable(), hostAnalyzerBuilder.ToImmutable(), cancellationToken).ConfigureAwait(false)
            : null;
        var analyzerToIdMap = new BidirectionalMap<string, DiagnosticAnalyzer>(analyzerMapBuilder);

        return new CompilationWithAnalyzersCacheEntry(_solutionChecksum, _project, compilationWithAnalyzers, analyzerToIdMap);

        static bool ShouldRedirectAnalyzers(Project project, AnalyzerReference reference)
        {
            // When replacing SDK CodeStyle analyzers we should redirect Features analyzers
            // so they are treated as project analyzers.
            return project.State.HasSdkCodeStyleAnalyzers && reference.IsFeaturesAnalyzer();
        }
    }

    private async Task<CompilationWithAnalyzersPair> CreateCompilationWithAnalyzerAsync(ImmutableArray<DiagnosticAnalyzer> projectAnalyzers, ImmutableArray<DiagnosticAnalyzer> hostAnalyzers, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(!projectAnalyzers.IsEmpty || !hostAnalyzers.IsEmpty);

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
        var projectAnalyzerOptions = new CompilationWithAnalyzersOptions(
            options: _project.AnalyzerOptions,
            onAnalyzerException: null,
            analyzerExceptionFilter: null,
            concurrentAnalysis: concurrentAnalysis,
            logAnalyzerExecutionTime: true,
            reportSuppressedDiagnostics: true);
        var hostAnalyzerOptions = new CompilationWithAnalyzersOptions(
            options: _project.HostAnalyzerOptions,
            onAnalyzerException: null,
            analyzerExceptionFilter: null,
            concurrentAnalysis: concurrentAnalysis,
            logAnalyzerExecutionTime: true,
            reportSuppressedDiagnostics: true);

        return new CompilationWithAnalyzersPair(
            projectAnalyzers.Any() ? compilation.WithAnalyzers(projectAnalyzers, projectAnalyzerOptions) : null,
            hostAnalyzers.Any() ? compilation.WithAnalyzers(hostAnalyzers, hostAnalyzerOptions) : null);
    }

    private sealed class CompilationWithAnalyzersCacheEntry
    {
        public Checksum SolutionChecksum { get; }
        public Project Project { get; }
        public CompilationWithAnalyzersPair? CompilationWithAnalyzers { get; }
        public BidirectionalMap<string, DiagnosticAnalyzer> AnalyzerToIdMap { get; }

        public CompilationWithAnalyzersCacheEntry(Checksum solutionChecksum, Project project, CompilationWithAnalyzersPair? compilationWithAnalyzers, BidirectionalMap<string, DiagnosticAnalyzer> analyzerToIdMap)
        {
            SolutionChecksum = solutionChecksum;
            Project = project;
            CompilationWithAnalyzers = compilationWithAnalyzers;
            AnalyzerToIdMap = analyzerToIdMap;
        }
    }
}
