// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        /// This cache CompilationWithAnalyzer for active/open files. 
        /// This will aggressively let go cached compilationWithAnalyzers to not hold them into memory too long.
        /// </summary>
        private class CompilationManager
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;
            private ConditionalWeakTable<Project, CompilationWithAnalyzers> _map;

            public CompilationManager(DiagnosticIncrementalAnalyzer owner)
            {
                _owner = owner;
                _map = new ConditionalWeakTable<Project, CompilationWithAnalyzers>();
            }

            /// <summary>
            /// Return CompilationWithAnalyzer for given project with given stateSets
            /// </summary>
            public async Task<CompilationWithAnalyzers> GetAnalyzerDriverAsync(Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
            {
                if (!project.SupportsCompilation)
                {
                    return null;
                }

                if (_map.TryGetValue(project, out var analyzerDriverOpt))
                {
                    // we have cached one, return that.
                    AssertAnalyzers(analyzerDriverOpt, stateSets);
                    return analyzerDriverOpt;
                }

                // Create driver that holds onto compilation and associated analyzers
                var includeSuppressedDiagnostics = true;
                var newAnalyzerDriverOpt = await CreateAnalyzerDriverAsync(project, stateSets, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                // Add new analyzer driver to the map
                analyzerDriverOpt = _map.GetValue(project, _ => newAnalyzerDriverOpt);

                // if somebody has beat us, make sure analyzers are good.
                if (analyzerDriverOpt != newAnalyzerDriverOpt)
                {
                    AssertAnalyzers(analyzerDriverOpt, stateSets);
                }

                // return driver
                return analyzerDriverOpt;
            }

            public Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(Project project, IEnumerable<StateSet> stateSets, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                var analyzers = stateSets.Select(s => s.Analyzer);
                return CreateAnalyzerDriverAsync(project, analyzers, includeSuppressedDiagnostics, cancellationToken);
            }

            public Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(
                Project project, IEnumerable<DiagnosticAnalyzer> analyzers, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                return _owner.Owner.CreateAnalyzerDriverAsync(project, analyzers, includeSuppressedDiagnostics, _owner.DiagnosticLogAggregator, cancellationToken);
            }

            private void ResetAnalyzerDriverMap()
            {
                // we basically eagarly clear the cache on some known changes
                // to let CompilationWithAnalyzer go.

                // we create new conditional weak table every time, it turns out 
                // only way to clear ConditionalWeakTable is re-creating it.
                // also, conditional weak table has a leak - https://github.com/dotnet/coreclr/issues/665
                _map = new ConditionalWeakTable<Project, CompilationWithAnalyzers>();
            }

            [Conditional("DEBUG")]
            private void AssertAnalyzers(CompilationWithAnalyzers analyzerDriver, IEnumerable<StateSet> stateSets)
            {
                if (analyzerDriver == null)
                {
                    // this can happen if project doesn't support compilation or no stateSets are given.
                    return;
                }

                // make sure analyzers are same.
                Contract.ThrowIfFalse(analyzerDriver.Analyzers.SetEquals(stateSets.Select(s => s.Analyzer).Where(a => !a.IsWorkspaceDiagnosticAnalyzer())));
            }

            #region state changed 
            public void OnActiveDocumentChanged()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnDocumentOpened()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnDocumentClosed()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnDocumentReset()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnDocumentRemoved()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnProjectRemoved()
            {
                ResetAnalyzerDriverMap();
            }

            public void OnNewSolution()
            {
                ResetAnalyzerDriverMap();
            }
            #endregion
        }
    }
}
