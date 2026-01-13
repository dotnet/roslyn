// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

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
    /// </summary>
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, ImmutableHashSet<string>?> s_analyzerToDeprioritizedDiagnosticIds = new();

    private async Task<bool> IsDeprioritizedAnalyzerAsync(
        Project project, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
    {
        await PopulateDeprioritizedDiagnosticIdMapAsync(project, cancellationToken).ConfigureAwait(false);

        // this can't fail as the above call populates the CWT entries for all analyzers within that project if missing.
        Contract.ThrowIfFalse(s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var set));

        return set != null;
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
            if (!s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var deprioritizedIds))
            {
                if (compilationWithAnalyzers is null)
                {
                    compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzers_OnlyCallInProcessAsync(
                        project, analyzers, GetOrCreateHostAnalyzerInfo_OnlyCallInProcess(project), this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                }

                deprioritizedIds = await ComputeDeprioritizedDiagnosticIdsAsync(analyzer).ConfigureAwait(false);

#if NET
                s_analyzerToDeprioritizedDiagnosticIds.TryAdd(analyzer, deprioritizedIds);
#else
                lock (s_analyzerToDeprioritizedDiagnosticIds)
                {
                    if (!s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var existing))
                        s_analyzerToDeprioritizedDiagnosticIds.Add(analyzer, deprioritizedIds);
                }
#endif

            }

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

        async ValueTask<ImmutableHashSet<string>?> ComputeDeprioritizedDiagnosticIdsAsync(DiagnosticAnalyzer analyzer)
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
}
