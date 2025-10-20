// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// <summary>
    /// Return all diagnostics that belong to given project for the given <see cref="DiagnosticAnalyzer"/> either
    /// from cache or by calculating them.
    /// </summary>
    private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticAnalysisResultsInProcessAsync(
        CompilationWithAnalyzers? compilationWithAnalyzers,
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

        static string GetProjectLogMessage(Project project, ImmutableArray<DocumentDiagnosticAnalyzer> analyzers)
            => $"project: ({project.Id}), ({string.Join(Environment.NewLine, analyzers.Select(a => a.ToString()))})";

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
                if (compilationWithAnalyzers?.Analyzers.Length > 0)
                {
                    // calculate regular diagnostic analyzers diagnostics
                    var resultMap = await this.AnalyzeInProcessAsync(
                        documentAnalysisScope: null, project, compilationWithAnalyzers, logPerformanceInfo: false, getTelemetryInfo: true, cancellationToken).ConfigureAwait(false);

                    result = resultMap.AnalysisResult;

                    // record telemetry data
                    UpdateAnalyzerTelemetryData(resultMap.TelemetryInfo);
                }

                // check whether there is IDE specific project diagnostic analyzer
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
                var compilation = compilationWithAnalyzers?.Compilation;

                foreach (var documentAnalyzer in ideAnalyzers)
                {
                    var builder = new DiagnosticAnalysisResultBuilder(project);

                    foreach (var textDocument in project.AdditionalDocuments.Concat(project.Documents))
                    {
                        var tree = textDocument is Document document
                            ? await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false)
                            : null;
                        var syntaxDiagnostics = await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, textDocument, AnalysisKind.Syntax, compilation, tree, cancellationToken).ConfigureAwait(false);
                        var semanticDiagnostics = await DocumentAnalysisExecutor.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, textDocument, AnalysisKind.Semantic, compilation, tree, cancellationToken).ConfigureAwait(false);

                        if (tree != null)
                        {
                            builder.AddSyntaxDiagnostics(tree, syntaxDiagnostics);
                            builder.AddSemanticDiagnostics(tree, semanticDiagnostics);
                        }
                        else
                        {
                            builder.AddExternalSyntaxDiagnostics(textDocument.Id, syntaxDiagnostics);
                            builder.AddExternalSemanticDiagnostics(textDocument.Id, semanticDiagnostics);
                        }
                    }

                    // merge the result to existing one.
                    // there can be existing one from compiler driver with empty set. overwrite it with
                    // ide one.
                    result = result.SetItem(documentAnalyzer, DiagnosticAnalysisResult.CreateFromBuilder(builder));
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        void UpdateAnalyzerTelemetryData(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetry)
        {
            foreach (var (analyzer, telemetryInfo) in telemetry)
            {
                var isTelemetryCollectionAllowed = _analyzerInfoCache.IsTelemetryCollectionAllowed(analyzer);
                _telemetry.UpdateAnalyzerActionsTelemetry(analyzer, telemetryInfo, isTelemetryCollectionAllowed);
            }
        }
    }
}
