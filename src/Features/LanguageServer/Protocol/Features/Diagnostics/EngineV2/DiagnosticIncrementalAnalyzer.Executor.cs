﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Return all cached local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer).
        /// Otherwise, return <code>null</code>.
        /// For the latter case, <paramref name="isAnalyzerSuppressed"/> indicates if the analyzer is suppressed
        /// for the given document/project. If suppressed, the caller does not need to compute the diagnostics for the given
        /// analyzer. Otherwise, diagnostics need to be computed.
        /// </summary>
        private (ActiveFileState, DocumentAnalysisData?) TryGetCachedDocumentAnalysisData(
            TextDocument document, StateSet stateSet,
            AnalysisKind kind, VersionStamp version,
            BackgroundAnalysisScope analysisScope,
            CompilerDiagnosticsScope compilerDiagnosticsScope,
            bool isActiveDocument, bool isVisibleDocument,
            bool isOpenDocument, bool isGeneratedRazorDocument,
            CancellationToken cancellationToken,
            out bool isAnalyzerSuppressed)
        {
            Debug.Assert(isActiveDocument || isOpenDocument || isGeneratedRazorDocument);

            isAnalyzerSuppressed = false;

            try
            {
                var state = stateSet.GetOrCreateActiveFileState(document.Id);
                var existingData = state.GetAnalysisData(kind);

                if (existingData.Version == version)
                {
                    return (state, existingData);
                }

                // Check whether analyzer is suppressed for project or document.
                // If so, we set the flag indicating that the client can skip analysis for this document.
                // Regardless of whether or not the analyzer is suppressed for project or document,
                // we return null to indicate that no diagnostics are cached for this document for the given version.
                isAnalyzerSuppressed = !DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(stateSet.Analyzer, document.Project, GlobalOptions) ||
                    !IsAnalyzerEnabledForDocument(stateSet.Analyzer, existingData, analysisScope, compilerDiagnosticsScope,
                        isActiveDocument, isVisibleDocument, isOpenDocument, isGeneratedRazorDocument);
                return (state, null);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            static bool IsAnalyzerEnabledForDocument(
                DiagnosticAnalyzer analyzer,
                DocumentAnalysisData previousData,
                BackgroundAnalysisScope analysisScope,
                CompilerDiagnosticsScope compilerDiagnosticsScope,
                bool isActiveDocument,
                bool isVisibleDocument,
                bool isOpenDocument,
                bool isGeneratedRazorDocument)
            {
                Debug.Assert(!isActiveDocument || isOpenDocument || isGeneratedRazorDocument);

                if (isGeneratedRazorDocument)
                {
                    // This is a generated Razor document, and they always want all analyzer diagnostics.
                    return true;
                }

                if (analyzer.IsCompilerAnalyzer())
                {
                    return compilerDiagnosticsScope switch
                    {
                        // Compiler diagnostics are disabled for all documents.
                        CompilerDiagnosticsScope.None => false,

                        // Compiler diagnostics are enabled for visible documents and open documents which had errors/warnings in prior snapshot.
                        CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics => IsVisibleDocumentOrOpenDocumentWithPriorReportedVisibleDiagnostics(isVisibleDocument, isOpenDocument, previousData),

                        // Compiler diagnostics are enabled for all open documents.
                        CompilerDiagnosticsScope.OpenFiles => isOpenDocument,

                        // Compiler diagnostics are enabled for all documents.
                        CompilerDiagnosticsScope.FullSolution => true,

                        _ => throw ExceptionUtilities.UnexpectedValue(analysisScope)
                    };
                }
                else
                {
                    return analysisScope switch
                    {
                        // Analyzers are disabled for all documents.
                        BackgroundAnalysisScope.None => false,

                        // Analyzers are enabled for visible documents and open documents which had errors/warnings in prior snapshot.
                        BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics => IsVisibleDocumentOrOpenDocumentWithPriorReportedVisibleDiagnostics(isVisibleDocument, isOpenDocument, previousData),

                        // Analyzers are enabled for all open documents.
                        BackgroundAnalysisScope.OpenFiles => isOpenDocument,

                        // Analyzers are enabled for all documents.
                        BackgroundAnalysisScope.FullSolution => true,

                        _ => throw ExceptionUtilities.UnexpectedValue(analysisScope)
                    };
                }
            }

            static bool IsVisibleDocumentOrOpenDocumentWithPriorReportedVisibleDiagnostics(
                bool isVisibleDocument,
                bool isOpenDocument,
                DocumentAnalysisData previousData)
            {
                if (isVisibleDocument)
                    return true;

                return isOpenDocument
                    && previousData.Items.Any(static d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning or DiagnosticSeverity.Info);
            }
        }

        /// <summary>
        /// Computes all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer).
        /// </summary>
        private static async Task<DocumentAnalysisData> ComputeDocumentAnalysisDataAsync(
            DocumentAnalysisExecutor executor, DiagnosticAnalyzer analyzer, ActiveFileState state, bool logTelemetry, CancellationToken cancellationToken)
        {
            var kind = executor.AnalysisScope.Kind;
            var document = executor.AnalysisScope.TextDocument;

            // get log title and functionId
            GetLogFunctionIdAndTitle(kind, out var functionId, out var title);

            var logLevel = logTelemetry ? LogLevel.Information : LogLevel.Trace;
            using (Logger.LogBlock(functionId, GetDocumentLogMessage, title, document, analyzer, cancellationToken, logLevel: logLevel))
            {
                try
                {
                    var diagnostics = await executor.ComputeDiagnosticsAsync(analyzer, cancellationToken).ConfigureAwait(false);

                    // this is no-op in product. only run in test environment
                    Logger.Log(functionId, (t, d, a, ds) => $"{GetDocumentLogMessage(t, d, a)}, {string.Join(Environment.NewLine, ds)}",
                        title, document, analyzer, diagnostics);

                    var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                    var existingData = state.GetAnalysisData(kind);
                    var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                    // we only care about local diagnostics
                    return new DocumentAnalysisData(version, text.Lines.Count, existingData.Items, diagnostics.ToImmutableArrayOrEmpty());
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }

        /// <summary>
        /// Return all diagnostics that belong to given project for the given StateSets (analyzers) either from cache or by calculating them
        /// </summary>
        private async Task<ProjectAnalysisData> GetProjectAnalysisDataAsync(
            CompilationWithAnalyzers? compilationWithAnalyzers, Project project, IdeAnalyzerOptions ideOptions, ImmutableArray<StateSet> stateSets, bool forceAnalyzerRun, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, stateSets, cancellationToken))
            {
                try
                {
                    // PERF: We need to flip this to false when we do actual diffing.
                    var avoidLoadingData = true;
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                    var existingData = await ProjectAnalysisData.CreateAsync(project, stateSets, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                    // We can't return here if we have open file only analyzers since saved data for open file only analyzer
                    // is incomplete -- it only contains info on open files rather than whole project.
                    if (existingData.Version == version && !CompilationHasOpenFileOnlyAnalyzers(compilationWithAnalyzers, ideOptions.CleanupOptions?.SimplifierOptions))
                    {
                        return existingData;
                    }

                    // PERF: Check whether we want to analyze this project or not.
                    var fullAnalysisEnabled = GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullAnalysisEnabled, out var analyzersFullAnalysisEnabled);
                    if (forceAnalyzerRun)
                    {
                        // We are forcing full solution analysis for all diagnostics.
                        fullAnalysisEnabled = true;
                        compilerFullAnalysisEnabled = true;
                        analyzersFullAnalysisEnabled = true;
                    }

                    if (!fullAnalysisEnabled)
                    {
                        Logger.Log(FunctionId.Diagnostics_ProjectDiagnostic, p => $"FSA off ({p.FilePath ?? p.Name})", project);

                        // If we are producing document diagnostics for some other document in this project, we still want to show
                        // certain project-level diagnostics that would cause file-level diagnostics to be broken. We will only do this though if
                        // some file that's open is depending on this project though -- that way we're going to only be analyzing projects
                        // that have already had compilations produced for.
                        var shouldProduceOutput = false;

                        var projectDependencyGraph = project.Solution.GetProjectDependencyGraph();

                        foreach (var openDocumentId in project.Solution.Workspace.GetOpenDocumentIds())
                        {
                            if (openDocumentId.ProjectId == project.Id || projectDependencyGraph.DoesProjectTransitivelyDependOnProject(openDocumentId.ProjectId, project.Id))
                            {
                                shouldProduceOutput = true;
                                break;
                            }
                        }

                        var results = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                        if (shouldProduceOutput)
                        {
                            (results, _) = await UpdateWithDocumentLoadAndGeneratorFailuresAsync(
                                results,
                                project,
                                version,
                                cancellationToken).ConfigureAwait(false);
                        }

                        return new ProjectAnalysisData(project.Id, VersionStamp.Default, existingData.Result, results);
                    }

                    // Reduce the state sets to analyze based on individual full solution analysis values
                    // for compiler diagnostics and analyzers.
                    if (!compilerFullAnalysisEnabled)
                    {
                        Debug.Assert(analyzersFullAnalysisEnabled);
                        stateSets = stateSets.WhereAsArray(s => !s.Analyzer.IsCompilerAnalyzer());
                    }
                    else if (!analyzersFullAnalysisEnabled)
                    {
                        stateSets = stateSets.WhereAsArray(s => s.Analyzer.IsCompilerAnalyzer() || s.Analyzer.IsWorkspaceDiagnosticAnalyzer());
                    }

                    var result = await ComputeDiagnosticsAsync(compilationWithAnalyzers, project, ideOptions, stateSets, forceAnalyzerRun, existingData.Result, cancellationToken).ConfigureAwait(false);

                    // If project is not loaded successfully, get rid of any semantic errors from compiler analyzer.
                    // Note: In the past when project was not loaded successfully we did not run any analyzers on the project.
                    // Now we run analyzers but filter out some information. So on such projects, there will be some perf degradation.
                    result = await RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(result, project, cancellationToken).ConfigureAwait(false);

                    return new ProjectAnalysisData(project.Id, version, existingData.Result, result);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }

        private static bool CompilationHasOpenFileOnlyAnalyzers(CompilationWithAnalyzers? compilationWithAnalyzers, SimplifierOptions? options)
        {
            if (compilationWithAnalyzers == null)
            {
                return false;
            }

            foreach (var analyzer in compilationWithAnalyzers.Analyzers)
            {
                if (analyzer.IsOpenFileOnly(options))
                {
                    return true;
                }
            }

            return false;
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

            var compilerAnalyzer = project.Solution.State.Analyzers.GetCompilerDiagnosticAnalyzer(project.Language);
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
            CompilationWithAnalyzers? compilationWithAnalyzers, Project project, ImmutableArray<DiagnosticAnalyzer> ideAnalyzers, bool forcedAnalysis, CancellationToken cancellationToken)
        {
            try
            {
                var result = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                // can be null if given project doesn't support compilation.
                if (compilationWithAnalyzers?.Analyzers.Length > 0)
                {
                    // calculate regular diagnostic analyzers diagnostics
                    var resultMap = await _diagnosticAnalyzerRunner.AnalyzeProjectAsync(project, compilationWithAnalyzers,
                        forcedAnalysis, logPerformanceInfo: false, getTelemetryInfo: true, cancellationToken).ConfigureAwait(false);

                    result = resultMap.AnalysisResult;

                    // record telemetry data
                    UpdateAnalyzerTelemetryData(resultMap.TelemetryInfo);
                }

                // check whether there is IDE specific project diagnostic analyzer
                return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, ideAnalyzers, compilationWithAnalyzers?.Compilation, result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
            CompilationWithAnalyzers? compilationWithAnalyzers, Project project, IdeAnalyzerOptions ideOptions, ImmutableArray<StateSet> stateSets, bool forcedAnalysis,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, CancellationToken cancellationToken)
        {
            try
            {
                // PERF: check whether we can reduce number of analyzers we need to run.
                //       this can happen since caller could have created the driver with different set of analyzers that are different
                //       than what we used to create the cache.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                var ideAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => a is ProjectDiagnosticAnalyzer or DocumentDiagnosticAnalyzer).ToImmutableArrayOrEmpty();

                if (compilationWithAnalyzers != null && TryReduceAnalyzersToRun(compilationWithAnalyzers, version, existing, ideOptions, out var analyzersToRun))
                {
                    // it looks like we can reduce the set. create new CompilationWithAnalyzer.
                    // if we reduced to 0, we just pass in null for analyzer drvier. it could be reduced to 0
                    // since we might have up to date results for analyzers from compiler but not for 
                    // workspace analyzers.

                    var compilationWithReducedAnalyzers = (analyzersToRun.Length == 0) ? null :
                        await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(
                            project,
                            ideOptions,
                            analyzersToRun,
                            compilationWithAnalyzers.AnalysisOptions.ReportSuppressedDiagnostics,
                            cancellationToken).ConfigureAwait(false);

                    var result = await ComputeDiagnosticsAsync(compilationWithReducedAnalyzers, project, ideAnalyzers, forcedAnalysis, cancellationToken).ConfigureAwait(false);
                    return MergeExistingDiagnostics(version, existing, result);
                }

                // we couldn't reduce the set.
                return await ComputeDiagnosticsAsync(compilationWithAnalyzers, project, ideAnalyzers, forcedAnalysis, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> MergeExistingDiagnostics(
            VersionStamp version, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            // quick bail out.
            if (existing.IsEmpty)
            {
                return result;
            }

            foreach (var (analyzer, results) in existing)
            {
                if (results.Version != version)
                {
                    continue;
                }

                result = result.SetItem(analyzer, results);
            }

            return result;
        }

        private static bool TryReduceAnalyzersToRun(
            CompilationWithAnalyzers compilationWithAnalyzers, VersionStamp version,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, IdeAnalyzerOptions ideOptions,
            out ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            analyzers = default;

            var existingAnalyzers = compilationWithAnalyzers.Analyzers;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzer in existingAnalyzers)
            {
                if (existing.TryGetValue(analyzer, out var analysisResult) &&
                    analysisResult.Version == version &&
                    !analyzer.IsOpenFileOnly(ideOptions.CleanupOptions?.SimplifierOptions))
                {
                    // we already have up to date result.
                    continue;
                }

                // analyzer that is out of date.
                // open file only analyzer is always out of date for project wide data
                builder.Add(analyzer);
            }

            // all of analyzers are out of date.
            if (builder.Count == existingAnalyzers.Length)
            {
                return false;
            }

            analyzers = builder.ToImmutable();
            return true;
        }

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
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

        private static async Task<(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> results, ImmutableHashSet<Document>? failedDocuments)> UpdateWithDocumentLoadAndGeneratorFailuresAsync(
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
                    lazyLoadDiagnostics.Add(document.Id, ImmutableArray.Create(DiagnosticData.Create(loadDiagnostic, document)));

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
                    others: ImmutableArray<DiagnosticData>.Empty,
                    documentIds: null));

            var generatorDiagnostics = await InProcOrRemoteHostAnalyzerRunner.GetSourceGeneratorDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
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

        private static void GetLogFunctionIdAndTitle(AnalysisKind kind, out FunctionId functionId, out string title)
        {
            switch (kind)
            {
                case AnalysisKind.Syntax:
                    functionId = FunctionId.Diagnostics_SyntaxDiagnostic;
                    title = "syntax";
                    break;
                case AnalysisKind.Semantic:
                    functionId = FunctionId.Diagnostics_SemanticDiagnostic;
                    title = "semantic";
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
