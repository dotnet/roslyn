// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Return all diagnostics that belong to given project for the given StateSets (analyzers) either from cache or by calculating them
        /// </summary>
        private async Task<ProjectAnalysisData> ComputeProjectAnalysisDataAsync(
            CompilationWithAnalyzersPair? compilationWithAnalyzers, Project project, ImmutableArray<StateSet> stateSets, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, stateSets, cancellationToken))
            {
                try
                {
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                    var result = await ComputeDiagnosticsAsync(compilationWithAnalyzers, project, stateSets, cancellationToken).ConfigureAwait(false);

                    // If project is not loaded successfully, get rid of any semantic errors from compiler analyzer.
                    // Note: In the past when project was not loaded successfully we did not run any analyzers on the project.
                    // Now we run analyzers but filter out some information. So on such projects, there will be some perf degradation.
                    result = await RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(result, project, cancellationToken).ConfigureAwait(false);

                    return new ProjectAnalysisData(result);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result, Project project, CancellationToken cancellationToken)
        {
            // see whether solution is loaded successfully
            var projectLoadedSuccessfully = await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (projectLoadedSuccessfully)
            {
                return result;
            }

            var compilerAnalyzer = project.Solution.SolutionState.Analyzers.GetCompilerDiagnosticAnalyzer(project.Language);
            if (compilerAnalyzer == null)
            {
                // this language doesn't support compiler analyzer
                return result;
            }

            if (!result.TryGetValue(compilerAnalyzer, out var analysisResult))
            {
                // no result from compiler analyzer
                return result;
            }

            Logger.Log(FunctionId.Diagnostics_ProjectDiagnostic, p => $"Failed to Load Successfully ({p.FilePath ?? p.Name})", project);

            // get rid of any result except syntax from compiler analyzer result
            var newCompilerAnalysisResult = analysisResult.DropExceptSyntax();

            // return new result
            return result.SetItem(compilerAnalyzer, newCompilerAnalysisResult);
        }

        /// <summary>
        /// Calculate all diagnostics for a given project using analyzers referenced by the project and specified IDE analyzers.
        /// </summary>
        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
            CompilationWithAnalyzersPair? compilationWithAnalyzers, Project project, ImmutableArray<DiagnosticAnalyzer> ideAnalyzers, CancellationToken cancellationToken)
        {
            try
            {
                var result = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                // can be null if given project doesn't support compilation.
                if (compilationWithAnalyzers?.ProjectAnalyzers.Length > 0
                    || compilationWithAnalyzers?.HostAnalyzers.Length > 0)
                {
                    // calculate regular diagnostic analyzers diagnostics
                    var resultMap = await _diagnosticAnalyzerRunner.AnalyzeProjectAsync(
                        project, compilationWithAnalyzers, logPerformanceInfo: false, getTelemetryInfo: true, cancellationToken).ConfigureAwait(false);

                    result = resultMap.AnalysisResult;

                    // record telemetry data
                    UpdateAnalyzerTelemetryData(resultMap.TelemetryInfo);
                }

                // check whether there is IDE specific project diagnostic analyzer
                Debug.Assert(ideAnalyzers.All(a => a is ProjectDiagnosticAnalyzer or DocumentDiagnosticAnalyzer));
                return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, ideAnalyzers, compilationWithAnalyzers?.HostCompilation, result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
            CompilationWithAnalyzersPair? compilationWithAnalyzers,
            Project project,
            ImmutableArray<StateSet> stateSets,
            CancellationToken cancellationToken)
        {
            try
            {
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                var ideAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => a is ProjectDiagnosticAnalyzer or DocumentDiagnosticAnalyzer).ToImmutableArrayOrEmpty();

                return await ComputeDiagnosticsAsync(compilationWithAnalyzers, project, ideAnalyzers, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
            Project project,
            ImmutableArray<DiagnosticAnalyzer> ideAnalyzers,
            Compilation? compilation,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result,
            CancellationToken cancellationToken)
        {
            try
            {
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                (result, var failedDocuments) = await UpdateWithDocumentLoadAndGeneratorFailuresAsync(result, project, version, cancellationToken).ConfigureAwait(false);

                foreach (var analyzer in ideAnalyzers)
                {
                    var builder = new DiagnosticAnalysisResultBuilder(project, version);

                    switch (analyzer)
                    {
                        case DocumentDiagnosticAnalyzer documentAnalyzer:
                            foreach (var document in project.Documents)
                            {
                                // don't analyze documents whose content failed to load
                                if (failedDocuments == null || !failedDocuments.Contains(document))
                                {
                                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                                    if (tree != null)
                                    {
                                        builder.AddSyntaxDiagnostics(tree, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                        builder.AddSemanticDiagnostics(tree, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                                    }
                                    else
                                    {
                                        builder.AddExternalSyntaxDiagnostics(document.Id, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                        builder.AddExternalSemanticDiagnostics(document.Id, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                                    }
                                }
                            }

                            break;

                        case ProjectDiagnosticAnalyzer projectAnalyzer:
                            builder.AddCompilationDiagnostics(await DocumentAnalysisExecutor.ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(projectAnalyzer, project, compilation, cancellationToken).ConfigureAwait(false));
                            break;
                    }

                    // merge the result to existing one.
                    // there can be existing one from compiler driver with empty set. overwrite it with
                    // ide one.
                    result = result.SetItem(analyzer, DiagnosticAnalysisResult.CreateFromBuilder(builder));
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> results, ImmutableHashSet<Document>? failedDocuments)> UpdateWithDocumentLoadAndGeneratorFailuresAsync(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> results,
            Project project,
            VersionStamp version,
            CancellationToken cancellationToken)
        {
            ImmutableHashSet<Document>.Builder? failedDocuments = null;
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder? lazyLoadDiagnostics = null;

            foreach (var document in project.Documents)
            {
                var loadDiagnostic = await document.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);
                if (loadDiagnostic != null)
                {
                    lazyLoadDiagnostics ??= ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    lazyLoadDiagnostics.Add(document.Id, [DiagnosticData.Create(loadDiagnostic, document)]);

                    failedDocuments ??= ImmutableHashSet.CreateBuilder<Document>();
                    failedDocuments.Add(document);
                }
            }

            results = results.SetItem(
                FileContentLoadAnalyzer.Instance,
                DiagnosticAnalysisResult.Create(
                    project,
                    version,
                    syntaxLocalMap: lazyLoadDiagnostics?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                    semanticLocalMap: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                    nonLocalMap: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                    others: [],
                    documentIds: null));

            var generatorDiagnostics = await _diagnosticAnalyzerRunner.GetSourceGeneratorDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            var diagnosticResultBuilder = new DiagnosticAnalysisResultBuilder(project, version);
            foreach (var generatorDiagnostic in generatorDiagnostics)
            {
                // We'll always treat generator diagnostics that are associated with a tree as a local diagnostic, because
                // we want that to be refreshed and deduplicated with regular document analysis.
                diagnosticResultBuilder.AddDiagnosticTreatedAsLocalSemantic(generatorDiagnostic);
            }

            results = results.SetItem(
                GeneratorDiagnosticsPlaceholderAnalyzer.Instance,
                DiagnosticAnalysisResult.CreateFromBuilder(diagnosticResultBuilder));

            return (results, failedDocuments?.ToImmutable());
        }

        private void UpdateAnalyzerTelemetryData(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetry)
        {
            foreach (var (analyzer, telemetryInfo) in telemetry)
            {
                var isTelemetryCollectionAllowed = DiagnosticAnalyzerInfoCache.IsTelemetryCollectionAllowed(analyzer);
                _telemetry.UpdateAnalyzerActionsTelemetry(analyzer, telemetryInfo, isTelemetryCollectionAllowed);
            }
        }
    }
}
