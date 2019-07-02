// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        internal readonly DebuggingSession DebuggingSession;
        internal readonly EditSessionTelemetry Telemetry;

        /// <summary>
        /// The solution captured when entering the break state.
        /// </summary>
        internal readonly Solution BaseSolution;

        private readonly ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> _nonRemappableRegions;

        /// <summary>
        /// Lazily calculated map of base active statements.
        /// </summary>
        internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

        /// <summary>
        /// For each base active statement the exception regions around that statement. 
        /// </summary>
        internal readonly AsyncLazy<ImmutableArray<ActiveStatementExceptionRegions>> BaseActiveExceptionRegions;

        /// <summary>
        /// Results of changed documents analysis. 
        /// The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
        /// Contains analyses of the latest observed document versions.
        /// </summary>
        private readonly Dictionary<DocumentId, (Document Document, AsyncLazy<DocumentAnalysisResults> Results)> _analyses
            = new Dictionary<DocumentId, (Document, AsyncLazy<DocumentAnalysisResults>)>();
        private readonly object _analysesGuard = new object();

        /// <summary>
        /// Errors to be reported when a project is updated but the corresponding module does not support EnC.
        /// 
        /// The capability of a module to apply edits may change during edit session if the user attaches debugger to 
        /// an additional process that doesn't support EnC (or detaches from such process). The diagnostic reflects 
        /// the state of the module when queried for the first time. Before we actually apply an edit to the module 
        /// we need to query again instead of just reusing the diagnostic.
        /// </summary>
        private readonly Dictionary<Guid, ImmutableArray<LocationlessDiagnostic>> _moduleDiagnostics
             = new Dictionary<Guid, ImmutableArray<LocationlessDiagnostic>>();
        private readonly object _moduleDiagnosticsGuard = new object();

        /// <summary>
        /// A <see cref="DocumentId"/> is added whenever <see cref="EditAndContinueDiagnosticAnalyzer"/> reports 
        /// rude edits or module diagnostics. At the end of the session we ask the diagnostic analyzer to reanalyze 
        /// the documents to clean up the diagnostics.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithReportedDiagnostics = new HashSet<DocumentId>();
        private readonly object _documentsWithReportedDiagnosticsGuard = new object();

        private bool _changesApplied;

        internal EditSession(DebuggingSession debuggingSession, EditSessionTelemetry telemetry)
        {
            Debug.Assert(debuggingSession != null);
            Debug.Assert(telemetry != null);

            DebuggingSession = debuggingSession;

            _nonRemappableRegions = debuggingSession.NonRemappableRegions;

            Telemetry = telemetry;
            BaseSolution = debuggingSession.LastCommittedSolution;

            BaseActiveStatements = new AsyncLazy<ActiveStatementsMap>(GetBaseActiveStatementsAsync, cacheResult: true);
            BaseActiveExceptionRegions = new AsyncLazy<ImmutableArray<ActiveStatementExceptionRegions>>(GetBaseActiveExceptionRegionsAsync, cacheResult: true);
        }

        internal CancellationToken CancellationToken => _cancellationSource.Token;
        internal void Cancel() => _cancellationSource.Cancel();

        public void Dispose()
        {
            _cancellationSource.Dispose();
        }

        internal void ModuleInstanceLoadedOrUnloaded(Guid mvid)
        {
            // invalidate diagnostic cache for the module:
            lock (_moduleDiagnosticsGuard)
            {
                _moduleDiagnostics.Remove(mvid);
            }
        }

        public ImmutableArray<LocationlessDiagnostic> GetModuleDiagnostics(Guid mvid, string projectDisplayName)
        {
            ImmutableArray<LocationlessDiagnostic> result;
            lock (_moduleDiagnosticsGuard)
            {
                if (_moduleDiagnostics.TryGetValue(mvid, out result))
                {
                    return result;
                }
            }

            var newResult = ImmutableArray<LocationlessDiagnostic>.Empty;
            if (!DebuggingSession.DebugeeModuleMetadataProvider.IsEditAndContinueAvailable(mvid, out var errorCode, out var localizedMessage))
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(errorCode);
                newResult = ImmutableArray.Create(new LocationlessDiagnostic(descriptor, new[] { projectDisplayName, localizedMessage }));
            }

            lock (_moduleDiagnosticsGuard)
            {
                if (!_moduleDiagnostics.TryGetValue(mvid, out result))
                {
                    _moduleDiagnostics.Add(mvid, result = newResult);
                }
            }

            return result;
        }

        private Project GetBaseProject(ProjectId id)
            => BaseSolution.GetProject(id);

        private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                return CreateActiveStatementsMap(BaseSolution, await DebuggingSession.ActiveStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return new ActiveStatementsMap(
                    SpecializedCollections.EmptyReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>>(),
                    SpecializedCollections.EmptyReadOnlyDictionary<ActiveInstructionId, ActiveStatement>());
            }
        }

        internal static bool SupportsEditAndContinue(Project project)
            => project.LanguageServices.GetService<IEditAndContinueAnalyzer>() != null;

        private ActiveStatementsMap CreateActiveStatementsMap(Solution solution, ImmutableArray<ActiveStatementDebugInfo> debugInfos)
        {
            var byDocument = PooledDictionary<DocumentId, ArrayBuilder<ActiveStatement>>.GetInstance();
            var byInstruction = PooledDictionary<ActiveInstructionId, ActiveStatement>.GetInstance();

            bool supportsEditAndContinue(DocumentId documentId)
                => SupportsEditAndContinue(solution.GetProject(documentId.ProjectId));

            foreach (var debugInfo in debugInfos)
            {
                var documentName = debugInfo.DocumentNameOpt;
                if (documentName == null)
                {
                    // Ignore active statements that do not have a source location.
                    continue;
                }

                var documentIds = solution.GetDocumentIdsWithFilePath(documentName);
                var firstDocumentId = documentIds.FirstOrDefault(supportsEditAndContinue);
                if (firstDocumentId == null)
                {
                    // Ignore active statements that don't belong to the solution or language that supports EnC service.
                    continue;
                }

                if (!byDocument.TryGetValue(firstDocumentId, out var primaryDocumentActiveStatements))
                {
                    byDocument.Add(firstDocumentId, primaryDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
                }

                var activeStatement = new ActiveStatement(
                    ordinal: byInstruction.Count,
                    primaryDocumentOrdinal: primaryDocumentActiveStatements.Count,
                    documentIds: documentIds,
                    flags: debugInfo.Flags,
                    span: GetUpToDateSpan(debugInfo),
                    instructionId: debugInfo.InstructionId,
                    threadIds: debugInfo.ThreadIds);

                primaryDocumentActiveStatements.Add(activeStatement);

                // TODO: associate only those documents that are from a project with the right module id
                // https://github.com/dotnet/roslyn/issues/24320
                for (var i = 1; i < documentIds.Length; i++)
                {
                    var documentId = documentIds[i];
                    if (!supportsEditAndContinue(documentId))
                    {
                        continue;
                    }

                    if (!byDocument.TryGetValue(documentId, out var linkedDocumentActiveStatements))
                    {
                        byDocument.Add(documentId, linkedDocumentActiveStatements = ArrayBuilder<ActiveStatement>.GetInstance());
                    }

                    linkedDocumentActiveStatements.Add(activeStatement);
                }

                try
                {
                    byInstruction.Add(debugInfo.InstructionId, activeStatement);
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException($"Multiple active statements with the same instruction id returned by " +
                        $"{DebuggingSession.ActiveStatementProvider.GetType()}.{nameof(IActiveStatementProvider.GetActiveStatementsAsync)}");
                }
            }

            return new ActiveStatementsMap(byDocument.ToDictionaryAndFree(), byInstruction.ToDictionaryAndFree());
        }

        private LinePositionSpan GetUpToDateSpan(ActiveStatementDebugInfo activeStatementInfo)
        {
            if ((activeStatementInfo.Flags & ActiveStatementFlags.MethodUpToDate) != 0)
            {
                return activeStatementInfo.LinePositionSpan;
            }

            // Map active statement spans in non-remappable regions to the latest source locations.
            if (_nonRemappableRegions.TryGetValue(activeStatementInfo.InstructionId.MethodId, out var regionsInMethod))
            {
                foreach (var region in regionsInMethod)
                {
                    if (region.Span.Contains(activeStatementInfo.LinePositionSpan))
                    {
                        return activeStatementInfo.LinePositionSpan.AddLineDelta(region.LineDelta);
                    }
                }
            }

            // The active statement is in a method that's not up-to-date but the active span have not changed.
            // We only add changed spans to non-remappable regions map, so we won't find unchanged span there.
            // Return the original span.
            return activeStatementInfo.LinePositionSpan;
        }

        private async Task<ImmutableArray<ActiveStatementExceptionRegions>> GetBaseActiveExceptionRegionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var instructionMap = baseActiveStatements.InstructionMap;
                var builder = ArrayBuilder<ActiveStatementExceptionRegions>.GetInstance(instructionMap.Count);
                builder.Count = instructionMap.Count;

                foreach (var activeStatement in instructionMap.Values)
                {
                    var document = BaseSolution.GetDocument(activeStatement.PrimaryDocumentId);

                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var analyzer = document.Project.LanguageServices.GetService<IEditAndContinueAnalyzer>();
                    var exceptionRegions = analyzer.GetExceptionRegions(sourceText, syntaxRoot, activeStatement.Span, activeStatement.IsNonLeaf, out var isCovered);

                    builder[activeStatement.Ordinal] = new ActiveStatementExceptionRegions(exceptionRegions, isCovered);
                }

                return builder.ToImmutableAndFree();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return ImmutableArray<ActiveStatementExceptionRegions>.Empty;
            }
        }

        private List<(DocumentId DocumentId, AsyncLazy<DocumentAnalysisResults> Results)> GetChangedDocumentsAnalyses(Project baseProject, Project project)
        {
            var changes = project.GetChanges(baseProject);
            var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());
            var result = new List<(DocumentId, AsyncLazy<DocumentAnalysisResults>)>();

            lock (_analysesGuard)
            {
                foreach (var changedDocumentId in changedDocuments)
                {
                    result.Add((changedDocumentId, GetDocumentAnalysisNoLock(project.GetDocument(changedDocumentId))));
                }
            }

            return result;
        }

        private async Task<HashSet<ISymbol>> GetAllAddedSymbolsAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                (Document Document, AsyncLazy<DocumentAnalysisResults> Results)[] analyses;
                lock (_analysesGuard)
                {
                    analyses = _analyses.Values.ToArray();
                }

                HashSet<ISymbol> addedSymbols = null;
                foreach (var (document, lazyResults) in analyses)
                {
                    // Only consider analyses for documents that belong the currently analyzed project.
                    if (document.Project == project)
                    {
                        var results = await lazyResults.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        if (!results.HasChangesAndErrors)
                        {
                            foreach (var edit in results.SemanticEdits)
                            {
                                if (edit.Kind == SemanticEditKind.Insert)
                                {
                                    if (addedSymbols == null)
                                    {
                                        addedSymbols = new HashSet<ISymbol>();
                                    }

                                    addedSymbols.Add(edit.NewSymbol);
                                }
                            }
                        }
                    }
                }

                return addedSymbols;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysis(Document document)
        {
            lock (_analysesGuard)
            {
                return GetDocumentAnalysisNoLock(document);
            }
        }

        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document document)
        {
            if (_analyses.TryGetValue(document.Id, out var analysis) && analysis.Document == document)
            {
                return analysis.Results;
            }

            var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

            var lazyResults = new AsyncLazy<DocumentAnalysisResults>(
                asynchronousComputeFunction: async cancellationToken =>
                {
                    try
                    {
                        var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        if (!baseActiveStatements.DocumentMap.TryGetValue(document.Id, out var documentBaseActiveStatements))
                        {
                            documentBaseActiveStatements = ImmutableArray<ActiveStatement>.Empty;
                        }

                        var trackingService = BaseSolution.Workspace.Services.GetService<IActiveStatementTrackingService>();

                        var baseProject = GetBaseProject(document.Project.Id);
                        return await analyzer.AnalyzeDocumentAsync(baseProject, documentBaseActiveStatements, document, trackingService, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                },
                cacheResult: true);

            _analyses[document.Id] = (document, lazyResults);
            return lazyResults;
        }

        internal ImmutableArray<DocumentId> GetDocumentsWithReportedDiagnostics()
        {
            lock (_documentsWithReportedDiagnosticsGuard)
            {
                return ImmutableArray.CreateRange(_documentsWithReportedDiagnostics);
            }
        }

        internal void TrackDocumentWithReportedDiagnostics(DocumentId documentId)
        {
            lock (_documentsWithReportedDiagnosticsGuard)
            {
                _documentsWithReportedDiagnostics.Add(documentId);
            }
        }

        public async Task<SolutionUpdateStatus> GetSolutionUpdateStatusAsync(Solution solution, string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                if (_changesApplied)
                {
                    return SolutionUpdateStatus.None;
                }

                var projects = (sourceFilePath == null) ? solution.Projects :
                    from documentId in solution.GetDocumentIdsWithFilePath(sourceFilePath)
                    select solution.GetDocument(documentId).Project;

                bool anyChanges = false;
                foreach (var project in projects)
                {
                    if (!SupportsEditAndContinue(project))
                    {
                        continue;
                    }

                    var baseProject = GetBaseProject(project.Id);

                    // When debugging session is started some projects might not have been loaded to the workspace yet. 
                    // We capture the base solution. Edits in files that are in projects that haven't been loaded won't be applied
                    // and will result in source mismatch when the user steps into them.
                    //
                    // TODO (https://github.com/dotnet/roslyn/issues/1204):
                    // hook up the debugger reported error, check that the project has not been loaded and report a better error.
                    // Here, we assume these projects are not modified.
                    if (baseProject == null)
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not loaded", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var (mvid, _) = await DebuggingSession.GetProjectModuleIdAsync(baseProject.Id, cancellationToken).ConfigureAwait(false);
                    if (mvid == Guid.Empty)
                    {
                        // project not built
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not built", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var changedDocumentAnalyses = GetChangedDocumentsAnalyses(baseProject, project);
                    var projectSummary = await GetProjectAnalysisSymmaryAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);

                    if (projectSummary == ProjectAnalysisSummary.ValidChanges && 
                        !GetModuleDiagnostics(mvid, project.Name).IsEmpty)
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: module blocking EnC", project.Id.DebugName, project.Id);
                        return SolutionUpdateStatus.Blocked;
                    }

                    EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: {2}", project.Id.DebugName, project.Id, projectSummary);

                    switch (projectSummary)
                    {
                        case ProjectAnalysisSummary.NoChanges:
                            continue;

                        case ProjectAnalysisSummary.CompilationErrors:
                        case ProjectAnalysisSummary.RudeEdits:
                            return SolutionUpdateStatus.Blocked;

                        case ProjectAnalysisSummary.ValidChanges:
                        case ProjectAnalysisSummary.ValidInsignificantChanges:
                            anyChanges = true;
                            continue;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(projectSummary);
                    }
                }

                return anyChanges ? SolutionUpdateStatus.Ready : SolutionUpdateStatus.None;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<ProjectAnalysisSummary> GetProjectAnalysisSymmaryAsync(
            List<(DocumentId DocumentId, AsyncLazy<DocumentAnalysisResults> Results)> documentAnalyses,
            CancellationToken cancellationToken)
        {
            if (documentAnalyses.Count == 0)
            {
                return ProjectAnalysisSummary.NoChanges;
            }

            bool hasChanges = false;
            bool hasSignificantValidChanges = false;

            foreach (var analysis in documentAnalyses)
            {
                var result = await analysis.Results.GetValueAsync(cancellationToken).ConfigureAwait(false);

                // skip documents that actually were not changed:
                if (!result.HasChanges)
                {
                    continue;
                }

                // rude edit detection wasn't completed due to errors in compilation:
                if (result.HasChangesAndCompilationErrors)
                {
                    return ProjectAnalysisSummary.CompilationErrors;
                }

                // rude edits detected:
                if (!result.RudeEditErrors.IsEmpty)
                {
                    return ProjectAnalysisSummary.RudeEdits;
                }

                hasChanges = true;
                hasSignificantValidChanges |= result.HasSignificantValidChanges;
            }

            if (!hasChanges)
            {
                // we get here if a document is closed and reopen without any actual change:
                return ProjectAnalysisSummary.NoChanges;
            }

            if (!hasSignificantValidChanges)
            {
                return ProjectAnalysisSummary.ValidInsignificantChanges;
            }

            return ProjectAnalysisSummary.ValidChanges;
        }

        private static async Task<ProjectChanges> GetProjectChangesAsync(List<(DocumentId Document, AsyncLazy<DocumentAnalysisResults> Results)> changedDocumentAnalyses, CancellationToken cancellationToken)
        {
            try
            {
                var allEdits = ArrayBuilder<SemanticEdit>.GetInstance();
                var allLineEdits = ArrayBuilder<(DocumentId, ImmutableArray<LineChange>)>.GetInstance();
                var activeStatementsInChangedDocuments = ArrayBuilder<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)>.GetInstance();

                foreach (var (documentId, asyncResult) in changedDocumentAnalyses)
                {
                    var result = await asyncResult.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    if (!result.HasSignificantValidChanges)
                    {
                        continue;
                    }

                    // we shouldn't be asking for deltas in presence of errors:
                    Debug.Assert(!result.HasChangesAndErrors);

                    allEdits.AddRange(result.SemanticEdits);
                    if (result.LineEdits.Length > 0)
                    {
                        allLineEdits.Add((documentId, result.LineEdits));
                    }

                    if (result.ActiveStatements.Length > 0)
                    {
                        activeStatementsInChangedDocuments.Add((documentId, result.ActiveStatements, result.ExceptionRegions));
                    }
                }

                return new ProjectChanges(allEdits.ToImmutableAndFree(), allLineEdits.ToImmutableAndFree(), activeStatementsInChangedDocuments.ToImmutableAndFree());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal ImmutableArray<LocationlessDiagnostic> GetDebugeeStateDiagnostics()
        {
            return ImmutableArray<LocationlessDiagnostic>.Empty;
        }

        public async Task<SolutionUpdate> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var deltas = ArrayBuilder<Deltas>.GetInstance();
            var emitBaselines = ArrayBuilder<(ProjectId, EmitBaseline)>.GetInstance();
            var moduleReaders = ArrayBuilder<IDisposable>.GetInstance();
            var diagnostics = ArrayBuilder<(ProjectId, ImmutableArray<Diagnostic>)>.GetInstance();

            try
            {
                bool isBlocked = false;

                foreach (var project in solution.Projects)
                {
                    if (!SupportsEditAndContinue(project))
                    {
                        continue;
                    }

                    var baseProject = GetBaseProject(project.Id);

                    // TODO (https://github.com/dotnet/roslyn/issues/1204):
                    // When debugging session is started some projects might not have been loaded to the workspace yet. 
                    // We capture the base solution. Edits in files that are in projects that haven't been loaded won't be applied
                    // and will result in source mismatch when the user steps into them.
                    // TODO: hook up the debugger reported error, check that the project has not been loaded and report a better error.
                    // Here, we assume these projects are not modified.
                    if (baseProject == null)
                    {
                        EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]: project not loaded", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(project.Id, cancellationToken).ConfigureAwait(false);
                    if (mvid == Guid.Empty && mvidReadError == null)
                    {
                        EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]: project not built", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var changedDocumentAnalyses = GetChangedDocumentsAnalyses(baseProject, project);
                    var projectSummary = await GetProjectAnalysisSymmaryAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);

                    if (projectSummary != ProjectAnalysisSummary.ValidChanges)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummary, ImmutableArray<string>.Empty);

                        if (projectSummary == ProjectAnalysisSummary.CompilationErrors || projectSummary == ProjectAnalysisSummary.RudeEdits)
                        {
                            isBlocked = true;
                        }

                        continue;
                    }

                    if (mvidReadError != null)
                    {
                        // The error hasn't been reported by GetDocumentDiagnosticsAsync since it might have been intermittent.
                        // The MVID is required for emit so we consider the error permanent and report it here.
                        diagnostics.Add((project.Id, ImmutableArray.Create(mvidReadError)));

                        Telemetry.LogProjectAnalysisSummary(projectSummary, ImmutableArray.Create(mvidReadError.Descriptor.Id));
                        isBlocked = true;
                        continue;
                    }

                    var moduleDiagnostics = GetModuleDiagnostics(mvid, project.Name);
                    if (!moduleDiagnostics.IsEmpty)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummary, moduleDiagnostics.SelectAsArray(d => d.Descriptor.Id));
                        isBlocked = true;
                        continue;
                    }

                    var projectChanges = await GetProjectChangesAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);
                    var currentCompilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var allAddedSymbols = await GetAllAddedSymbolsAsync(project, cancellationToken).ConfigureAwait(false);
                    var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var baseActiveExceptionRegions = await BaseActiveExceptionRegions.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var lineEdits = projectChanges.LineChanges.SelectAsArray((lineChange, p) => (p.GetDocument(lineChange.DocumentId).FilePath, lineChange.Changes), project);

                    // Dispatch to a background thread - the compiler reads symbols and ISymUnmanagedReader requires MTA thread.
                    // We also don't want to block the UI thread - emit might perform IO.
                    if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                    {
                        await Task.Factory.SafeStartNew(Emit, cancellationToken, TaskScheduler.Default).ConfigureAwait(false);
                    }
                    else
                    {
                        Emit();
                    }

                    void Emit()
                    {
                        Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "SymReader requires MTA");

                        var baseline = DebuggingSession.GetOrCreateEmitBaseline(project.Id, mvid);

                        DebugInformationReaderProvider debugInfoReaderProvider = null;
                        try
                        {
                            // The metadata blob is guaranteed to not be disposed while "continue" operation is being executed.
                            // If it is disposed it means it had been disposed when "continue" operation started.
                            if (baseline == null || baseline.OriginalMetadata.IsDisposed)
                            {
                                // If we have no baseline the module has not been loaded yet.
                                // We need to create the baseline from compiler outputs.
                                var outputs = DebuggingSession.CompilationOutputsProvider.GetCompilationOutputs(project.Id);
                                if (CreateInitialBaselineForDeferredModuleUpdate(outputs, out var createBaselineDiagnostics, out baseline, out debugInfoReaderProvider, out var metadataReaderProvider))
                                {
                                    moduleReaders.Add(metadataReaderProvider);
                                }
                                else
                                {
                                    diagnostics.Add((project.Id, createBaselineDiagnostics));
                                    Telemetry.LogProjectAnalysisSummary(projectSummary, createBaselineDiagnostics);
                                    isBlocked = true;
                                    return;
                                }
                            }

                            EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]", project.Id.DebugName, project.Id);

                            using var pdbStream = SerializableBytes.CreateWritableStream();
                            using var metadataStream = SerializableBytes.CreateWritableStream();
                            using var ilStream = SerializableBytes.CreateWritableStream();

                            var updatedMethods = ImmutableArray.CreateBuilder<MethodDefinitionHandle>();

                            var emitResult = currentCompilation.EmitDifference(
                                baseline,
                                projectChanges.SemanticEdits,
                                s => allAddedSymbols?.Contains(s) ?? false,
                                metadataStream,
                                ilStream,
                                pdbStream,
                                updatedMethods,
                                cancellationToken);

                            if (emitResult.Success)
                            {
                                var updatedMethodTokens = updatedMethods.SelectAsArray(h => MetadataTokens.GetToken(h));

                                // Determine all active statements whose span changed and exception region span deltas.
                                GetActiveStatementAndExceptionRegionSpans(
                                    mvid,
                                    baseActiveStatements,
                                    baseActiveExceptionRegions,
                                    updatedMethodTokens,
                                    _nonRemappableRegions,
                                    projectChanges.NewActiveStatements,
                                    out var activeStatementsInUpdatedMethods,
                                    out var nonRemappableRegions);

                                deltas.Add(new Deltas(
                                    mvid,
                                    ilStream.ToImmutableArray(),
                                    metadataStream.ToImmutableArray(),
                                    pdbStream.ToImmutableArray(),
                                    updatedMethodTokens,
                                    lineEdits,
                                    nonRemappableRegions,
                                    activeStatementsInUpdatedMethods));

                                emitBaselines.Add((project.Id, emitResult.Baseline));
                            }
                            else
                            {
                                // error
                                isBlocked = true;
                            }

                            // TODO: https://github.com/dotnet/roslyn/issues/36061
                            // We should only report diagnostics from emit phase.
                            // Syntax and semantic diagnostics are already reported by the diagnostic analyzer.
                            // Currently we do not have means to distinguish between diagnostics reported from compilation and emit phases.
                            // Querying diagnostics of the entire compilation or just the updated files migth be slow.
                            // In fact, it is desirable to allow emitting deltas for symbols affected by the change while allowing untouched
                            // method bodies to have errors.
                            diagnostics.Add((project.Id, emitResult.Diagnostics));
                            Telemetry.LogProjectAnalysisSummary(projectSummary, emitResult.Diagnostics);
                        }
                        finally
                        {
                            // Dispose PDB reader now. The debug information is only needed during emit and does not need to be kept alive
                            // while the baseline is. On the other hand, the metadata backing the symbols held on by the baseline needs to be.
                            debugInfoReaderProvider?.Dispose();
                        }
                    }
                }

                if (isBlocked)
                {
                    deltas.Free();
                    emitBaselines.Free();

                    foreach (var peReader in moduleReaders)
                    {
                        peReader.Dispose();
                    }

                    moduleReaders.Free();

                    return SolutionUpdate.Blocked(diagnostics.ToImmutableAndFree());
                }

                return new SolutionUpdate(
                    (deltas.Count > 0) ? SolutionUpdateStatus.Ready : SolutionUpdateStatus.None,
                    deltas.ToImmutableAndFree(),
                    moduleReaders.ToImmutableAndFree(),
                    emitBaselines.ToImmutableAndFree(),
                    diagnostics.ToImmutableAndFree());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static unsafe bool CreateInitialBaselineForDeferredModuleUpdate(
            CompilationOutputs compilationOutputs,
            out ImmutableArray<Diagnostic> diagnostics,
            out EmitBaseline baseline,
            out DebugInformationReaderProvider debugInfoReaderProvider,
            out MetadataReaderProvider metadataReaderProvider)
        {
            // Since the module has not been loaded to the debuggee the debugger does not have its metadata or symbols available yet.
            // Read the metadata and symbols from the disk. Close the files as soon as we are done emitting the delta to minimize 
            // the time when they are being locked. Since we need to use the baseline that is produced by delta emit for the subsequent
            // delta emit we need to keep the module metadata and symbol info backing the symbols of the baseline alive in memory. 
            // Alternatively, we could drop the data once we are done with emitting the delta and re-emit the baseline again 
            // when we need it next time and the module is loaded.

            diagnostics = default;
            baseline = null;
            debugInfoReaderProvider = null;
            metadataReaderProvider = null;

            bool success = false;
            string fileBeingRead = compilationOutputs.PdbDisplayPath;
            try
            {
                debugInfoReaderProvider = compilationOutputs.OpenPdb();
                if (debugInfoReaderProvider == null)
                {
                    throw new FileNotFoundException();
                }

                var debugInfoReader = debugInfoReaderProvider.CreateEditAndContinueMethodDebugInfoReader();

                fileBeingRead = compilationOutputs.AssemblyDisplayPath;

                metadataReaderProvider = compilationOutputs.OpenAssemblyMetadata(prefetch: true);
                if (metadataReaderProvider == null)
                {
                    throw new FileNotFoundException();
                }

                var metadataReader = metadataReaderProvider.GetMetadataReader();
                var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)metadataReader.MetadataPointer, metadataReader.MetadataLength);

                baseline = EmitBaseline.CreateInitialBaseline(
                    moduleMetadata,
                    debugInfoReader.GetDebugInfo,
                    debugInfoReader.GetLocalSignature,
                    debugInfoReader.IsPortable);

                success = true;
            }
            catch (Exception e)
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
                diagnostics = ImmutableArray.Create(Diagnostic.Create(descriptor, Location.None, new[] { fileBeingRead, e.Message }));
            }
            finally
            {
                if (!success)
                {
                    debugInfoReaderProvider?.Dispose();
                    metadataReaderProvider?.Dispose();
                }
            }

            return success;
        }

        // internal for testing
        internal static void GetActiveStatementAndExceptionRegionSpans(
            Guid moduleId,
            ActiveStatementsMap baseActiveStatements,
            ImmutableArray<ActiveStatementExceptionRegions> baseActiveExceptionRegions,
            ImmutableArray<int> updatedMethodTokens,
            ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<ActiveStatement> ActiveStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions)> newActiveStatementsInChangedDocuments,
            out ImmutableArray<(Guid ThreadId, ActiveInstructionId OldInstructionId, LinePositionSpan NewSpan)> activeStatementsInUpdatedMethods,
            out ImmutableArray<(ActiveMethodId Method, NonRemappableRegion Region)> nonRemappableRegions)
        {
            var changedNonRemappableSpans = PooledDictionary<(int MethodToken, int MethodVersion, LinePositionSpan BaseSpan), LinePositionSpan>.GetInstance();
            var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<(Guid, ActiveInstructionId, LinePositionSpan)>.GetInstance();
            var nonRemappableRegionsBuilder = ArrayBuilder<(ActiveMethodId Method, NonRemappableRegion Region)>.GetInstance();

            // Process active statements and their exception regions in changed documents of this project/module:
            foreach (var (documentId, newActiveStatements, newExceptionRegions) in newActiveStatementsInChangedDocuments)
            {
                var oldActiveStatements = baseActiveStatements.DocumentMap[documentId];
                Debug.Assert(oldActiveStatements.Length == newActiveStatements.Length);
                Debug.Assert(newActiveStatements.Length == newExceptionRegions.Length);

                for (var i = 0; i < newActiveStatements.Length; i++)
                {
                    var oldActiveStatement = oldActiveStatements[i];
                    var newActiveStatement = newActiveStatements[i];
                    var oldInstructionId = oldActiveStatement.InstructionId;
                    var methodToken = oldInstructionId.MethodId.Token;
                    var methodVersion = oldInstructionId.MethodId.Version;

                    var isMethodUpdated = updatedMethodTokens.Contains(methodToken);
                    if (isMethodUpdated)
                    {
                        foreach (var threadId in oldActiveStatement.ThreadIds)
                        {
                            activeStatementsInUpdatedMethodsBuilder.Add((threadId, oldInstructionId, newActiveStatement.Span));
                        }
                    }

                    void AddNonRemappableRegion(LinePositionSpan oldSpan, LinePositionSpan newSpan, bool isExceptionRegion)
                    {
                        if (oldActiveStatement.IsMethodUpToDate)
                        {
                            // Start tracking non-remappable regions for active statements in methods that were up-to-date 
                            // when break state was entered and now being updated (regardless of whether the active span changed or not).
                            if (isMethodUpdated)
                            {
                                var lineDelta = oldSpan.GetLineDelta(newSpan: newSpan);
                                nonRemappableRegionsBuilder.Add((oldInstructionId.MethodId, new NonRemappableRegion(oldSpan, lineDelta, isExceptionRegion)));
                            }

                            // If the method has been up-to-date and it is not updated now then either the active statement span has not changed,
                            // or the entire method containing it moved. In neither case do we need to start tracking non-remapable region
                            // for the active statement since movement of whole method bodies (if any) is handled only on PDB level without 
                            // triggering any remapping on the IL level.
                        }
                        else if (oldSpan != newSpan)
                        {
                            // The method is not up-to-date hence we maintain non-remapable span map for it that needs to be updated.
                            changedNonRemappableSpans[(methodToken, methodVersion, oldSpan)] = newSpan;
                        }
                    }

                    AddNonRemappableRegion(oldActiveStatement.Span, newActiveStatement.Span, isExceptionRegion: false);

                    var j = 0;
                    foreach (var oldSpan in baseActiveExceptionRegions[oldActiveStatement.Ordinal].Spans)
                    {
                        AddNonRemappableRegion(oldSpan, newExceptionRegions[oldActiveStatement.PrimaryDocumentOrdinal][j++], isExceptionRegion: true);
                    }
                }
            }

            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

            // Gather all active method instances contained in this project/module that are not up-to-date:
            var unremappedActiveMethods = PooledHashSet<ActiveMethodId>.GetInstance();
            foreach (var (instruction, baseActiveStatement) in baseActiveStatements.InstructionMap)
            {
                if (moduleId == instruction.MethodId.ModuleId && !baseActiveStatement.IsMethodUpToDate)
                {
                    unremappedActiveMethods.Add(instruction.MethodId);
                }
            }

            if (unremappedActiveMethods.Count > 0)
            {
                foreach (var (methodInstance, regionsInMethod) in previousNonRemappableRegions)
                {
                    // Skip non-remappable regions that belong to method instances that are from a different module 
                    // or no longer active (all active statements in these method instances have been remapped to newer versions).
                    if (!unremappedActiveMethods.Contains(methodInstance))
                    {
                        continue;
                    }

                    foreach (var region in regionsInMethod)
                    {
                        // We have calculated changes against a base snapshot (last break state):
                        var baseSpan = region.Span.AddLineDelta(region.LineDelta);

                        NonRemappableRegion newRegion;
                        if (changedNonRemappableSpans.TryGetValue((methodInstance.Token, methodInstance.Version, baseSpan), out var newSpan))
                        {
                            // all spans must be of the same size:
                            Debug.Assert(newSpan.End.Line - newSpan.Start.Line == baseSpan.End.Line - baseSpan.Start.Line);
                            Debug.Assert(region.Span.End.Line - region.Span.Start.Line == baseSpan.End.Line - baseSpan.Start.Line);

                            newRegion = region.WithLineDelta(region.Span.GetLineDelta(newSpan: newSpan));
                        }
                        else
                        {
                            newRegion = region;
                        }

                        nonRemappableRegionsBuilder.Add((methodInstance, newRegion));
                    }
                }
            }

            nonRemappableRegions = nonRemappableRegionsBuilder.ToImmutableAndFree();
            changedNonRemappableSpans.Free();
            unremappedActiveMethods.Free();
        }

        internal void ChangesApplied()
        {
            Debug.Assert(!_changesApplied);
            _changesApplied = true;
        }
    }
}
