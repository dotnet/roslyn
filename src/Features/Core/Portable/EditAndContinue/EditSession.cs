// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

#nullable enable

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        internal readonly DebuggingSession DebuggingSession;
        internal readonly EditSessionTelemetry Telemetry;
        internal readonly IDebuggeeModuleMetadataProvider DebugeeModuleMetadataProvider;

        private readonly ImmutableDictionary<ActiveMethodId, ImmutableArray<NonRemappableRegion>> _nonRemappableRegions;

        /// <summary>
        /// Lazily calculated map of base active statements.
        /// </summary>
        internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

        /// <summary>
        /// For each base active statement the exception regions around that statement. 
        /// </summary>
        internal ImmutableArray<ActiveStatementExceptionRegions> _lazyBaseActiveExceptionRegions;

        /// <summary>
        /// Results of changed documents analysis. 
        /// The work is triggered by an incremental analyzer on idle or explicitly when "continue" operation is executed.
        /// Contains analyses of the latest observed document versions.
        /// </summary>
        private readonly Dictionary<DocumentId, (Document Document, AsyncLazy<DocumentAnalysisResults> Results)> _analyses
            = new Dictionary<DocumentId, (Document, AsyncLazy<DocumentAnalysisResults>)>();
        private readonly object _analysesGuard = new object();

        /// <summary>
        /// A <see cref="DocumentId"/> is added whenever EnC analyzer reports 
        /// rude edits or module diagnostics. At the end of the session we ask the diagnostic analyzer to reanalyze 
        /// the documents to clean up the diagnostics.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithReportedDiagnostics = new HashSet<DocumentId>();
        private readonly object _documentsWithReportedDiagnosticsGuard = new object();

        private PendingSolutionUpdate? _pendingUpdate;
        private bool _changesApplied;

        internal EditSession(
            DebuggingSession debuggingSession,
            EditSessionTelemetry telemetry,
            ActiveStatementProvider activeStatementProvider,
            IDebuggeeModuleMetadataProvider debugeeModuleMetadataProvider)
        {
            DebuggingSession = debuggingSession;
            Telemetry = telemetry;
            DebugeeModuleMetadataProvider = debugeeModuleMetadataProvider;

            _nonRemappableRegions = debuggingSession.NonRemappableRegions;

            BaseActiveStatements = new AsyncLazy<ActiveStatementsMap>(cancellationToken => GetBaseActiveStatementsAsync(activeStatementProvider, cancellationToken), cacheResult: true);
        }

        internal PendingSolutionUpdate? Test_GetPendingSolutionUpdate() => _pendingUpdate;

        internal CancellationToken CancellationToken => _cancellationSource.Token;
        internal void Cancel() => _cancellationSource.Cancel();

        public void Dispose()
            => _cancellationSource.Dispose();

        /// <summary>
        /// Errors to be reported when a project is updated but the corresponding module does not support EnC.
        /// </summary>
        /// <returns><see langword="default"/> if the module is not loaded.</returns>
        public async Task<ImmutableArray<Diagnostic>?> GetModuleDiagnosticsAsync(Guid mvid, string projectDisplayName, CancellationToken cancellationToken)
        {
            var availability = await DebugeeModuleMetadataProvider.GetEncAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
            if (availability == null)
            {
                return null;
            }

            var (errorCode, localizedMessage) = availability.Value;
            if (errorCode == 0)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var descriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(errorCode);
            return ImmutableArray.Create(Diagnostic.Create(descriptor, Location.None, new[] { projectDisplayName, localizedMessage }));
        }

        private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(ActiveStatementProvider activeStatementProvider, CancellationToken cancellationToken)
        {
            try
            {
                // Last committed solution reflects the state of the source that is in sync with the binaries that are loaded in the debuggee.
                return CreateActiveStatementsMap(await activeStatementProvider(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return new ActiveStatementsMap(
                    SpecializedCollections.EmptyReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>>(),
                    SpecializedCollections.EmptyReadOnlyDictionary<ActiveInstructionId, ActiveStatement>());
            }
        }

        private ActiveStatementsMap CreateActiveStatementsMap(ImmutableArray<ActiveStatementDebugInfo> debugInfos)
        {
            var byDocument = PooledDictionary<DocumentId, ArrayBuilder<ActiveStatement>>.GetInstance();
            var byInstruction = PooledDictionary<ActiveInstructionId, ActiveStatement>.GetInstance();

            bool supportsEditAndContinue(DocumentId documentId)
                => EditAndContinueWorkspaceService.SupportsEditAndContinue(DebuggingSession.LastCommittedSolution.GetProject(documentId.ProjectId)!);

            foreach (var debugInfo in debugInfos)
            {
                var documentName = debugInfo.DocumentNameOpt;
                if (documentName == null)
                {
                    // Ignore active statements that do not have a source location.
                    continue;
                }

                var documentIds = DebuggingSession.LastCommittedSolution.GetDocumentIdsWithFilePath(documentName);
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
                    throw new InvalidOperationException($"Multiple active statements with the same instruction id returned by Active Statement Provider");
                }
            }

            return new ActiveStatementsMap(byDocument.ToMultiDictionaryAndFree(), byInstruction.ToDictionaryAndFree());
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

        /// <summary>
        /// Calculates exception regions for all active statements.
        /// If an active statement is in a document that's out-of-sync returns default(<see cref="ActiveStatementExceptionRegions"/>) for that statement.
        /// </summary>
        internal async Task<ImmutableArray<ActiveStatementExceptionRegions>> GetBaseActiveExceptionRegionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!_lazyBaseActiveExceptionRegions.IsDefault)
                {
                    return _lazyBaseActiveExceptionRegions;
                }

                var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                var instructionMap = baseActiveStatements.InstructionMap;
                using var builderDisposer = ArrayBuilder<ActiveStatementExceptionRegions>.GetInstance(instructionMap.Count, out var builder);
                builder.Count = instructionMap.Count;

                var hasOutOfSyncDocuments = false;

                foreach (var activeStatement in instructionMap.Values)
                {
                    bool isCovered;
                    ImmutableArray<LinePositionSpan> exceptionRegions;

                    // Can't calculate exception regions for active statements in out-of-sync documents.
                    var (document, _) = await DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(activeStatement.PrimaryDocumentId, cancellationToken).ConfigureAwait(false);
                    if (document != null)
                    {
                        Debug.Assert(document.SupportsSyntaxTree);

                        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        Contract.ThrowIfNull(syntaxRoot);

                        // The analyzer service have to be available as we only track active statements in projects that support EnC.
                        var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();
                        exceptionRegions = analyzer.GetExceptionRegions(sourceText, syntaxRoot, activeStatement.Span, activeStatement.IsNonLeaf, out isCovered);
                    }
                    else
                    {
                        // Document is either out-of-sync, design-time-only or missing from the baseline.
                        // If it's missing or design-time-only it can't have active statements.
                        hasOutOfSyncDocuments = true;
                        isCovered = false;
                        exceptionRegions = default;
                    }

                    builder[activeStatement.Ordinal] = new ActiveStatementExceptionRegions(exceptionRegions, isCovered);
                }

                var result = builder.ToImmutable();

                // Only cache results if no active statements are in out-of-sync documents.
                if (!hasOutOfSyncDocuments)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyBaseActiveExceptionRegions, result);
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return ImmutableArray<ActiveStatementExceptionRegions>.Empty;
            }
        }

        private static async Task PopulateChangedAndAddedDocumentsAsync(CommittedSolution baseSolution, Project project, ArrayBuilder<Document> changedDocuments, ArrayBuilder<Document> addedDocuments, CancellationToken cancellationToken)
        {
            changedDocuments.Clear();
            addedDocuments.Clear();

            if (!EditAndContinueWorkspaceService.SupportsEditAndContinue(project))
            {
                return;
            }

            var baseProject = baseSolution.GetProject(project.Id);
            if (baseProject == project)
            {
                return;
            }

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
                return;
            }

            var changes = project.GetChanges(baseProject);
            foreach (var documentId in changes.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
            {
                var document = project.GetDocument(documentId)!;
                if (document.State.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                // Check if the currently observed document content has changed compared to the base document content.
                // This is an important optimization that aims to avoid IO while stepping in sources that have not changed.
                //
                // We may be comparing out-of-date committed document content but we only make a decision based on that content
                // if it matches the current content. If the current content is equal to baseline content that does not match
                // the debuggee then the workspace has not observed the change made to the file on disk since baseline was captured
                // (there had to be one as the content doesn't match). When we are about to apply changes it is ok to ignore this
                // document because the user does not see the change yet in the buffer (if the doc is open) and won't be confused
                // if it is not applied yet. The change will be applied later after it's observed by the workspace.
                var baseSource = await baseProject.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var source = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (baseSource.ContentEquals(source))
                {
                    continue;
                }

                changedDocuments.Add(document);
            }

            foreach (var documentId in changes.GetAddedDocuments())
            {
                var document = project.GetDocument(documentId)!;
                if (document.State.Attributes.DesignTimeOnly)
                {
                    continue;
                }

                addedDocuments.Add(document);
            }
        }

        private async Task<(ImmutableArray<(Document Document, AsyncLazy<DocumentAnalysisResults> Results)>, ImmutableArray<Diagnostic> DocumentDiagnostics)> AnalyzeDocumentsAsync(
            ArrayBuilder<Document> changedDocuments, ArrayBuilder<Document> addedDocuments, SolutionActiveStatementSpanProvider newDocumentActiveStatementSpanProvider, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<Diagnostic>.GetInstance(out var documentDiagnostics);
            using var _2 = ArrayBuilder<(Document? Old, Document New, ImmutableArray<TextSpan> NewActiveStatementSpans)>.GetInstance(out var builder);

            foreach (var document in changedDocuments)
            {
                var (oldDocument, oldDocumentState) = await DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(document.Id, cancellationToken, reloadOutOfSyncDocument: true).ConfigureAwait(false);
                switch (oldDocumentState)
                {
                    case CommittedSolution.DocumentState.DesignTimeOnly:
                        continue;

                    case CommittedSolution.DocumentState.Indeterminate:
                    case CommittedSolution.DocumentState.OutOfSync:
                        var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor((oldDocumentState == CommittedSolution.DocumentState.Indeterminate) ?
                            EditAndContinueErrorCode.UnableToReadSourceFileOrPdb : EditAndContinueErrorCode.DocumentIsOutOfSyncWithDebuggee);
                        documentDiagnostics.Add(Diagnostic.Create(descriptor, Location.Create(document.FilePath!, textSpan: default, lineSpan: default), new[] { document.FilePath }));
                        continue;

                    case CommittedSolution.DocumentState.MatchesBuildOutput:
                        // Include the document regardless of whether the module it was built into has been loaded or not.
                        // If the module has been built it might get loaded later during the debugging session,
                        // at which point we apply all changes that have been made to the project so far.

                        // Fetch the active statement spans for the new document snapshot.
                        // These are the locations of the spans tracked by the editor from the base document to the current snapshot.
                        var activeStatementSpans = await newDocumentActiveStatementSpanProvider(document.Id, cancellationToken).ConfigureAwait(false);

                        builder.Add((oldDocument, document, activeStatementSpans));
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(oldDocumentState);
                }
            }

            foreach (var document in addedDocuments)
            {
                // No existing active statements are located in newly added documents.
                builder.Add((null, document, ImmutableArray<TextSpan>.Empty));
            }

            var result = ImmutableArray<(Document, AsyncLazy<DocumentAnalysisResults>)>.Empty;
            if (builder.Count != 0)
            {
                lock (_analysesGuard)
                {
                    result = builder.SelectAsArray(change => (change.New, GetDocumentAnalysisNoLock(change.Old, change.New, change.NewActiveStatementSpans)));
                }
            }

            return (result, documentDiagnostics.ToImmutable());
        }

        public AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysis(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans)
        {
            lock (_analysesGuard)
            {
                return GetDocumentAnalysisNoLock(baseDocument, document, activeStatementSpans);
            }
        }

        /// <summary>
        /// Returns a document analysis or kicks off a new one if one is not available for the specified document snapshot.
        /// </summary>
        /// <param name="baseDocument">Base document or null if the document did not exist in the baseline.</param>
        /// <param name="document">Document snapshot to analyze.</param>
        private AsyncLazy<DocumentAnalysisResults> GetDocumentAnalysisNoLock(Document? baseDocument, Document document, ImmutableArray<TextSpan> activeStatementSpans)
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

                        return await analyzer.AnalyzeDocumentAsync(baseDocument, documentBaseActiveStatements, document, activeStatementSpans, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                },
                cacheResult: true);

            // TODO: this will replace potentially running analysis with another one.
            // Consider cancelling the replaced one.
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

        /// <summary>
        /// Determines whether projects contain any changes that might need to be applied.
        /// Checks only projects containing a given <paramref name="sourceFilePath"/> or all projects of the solution if <paramref name="sourceFilePath"/> is null.
        /// Invoked by the debugger on every step. It is critical for stepping performance that this method returns as fast as possible in absence of changes.
        /// </summary>
        public async Task<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider solutionActiveStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                if (_changesApplied)
                {
                    return false;
                }

                var baseSolution = DebuggingSession.LastCommittedSolution;
                if (baseSolution.HasNoChanges(solution))
                {
                    return false;
                }

                var projects = (sourceFilePath == null) ? solution.Projects :
                    from documentId in solution.GetDocumentIdsWithFilePath(sourceFilePath)
                    select solution.GetDocument(documentId)!.Project;

                using var changedDocumentsDisposer = ArrayBuilder<Document>.GetInstance(out var changedDocuments);
                using var addedDocumentsDisposer = ArrayBuilder<Document>.GetInstance(out var addedDocuments);

                foreach (var project in projects)
                {
                    await PopulateChangedAndAddedDocumentsAsync(baseSolution, project, changedDocuments, addedDocuments, cancellationToken).ConfigureAwait(false);
                    if (changedDocuments.IsEmpty() && addedDocuments.IsEmpty())
                    {
                        continue;
                    }

                    // Check MVID before analyzing documents as the analysis needs to read the PDB which will likely fail if we can't even read the MVID.
                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                    if (mvidReadError != null)
                    {
                        // Can't read MVID. This might be an intermittent failure, so don't report it here.
                        // Report the project as containing changes, so that we proceed to EmitSolutionUpdateAsync where we report the error if it still persists.
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not built", project.Id.DebugName, project.Id);
                        return true;
                    }

                    if (mvid == Guid.Empty)
                    {
                        // Project not built. We ignore any changes made in its sources.
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: project not built", project.Id.DebugName, project.Id);
                        continue;
                    }

                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(changedDocuments, addedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    if (documentDiagnostics.Any())
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: out-of-sync documents present (diagnostic: '{2}')",
                            project.Id.DebugName, project.Id, documentDiagnostics[0]);

                        // Although we do not apply changes in out-of-sync/indeterminate documents we report that changes are present,
                        // so that the debugger triggers emit of updates. There we check if these documents are still in a bad state and report warnings
                        // that any changes in such documents are not applied.
                        return true;
                    }

                    var projectSummary = await GetProjectAnalysisSymmaryAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);
                    if (projectSummary != ProjectAnalysisSummary.NoChanges)
                    {
                        EditAndContinueWorkspaceService.Log.Write("EnC state of '{0}' [0x{1:X8}] queried: {2}", project.Id.DebugName, project.Id, projectSummary);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<ProjectAnalysisSummary> GetProjectAnalysisSymmaryAsync(
            ImmutableArray<(Document Document, AsyncLazy<DocumentAnalysisResults> Results)> documentAnalyses,
            CancellationToken cancellationToken)
        {
            var hasChanges = false;
            var hasSignificantValidChanges = false;

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

        private static async Task<ProjectChanges> GetProjectChangesAsync(ImmutableArray<(Document Document, AsyncLazy<DocumentAnalysisResults> Results)> changedDocumentAnalyses, CancellationToken cancellationToken)
        {
            try
            {
                var allEdits = ArrayBuilder<SemanticEdit>.GetInstance();
                var allLineEdits = ArrayBuilder<(DocumentId, ImmutableArray<LineChange>)>.GetInstance();
                var activeStatementsInChangedDocuments = ArrayBuilder<(DocumentId, ImmutableArray<ActiveStatement>, ImmutableArray<ImmutableArray<LinePositionSpan>>)>.GetInstance();
                using var _ = ArrayBuilder<ISymbol>.GetInstance(out var allAddedSymbols);

                foreach (var (document, asyncResult) in changedDocumentAnalyses)
                {
                    var result = await asyncResult.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    if (!result.HasSignificantValidChanges)
                    {
                        continue;
                    }

                    // we shouldn't be asking for deltas in presence of errors:
                    Debug.Assert(!result.HasChangesAndErrors);

                    allEdits.AddRange(result.SemanticEdits);

                    if (!result.HasChangesAndErrors)
                    {
                        foreach (var edit in result.SemanticEdits)
                        {
                            if (edit.Kind == SemanticEditKind.Insert)
                            {
                                allAddedSymbols.Add(edit.NewSymbol!);
                            }
                        }
                    }

                    if (result.LineEdits.Length > 0)
                    {
                        allLineEdits.Add((document.Id, result.LineEdits));
                    }

                    if (result.ActiveStatements.Length > 0)
                    {
                        activeStatementsInChangedDocuments.Add((document.Id, result.ActiveStatements, result.ExceptionRegions));
                    }
                }

                var allAddedSymbolResult = allAddedSymbols.ToImmutableHashSet();

                return new ProjectChanges(
                    allEdits.ToImmutableAndFree(),
                    allLineEdits.ToImmutableAndFree(),
                    allAddedSymbolResult,
                    activeStatementsInChangedDocuments.ToImmutableAndFree());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task<SolutionUpdate> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider solutionActiveStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                using var deltasDisposer = ArrayBuilder<Deltas>.GetInstance(out var deltas);
                using var emitBaselinesDisposer = ArrayBuilder<(ProjectId, EmitBaseline)>.GetInstance(out var emitBaselines);
                using var readersDisposer = ArrayBuilder<IDisposable>.GetInstance(out var readers);
                using var diagnosticsDisposer = ArrayBuilder<(ProjectId, ImmutableArray<Diagnostic>)>.GetInstance(out var diagnostics);
                using var changedDocumentsDisposer = ArrayBuilder<Document>.GetInstance(out var changedDocuments);
                using var addedDocumentsDisposer = ArrayBuilder<Document>.GetInstance(out var addedDocuments);

                var baseSolution = DebuggingSession.LastCommittedSolution;

                var isBlocked = false;
                foreach (var project in solution.Projects)
                {
                    await PopulateChangedAndAddedDocumentsAsync(baseSolution, project, changedDocuments, addedDocuments, cancellationToken).ConfigureAwait(false);
                    if (changedDocuments.IsEmpty() && addedDocuments.IsEmpty())
                    {
                        continue;
                    }

                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                    if (mvidReadError != null)
                    {
                        // The error hasn't been reported by GetDocumentDiagnosticsAsync since it might have been intermittent.
                        // The MVID is required for emit so we consider the error permanent and report it here.
                        // Bail before analyzing documents as the analysis needs to read the PDB which will likely fail if we can't even read the MVID.
                        diagnostics.Add((project.Id, ImmutableArray.Create(mvidReadError)));

                        Telemetry.LogProjectAnalysisSummary(ProjectAnalysisSummary.ValidChanges, ImmutableArray.Create(mvidReadError.Descriptor.Id));
                        isBlocked = true;
                        continue;
                    }

                    if (mvid == Guid.Empty)
                    {
                        EditAndContinueWorkspaceService.Log.Write("Emitting update of '{0}' [0x{1:X8}]: project not built", project.Id.DebugName, project.Id);
                        continue;
                    }

                    // Ensure that all changed documents are in-sync. Once a document is in-sync it can't get out-of-sync.
                    // Therefore, results of further computations based on base snapshots of changed documents can't be invalidated by 
                    // incoming events updating the content of out-of-sync documents.
                    // 
                    // If in past we concluded that a document is out-of-sync, attempt to check one more time before we block apply.
                    // The source file content might have been updated since the last time we checked.
                    //
                    // TODO (investigate): https://github.com/dotnet/roslyn/issues/38866
                    // It is possible that the result of Rude Edit semantic analysis of an unchanged document will change if there
                    // another document is updated. If we encounter a significant case of this we should consider caching such a result per project,
                    // rather then per document. Also, we might be observing an older semantics if the document that is causing the change is out-of-sync --
                    // e.g. the binary was built with an overload C.M(object), but a generator updated class C to also contain C.M(string),
                    // which change we have not observed yet. Then call-sites of C.M in a changed document observed by the analysis will be seen as C.M(object) 
                    // instead of the true C.M(string).
                    var (changedDocumentAnalyses, documentDiagnostics) = await AnalyzeDocumentsAsync(changedDocuments, addedDocuments, solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    if (documentDiagnostics.Any())
                    {
                        // The diagnostic hasn't been reported by GetDocumentDiagnosticsAsync since out-of-sync documents are likely to be synchronized
                        // before the changes are attempted to be applied. If we still have any out-of-sync documents we report warnings and ignore changes in them.
                        // If in future the file is updated so that its content matches the PDB checksum, the document transitions to a matching state, 
                        // and we consider any further changes to it for application.
                        diagnostics.Add((project.Id, documentDiagnostics));
                    }

                    // The capability of a module to apply edits may change during edit session if the user attaches debugger to 
                    // an additional process that doesn't support EnC (or detaches from such process). Before we apply edits 
                    // we need to check with the debugger.
                    var (moduleDiagnostics, isModuleLoaded) = await GetModuleDiagnosticsAsync(mvid, project.Name, cancellationToken).ConfigureAwait(false);

                    var isModuleEncBlocked = isModuleLoaded && !moduleDiagnostics.IsEmpty;
                    if (isModuleEncBlocked)
                    {
                        diagnostics.Add((project.Id, moduleDiagnostics));
                        isBlocked = true;
                    }

                    var projectSummary = await GetProjectAnalysisSymmaryAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);
                    if (projectSummary == ProjectAnalysisSummary.CompilationErrors || projectSummary == ProjectAnalysisSummary.RudeEdits)
                    {
                        isBlocked = true;
                    }

                    if (isModuleEncBlocked || projectSummary != ProjectAnalysisSummary.ValidChanges)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummary, moduleDiagnostics.NullToEmpty().SelectAsArray(d => d.Descriptor.Id));
                        continue;
                    }

                    var projectChanges = await GetProjectChangesAsync(changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);
                    var currentCompilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var baseActiveStatements = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // project must support compilations since it supports EnC
                    Contract.ThrowIfNull(currentCompilation);

                    // Exception regions of active statements in changed documents are calculated (non-default),
                    // since we already checked that no changed document is out-of-sync above.
                    var baseActiveExceptionRegions = await GetBaseActiveExceptionRegionsAsync(cancellationToken).ConfigureAwait(false);

                    var lineEdits = projectChanges.LineChanges.SelectAsArray((lineChange, p) => (p.GetDocument(lineChange.DocumentId)!.FilePath, lineChange.Changes), project);

                    // Dispatch to a background thread - the compiler reads symbols and ISymUnmanagedReader requires MTA thread.
                    // We also don't want to block the UI thread - emit might perform IO.
                    if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                Emit();
                            }
                            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
                            {
                                throw ExceptionUtilities.Unreachable;
                            }
                        }, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default).ConfigureAwait(false);
                    }
                    else
                    {
                        Emit();
                    }

                    void Emit()
                    {
                        Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA, "SymReader requires MTA");

                        // TODO: Use moduleLoaded to determine whether or not to create an initial baseline, once we move OOP.
                        var baseline = DebuggingSession.GetOrCreateEmitBaseline(project.Id, mvid, DebugeeModuleMetadataProvider);

                        // The metadata blob is guaranteed to not be disposed while "continue" operation is being executed.
                        // If it is disposed it means it had been disposed when "continue" operation started.
                        if (baseline == null || baseline.OriginalMetadata.IsDisposed)
                        {
                            // If we have no baseline the module has not been loaded yet.
                            // We need to create the baseline from compiler outputs.
                            var outputs = DebuggingSession.GetCompilationOutputs(project);
                            if (CreateInitialBaselineForDeferredModuleUpdate(outputs, out var createBaselineDiagnostics, out baseline, out var debugInfoReaderProvider, out var metadataReaderProvider))
                            {
                                readers.Add(metadataReaderProvider);
                                readers.Add(debugInfoReaderProvider);
                            }
                            else
                            {
                                // Report diagnosics even when the module is never going to be loaded (e.g. in multi-targeting scenario, where only one framework being debugged).
                                // This is consistent with reporting compilation errors - the IDE reports them for all TFMs regardless of what framework the app is running on.
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
                            projectChanges.AddedSymbols.Contains,
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
                }

                if (isBlocked)
                {
                    foreach (var reader in readers)
                    {
                        reader.Dispose();
                    }

                    return SolutionUpdate.Blocked(diagnostics.ToImmutable());
                }

                return new SolutionUpdate(
                    (deltas.Count > 0) ? SolutionUpdateStatus.Ready : SolutionUpdateStatus.None,
                    deltas.ToImmutable(),
                    readers.ToImmutable(),
                    emitBaselines.ToImmutable(),
                    diagnostics.ToImmutable());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static unsafe bool CreateInitialBaselineForDeferredModuleUpdate(
            CompilationOutputs compilationOutputs,
            out ImmutableArray<Diagnostic> diagnostics,
            [NotNullWhen(true)] out EmitBaseline? baseline,
            [NotNullWhen(true)] out DebugInformationReaderProvider? debugInfoReaderProvider,
            [NotNullWhen(true)] out MetadataReaderProvider? metadataReaderProvider)
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

            var success = false;
            var fileBeingRead = compilationOutputs.PdbDisplayPath;
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
                return true;
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

            return false;
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
            using var _1 = PooledDictionary<(int MethodToken, int MethodVersion, LinePositionSpan BaseSpan), LinePositionSpan>.GetInstance(out var changedNonRemappableSpans);
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

                    // The spans of the exception regions are known (non-default) for active statements in changed documents
                    // as we ensured earlier that all changed documents are in-sync. The outer loop only enumerates active 
                    // statements of changed documents, so the corresponding exception regions are initialized.

                    var j = 0;
                    foreach (var oldSpan in baseActiveExceptionRegions[oldActiveStatement.Ordinal].Spans)
                    {
                        AddNonRemappableRegion(oldSpan, newExceptionRegions[oldActiveStatement.PrimaryDocumentOrdinal][j++], isExceptionRegion: true);
                    }
                }
            }

            activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

            // Gather all active method instances contained in this project/module that are not up-to-date:
            using var _2 = PooledHashSet<ActiveMethodId>.GetInstance(out var unremappedActiveMethods);
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
        }

        internal void StorePendingUpdate(Solution solution, SolutionUpdate update)
        {
            var previousPendingUpdate = Interlocked.Exchange(ref _pendingUpdate, new PendingSolutionUpdate(
                solution,
                update.EmitBaselines,
                update.Deltas,
                update.ModuleReaders));

            // commit/discard was not called:
            Contract.ThrowIfFalse(previousPendingUpdate == null);
        }

        internal PendingSolutionUpdate RetrievePendingUpdate()
        {
            var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
            Contract.ThrowIfNull(pendingUpdate);
            return pendingUpdate;
        }

        internal void ChangesApplied()
        {
            Debug.Assert(!_changesApplied);
            _changesApplied = true;
        }
    }
}
