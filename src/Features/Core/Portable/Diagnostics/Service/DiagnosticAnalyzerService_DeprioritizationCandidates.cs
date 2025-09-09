// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    private static readonly ConditionalWeakTable<DiagnosticAnalyzer, StrongBox<bool>> s_analyzerToIsDeprioritizationCandidateMap = new();

    private async Task<ImmutableArray<DiagnosticAnalyzer>> GetDeprioritizationCandidatesInProcessAsync(
        Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var builder);

        HostAnalyzerInfo? hostAnalyzerInfo = null;
        CompilationWithAnalyzersPair? compilationWithAnalyzers = null;

        foreach (var analyzer in analyzers)
        {
            if (!s_analyzerToIsDeprioritizationCandidateMap.TryGetValue(analyzer, out var boxedBool))
            {
                if (hostAnalyzerInfo is null)
                {
                    hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo(project);
                    compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(
                        project, analyzers, hostAnalyzerInfo, this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                }

                boxedBool = new(await IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(analyzer).ConfigureAwait(false));
#if NET
                s_analyzerToIsDeprioritizationCandidateMap.TryAdd(analyzer, boxedBool);
#else
                lock (s_analyzerToIsDeprioritizationCandidateMap)
                {
                    if (!s_analyzerToIsDeprioritizationCandidateMap.TryGetValue(analyzer, out var existing))
                        s_analyzerToIsDeprioritizationCandidateMap.Add(analyzer, boxedBool);
                }
#endif
            }

            if (boxedBool.Value)
                builder.Add(analyzer);
        }

        return builder.ToImmutableAndClear();

        async Task<bool> IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(DiagnosticAnalyzer analyzer)
        {
            // We deprioritize SymbolStart/End and SemanticModel analyzers from 'Normal' to 'Low' priority bucket,
            // as these are computationally more expensive.
            // Note that we never de-prioritize compiler analyzer, even though it registers a SemanticModel action.
            if (compilationWithAnalyzers == null ||
                analyzer.IsWorkspaceDiagnosticAnalyzer() ||
                analyzer.IsCompilerAnalyzer())
            {
                return false;
            }

            var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
            if (telemetryInfo == null)
                return false;

            return telemetryInfo.SymbolStartActionsCount > 0 || telemetryInfo.SemanticModelActionsCount > 0;
        }
    }
}
