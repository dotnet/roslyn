// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// This is responsible for getting diagnostics for given input.
        /// It either return one from cache or calculate new one.
        /// </summary>
        private class Executor
        {
            private readonly DiagnosticIncrementalAnalyzer _owner;

            public Executor(DiagnosticIncrementalAnalyzer owner)
            {
                _owner = owner;
            }

            public IEnumerable<DiagnosticData> ConvertToLocalDiagnostics(Document targetDocument, IEnumerable<Diagnostic> diagnostics, TextSpan? span = null)
            {
                var project = targetDocument.Project;

                if (project.SupportsCompilation)
                {
                    return ConvertToLocalDiagnosticsWithCompilation(targetDocument, diagnostics, span);
                }

                return ConvertToLocalDiagnosticsWithoutCompilation(targetDocument, diagnostics, span);
            }

            /// <summary>
            /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) either from cache or by calculating them
            /// </summary>
            public async Task<DocumentAnalysisData> GetDocumentAnalysisDataAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Document document, StateSet stateSet, AnalysisKind kind, CancellationToken cancellationToken)
            {
                // get log title and functionId
                string title;
                FunctionId functionId;
                GetLogFunctionIdAndTitle(kind, out functionId, out title);

                using (Logger.LogBlock(functionId, GetDocumentLogMessage, title, document, stateSet.Analyzer, cancellationToken))
                {
                    try
                    {
                        var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                        var state = stateSet.GetActiveFileState(document.Id);
                        var existingData = state.GetAnalysisData(kind);

                        if (existingData.Version == version)
                        {
                            return existingData;
                        }

                        // perf optimization. check whether analyzer is suppressed and avoid getting diagnostics if suppressed.
                        // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                        if (_owner.Owner.IsAnalyzerSuppressed(stateSet.Analyzer, document.Project))
                        {
                            return new DocumentAnalysisData(version, existingData.Items, ImmutableArray<DiagnosticData>.Empty);
                        }

                        var nullFilterSpan = (TextSpan?)null;
                        var diagnostics = await ComputeDiagnosticsAsync(analyzerDriverOpt, document, stateSet.Analyzer, kind, nullFilterSpan, cancellationToken).ConfigureAwait(false);

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
            public async Task<ProjectAnalysisData> GetProjectAnalysisDataAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, bool ignoreFullAnalysisOptions, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, stateSets, cancellationToken))
                {
                    try
                    {
                        // PERF: we need to flip this to false when we do actual diffing.
                        var avoidLoadingData = true;
                        var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                        var existingData = await ProjectAnalysisData.CreateAsync(project, stateSets, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                        if (existingData.Version == version)
                        {
                            return existingData;
                        }

                        // perf optimization. check whether we want to analyze this project or not.
                        if (!await FullAnalysisEnabledAsync(project, ignoreFullAnalysisOptions, cancellationToken).ConfigureAwait(false))
                        {
                            return new ProjectAnalysisData(project.Id, VersionStamp.Default, existingData.Result, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
                        }

                        var result = await ComputeDiagnosticsAsync(analyzerDriverOpt, project, stateSets, existingData.Result, cancellationToken).ConfigureAwait(false);

                        return new ProjectAnalysisData(project.Id, version, existingData.Result, result);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            }

            /// <summary>
            /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) by calculating them
            /// </summary>
            public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? spanOpt, CancellationToken cancellationToken)
            {
                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    var diagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, kind, analyzerDriverOpt?.Compilation, cancellationToken).ConfigureAwait(false);
                    return ConvertToLocalDiagnostics(document, diagnostics);
                }

                var documentDiagnostics = await ComputeDiagnosticAnalyzerDiagnosticsAsync(analyzerDriverOpt, document, analyzer, kind, spanOpt, cancellationToken).ConfigureAwait(false);
                return ConvertToLocalDiagnostics(document, documentDiagnostics);
            }

            /// <summary>
            /// Return all diagnostics that belong to given project for the given StateSets (analyzers) by calculating them
            /// </summary>
            public async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
            {
                try
                {
                    var result = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                    // analyzerDriver can be null if given project doesn't support compilation.
                    if (analyzerDriverOpt != null)
                    {
                        // calculate regular diagnostic analyzers diagnostics
                        var compilerResult = await AnalyzeAsync(analyzerDriverOpt, project, cancellationToken).ConfigureAwait(false);
                        result = compilerResult.AnalysisResult;

                        // record telemetry data
                        UpdateAnalyzerTelemetryData(compilerResult, project, cancellationToken);
                    }

                    // check whether there is IDE specific project diagnostic analyzer
                    return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, stateSets, analyzerDriverOpt?.Compilation, result, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, CancellationToken cancellationToken)
            {
                try
                {
                    // PERF: check whether we can reduce number of analyzers we need to run.
                    //       this can happen since caller could have created the driver with different set of analyzers that are different
                    //       than what we used to create the cache.
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                    ImmutableArray<DiagnosticAnalyzer> analyzersToRun;
                    if (TryReduceAnalyzersToRun(analyzerDriverOpt, version, existing, out analyzersToRun))
                    {
                        // it looks like we can reduce the set. create new CompilationWithAnalyzer.
                        var analyzerDriverWithReducedSet = await _owner._compilationManager.CreateAnalyzerDriverAsync(
                            project, analyzersToRun, analyzerDriverOpt.AnalysisOptions.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                        var result = await ComputeDiagnosticsAsync(analyzerDriverWithReducedSet, project, stateSets, cancellationToken).ConfigureAwait(false);
                        return MergeExistingDiagnostics(version, existing, result);
                    }

                    // we couldn't reduce the set.
                    return await ComputeDiagnosticsAsync(analyzerDriverOpt, project, stateSets, cancellationToken).ConfigureAwait(false);
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

                foreach (var kv in existing)
                {
                    if (kv.Value.Version != version)
                    {
                        continue;
                    }

                    result = result.SetItem(kv.Key, kv.Value);
                }

                return result;
            }

            private bool TryReduceAnalyzersToRun(
                CompilationWithAnalyzers analyzerDriverOpt, VersionStamp version,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing,
                out ImmutableArray<DiagnosticAnalyzer> analyzers)
            {
                analyzers = default(ImmutableArray<DiagnosticAnalyzer>);

                // we don't have analyzer driver, nothing to reduce.
                if (analyzerDriverOpt == null)
                {
                    return false;
                }

                var existingAnalyzers = analyzerDriverOpt.Analyzers;
                var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
                foreach (var analyzer in existingAnalyzers)
                {
                    DiagnosticAnalysisResult analysisResult;
                    if (existing.TryGetValue(analyzer, out analysisResult) &&
                        analysisResult.Version == version)
                    {
                        // we already have up to date result.
                        continue;
                    }

                    // analyzer that is out of date.
                    builder.Add(analyzer);
                }

                // if this condition is true, it shouldn't be called.
                Contract.ThrowIfTrue(builder.Count == 0);

                // all of analyzers are out of date.
                if (builder.Count == existingAnalyzers.Length)
                {
                    return false;
                }

                analyzers = builder.ToImmutable();
                return true;
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
                Project project, IEnumerable<StateSet> stateSets, Compilation compilationOpt, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result, CancellationToken cancellationToken)
            {
                try
                {
                    // check whether there is IDE specific project diagnostic analyzer
                    var ideAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => a is ProjectDiagnosticAnalyzer || a is DocumentDiagnosticAnalyzer).ToImmutableArrayOrEmpty();
                    if (ideAnalyzers.Length <= 0)
                    {
                        return result;
                    }

                    // create result map
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                    var builder = new DiagnosticAnalysisResultBuilder(project, version);

                    foreach (var analyzer in ideAnalyzers)
                    {
                        var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                        if (documentAnalyzer != null)
                        {
                            foreach (var document in project.Documents)
                            {
                                if (document.SupportsSyntaxTree)
                                {
                                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                                    builder.AddSyntaxDiagnostics(tree, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Syntax, compilationOpt, cancellationToken).ConfigureAwait(false));
                                    builder.AddSemanticDiagnostics(tree, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Semantic, compilationOpt, cancellationToken).ConfigureAwait(false));
                                }
                                else
                                {
                                    builder.AddExternalSyntaxDiagnostics(document.Id, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Syntax, compilationOpt, cancellationToken).ConfigureAwait(false));
                                    builder.AddExternalSemanticDiagnostics(document.Id, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Semantic, compilationOpt, cancellationToken).ConfigureAwait(false));
                                }
                            }
                        }

                        var projectAnalyzer = analyzer as ProjectDiagnosticAnalyzer;
                        if (projectAnalyzer != null)
                        {
                            builder.AddCompilationDiagnostics(await ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(project, projectAnalyzer, compilationOpt, cancellationToken).ConfigureAwait(false));
                        }

                        // merge the result to existing one.
                        // there can be existing one from compiler driver with empty set. overwrite it with
                        // ide one.
                        result = result.SetItem(analyzer, new DiagnosticAnalysisResult(builder));
                    }

                    return result;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<IEnumerable<Diagnostic>> ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(
                Project project, ProjectDiagnosticAnalyzer analyzer, Compilation compilationOpt, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var diagnostics = await analyzer.AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);

                    // Apply filtering from compilation options (source suppressions, ruleset, etc.)
                    if (compilationOpt != null)
                    {
                        diagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt).ToImmutableArrayOrEmpty();
                    }

                    return diagnostics;
                }
                catch (Exception e) when (!IsCanceled(e, cancellationToken))
                {
                    OnAnalyzerException(analyzer, project.Id, compilationOpt, e);
                    return ImmutableArray<Diagnostic>.Empty;
                }
            }

            private async Task<IEnumerable<Diagnostic>> ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                Document document, DocumentDiagnosticAnalyzer analyzer, AnalysisKind kind, Compilation compilationOpt, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Task<ImmutableArray<Diagnostic>> analyzeAsync;

                    switch (kind)
                    {
                        case AnalysisKind.Syntax:
                            analyzeAsync = analyzer.AnalyzeSyntaxAsync(document, cancellationToken);
                            break;

                        case AnalysisKind.Semantic:
                            analyzeAsync = analyzer.AnalyzeSemanticsAsync(document, cancellationToken);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(kind);
                    }

                    var diagnostics = (await analyzeAsync.ConfigureAwait(false)).NullToEmpty();
                    if (compilationOpt != null)
                    {
                        return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt);
                    }

                    return diagnostics;
                }
                catch (Exception e) when (!IsCanceled(e, cancellationToken))
                {
                    OnAnalyzerException(analyzer, document.Project.Id, compilationOpt, e);
                    return ImmutableArray<Diagnostic>.Empty;
                }
            }

            private async Task<IEnumerable<Diagnostic>> ComputeDiagnosticAnalyzerDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? spanOpt, CancellationToken cancellationToken)
            {
                // quick optimization to reduce allocations.
                if (analyzerDriverOpt == null || !_owner.SupportAnalysisKind(analyzer, document.Project.Language, kind))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        var diagnostics = await analyzerDriverOpt.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false);

                        Contract.Requires(diagnostics.Count() == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation).Count());
                        return diagnostics.ToImmutableArrayOrEmpty();
                    case AnalysisKind.Semantic:
                        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        diagnostics = await analyzerDriverOpt.GetAnalyzerSemanticDiagnosticsAsync(model, spanOpt, oneAnalyzers, cancellationToken).ConfigureAwait(false);

                        Contract.Requires(diagnostics.Count() == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation).Count());
                        return diagnostics.ToImmutableArrayOrEmpty();
                    default:
                        return Contract.FailWithReturn<ImmutableArray<Diagnostic>>("shouldn't reach here");
                }
            }

            private void UpdateAnalyzerTelemetryData(
                DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult> analysisResults, Project project, CancellationToken cancellationToken)
            {
                foreach (var kv in analysisResults.TelemetryInfo)
                {
                    DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(kv.Key, kv.Value, project, _owner.DiagnosticLogAggregator);
                }
            }

            private static async Task<bool> FullAnalysisEnabledAsync(Project project, bool ignoreFullAnalysisOptions, CancellationToken cancellationToken)
            {
                if (ignoreFullAnalysisOptions)
                {
                    return await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(project) ||
                    !project.Solution.Options.GetOption(RuntimeOptions.FullSolutionAnalysis))
                {
                    return false;
                }

                return await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            }

            private static bool IsCanceled(Exception ex, CancellationToken cancellationToken)
            {
                return (ex as OperationCanceledException)?.CancellationToken == cancellationToken;
            }

            private void OnAnalyzerException(DiagnosticAnalyzer analyzer, ProjectId projectId, Compilation compilationOpt, Exception ex)
            {
                var exceptionDiagnostic = AnalyzerHelper.CreateAnalyzerExceptionDiagnostic(analyzer, ex);

                if (compilationOpt != null)
                {
                    exceptionDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(ImmutableArray.Create(exceptionDiagnostic), compilationOpt).SingleOrDefault();
                }

                var onAnalyzerException = _owner.GetOnAnalyzerException(projectId);
                onAnalyzerException(ex, analyzer, exceptionDiagnostic);
            }

            private IEnumerable<DiagnosticData> ConvertToLocalDiagnosticsWithoutCompilation(Document targetDocument, IEnumerable<Diagnostic> diagnostics, TextSpan? span = null)
            {
                var project = targetDocument.Project;
                Contract.ThrowIfTrue(project.SupportsCompilation);

                foreach (var diagnostic in diagnostics)
                {
                    var location = diagnostic.Location;
                    if (location.Kind != LocationKind.ExternalFile)
                    {
                        continue;
                    }

                    var lineSpan = location.GetLineSpan();

                    var documentIds = project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path);
                    if (documentIds.IsEmpty || documentIds.All(id => id != targetDocument.Id))
                    {
                        continue;
                    }

                    yield return DiagnosticData.Create(targetDocument, diagnostic);
                }
            }

            private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(
                CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
            {
                // quick bail out
                if (analyzerDriver.Analyzers.Length == 0)
                {
                    return DiagnosticAnalysisResultMap.Create(
                        ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty,
                        ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
                }

                var executor = project.Solution.Workspace.Services.GetService<ICodeAnalysisDiagnosticAnalyzerExecutor>();
                return await executor.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
            }

            private IEnumerable<DiagnosticData> ConvertToLocalDiagnosticsWithCompilation(Document targetDocument, IEnumerable<Diagnostic> diagnostics, TextSpan? span = null)
            {
                var project = targetDocument.Project;
                Contract.ThrowIfFalse(project.SupportsCompilation);

                foreach (var diagnostic in diagnostics)
                {
                    var document = project.GetDocument(diagnostic.Location.SourceTree);
                    if (document == null || document != targetDocument)
                    {
                        continue;
                    }

                    if (span.HasValue && !span.Value.Contains(diagnostic.Location.SourceSpan))
                    {
                        continue;
                    }

                    yield return DiagnosticData.Create(document, diagnostic);
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
                        functionId = FunctionId.Diagnostics_ProjectDiagnostic;
                        title = "nonLocal";
                        Contract.Fail("shouldn't reach here");
                        break;
                }
            }
        }
    }
}
