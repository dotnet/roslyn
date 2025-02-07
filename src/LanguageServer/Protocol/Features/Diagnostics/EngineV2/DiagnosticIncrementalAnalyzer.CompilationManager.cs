// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private static Task<CompilationWithAnalyzersPair?> CreateCompilationWithAnalyzersAsync(Project project, ImmutableArray<StateSet> stateSets, bool crashOnAnalyzerException, CancellationToken cancellationToken)
            => DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                project,
                stateSets.SelectAsArray(s => !s.IsHostAnalyzer, s => s.Analyzer),
                stateSets.SelectAsArray(s => s.IsHostAnalyzer, s => s.Analyzer),
                crashOnAnalyzerException,
                cancellationToken);
    }

    private static async Task<CompilationWithAnalyzersPair?> GetOrCreateCompilationWithAnalyzersAsync(
        Project project,
        ImmutableArray<StateSet> stateSets,
        bool crashOnAnalyzerException,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        if (!s_projectToCompilationWithAnalyzers.TryGetValue(project, out var compilationWithAnalyzersPair))
            compilationWithAnalyzersPair = await ComputeAndCacheCompilationWithAnalyzersAsync().ConfigureAwait(false);

        if (compilationWithAnalyzersPair is null)
            return null;

        // Make sure the cached pair matches the state sets we're asking about.  if not, recompute and cache
        // with the new state sets.
        if (HasAllAnalyzers(stateSets, compilationWithAnalyzersPair))
            return compilationWithAnalyzersPair;

        return await ComputeAndCacheCompilationWithAnalyzersAsync().ConfigureAwait(false);

        async Task<CompilationWithAnalyzersPair?> ComputeAndCacheCompilationWithAnalyzersAsync()
        {
            var compilationWithAnalyzersPair = await CreateCompilationWithAnalyzersAsync(project, stateSets, crashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

            // Make a best effort attempt to store the latest computed value against these state sets. If this
            // fails (because another thread interleaves with this), that's ok.  We still return the pair we 
            // computed, so our caller will still see the right data
            s_projectToCompilationWithAnalyzers.Remove(project);
            s_projectToCompilationWithAnalyzers.GetValue(project, _ => compilationWithAnalyzersPair);

            return compilationWithAnalyzersPair;
        }
        s_lastProjectAndCompilationWithAnalyzers.SetTarget(new ProjectAndCompilationWithAnalyzers(project, compilationWithAnalyzers));
        return compilationWithAnalyzers;

        static bool HasAllAnalyzers(ImmutableArray<StateSet> stateSets, CompilationWithAnalyzersPair compilationWithAnalyzers)
        {
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
