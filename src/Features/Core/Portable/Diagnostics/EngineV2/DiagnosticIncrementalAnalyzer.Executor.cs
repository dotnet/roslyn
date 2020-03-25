﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) either from cache or by calculating them
        /// </summary>
        private async Task<DocumentAnalysisData> GetDocumentAnalysisDataAsync(
            CompilationWithAnalyzers? compilation, Document document, StateSet stateSet, AnalysisKind kind, CancellationToken cancellationToken)
        {
            // get log title and functionId
            GetLogFunctionIdAndTitle(kind, out var functionId, out var title);

            using (Logger.LogBlock(functionId, GetDocumentLogMessage, title, document, stateSet.Analyzer, cancellationToken))
            {
                try
                {
                    var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                    var state = stateSet.GetOrCreateActiveFileState(document.Id);
                    var existingData = state.GetAnalysisData(kind);

                    if (existingData.Version == version)
                    {
                        return existingData;
                    }

                    // perf optimization. check whether analyzer is suppressed and avoid getting diagnostics if suppressed.
                    if (DiagnosticAnalyzerInfoCache.IsAnalyzerSuppressed(stateSet.Analyzer, document.Project))
                    {
                        return new DocumentAnalysisData(version, existingData.Items, ImmutableArray<DiagnosticData>.Empty);
                    }

                    var diagnostics = await AnalyzerHelper.ComputeDiagnosticsAsync(stateSet.Analyzer, document, kind, compilation, GetOrCreateSkippedAnalyzersInfo, span: null, cancellationToken).ConfigureAwait(false);

                    // this is no-op in product. only run in test environment
                    Logger.Log(functionId, (t, d, a, ds) => $"{GetDocumentLogMessage(t, d, a)}, {string.Join(Environment.NewLine, ds)}",
                        title, document, stateSet.Analyzer, diagnostics);

                    // we only care about local diagnostics
                    return new DocumentAnalysisData(version, existingData.Items, diagnostics.ToImmutableArrayOrEmpty());
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        /// <summary>
        /// Return all diagnostics that belong to given project for the given StateSets (analyzers) either from cache or by calculating them
        /// </summary>
        private async Task<ProjectAnalysisData> GetProjectAnalysisDataAsync(
            CompilationWithAnalyzers? compilation, Project project, IEnumerable<StateSet> stateSets, bool forceAnalyzerRun, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, stateSets, cancellationToken))
            {
                try
                {
                    // PERF: We need to flip this to false when we do actual diffing.
                    var avoidLoadingData = true;
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                    var existingData = await ProjectAnalysisData.CreateAsync(PersistentStorageService, project, stateSets, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                    // We can't return here if we have open file only analyzers since saved data for open file only analyzer
                    // is incomplete -- it only contains info on open files rather than whole project.
                    if (existingData.Version == version && !CompilationHasOpenFileOnlyAnalyzers(compilation, project.Solution.Options))
                    {
                        return existingData;
                    }

                    // PERF: Check whether we want to analyze this project or not.
                    if (!FullAnalysisEnabled(project, forceAnalyzerRun))
                    {
                        Logger.Log(FunctionId.Diagnostics_ProjectDiagnostic, p => $"FSA off ({p.FilePath ?? p.Name})", project);

                        return new ProjectAnalysisData(project.Id, VersionStamp.Default, existingData.Result, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
                    }

                    var result = await ComputeDiagnosticsAsync(compilation, project, stateSets, forceAnalyzerRun, existingData.Result, cancellationToken).ConfigureAwait(false);

                    // If project is not loaded successfully, get rid of any semantic errors from compiler analyzer.
                    // Note: In the past when project was not loaded successfully we did not run any analyzers on the project.
                    // Now we run analyzers but filter out some information. So on such projects, there will be some perf degradation.
                    result = await RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(result, project, cancellationToken).ConfigureAwait(false);

                    return new ProjectAnalysisData(project.Id, version, existingData.Result, result);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static bool CompilationHasOpenFileOnlyAnalyzers(CompilationWithAnalyzers? compilation, OptionSet options)
        {
            if (compilation == null)
            {
                return false;
            }

            foreach (var analyzer in compilation.Analyzers)
            {
                if (analyzer.IsOpenFileOnly(options))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> RemoveCompilerSemanticErrorsIfProjectNotLoadedAsync(
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result, Project project, CancellationToken cancellationToken)
        {
            // see whether solution is loaded successfully
            var projectLoadedSuccessfully = await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (projectLoadedSuccessfully)
            {
                return result;
            }

            var compilerAnalyzer = HostAnalyzers.GetCompilerDiagnosticAnalyzer(project.Language);
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
            CompilationWithAnalyzers? compilation, Project project, ImmutableArray<DiagnosticAnalyzer> ideAnalyzers, bool forcedAnalysis, CancellationToken cancellationToken)
        {
            try
            {
                var result = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                // can be null if given project doesn't support compilation.
                if (compilation != null && compilation.Analyzers.Length != 0)
                {
                    // calculate regular diagnostic analyzers diagnostics
                    var resultMap = await _diagnosticAnalyzerRunner.AnalyzeAsync(compilation, project, GetOrCreateSkippedAnalyzersInfo, forcedAnalysis, cancellationToken).ConfigureAwait(false);

                    result = resultMap.AnalysisResult;

                    // record telemetry data
                    UpdateAnalyzerTelemetryData(resultMap.TelemetryInfo);
                }

                // check whether there is IDE specific project diagnostic analyzer
                return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, ideAnalyzers, compilation?.Compilation, result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private ISkippedAnalyzersInfo GetOrCreateSkippedAnalyzersInfo(Project project)
            => DiagnosticAnalyzerInfoCache.GetOrCreateSkippedAnalyzersInfo(project, HostAnalyzers);

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
            CompilationWithAnalyzers? compilation, Project project, IEnumerable<StateSet> stateSets, bool forcedAnalysis,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, CancellationToken cancellationToken)
        {
            try
            {
                // PERF: check whether we can reduce number of analyzers we need to run.
                //       this can happen since caller could have created the driver with different set of analyzers that are different
                //       than what we used to create the cache.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

                var ideAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => a is ProjectDiagnosticAnalyzer || a is DocumentDiagnosticAnalyzer).ToImmutableArrayOrEmpty();

                if (compilation != null && TryReduceAnalyzersToRun(compilation, project, version, existing, out var analyzersToRun))
                {
                    // it looks like we can reduce the set. create new CompilationWithAnalyzer.
                    // if we reduced to 0, we just pass in null for analyzer drvier. it could be reduced to 0
                    // since we might have up to date results for analyzers from compiler but not for 
                    // workspace analyzers.
                    var compilationWithReducedAnalyzers = (analyzersToRun.Length == 0) ? null :
                        await AnalyzerHelper.CreateCompilationWithAnalyzersAsync(project, analyzersToRun, compilation.AnalysisOptions.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                    var result = await ComputeDiagnosticsAsync(compilationWithReducedAnalyzers, project, ideAnalyzers, forcedAnalysis, cancellationToken).ConfigureAwait(false);
                    return MergeExistingDiagnostics(version, existing, result);
                }

                // we couldn't reduce the set.
                return await ComputeDiagnosticsAsync(compilation, project, ideAnalyzers, forcedAnalysis, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> MergeExistingDiagnostics(
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

        private bool TryReduceAnalyzersToRun(
            CompilationWithAnalyzers compilation, Project project, VersionStamp version,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing,
            out ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            analyzers = default;

            var options = project.Solution.Options;

            var existingAnalyzers = compilation.Analyzers;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzer in existingAnalyzers)
            {
                if (existing.TryGetValue(analyzer, out var analysisResult) &&
                    analysisResult.Version == version &&
                    !analyzer.IsOpenFileOnly(options))
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

                var (fileLoadAnalysisResult, failedDocuments) = await GetDocumentLoadFailuresAsync(project, version, cancellationToken).ConfigureAwait(false);
                result = result.SetItem(FileContentLoadAnalyzer.Instance, fileLoadAnalysisResult);

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
                                        builder.AddSyntaxDiagnostics(tree, await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                        builder.AddSemanticDiagnostics(tree, await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                                    }
                                    else
                                    {
                                        builder.AddExternalSyntaxDiagnostics(document.Id, await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Syntax, compilation, cancellationToken).ConfigureAwait(false));
                                        builder.AddExternalSemanticDiagnostics(document.Id, await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(documentAnalyzer, document, AnalysisKind.Semantic, compilation, cancellationToken).ConfigureAwait(false));
                                    }
                                }
                            }

                            break;

                        case ProjectDiagnosticAnalyzer projectAnalyzer:
                            builder.AddCompilationDiagnostics(await AnalyzerHelper.ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(projectAnalyzer, project, compilation, cancellationToken).ConfigureAwait(false));
                            break;
                    }

                    // merge the result to existing one.
                    // there can be existing one from compiler driver with empty set. overwrite it with
                    // ide one.
                    result = result.SetItem(analyzer, DiagnosticAnalysisResult.CreateFromBuilder(builder));
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<(DiagnosticAnalysisResult loadDiagnostics, ImmutableHashSet<Document>? failedDocuments)> GetDocumentLoadFailuresAsync(Project project, VersionStamp version, CancellationToken cancellationToken)
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

            var result = DiagnosticAnalysisResult.Create(
                project,
                version,
                syntaxLocalMap: lazyLoadDiagnostics?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                semanticLocalMap: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                nonLocalMap: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                others: ImmutableArray<DiagnosticData>.Empty,
                documentIds: null);

            return (result, failedDocuments?.ToImmutable());
        }

        private void UpdateAnalyzerTelemetryData(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> telemetry)
        {
            foreach (var (analyzer, telemetryInfo) in telemetry)
            {
                bool isTelemetryCollectionAllowed = DiagnosticAnalyzerInfoCache.IsTelemetryCollectionAllowed(analyzer);
                _telemetry.UpdateAnalyzerActionsTelemetry(analyzer, telemetryInfo, isTelemetryCollectionAllowed);
            }
        }

        internal static bool FullAnalysisEnabled(Project project, bool forceAnalyzerRun)
        {
            if (forceAnalyzerRun)
            {
                // asked to ignore any checks.
                return true;
            }

            return SolutionCrawlerOptions.GetBackgroundAnalysisScope(project) == BackgroundAnalysisScope.FullSolution;
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
