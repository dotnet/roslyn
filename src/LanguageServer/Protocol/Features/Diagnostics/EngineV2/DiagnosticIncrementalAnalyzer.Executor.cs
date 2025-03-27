﻿// Licensed to the .NET Foundation under one or more agreements.
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

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Return all diagnostics that belong to given project for the given <see cref="DiagnosticAnalyzer"/> either
        /// from cache or by calculating them.
        /// </summary>
        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticAnalysisResultsAsync(
            CompilationWithAnalyzersPair? compilationWithAnalyzers,
            Project project,
            ImmutableArray<DocumentDiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, analyzers, cancellationToken))
            {
                try
                {
                    var result = await ComputeDiagnosticsForAnalyzersAsync(analyzers).ConfigureAwait(false);

                    // If project is not loaded successfully, get rid of any semantic errors from compiler analyzer.
                    // Note: In the past when project was not loaded successfully we did not run any analyzers on the project.
                    // Now we run analyzers but filter out some information. So on such projects, there will be some perf degradation.
                    result = await RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(result).ConfigureAwait(false);

                    return result;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
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

            // <summary>
            // Calculate all diagnostics for a given project using analyzers referenced by the project and specified IDE analyzers.
            // </summary>
            async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsForAnalyzersAsync(
                ImmutableArray<DocumentDiagnosticAnalyzer> ideAnalyzers)
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

                    return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(ideAnalyzers, result).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
                ImmutableArray<DocumentDiagnosticAnalyzer> ideAnalyzers,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
            {
                try
                {
                    var compilation = compilationWithAnalyzers?.HostCompilation;

                    result = await UpdateWithGeneratorFailuresAsync(result).ConfigureAwait(false);

                    foreach (var analyzer in ideAnalyzers)
                    {
                        var builder = new DiagnosticAnalysisResultBuilder(project);

                        foreach (var document in project.Documents)
                        {
                            // don't analyze documents whose content failed to load
                            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                            if (tree != null)
                            {
                                builder.AddSyntaxDiagnostics(tree, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(analyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                builder.AddSemanticDiagnostics(tree, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(analyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                            }
                            else
                            {
                                builder.AddExternalSyntaxDiagnostics(document.Id, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(analyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                builder.AddExternalSemanticDiagnostics(document.Id, await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(analyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                            }
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

            async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> UpdateWithGeneratorFailuresAsync(
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> results)
            {
                var generatorDiagnostics = await _diagnosticAnalyzerRunner.GetSourceGeneratorDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
                var diagnosticResultBuilder = new DiagnosticAnalysisResultBuilder(project);
                foreach (var generatorDiagnostic in generatorDiagnostics)
                {
                    // We'll always treat generator diagnostics that are associated with a tree as a local diagnostic, because
                    // we want that to be refreshed and deduplicated with regular document analysis.
                    diagnosticResultBuilder.AddDiagnosticTreatedAsLocalSemantic(generatorDiagnostic);
                }

                results = results.SetItem(
                    GeneratorDiagnosticsPlaceholderAnalyzer.Instance,
                    DiagnosticAnalysisResult.CreateFromBuilder(diagnosticResultBuilder));

                return results;
            }

            void UpdateAnalyzerTelemetryData(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetry)
            {
                foreach (var (analyzer, telemetryInfo) in telemetry)
                {
                    var isTelemetryCollectionAllowed = DiagnosticAnalyzerInfoCache.IsTelemetryCollectionAllowed(analyzer);
                    _telemetry.UpdateAnalyzerActionsTelemetry(analyzer, telemetryInfo, isTelemetryCollectionAllowed);
                }
            }
        }
    }
}
