// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public Task ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
            => AnalyzeProjectAsync(project, forceAnalyzerRun: true, cancellationToken);

        private async Task AnalyzeProjectAsync(Project project, bool forceAnalyzerRun, CancellationToken cancellationToken)
        {
            try
            {
                var stateSets = GetStateSetsForFullSolutionAnalysis(_stateManager.GetOrUpdateStateSets(project), project);

                // get driver only with active analyzers.
                var ideOptions = AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

                // PERF: get analyzers that are not suppressed and marked as open file only
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                var activeAnalyzers = stateSets
                                        .Select(s => s.Analyzer)
                                        .Where(a => (forceAnalyzerRun || DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(a, project, GlobalOptions)) && !a.IsOpenFileOnly(ideOptions.CleanupOptions?.SimplifierOptions));

                CompilationWithAnalyzers? compilationWithAnalyzers = null;

                if (forceAnalyzerRun || GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language))
                {
                    compilationWithAnalyzers = await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(project, ideOptions, activeAnalyzers, includeSuppressedDiagnostics: true, cancellationToken).ConfigureAwait(false);
                }

                var result = await GetProjectAnalysisDataAsync(compilationWithAnalyzers, project, ideOptions, stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);

                // no cancellation after this point.
                using var _ = ArrayBuilder<StateSet>.GetInstance(out var analyzedStateSetsBuilder);
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetOrCreateProjectState(project.Id);

                    if (result.TryGetResult(stateSet.Analyzer, out var analyzerResult))
                    {
                        await state.SaveToInMemoryStorageAsync(project, analyzerResult).ConfigureAwait(false);
                        analyzedStateSetsBuilder.Add(stateSet);
                    }
                }

                if (analyzedStateSetsBuilder.Count > 0)
                {
                    var oldResult = result.OldResult ?? ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
                    RaiseProjectDiagnosticsIfNeeded(project, analyzedStateSetsBuilder.ToImmutable(), oldResult, result.Result);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task TextDocumentOpenAsync(TextDocument document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // can not be canceled
                foreach (var stateSet in stateSets)
                    await stateSet.OnDocumentOpenedAsync(document).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Return list of <see cref="StateSet"/> to be used for full solution analysis.
        /// </summary>
        private ImmutableArray<StateSet> GetStateSetsForFullSolutionAnalysis(ImmutableArray<StateSet> stateSets, Project project)
        {
            // If full analysis is off, remove state that is created from build.
            // this will make sure diagnostics from build (converted from build to live) will never be cleared
            // until next build.
            _ = GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!compilerFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for compiler diagnostics,
                // so we remove the compiler analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either not for compiler analyzer or those which are for compiler
                // analyzer, but not from build.
                stateSets = stateSets.WhereAsArray(s => !s.Analyzer.IsCompilerAnalyzer() || !s.FromBuild(project.Id));
            }

            if (!analyzersFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for analyzer diagnostics,
                // so we remove the analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either for the special compiler/workspace analyzers or those which are for
                // other analyzers, but not from build.
                stateSets = stateSets.WhereAsArray(s => s.Analyzer.IsCompilerAnalyzer() || s.Analyzer.IsWorkspaceDiagnosticAnalyzer() || !s.FromBuild(project.Id));
            }

            // Include only analyzers we want to run for full solution analysis.
            // Analyzers not included here will never be saved because result is unknown.
            return stateSets.WhereAsArray(s => IsCandidateForFullSolutionAnalysis(s.Analyzer, project));
        }

        private bool IsCandidateForFullSolutionAnalysis(DiagnosticAnalyzer analyzer, Project project)
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
            var descriptors = DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer);
            var analyzerConfigOptions = project.GetAnalyzerConfigOptions();

            return descriptors.Any(static (d, arg) => d.GetEffectiveSeverity(arg.CompilationOptions, arg.analyzerConfigOptions?.AnalyzerOptions, arg.analyzerConfigOptions?.TreeOptions) != ReportDiagnostic.Hidden, (project.CompilationOptions, analyzerConfigOptions));
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            RaiseProjectDiagnosticsIfNeeded(project, stateSets, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, result);
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> oldResult,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> newResult)
        {
            if (oldResult.Count == 0 && newResult.Count == 0)
            {
                // there is nothing to update
                return;
            }

            AnalyzerService.RaiseBulkDiagnosticsUpdated(async raiseEvents =>
            {
                using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
                foreach (var stateSet in stateSets)
                {
                    var analyzer = stateSet.Analyzer;

                    var oldAnalysisResult = GetResultOrEmpty(oldResult, analyzer, project.Id, VersionStamp.Default);
                    var newAnalysisResult = GetResultOrEmpty(newResult, analyzer, project.Id, VersionStamp.Default);

                    // Perf - 4 different cases.
                    // upper 3 cases can be removed and it will still work. but this is hot path so if we can bail out
                    // without any allocations, that's better.
                    if (oldAnalysisResult.IsEmpty && newAnalysisResult.IsEmpty)
                    {
                        // nothing to do
                        continue;
                    }

                    if (!oldAnalysisResult.IsEmpty && newAnalysisResult.IsEmpty)
                    {
                        RoslynDebug.Assert(oldAnalysisResult.DocumentIds != null);

                        // remove old diagnostics
                        AddProjectDiagnosticsRemovedArgs(ref argsBuilder.AsRef(), stateSet, oldAnalysisResult.ProjectId, oldAnalysisResult.DocumentIds, handleActiveFile: false);
                        continue;
                    }

                    if (oldAnalysisResult.IsEmpty && !newAnalysisResult.IsEmpty)
                    {
                        // add new diagnostics
                        argsBuilder.AddRange(await CreateProjectDiagnosticsCreatedArgsAsync(project, stateSet, oldAnalysisResult, newAnalysisResult, CancellationToken.None).ConfigureAwait(false));
                        continue;
                    }

                    // both old and new has items in them. update existing items
                    RoslynDebug.Assert(oldAnalysisResult.DocumentIds != null);
                    RoslynDebug.Assert(newAnalysisResult.DocumentIds != null);

                    // first remove ones no longer needed.
                    var documentsToRemove = oldAnalysisResult.DocumentIds.Except(newAnalysisResult.DocumentIds);
                    AddProjectDiagnosticsRemovedArgs(ref argsBuilder.AsRef(), stateSet, oldAnalysisResult.ProjectId, documentsToRemove, handleActiveFile: false);

                    // next update or create new ones
                    argsBuilder.AddRange(await CreateProjectDiagnosticsCreatedArgsAsync(project, stateSet, oldAnalysisResult, newAnalysisResult, CancellationToken.None).ConfigureAwait(false));
                }

                raiseEvents(argsBuilder.ToImmutableAndClear());
            });
        }

        private void AddDocumentDiagnosticsArgsIfNeeded(
            ref TemporaryArray<DiagnosticsUpdatedArgs> builder,
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind,
            DiagnosticAnalysisResult oldResult, DiagnosticAnalysisResult newResult)
        {
            // if our old result is from build and we don't have actual data, don't try micro-optimize and always refresh diagnostics.
            // most of time, we don't actually load or hold the old data in memory from persistent storage due to perf reasons.
            //
            // we need this special behavior for errors from build since unlike live errors, we don't know whether errors
            // from build is for syntax, semantic or others. due to that, we blindly mark them as semantic errors (most common type of errors from build)
            //
            // that can sometime cause issues. for example, if the error turns out to be syntax error (live) then we at the end fail to de-dup.
            // but since this optimization saves us a lot of refresh between live errors analysis we want to disable this only in this condition.
            var forceUpdate = oldResult.FromBuild && oldResult.IsAggregatedForm;

            var oldItems = oldResult.GetDocumentDiagnostics(document.Id, kind);
            var newItems = newResult.GetDocumentDiagnostics(document.Id, kind);

            AddDocumentDiagnosticsArgsIfNeeded(ref builder, document, analyzer, kind, oldItems, newItems, forceUpdate);
        }

        private void AddDocumentDiagnosticsArgsIfNeeded(
            ref TemporaryArray<DiagnosticsUpdatedArgs> builder,
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind,
            ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems,
            bool forceUpdate)
        {
            if (!forceUpdate && oldItems.IsEmpty && newItems.IsEmpty)
            {
                // there is nothing to update
                return;
            }

            AddDiagnosticsCreatedArgs(ref builder, document, analyzer, kind, newItems);
        }

        private async Task<ImmutableArray<DiagnosticsUpdatedArgs>> CreateProjectDiagnosticsCreatedArgsAsync(Project project, StateSet stateSet, DiagnosticAnalysisResult oldAnalysisResult, DiagnosticAnalysisResult newAnalysisResult, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(newAnalysisResult.DocumentIds != null);

            using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
            foreach (var documentId in newAnalysisResult.DocumentIds)
            {
                var document = project.GetTextDocument(documentId);

                // If we couldn't find a normal document, and all features are enabled for source generated documents,
                // attempt to locate a matching source generated document in the project.
                if (document is null
                    && project.Solution.Services.GetService<ISolutionCrawlerOptionsService>()?.EnableDiagnosticsInSourceGeneratedFiles == true)
                {
                    document = await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                }

                if (document == null)
                {
                    // it can happen with build synchronization since, in build case, 
                    // we don't have actual snapshot (we have no idea what sources out of proc build has picked up)
                    // so we might be out of sync.
                    // example of such cases will be changing anything about solution while building is going on.
                    // it can be user explicit actions such as unloading project, deleting a file, but also it can be 
                    // something project system or roslyn workspace does such as populating workspace right after
                    // solution is loaded.
                    continue;
                }

                AddDocumentDiagnosticsArgsIfNeeded(ref argsBuilder.AsRef(), document, stateSet.Analyzer, AnalysisKind.NonLocal, oldAnalysisResult, newAnalysisResult);

                // we don't raise events for active file. it will be taken cared by active file analysis
                if (stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                AddDocumentDiagnosticsArgsIfNeeded(ref argsBuilder.AsRef(), document, stateSet.Analyzer, AnalysisKind.Syntax, oldAnalysisResult, newAnalysisResult);
                AddDocumentDiagnosticsArgsIfNeeded(ref argsBuilder.AsRef(), document, stateSet.Analyzer, AnalysisKind.Semantic, oldAnalysisResult, newAnalysisResult);
            }

            AddDiagnosticsCreatedArgs(ref argsBuilder.AsRef(), project, stateSet.Analyzer, newAnalysisResult.GetOtherDiagnostics());

            return argsBuilder.ToImmutableAndClear();
        }

        private void AddProjectDiagnosticsRemovedArgs(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, StateSet stateSet, ProjectId projectId, IEnumerable<DocumentId> documentIds, bool handleActiveFile)
        {
            foreach (var documentId in documentIds)
            {
                AddDiagnosticsRemovedArgs(ref builder, documentId, solution: null, stateSet.Analyzer, AnalysisKind.NonLocal);

                // we don't raise events for active file. it will be taken care of by active file analysis
                if (!handleActiveFile && stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                AddDiagnosticsRemovedArgs(ref builder, documentId, solution: null, stateSet.Analyzer, AnalysisKind.Syntax);
                AddDiagnosticsRemovedArgs(ref builder, documentId, solution: null, stateSet.Analyzer, AnalysisKind.Semantic);
            }

            AddDiagnosticsRemovedArgs(ref builder, projectId, solution: null, stateSet.Analyzer);
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor(DiagnosticIncrementalAnalyzer diagnosticIncrementalAnalyzer)
        {
            public Task TextDocumentOpenAsync(TextDocument document)
                => diagnosticIncrementalAnalyzer.TextDocumentOpenAsync(document, CancellationToken.None);
        }
    }
}
