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
    /// Each Lazy wraps a single non-cancelable computation task, so request cancellation only cancels that
    /// request's wait and cannot restart the analyzer initialization.
    /// </summary>
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, Lazy<Task<ImmutableHashSet<string>?>>> s_analyzerToDeprioritizedDiagnosticIds = new();

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

                // Concurrent cache misses can create multiple candidate lazies, but only the lazy stored in the
                // ConditionalWeakTable is evaluated. ExecutionAndPublication then ensures that every request through
                // that lazy shares the same task.
#pragma warning disable VSTHRD011 // The value factory only queues work; callers never synchronously wait on its task.
                var createdLazy = new Lazy<Task<ImmutableHashSet<string>?>>(
                    () => Task.Run(
                        () => ComputeDeprioritizedDiagnosticIdsAsync(analyzer),
                        CancellationToken.None),
                    LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore VSTHRD011 // The value factory only queues work; callers never synchronously wait on its task.
                lazyDeprioritizedIds = s_analyzerToDeprioritizedDiagnosticIds.GetValue(analyzer, _ => createdLazy);

                if (ReferenceEquals(lazyDeprioritizedIds, createdLazy))
                {
                    var createdComputationTask = GetLazyValueAsync(createdLazy, CancellationToken.None);
                    _ = createdComputationTask.ContinueWith(
                        task =>
                        {
                            // The exception was already reported inside the computation. If every caller canceled its
                            // wait, nobody else will observe the shared task's fault, so observe it here. Remove any
                            // faulted or canceled computation so a later lookup can retry.
                            if (task.IsFaulted)
                                _ = task.Exception;

                            s_analyzerToDeprioritizedDiagnosticIds.Remove(analyzer);
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }

            var deprioritizedIds = await GetLazyValueAsync(lazyDeprioritizedIds, cancellationToken).ConfigureAwait(false);
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

        async Task<ImmutableHashSet<string>?> ComputeDeprioritizedDiagnosticIdsAsync(DiagnosticAnalyzer analyzer)
        {
            try
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

                var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None).ConfigureAwait(false);
                if (telemetryInfo == null)
                    return null;

                if (telemetryInfo is { SymbolStartActionsCount: 0, SemanticModelActionsCount: 0 })
                    return null;

                return [.. analyzer.SupportedDiagnostics.Select(d => d.Id)];
            }
            // Report while the original stack is unwinding, then let the same exception fault the shared task so
            // every caller already waiting on it observes the failure.
            catch (Exception ex) when (FatalError.ReportAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }

    private static async Task<ImmutableHashSet<string>?> GetCachedDeprioritizedDiagnosticIdsAsync(
        DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var lazy));

        return await GetLazyValueAsync(lazy, cancellationToken).ConfigureAwait(false);
    }

    private static Task<T> GetLazyValueAsync<T>(Lazy<Task<T>> lazy, CancellationToken cancellationToken)
    {
#pragma warning disable VSTHRD011 // This lazy intentionally owns one shared task; callers never block synchronously.
        var task = lazy.Value;
#pragma warning restore VSTHRD011 // This lazy intentionally owns one shared task; callers never block synchronously.

#if NET
        return task.WaitAsync(cancellationToken);
#else
        // Compatibility implementation of Task.WaitAsync(CancellationToken), which is unavailable on netstandard2.0.
        // The continuation is only a proxy for this caller: its token can cancel the wait without canceling or restarting
        // the shared task. Returning and unwrapping the antecedent preserves its exact completion state.
        return !cancellationToken.CanBeCanceled || task.IsCompleted
            ? task
            : task.ContinueWith(
                static task => task,
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default).Unwrap();
#endif
    }
}
