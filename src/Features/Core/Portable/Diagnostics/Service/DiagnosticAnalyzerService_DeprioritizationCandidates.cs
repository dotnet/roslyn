// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// A cache from DiagnosticAnalyzer to whether or not it is a candidate for deprioritization when lightbulbs
    /// compute diagnostics for a particular priority class.  Note: as this caches data, it may technically be
    /// inaccurate as things change in the system.  For example, this is based on the registered actions made
    /// by an analyzer.  Hypothetically, such an analyzer might register different actions based on on things
    /// like appearing in a different language's compilation, or a compilation with different references, etc.
    /// We accept that this cache may be inaccurate in such scenarios as they are likely rare, and this only
    /// serves as a simple heuristic to order analyzer execution.  If wrong, it's not a major deal.
    /// Each AsyncLazy wraps a single non-cancelable computation task, so request cancellation only cancels that
    /// request's wait and cannot restart the analyzer initialization.
    /// </summary>
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, AsyncLazy<ImmutableHashSet<string>?>> s_analyzerToDeprioritizedDiagnosticIds = new();

    private async Task<bool> IsDeprioritizedAnalyzerAsync(
        Project project, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
    {
        await PopulateDeprioritizedDiagnosticIdMapAsync(project, cancellationToken).ConfigureAwait(false);

        return await GetCachedDeprioritizedDiagnosticIdsAsync(analyzer, cancellationToken).ConfigureAwait(false) != null;
    }

    private async ValueTask PopulateDeprioritizedDiagnosticIdMapAsync(Project project, CancellationToken cancellationToken)
    {
        await IsAnyDiagnosticIdDeprioritizedAsync(project, diagnosticIds: [], cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsAnyDeprioritizedDiagnosticIdInProcessAsync(
        Project project, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken)
    {
        CompilationWithAnalyzers? compilationWithAnalyzers = null;

        var analyzers = GetProjectAnalyzers_OnlyCallInProcess(project);
        foreach (var analyzer in analyzers)
        {
            if (!s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var lazyDeprioritizedIds))
            {
                if (compilationWithAnalyzers is null)
                {
                    compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzers_OnlyCallInProcessAsync(
                        project, analyzers, GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(project), this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                }

                // Concurrent cache misses can create multiple candidate lazies. Defer Task.Run so candidates that
                // lose the ConditionalWeakTable race do not start duplicate analyzer work.
                var computationTaskGate = new object();
                Task<ImmutableHashSet<string>?>? computationTask = null;
                var createdLazy = AsyncLazy.Create(_ =>
                {
                    lock (computationTaskGate)
                    {
                        // AsyncLazy can invoke its delegate again after all of its requesters cancel, even if the
                        // previous computation ignored cancellation and is still running. Always return the same
                        // one-shot task so cancellation cannot start duplicate analyzer work.
                        return computationTask ??= Task.Run(
                            () => ComputeDeprioritizedDiagnosticIdsAsync(analyzer, CancellationToken.None),
                            CancellationToken.None);
                    }
                });
                lazyDeprioritizedIds = s_analyzerToDeprioritizedDiagnosticIds.GetValue(analyzer, _ => createdLazy);

                if (ReferenceEquals(lazyDeprioritizedIds, createdLazy))
                {
                    // AsyncLazy gives each caller an independently cancelable wait. The shared task itself is
                    // non-cancelable, so all callers can cancel without discarding and restarting its work.
                    var keepAliveTask = createdLazy.GetValueAsync(CancellationToken.None);
                    _ = ObserveKeepAliveTaskAsync(analyzer, createdLazy, keepAliveTask);
                }
            }

            var deprioritizedIds = await lazyDeprioritizedIds.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (deprioritizedIds != null)
            {
                foreach (var id in diagnosticIds)
                {
                    if (deprioritizedIds.Contains(id))
                        return true;
                }
            }
        }

        return false;

        async Task<ImmutableHashSet<string>?> ComputeDeprioritizedDiagnosticIdsAsync(
            DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            // We deprioritize SymbolStart/End and SemanticModel analyzers from 'Normal' to 'Low' priority bucket,
            // as these are computationally more expensive.
            // Note that we never de-prioritize compiler analyzer, even though it registers a SemanticModel action.
            if (compilationWithAnalyzers == null ||
                analyzer.IsWorkspaceDiagnosticAnalyzer() ||
                analyzer.IsCompilerAnalyzer())
            {
                return null;
            }

            var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
            if (telemetryInfo == null)
                return null;

            if (telemetryInfo is { SymbolStartActionsCount: 0, SemanticModelActionsCount: 0 })
                return null;

            return [.. analyzer.SupportedDiagnostics.Select(d => d.Id)];
        }
    }

    private static async Task ObserveKeepAliveTaskAsync(
        DiagnosticAnalyzer analyzer,
        AsyncLazy<ImmutableHashSet<string>?> createdLazy,
        Task<ImmutableHashSet<string>?> keepAliveTask)
    {
        try
        {
            await keepAliveTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
            // Every request through createdLazy resolves to this same one-shot task. Existing callers still observe
            // this exception, but a stale reference cannot start a retry. Removing the matching cache entry allows
            // a later lookup to create a fresh task instead.
            if (s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var currentLazy) &&
                ReferenceEquals(currentLazy, createdLazy))
            {
                s_analyzerToDeprioritizedDiagnosticIds.Remove(analyzer);
            }
        }
    }

    private static async Task<ImmutableHashSet<string>?> GetCachedDeprioritizedDiagnosticIdsAsync(
        DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var lazy));

        // AsyncLazy only coordinates each caller's cancellation. Its delegate returns the same one-shot task even
        // after a fault, so this await cannot start another computation through a stale cache entry.
        return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }
}
