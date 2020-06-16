// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal class DiagnosticComputer
    {
        private readonly Project _project;
        private readonly IPerformanceTrackerService? _performanceTracker;
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

        public DiagnosticComputer(Project project, DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            _project = project;
            _analyzerInfoCache = analyzerInfoCache;

            // we only track performance from primary branch. all forked branch we don't care such as preview.
            _performanceTracker = project.IsFromPrimaryBranch() ? project.Solution.Workspace.Services.GetService<IPerformanceTrackerService>() : null;
        }

        public async Task<DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder>> GetDiagnosticsAsync(
            IEnumerable<string> analyzerIds,
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            CancellationToken cancellationToken)
        {
            var analyzerMap = CreateAnalyzerMap(_project);
            var analyzers = GetAnalyzers(analyzerMap, analyzerIds);

            if (analyzers.Length == 0)
            {
                return DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder>.Empty;
            }

            var cacheService = _project.Solution.Workspace.Services.GetRequiredService<IProjectCacheService>();
            using var cache = cacheService.EnableCaching(_project.Id);
            var skippedAnalyzersInfo = _project.GetSkippedAnalyzersInfo(_analyzerInfoCache);
            return await AnalyzeAsync(analyzerMap, analyzers, skippedAnalyzersInfo, reportSuppressedDiagnostics, logAnalyzerExecutionTime, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder>> AnalyzeAsync(
            BidirectionalMap<string, DiagnosticAnalyzer> analyzerMap,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            SkippedHostAnalyzersInfo skippedAnalyzersInfo,
            bool reportSuppressedDiagnostics,
            bool logAnalyzerExecutionTime,
            CancellationToken cancellationToken)
        {
            // flag that controls concurrency
            var useConcurrent = true;

            // get original compilation
            var compilation = await _project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // fork compilation with concurrent build. this is okay since WithAnalyzers will fork compilation
            // anyway to attach event queue. this should make compiling compilation concurrent and make things
            // faster
            compilation = compilation.WithOptions(compilation.Options.WithConcurrentBuild(useConcurrent));

            // TODO: can we support analyzerExceptionFilter in remote host? 
            //       right now, host doesn't support watson, we might try to use new NonFatal watson API?
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                options: new WorkspaceAnalyzerOptions(_project.AnalyzerOptions, _project.Solution),
                onAnalyzerException: null,
                analyzerExceptionFilter: null,
                concurrentAnalysis: useConcurrent,
                logAnalyzerExecutionTime: logAnalyzerExecutionTime,
                reportSuppressedDiagnostics: reportSuppressedDiagnostics);

            var analyzerDriver = compilation.WithAnalyzers(analyzers, analyzerOptions);

            // PERF: Run all analyzers at once using the new GetAnalysisResultAsync API.
            var analysisResult = await analyzerDriver.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);

            // record performance if tracker is available
            if (_performanceTracker != null)
            {
                // +1 to include project itself
                _performanceTracker.AddSnapshot(analysisResult.AnalyzerTelemetryInfo.ToAnalyzerPerformanceInfo(_analyzerInfoCache), _project.DocumentIds.Count + 1);
            }

            var builderMap = analysisResult.ToResultBuilderMap(_project, VersionStamp.Default, compilation, analysisResult.Analyzers, skippedAnalyzersInfo, cancellationToken);

            return DiagnosticAnalysisResultMap.Create(
                builderMap.ToImmutableDictionary(kv => GetAnalyzerId(analyzerMap, kv.Key), kv => kv.Value),
                analysisResult.AnalyzerTelemetryInfo.ToImmutableDictionary(kv => GetAnalyzerId(analyzerMap, kv.Key), kv => kv.Value));
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

        private static BidirectionalMap<string, DiagnosticAnalyzer> CreateAnalyzerMap(Project project)
        {
            // we could consider creating a service so that we don't do this repeatedly if this shows up as perf cost
            using var pooledObject = SharedPools.Default<HashSet<object>>().GetPooledObject();
            using var pooledMap = SharedPools.Default<Dictionary<string, DiagnosticAnalyzer>>().GetPooledObject();
            var referenceSet = pooledObject.Object;
            var analyzerMap = pooledMap.Object;

            // this follow what we do in DiagnosticAnalyzerInfoCache.CheckAnalyzerReferenceIdentity
            foreach (var reference in project.Solution.AnalyzerReferences.Concat(project.AnalyzerReferences))
            {
                if (!referenceSet.Add(reference.Id))
                {
                    // already exist
                    continue;
                }

                analyzerMap.AppendAnalyzerMap(reference.GetAnalyzers(project.Language));
            }

            // convert regular map to bidirectional map
            return new BidirectionalMap<string, DiagnosticAnalyzer>(analyzerMap);
        }
    }
}
