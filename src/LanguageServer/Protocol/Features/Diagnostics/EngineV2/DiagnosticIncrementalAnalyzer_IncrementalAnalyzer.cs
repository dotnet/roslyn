// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
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

                // get driver only with active analyzers.
                var ideOptions = AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

                // PERF: get analyzers that are not suppressed and marked as open file only
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                var activeAnalyzers = stateSets.SelectAsArray(s => s.Analyzer);

                CompilationWithAnalyzers? compilationWithAnalyzers = null;

                compilationWithAnalyzers = await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                    project, ideOptions, activeAnalyzers, includeSuppressedDiagnostics: true, cancellationToken).ConfigureAwait(false);

                var result = await GetProjectAnalysisDataAsync(compilationWithAnalyzers, project, ideOptions, stateSets, cancellationToken).ConfigureAwait(false);

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

        private async Task TextDocumentOpenAsync(TextDocument document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // can not be canceled
                foreach (var stateSet in stateSets)
                    await stateSet.OnDocumentOpenedAsync(document).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Return list of <see cref="StateSet"/> to be used for full solution analysis.
        /// </summary>
        private ImmutableArray<StateSet> GetStateSetsForFullSolutionAnalysis(ImmutableArray<StateSet> stateSets, Project project)
        {
            // If full analysis is off, remove state that is created from build.
            // this will make sure diagnostics from build (converted from build to live) will never be cleared
            // until next build.
            _ = GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!compilerFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for compiler diagnostics,
                // so we remove the compiler analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either not for compiler analyzer or those which are for compiler
                // analyzer, but not from build.
                stateSets = stateSets.WhereAsArray(s => !s.Analyzer.IsCompilerAnalyzer() || !s.FromBuild(project.Id));
            }

            if (!analyzersFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for analyzer diagnostics,
                // so we remove the analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either for the special compiler/workspace analyzers or those which are for
                // other analyzers, but not from build.
                stateSets = stateSets.WhereAsArray(s => s.Analyzer.IsCompilerAnalyzer() || s.Analyzer.IsWorkspaceDiagnosticAnalyzer() || !s.FromBuild(project.Id));
            }

            // Include only analyzers we want to run for full solution analysis.
            // Analyzers not included here will never be saved because result is unknown.
            return stateSets.WhereAsArray(s => IsCandidateForFullSolutionAnalysis(s.Analyzer, project));
        }

        private bool IsCandidateForFullSolutionAnalysis(DiagnosticAnalyzer analyzer, Project project)
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

            return descriptors.Any(static (d, arg) => d.GetEffectiveSeverity(arg.CompilationOptions, arg.analyzerConfigOptions?.ConfigOptions, arg.analyzerConfigOptions?.TreeOptions) != ReportDiagnostic.Hidden, (project.CompilationOptions, analyzerConfigOptions));
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor(DiagnosticIncrementalAnalyzer diagnosticIncrementalAnalyzer)
        {
            public Task TextDocumentOpenAsync(TextDocument document)
                => diagnosticIncrementalAnalyzer.TextDocumentOpenAsync(document, CancellationToken.None);
        }
    }
}
