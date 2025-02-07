// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private readonly ConditionalWeakTable<Project, StrongBox<(ImmutableArray<StateSet> stateSets, ProjectAnalysisData projectAnalysisData)>> _projectToForceAnalysisData = new();

        public async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                if (!_projectToForceAnalysisData.TryGetValue(project, out var box))
                {
                    box = new(await ComputeForceAnalyzeProjectAsync().ConfigureAwait(false));
                    box = _projectToForceAnalysisData.GetValue(project, _ => box);
                }

                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);

                var (stateSets, projectAnalysisData) = box.Value;
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetOrCreateProjectState(project.Id);

                    if (projectAnalysisData.TryGetResult(stateSet.Analyzer, out var analyzerResult))
                        diagnostics.AddRange(analyzerResult.GetAllDiagnostics());
                }

                return diagnostics.ToImmutableAndClear();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            async Task<(ImmutableArray<StateSet> stateSets, ProjectAnalysisData projectAnalysisData)> ComputeForceAnalyzeProjectAsync()
            {
                var allStateSets = await _stateManager.GetOrCreateStateSetsAsync(project, cancellationToken).ConfigureAwait(false);
                var fullSolutionAnalysisStateSets = allStateSets.WhereAsArray(
                    static (stateSet, arg) => arg.self.IsCandidateForFullSolutionAnalysis(stateSet.Analyzer, stateSet.IsHostAnalyzer, arg.project),
                    (self: this, project));

                var compilationWithAnalyzers = await CreateCompilationWithAnalyzersAsync(
                    project, fullSolutionAnalysisStateSets, AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                var projectAnalysisData = await GetProjectAnalysisDataAsync(compilationWithAnalyzers, project, fullSolutionAnalysisStateSets, cancellationToken).ConfigureAwait(false);
                return (fullSolutionAnalysisStateSets, projectAnalysisData);
            }
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
