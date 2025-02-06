// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                var stateSetsForProject = await _stateManager.GetOrCreateStateSetsAsync(project, cancellationToken).ConfigureAwait(false);
                var stateSets = GetStateSetsForFullSolutionAnalysis(stateSetsForProject, project);

                // PERF: get analyzers that are not suppressed and marked as open file only
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                var activeProjectAnalyzers = stateSets.SelectAsArray(s => !s.IsHostAnalyzer, s => s.Analyzer);
                var activeHostAnalyzers = stateSets.SelectAsArray(s => s.IsHostAnalyzer, s => s.Analyzer);

                CompilationWithAnalyzersPair? compilationWithAnalyzers = null;

                compilationWithAnalyzers = await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                    project, activeProjectAnalyzers, activeHostAnalyzers, AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                var result = await GetProjectAnalysisDataAsync(compilationWithAnalyzers, project, stateSets, cancellationToken).ConfigureAwait(false);

                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);

                // no cancellation after this point.
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetOrCreateProjectState(project.Id);

                    if (result.TryGetResult(stateSet.Analyzer, out var analyzerResult))
                    {
                        diagnostics.AddRange(analyzerResult.GetAllDiagnostics());
                        await state.SaveToInMemoryStorageAsync(project, analyzerResult).ConfigureAwait(false);
                    }
                }

                return diagnostics.ToImmutableAndClear();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Return list of <see cref="StateSet"/> to be used for full solution analysis.
        /// </summary>
        private ImmutableArray<StateSet> GetStateSetsForFullSolutionAnalysis(ImmutableArray<StateSet> stateSets, Project project)
        {
            // Include only analyzers we want to run for full solution analysis.
            // Analyzers not included here will never be saved because result is unknown.
            return stateSets.WhereAsArray(static (s, arg) => arg.self.IsCandidateForFullSolutionAnalysis(s.Analyzer, s.IsHostAnalyzer, arg.project), (self: this, project));
        }

        private bool IsCandidateForFullSolutionAnalysis(DiagnosticAnalyzer analyzer, bool isHostAnalyzer, Project project)
        {
            // PERF: Don't query descriptors for compiler analyzer or workspace load analyzer, always execute them.
            if (analyzer == FileContentLoadAnalyzer.Instance ||
                analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance ||
                analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            if (analyzer.IsBuiltInAnalyzer())
            {
                // always return true for builtin analyzer. we can't use
                // descriptor check since many builtin analyzer always return 
                // hidden descriptor regardless what descriptor it actually
                // return on runtime. they do this so that they can control
                // severity through option page rather than rule set editor.
                // this is special behavior only ide analyzer can do. we hope
                // once we support editorconfig fully, third party can use this
                // ability as well and we can remove this kind special treatment on builtin
                // analyzer.
                return true;
            }

            if (analyzer is DiagnosticSuppressor)
            {
                // Always execute diagnostic suppressors.
                return true;
            }

            if (project.CompilationOptions is null)
            {
                // Skip compilation options based checks for non-C#/VB projects.
                return true;
            }

            // For most of analyzers, the number of diagnostic descriptors is small, so this should be cheap.
            var descriptors = DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer);
            var analyzerConfigOptions = project.GetAnalyzerConfigOptions();

            return descriptors.Any(static (d, arg) => d.GetEffectiveSeverity(arg.CompilationOptions, arg.isHostAnalyzer ? arg.analyzerConfigOptions?.ConfigOptionsWithFallback : arg.analyzerConfigOptions?.ConfigOptionsWithoutFallback, arg.analyzerConfigOptions?.TreeOptions) != ReportDiagnostic.Hidden, (project.CompilationOptions, isHostAnalyzer, analyzerConfigOptions));
        }
    }
}
