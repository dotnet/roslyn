// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
                Contract.ThrowIfFalse(project.SupportsCompilation);

                CompilationWithAnalyzers analyzerDriver;
                if (_map.TryGetValue(project, out analyzerDriver))
                {
                    // we have cached one, return that.
                    AssertAnalyzers(analyzerDriver, stateSets);
                    return analyzerDriver;
                }

                // Create driver that holds onto compilation and associated analyzers
                var concurrentAnalysis = false;
                var includeSuppressedDiagnostics = true;
                var newAnalyzerDriver = await CreateAnalyzerDriverAsync(project, stateSets, concurrentAnalysis, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                // Add new analyzer driver to the map
                analyzerDriver = _map.GetValue(project, _ => newAnalyzerDriver);

                // if somebody has beat us, make sure analyzers are good.
                if (analyzerDriver != newAnalyzerDriver)
                {
                    AssertAnalyzers(analyzerDriver, stateSets);
                }

                // return driver
                return analyzerDriver;
            }

            public Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(
                Project project, IEnumerable<StateSet> stateSets, bool concurrentAnalysis, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                var analyzers = stateSets.Select(s => s.Analyzer).ToImmutableArrayOrEmpty();
                return CreateAnalyzerDriverAsync(project, analyzers, concurrentAnalysis, includeSuppressedDiagnostics, cancellationToken);
            }

            public Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(
                Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                var concurrentAnalysis = false;
                return CreateAnalyzerDriverAsync(project, analyzers, concurrentAnalysis, includeSuppressedDiagnostics, cancellationToken);
            }

            public async Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(
                Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, bool concurrentAnalysis, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(project.SupportsCompilation);

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                // Create driver that holds onto compilation and associated analyzers
                return CreateAnalyzerDriver(
                    project, compilation, analyzers, concurrentAnalysis: concurrentAnalysis, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: includeSuppressedDiagnostics);
            }

            public CompilationWithAnalyzers CreateAnalyzerDriver(
                Project project,
                Compilation compilation,
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                bool concurrentAnalysis,
                bool logAnalyzerExecutionTime,
                bool reportSuppressedDiagnostics)
            {
                Contract.ThrowIfFalse(project.SupportsCompilation);
                AssertCompilation(project, compilation);

                var analysisOptions = GetAnalyzerOptions(project, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics);

                // Create driver that holds onto compilation and associated analyzers
                return compilation.WithAnalyzers(analyzers, analysisOptions);
            }

            private CompilationWithAnalyzersOptions GetAnalyzerOptions(
                Project project,
                bool concurrentAnalysis,
                bool logAnalyzerExecutionTime,
                bool reportSuppressedDiagnostics)
            {
                return new CompilationWithAnalyzersOptions(
                    options: new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Workspace),
                    onAnalyzerException: GetOnAnalyzerException(project.Id),
                    analyzerExceptionFilter: GetAnalyzerExceptionFilter(project),
                    concurrentAnalysis: concurrentAnalysis,
                    logAnalyzerExecutionTime: logAnalyzerExecutionTime,
                    reportSuppressedDiagnostics: reportSuppressedDiagnostics);
            }

            private Func<Exception, bool> GetAnalyzerExceptionFilter(Project project)
            {
                return ex =>
                {
                    if (project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CrashOnAnalyzerException))
                    {
                        // if option is on, crash the host to get crash dump.
                        FatalError.ReportUnlessCanceled(ex);
                    }

                    return true;
                };
            }

            private Action<Exception, DiagnosticAnalyzer, Diagnostic> GetOnAnalyzerException(ProjectId projectId)
            {
                return _owner.Owner.GetOnAnalyzerException(projectId, _owner.DiagnosticLogAggregator);
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
                // make sure analyzers are same.
                Contract.ThrowIfFalse(analyzerDriver.Analyzers.SetEquals(stateSets.Select(s => s.Analyzer)));
            }

            [Conditional("DEBUG")]
            private void AssertCompilation(Project project, Compilation compilation1)
            {
                // given compilation must be from given project.
                Compilation compilation2;
                Contract.ThrowIfFalse(project.TryGetCompilation(out compilation2));
                Contract.ThrowIfFalse(compilation1 == compilation2);
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
