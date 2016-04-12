// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Text;
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

            public async Task<DocumentAnalysisData> GetDocumentAnalysisDataAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Document document, StateSet stateSet, AnalysisKind kind, CancellationToken cancellationToken)
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

            public async Task<ProjectAnalysisData> GetProjectAnalysisDataAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
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
                    if (!await FullAnalysisEnabledAsync(project, cancellationToken).ConfigureAwait(false))
                    {
                        return new ProjectAnalysisData(project.Id, version, existingData.Result, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty);
                    }

                    var result = await ComputeDiagnosticsAsync(analyzerDriverOpt, project, stateSets, cancellationToken).ConfigureAwait(false);

                    return new ProjectAnalysisData(project.Id, version, existingData.Result, result);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

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

            public async Task<ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriverOpt, Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
            {
                var result = ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty;

                // analyzerDriver can be null if given project doesn't support compilation.
                if (analyzerDriverOpt != null)
                {
                    // calculate regular diagnostic analyzers diagnostics
                    result = await analyzerDriverOpt.AnalyzeAsync(project, cancellationToken).ConfigureAwait(false);

                    // record telemetry data
                    await UpdateAnalyzerTelemetryDataAsync(analyzerDriverOpt, project, cancellationToken).ConfigureAwait(false);
                }

                // check whether there is IDE specific project diagnostic analyzer
                return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, stateSets, analyzerDriverOpt?.Compilation, result, cancellationToken).ConfigureAwait(false);
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
                Project project, IEnumerable<StateSet> stateSets, Compilation compilationOpt, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> result, CancellationToken cancellationToken)
            {
                // check whether there is IDE specific project diagnostic analyzer
                var ideAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => a is ProjectDiagnosticAnalyzer || a is DocumentDiagnosticAnalyzer).ToImmutableArrayOrEmpty();
                if (ideAnalyzers.Length <= 0)
                {
                    return result;
                }

                // create result map
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var builder = new CompilerDiagnosticExecutor.Builder(project, version);

                foreach (var analyzer in ideAnalyzers)
                {
                    var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                    if (documentAnalyzer != null)
                    {
                        foreach (var document in project.Documents)
                        {
                            if (project.SupportsCompilation)
                            {
                                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                                builder.AddSyntaxDiagnostics(tree, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Syntax, compilationOpt, cancellationToken).ConfigureAwait(false));
                                builder.AddSemanticDiagnostics(tree, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Semantic, compilationOpt, cancellationToken).ConfigureAwait(false));
                            }
                            else
                            {
                                builder.AddSyntaxDiagnostics(document.Id, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Syntax, compilationOpt, cancellationToken).ConfigureAwait(false));
                                builder.AddSemanticDiagnostics(document.Id, await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, AnalysisKind.Semantic, compilationOpt, cancellationToken).ConfigureAwait(false));
                            }
                        }
                    }

                    var projectAnalyzer = analyzer as ProjectDiagnosticAnalyzer;
                    if (projectAnalyzer != null)
                    {
                        builder.AddCompilationDiagnostics(await ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(project, projectAnalyzer, compilationOpt, cancellationToken).ConfigureAwait(false));
                    }

                    // merge the result to existing one.
                    result = result.Add(analyzer, builder.ToResult());
                }

                return result;
            }

            private async Task<IEnumerable<Diagnostic>> ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(
                Project project, ProjectDiagnosticAnalyzer analyzer, Compilation compilationOpt, CancellationToken cancellationToken)
            {
                using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                {
                    var diagnostics = pooledObject.Object;
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await analyzer.AnalyzeProjectAsync(project, diagnostics.Add, cancellationToken).ConfigureAwait(false);

                        // REVIEW: V1 doesn't convert diagnostics to effective diagnostics. not sure why.
                        return compilationOpt == null ? diagnostics.ToImmutableArrayOrEmpty() : CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt);
                    }
                    catch (Exception e) when (!IsCanceled(e, cancellationToken))
                    {
                        OnAnalyzerException(analyzer, project.Id, compilationOpt, e);
                        return ImmutableArray<Diagnostic>.Empty;
                    }
                }
            }

            private async Task<IEnumerable<Diagnostic>> ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                Document document, DocumentDiagnosticAnalyzer analyzer, AnalysisKind kind, Compilation compilationOpt, CancellationToken cancellationToken)
            {
                using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                {
                    var diagnostics = pooledObject.Object;
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        switch (kind)
                        {
                            case AnalysisKind.Syntax:
                                await analyzer.AnalyzeSyntaxAsync(document, diagnostics.Add, cancellationToken).ConfigureAwait(false);
                                return compilationOpt == null ? diagnostics.ToImmutableArrayOrEmpty() : CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt);
                            case AnalysisKind.Semantic:
                                await analyzer.AnalyzeSemanticsAsync(document, diagnostics.Add, cancellationToken).ConfigureAwait(false);
                                return compilationOpt == null ? diagnostics.ToImmutableArrayOrEmpty() : CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt);
                            default:
                                return Contract.FailWithReturn<ImmutableArray<Diagnostic>>("shouldn't reach here");
                        }
                    }
                    catch (Exception e) when (!IsCanceled(e, cancellationToken))
                    {
                        OnAnalyzerException(analyzer, document.Project.Id, compilationOpt, e);
                        return ImmutableArray<Diagnostic>.Empty;
                    }
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
                        return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation);
                    case AnalysisKind.Semantic:
                        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        diagnostics = await analyzerDriverOpt.GetAnalyzerSemanticDiagnosticsAsync(model, spanOpt, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                        return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation);
                    default:
                        return Contract.FailWithReturn<ImmutableArray<Diagnostic>>("shouldn't reach here");
                }
            }

            private async Task UpdateAnalyzerTelemetryDataAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
            {
                foreach (var analyzer in analyzerDriver.Analyzers)
                {
                    await UpdateAnalyzerTelemetryDataAsync(analyzerDriver, analyzer, project, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task UpdateAnalyzerTelemetryDataAsync(CompilationWithAnalyzers analyzerDriver, DiagnosticAnalyzer analyzer, Project project, CancellationToken cancellationToken)
            {
                try
                {
                    var analyzerTelemetryInfo = await analyzerDriver.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(analyzer, analyzerTelemetryInfo, project, _owner.DiagnosticLogAggregator);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private static async Task<bool> FullAnalysisEnabledAsync(Project project, CancellationToken cancellationToken)
            {
                var workspace = project.Solution.Workspace;
                var language = project.Language;

                if (!workspace.Options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language) ||
                    !workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysis))
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
                    if (documentIds.IsEmpty || documentIds.Any(id => id != targetDocument.Id))
                    {
                        continue;
                    }

                    yield return DiagnosticData.Create(targetDocument, diagnostic);
                }
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
        }
    }
}
