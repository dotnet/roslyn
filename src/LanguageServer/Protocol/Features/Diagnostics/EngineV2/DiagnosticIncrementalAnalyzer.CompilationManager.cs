// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// Cached data from a <see cref="Project"/> to the last <see cref="CompilationWithAnalyzersPair"/> instance created
    /// for it.  Note: the CompilationWithAnalyzersPair instance is dependent on the set of <see cref="StateSet"/>s
    /// passed along with the project.  As such, we might not be able to use a prior cached value if the set of state
    /// sets changes.  In that case, a new instance will be created and will be cached for the next caller.
    /// </summary>
    private static readonly ConditionalWeakTable<Project, CompilationWithAnalyzersPair?> s_projectToCompilationWithAnalyzers = new();

    private static async Task<CompilationWithAnalyzersPair?> GetOrCreateCompilationWithAnalyzersAsync(
        Project project,
        ImmutableArray<StateSet> stateSets,
        bool crashOnAnalyzerException,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        // Make sure the cached pair matches the state sets we're asking about.  if not, recompute and cache with
        // the new state sets.
        if (!s_projectToCompilationWithAnalyzers.TryGetValue(project, out var compilationWithAnalyzersPair) ||
            !HasAllAnalyzers(stateSets, compilationWithAnalyzersPair))
        {
            compilationWithAnalyzersPair = await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                project,
                stateSets.SelectAsArray(s => !s.IsHostAnalyzer, s => s.Analyzer),
                stateSets.SelectAsArray(s => s.IsHostAnalyzer, s => s.Analyzer),
                crashOnAnalyzerException,
                cancellationToken).ConfigureAwait(false);

            // Make a best effort attempt to store the latest computed value against these state sets. If this
            // fails (because another thread interleaves with this), that's ok.  We still return the pair we 
            // computed, so our caller will still see the right data
            s_projectToCompilationWithAnalyzers.Remove(project);

            // Intentionally ignore the result of this.  We still want to use the value we computed above, even if
            // another thread interleaves and sets a different value.
            s_projectToCompilationWithAnalyzers.GetValue(project, _ => compilationWithAnalyzersPair);
        }

        return compilationWithAnalyzersPair;

        static bool HasAllAnalyzers(ImmutableArray<StateSet> stateSets, CompilationWithAnalyzersPair? compilationWithAnalyzers)
        {
            if (compilationWithAnalyzers is null)
                return false;

            foreach (var stateSet in stateSets)
            {
                if (stateSet.IsHostAnalyzer && !compilationWithAnalyzers.HostAnalyzers.Contains(stateSet.Analyzer))
                    return false;
                else if (!stateSet.IsHostAnalyzer && !compilationWithAnalyzers.ProjectAnalyzers.Contains(stateSet.Analyzer))
                    return false;
            }

            return true;
        }
    }
}
