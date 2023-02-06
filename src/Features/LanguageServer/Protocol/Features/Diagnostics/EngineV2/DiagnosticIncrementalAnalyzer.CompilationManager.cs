// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Return CompilationWithAnalyzer for given project with given stateSets
        /// </summary>
        private async Task<CompilationWithAnalyzers?> GetOrCreateCompilationWithAnalyzersAsync(Project project, ImmutableArray<StateSet> stateSets, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                return null;
            }

            var ideOptions = AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

            if (_projectCompilationsWithAnalyzers.TryGetValue(project, out var compilationWithAnalyzers))
            {
                // We may have cached a null entry if we determiend that there are no actual analyzers to run.
                if (compilationWithAnalyzers is null)
                {
                    return null;
                }
                else if (((WorkspaceAnalyzerOptions)compilationWithAnalyzers.AnalysisOptions.Options!).IdeOptions == ideOptions)
                {
                    // we have cached one, return that.
                    AssertAnalyzers(compilationWithAnalyzers, stateSets);
                    return compilationWithAnalyzers;
                }
            }

            // Create driver that holds onto compilation and associated analyzers
            var newCompilationWithAnalyzers = await CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets, includeSuppressedDiagnostics: true, cancellationToken).ConfigureAwait(false);

            // Add new analyzer driver to the map
            compilationWithAnalyzers = _projectCompilationsWithAnalyzers.GetValue(project, _ => newCompilationWithAnalyzers);

            // if somebody has beat us, make sure analyzers are good.
            if (compilationWithAnalyzers != newCompilationWithAnalyzers)
            {
                AssertAnalyzers(compilationWithAnalyzers, stateSets);
            }

            return compilationWithAnalyzers;
        }

        private static Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzersAsync(Project project, IdeAnalyzerOptions ideOptions, IEnumerable<StateSet> stateSets, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets.Select(s => s.Analyzer), includeSuppressedDiagnostics, cancellationToken);

        private void ClearCompilationsWithAnalyzersCache(Project project)
            => _projectCompilationsWithAnalyzers.Remove(project);

        private void ClearCompilationsWithAnalyzersCache()
        {
            // we basically eagarly clear the cache on some known changes
            // to let CompilationWithAnalyzer go.

            // we create new conditional weak table every time netstandard as that's the only way it has to clear it.
#if NETSTANDARD
            _projectCompilationsWithAnalyzers = new ConditionalWeakTable<Project, CompilationWithAnalyzers?>();
#else
            _projectCompilationsWithAnalyzers.Clear();
#endif
        }

        [Conditional("DEBUG")]
        private static void AssertAnalyzers(CompilationWithAnalyzers? compilation, IEnumerable<StateSet> stateSets)
        {
            if (compilation == null)
            {
                // this can happen if project doesn't support compilation or no stateSets are given.
                return;
            }

            // make sure analyzers are same.
            Contract.ThrowIfFalse(compilation.Analyzers.SetEquals(stateSets.Select(s => s.Analyzer).Where(a => !a.IsWorkspaceDiagnosticAnalyzer())));
        }
    }
}
