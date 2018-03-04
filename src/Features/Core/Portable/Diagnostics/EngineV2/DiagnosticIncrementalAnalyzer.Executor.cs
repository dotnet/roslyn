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
                GetLogFunctionIdAndTitle(kind, out var functionId, out var title);

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
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, bool forceAnalyzerRun, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, project, stateSets, cancellationToken))
                {
                    try
                    {
                        // PERF: we need to flip this to false when we do actual diffing.
                        var avoidLoadingData = true;
                        var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                        var existingData = await ProjectAnalysisData.CreateAsync(project, stateSets, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                        // we can't return here if we have open file only analyzers sine saved data for open file only analyzer
                        // is wrong. (since it only contains info on open files rather than whole project)
                        if (existingData.Version == version && !analyzerDriverOpt.ContainsOpenFileOnlyAnalyzers(project.Solution.Workspace))
                        {
                            return existingData;
                        }

                        // perf optimization. check whether we want to analyze this project or not.
                        if (!FullAnalysisEnabled(project, forceAnalyzerRun))
                        {
                            Logger.Log(FunctionId.Diagnostics_ProjectDiagnostic, p => $"FSA off ({p.FilePath ?? p.Name})", project);

                            return new ProjectAnalysisData(project.Id, VersionStamp.Default, existingData.Result, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
                        }

                        var result = await ComputeDiagnosticsAsync(analyzerDriverOpt, project, stateSets, forceAnalyzerRun, existingData.Result, cancellationToken).ConfigureAwait(false);

                        // if project is not loaded successfully, get rid of any semantic errors from compiler analyzer
                        // * NOTE * previously when project is not loaded successfully, we actually dropped doing anything on the project, but now
                        //          we do everything but filter out some information. so on such projects, there will be some perf degradation.
                        result = await FilterOutCompilerSemanticErrorsIfNeccessaryAsync(project, result, cancellationToken).ConfigureAwait(false);

                        return new ProjectAnalysisData(project.Id, version, existingData.Result, result);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> FilterOutCompilerSemanticErrorsIfNeccessaryAsync(
                Project project, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result, CancellationToken cancellationToken)
            {
                // see whether solution is loaded successfully
                var projectLoadedSuccessfully = await project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);
                if (projectLoadedSuccessfully)
                {
                    return result;
                }

                var compilerAnalyzer = _owner.HostAnalyzerManager.GetCompilerDiagnosticAnalyzer(project.Language);
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
                var newCompilerAnalysisResult = new DiagnosticAnalysisResult(
                    analysisResult.ProjectId,
                    analysisResult.Version,
                    analysisResult.SyntaxLocals,
                    semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                    nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                    others: ImmutableArray<DiagnosticData>.Empty,
                    documentIds: null,
                    fromBuild: false);

                // return new result
                return result.SetItem(compilerAnalyzer, newCompilerAnalysisResult);
            }

            /// <summary>
            /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) by calculating them
            /// </summary>
            public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? spanOpt, CancellationToken cancellationToken)
            {
                if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
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
            private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, bool forcedAnalysis, CancellationToken cancellationToken)
            {
                try
                {
                    var result = ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;

                    // analyzerDriver can be null if given project doesn't support compilation.
                    if (analyzerDriverOpt != null)
                    {
                        // calculate regular diagnostic analyzers diagnostics
                        var compilerResult = await AnalyzeAsync(analyzerDriverOpt, project, forcedAnalysis, cancellationToken).ConfigureAwait(false);
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
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, bool forcedAnalysis,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing, CancellationToken cancellationToken)
            {
                try
                {
                    // PERF: check whether we can reduce number of analyzers we need to run.
                    //       this can happen since caller could have created the driver with different set of analyzers that are different
                    //       than what we used to create the cache.
                    var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                    if (TryReduceAnalyzersToRun(analyzerDriverOpt, project, version, existing, out var analyzersToRun))
                    {
                        // it looks like we can reduce the set. create new CompilationWithAnalyzer.
                        // if we reduced to 0, we just pass in null for analyzer drvier. it could be reduced to 0
                        // since we might have up to date results for analyzers from compiler but not for 
                        // workspace analyzers.
                        var analyzerDriverWithReducedSet =
                            analyzersToRun.Length == 0 ?
                                null : await _owner._compilationManager.CreateAnalyzerDriverAsync(
                                        project, analyzersToRun, analyzerDriverOpt.AnalysisOptions.ReportSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                        var result = await ComputeDiagnosticsAsync(analyzerDriverWithReducedSet, project, stateSets, forcedAnalysis, cancellationToken).ConfigureAwait(false);
                        return MergeExistingDiagnostics(version, existing, result);
                    }

                    // we couldn't reduce the set.
                    return await ComputeDiagnosticsAsync(analyzerDriverOpt, project, stateSets, forcedAnalysis, cancellationToken).ConfigureAwait(false);
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
                CompilationWithAnalyzers analyzerDriverOpt, Project project, VersionStamp version,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> existing,
                out ImmutableArray<DiagnosticAnalyzer> analyzers)
            {
                analyzers = default;

                // we don't have analyzer driver, nothing to reduce.
                if (analyzerDriverOpt == null)
                {
                    return false;
                }

                var existingAnalyzers = analyzerDriverOpt.Analyzers;
                var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
                foreach (var analyzer in existingAnalyzers)
                {
                    if (existing.TryGetValue(analyzer, out var analysisResult) &&
                        analysisResult.Version == version &&
                        !analyzer.IsOpenFileOnly(project.Solution.Workspace))
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

                    foreach (var analyzer in ideAnalyzers)
                    {
                        var builder = new DiagnosticAnalysisResultBuilder(project, version);

                        if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
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

                        if (analyzer is ProjectDiagnosticAnalyzer projectAnalyzer)
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
                    var diagnostics = (await analyzer.AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false)).NullToEmpty();

                    // Apply filtering from compilation options (source suppressions, ruleset, etc.)
                    if (compilationOpt != null)
                    {
                        diagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt).ToImmutableArrayOrEmpty();
                    }

#if DEBUG
                    // since all ProjectDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                    // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                    // from intern teams.
                    await VerifyDiagnosticLocationsAsync(diagnostics, project, cancellationToken).ConfigureAwait(false);
#endif

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

#if DEBUG
                    // since all DocumentDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                    // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                    // from intern teams.
                    await VerifyDiagnosticLocationsAsync(diagnostics, document.Project, cancellationToken).ConfigureAwait(false);
#endif

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

                if (!await AnalysisEnabled(document, analyzer, kind, cancellationToken).ConfigureAwait(false))
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

            private async Task<bool> AnalysisEnabled(Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, CancellationToken cancellationToken)
            {
                // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
                if (kind == AnalysisKind.Syntax || !_owner.HostAnalyzerManager.IsCompilerDiagnosticAnalyzer(document.Project.Language, analyzer))
                {
                    return true;
                }

                var enabled = await document.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a.ToString()}, ({d.FilePath ?? d.Name}), Enabled:{e}", analyzer, document, enabled);

                return enabled;
            }

            private void UpdateAnalyzerTelemetryData(
                DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult> analysisResults, Project project, CancellationToken cancellationToken)
            {
                foreach (var kv in analysisResults.TelemetryInfo)
                {
                    DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(kv.Key, kv.Value, project, _owner.DiagnosticLogAggregator);
                }
            }

            private static bool FullAnalysisEnabled(Project project, bool forceAnalyzerRun)
            {
                if (forceAnalyzerRun)
                {
                    // asked to ignore any checks.
                    return true;
                }

                if (!ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(project) ||
                    !project.Solution.Options.GetOption(RuntimeOptions.FullSolutionAnalysis))
                {
                    return false;
                }

                return true;
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
                CompilationWithAnalyzers analyzerDriver, Project project, bool forcedAnalysis, CancellationToken cancellationToken)
            {
                // quick bail out
                if (analyzerDriver.Analyzers.Length == 0)
                {
                    return DiagnosticAnalysisResultMap.Create(
                        ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty,
                        ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
                }

                var executor = project.Solution.Workspace.Services.GetService<ICodeAnalysisDiagnosticAnalyzerExecutor>();
                return await executor.AnalyzeAsync(analyzerDriver, project, forcedAnalysis, cancellationToken).ConfigureAwait(false);
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

            private static async Task VerifyDiagnosticLocationsAsync(ImmutableArray<Diagnostic> diagnostics, Project project, CancellationToken cancellationToken)
            {
                foreach (var diagnostic in diagnostics)
                {
                    await VerifyDiagnosticLocationAsync(diagnostic.Id, diagnostic.Location, project, cancellationToken).ConfigureAwait(false);

                    if (diagnostic.AdditionalLocations != null)
                    {
                        foreach (var location in diagnostic.AdditionalLocations)
                        {
                            await VerifyDiagnosticLocationAsync(diagnostic.Id, diagnostic.Location, project, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            private static async Task VerifyDiagnosticLocationAsync(string id, Location location, Project project, CancellationToken cancellationToken)
            {
                switch (location.Kind)
                {
                    case LocationKind.None:
                    case LocationKind.MetadataFile:
                    case LocationKind.XmlFile:
                        // ignore these kinds
                        break;
                    case LocationKind.SourceFile:
                    {
                        if (project.GetDocument(location.SourceTree) == null)
                        {
                            // Disallow diagnostics with source locations outside this project.
                            throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_in_file_1_which_is_not_part_of_the_compilation_being_analyzed, id, location.SourceTree.FilePath), "diagnostic");
                        }

                        if (location.SourceSpan.End > location.SourceTree.Length)
                        {
                            // Disallow diagnostics with source locations outside this project.
                            throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_1_in_file_2_which_is_outside_of_the_given_file, id, location.SourceSpan, location.SourceTree.FilePath), "diagnostic");
                        }
                    }
                    break;
                    case LocationKind.ExternalFile:
                    {
                        var filePath = location.GetLineSpan().Path;
                        var document = TryGetDocumentWithFilePath(project, filePath);
                        if (document == null)
                        {
                            // this is not a roslyn file. we don't care about this file.
                            return;
                        }

                        // this can be potentially expensive since it will load text if it is not already loaded.
                        // but, this text is most likely already loaded since producer of this diagnostic (Document/ProjectDiagnosticAnalyzers)
                        // should have loaded it to produce the diagnostic at the first place. once loaded, it should stay in memory until
                        // project cache goes away. when text is already there, await should return right away.
                        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        if (location.SourceSpan.End > text.Length)
                        {
                            // Disallow diagnostics with locations outside this project.
                            throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_1_in_file_2_which_is_outside_of_the_given_file, id, location.SourceSpan, filePath), "diagnostic");
                        }
                    }
                    break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            private static Document TryGetDocumentWithFilePath(Project project, string path)
            {
                foreach (var documentId in project.Solution.GetDocumentIdsWithFilePath(path))
                {
                    if (documentId.ProjectId == project.Id)
                    {
                        return project.GetDocument(documentId);
                    }
                }

                return null;
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
