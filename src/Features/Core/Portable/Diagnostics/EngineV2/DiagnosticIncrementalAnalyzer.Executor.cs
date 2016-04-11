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

            public async Task<DocumentAnalysisData> GetDocumentAnalysisDataAsync(
                CompilationWithAnalyzers analyzerDriver, Document document, StateSet stateSet, AnalysisKind kind, CancellationToken cancellationToken)
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
                    var diagnostics = await ComputeDiagnosticsAsync(analyzerDriver, document, stateSet.Analyzer, kind, nullFilterSpan, cancellationToken).ConfigureAwait(false);

                    // we only care about local diagnostics
                    return new DocumentAnalysisData(version, existingData.Items, diagnostics.ToImmutableArrayOrEmpty());
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            public async Task<ProjectAnalysisData> GetProjectAnalysisDataAsync(CompilationWithAnalyzers analyzerDriver, Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
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
                        return new ProjectAnalysisData(version, existingData.Result, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty);
                    }

                    var result = await ComputeDiagnosticsAsync(analyzerDriver, project, stateSets, cancellationToken).ConfigureAwait(false);

                    return new ProjectAnalysisData(version, existingData.Result, result);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriver, Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? spanOpt, CancellationToken cancellationToken)
            {
                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    var diagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(document, documentAnalyzer, kind, analyzerDriver.Compilation, cancellationToken).ConfigureAwait(false);
                    return ConvertToLocalDiagnostics(document, diagnostics);
                }

                var documentDiagnostics = await ComputeDiagnosticAnalyzerDiagnosticsAsync(analyzerDriver, document, analyzer, kind, spanOpt, cancellationToken).ConfigureAwait(false);
                return ConvertToLocalDiagnostics(document, documentDiagnostics);
            }

            public async Task<ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>> ComputeDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriver, Project project, IEnumerable<StateSet> stateSets, CancellationToken cancellationToken)
            {
                // calculate regular diagnostic analyzers diagnostics
                var result = await analyzerDriver.AnalyzeAsync(project, cancellationToken).ConfigureAwait(false);

                // record telemetry data
                await UpdateAnalyzerTelemetryDataAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);

                // check whether there is IDE specific project diagnostic analyzer
                return await MergeProjectDiagnosticAnalyzerDiagnosticsAsync(project, stateSets, analyzerDriver.Compilation, result, cancellationToken).ConfigureAwait(false);
            }

            private async Task<ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>> MergeProjectDiagnosticAnalyzerDiagnosticsAsync(
                Project project, IEnumerable<StateSet> stateSets, Compilation compilation, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> result, CancellationToken cancellationToken)
            {
                // check whether there is IDE specific project diagnostic analyzer
                var projectAnalyzers = stateSets.Select(s => s.Analyzer).OfType<ProjectDiagnosticAnalyzer>().ToImmutableArrayOrEmpty();
                if (projectAnalyzers.Length <= 0)
                {
                    return result;
                }

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                using (var diagnostics = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                {
                    foreach (var analyzer in projectAnalyzers)
                    {
                        // reset pooled list
                        diagnostics.Object.Clear();

                        try
                        {
                            await analyzer.AnalyzeProjectAsync(project, diagnostics.Object.Add, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e) when (!IsCanceled(e, cancellationToken))
                        {
                            OnAnalyzerException(analyzer, project.Id, compilation, e);
                            continue;
                        }

                        // create result map
                        var builder = new CompilerDiagnosticExecutor.Builder(project, version);
                        builder.AddCompilationDiagnostics(diagnostics.Object);

                        // merge the result to existing one.
                        result = result.Add(analyzer, builder.ToResult());
                    }
                }

                return result;
            }

            private async Task<IEnumerable<Diagnostic>> ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                Document document, DocumentDiagnosticAnalyzer analyzer, AnalysisKind kind, Compilation compilation, CancellationToken cancellationToken)
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
                                return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation);
                            case AnalysisKind.Semantic:
                                await analyzer.AnalyzeSemanticsAsync(document, diagnostics.Add, cancellationToken).ConfigureAwait(false);
                                return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation);
                            default:
                                return Contract.FailWithReturn<ImmutableArray<Diagnostic>>("shouldn't reach here");
                        }
                    }
                    catch (Exception e) when (!IsCanceled(e, cancellationToken))
                    {
                        OnAnalyzerException(analyzer, document.Project.Id, compilation, e);

                        return ImmutableArray<Diagnostic>.Empty;
                    }
                }
            }

            private async Task<IEnumerable<Diagnostic>> ComputeDiagnosticAnalyzerDiagnosticsAsync(
                CompilationWithAnalyzers analyzerDriver, Document document, DiagnosticAnalyzer analyzer, AnalysisKind kind, TextSpan? spanOpt, CancellationToken cancellationToken)
            {
                // quick optimization to reduce allocations.
                if (!_owner.SupportAnalysisKind(analyzer, document.Project.Language, kind))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        var diagnostics = await analyzerDriver.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                        return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriver.Compilation);
                    case AnalysisKind.Semantic:
                        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        diagnostics = await analyzerDriver.GetAnalyzerSemanticDiagnosticsAsync(model, spanOpt, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                        return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriver.Compilation);
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

            private void OnAnalyzerException(DiagnosticAnalyzer analyzer, ProjectId projectId, Compilation compilation, Exception ex)
            {
                var exceptionDiagnostic = AnalyzerHelper.CreateAnalyzerExceptionDiagnostic(analyzer, ex);

                if (compilation != null)
                {
                    exceptionDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(ImmutableArray.Create(exceptionDiagnostic), compilation).SingleOrDefault();
                }

                var onAnalyzerException = _owner.GetOnAnalyzerException(projectId);
                onAnalyzerException(ex, analyzer, exceptionDiagnostic);
            }
        }
    }
}
