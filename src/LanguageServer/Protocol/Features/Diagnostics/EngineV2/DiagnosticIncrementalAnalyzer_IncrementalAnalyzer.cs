// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Cached data from a real <see cref="ProjectState"/> instance to the cached diagnostic data produced by
        /// <em>all</em> the analyzers for the project.  This data can then be used by <see
        /// cref="GetDiagnosticsForIdsAsync"/> to speed up subsequent calls through the normal <see
        /// cref="IDiagnosticAnalyzerService"/> entry points as long as the project hasn't changed at all.
        /// </summary>
        /// <remarks>
        /// This table is keyed off of <see cref="ProjectState"/> but stores data from <see cref="SolutionState"/> on
        /// it.  Specifically <see cref="SolutionState.Analyzers"/>.  Normally keying off a ProjectState would not be ok
        /// as the ProjectState might stay the same while the SolutionState changed.  However, that can't happen as
        /// SolutionState has the data for Analyzers computed prior to Projects being added, and then never changes.
        /// Practically, solution analyzers are the core Roslyn analyzers themselves we distribute, or analyzers shipped
        /// by vsix (not nuget).  These analyzers do not get loaded after changing *until* VS restarts.
        /// </remarks>
        private static readonly ConditionalWeakTable<ProjectState, StrongBox<(Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> diagnosticAnalysisResults)>> s_projectToForceAnalysisData = new();

        public async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var projectState = project.State;
            var checksum = await project.GetDiagnosticChecksumAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!s_projectToForceAnalysisData.TryGetValue(projectState, out var box) ||
                    box.Value.checksum != checksum)
                {
                    box = new(await ComputeForceAnalyzeProjectAsync().ConfigureAwait(false));

                    // Try to add the new computed data to the CWT.  But use any existing value that another thread
                    // might have beaten us to storing in it.
#if NET
                    if (!s_projectToForceAnalysisData.TryAdd(projectState, box))
                        Contract.ThrowIfFalse(s_projectToForceAnalysisData.TryGetValue(projectState, out box));
#else
                    box = s_projectToForceAnalysisData.GetValue(projectState, _ => box);
#endif
                }

                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);

                var (_, analyzers, projectAnalysisData) = box.Value;
                foreach (var analyzer in analyzers)
                {
                    if (projectAnalysisData.TryGetValue(analyzer, out var analyzerResult))
                        diagnostics.AddRange(analyzerResult.GetAllDiagnostics());
                }

                return diagnostics.ToImmutableAndClear();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            async Task<(Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> diagnosticAnalysisResults)> ComputeForceAnalyzeProjectAsync()
            {
                var solutionState = project.Solution.SolutionState;
                var allAnalyzers = await _stateManager.GetOrCreateAnalyzersAsync(solutionState, projectState, cancellationToken).ConfigureAwait(false);
                var hostAnalyzerInfo = await _stateManager.GetOrCreateHostAnalyzerInfoAsync(solutionState, projectState, cancellationToken).ConfigureAwait(false);

                var fullSolutionAnalysisAnalyzers = allAnalyzers.WhereAsArray(
                    static (analyzer, arg) => IsCandidateForFullSolutionAnalysis(
                        arg.self.DiagnosticAnalyzerInfoCache, analyzer, arg.hostAnalyzerInfo.IsHostAnalyzer(analyzer), arg.project),
                    (self: this, project, hostAnalyzerInfo));

                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(
                    project, fullSolutionAnalysisAnalyzers, hostAnalyzerInfo, AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                var projectAnalysisData = await ComputeDiagnosticAnalysisResultsAsync(compilationWithAnalyzers, project, fullSolutionAnalysisAnalyzers, cancellationToken).ConfigureAwait(false);
                return (checksum, fullSolutionAnalysisAnalyzers, projectAnalysisData);
            }

            static bool IsCandidateForFullSolutionAnalysis(
                DiagnosticAnalyzerInfoCache infoCache, DiagnosticAnalyzer analyzer, bool isHostAnalyzer, Project project)
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
                var descriptors = infoCache.GetDiagnosticDescriptors(analyzer);
                var analyzerConfigOptions = project.GetAnalyzerConfigOptions();

                return descriptors.Any(static (d, arg) => d.GetEffectiveSeverity(arg.CompilationOptions, arg.isHostAnalyzer ? arg.analyzerConfigOptions?.ConfigOptionsWithFallback : arg.analyzerConfigOptions?.ConfigOptionsWithoutFallback, arg.analyzerConfigOptions?.TreeOptions) != ReportDiagnostic.Hidden, (project.CompilationOptions, isHostAnalyzer, analyzerConfigOptions));
            }
        }
    }
}
