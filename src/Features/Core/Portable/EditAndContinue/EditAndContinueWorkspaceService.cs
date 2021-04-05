// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    internal sealed class EditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService)), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
                => new EditAndContinueWorkspaceService();
        }

        internal static readonly TraceLog Log = new(2048, "EnC");

        private readonly EditSessionTelemetry _editSessionTelemetry;
        private readonly DebuggingSessionTelemetry _debuggingSessionTelemetry;
        private readonly Func<Project, CompilationOutputs> _compilationOutputsProvider;
        private readonly Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

        private DebuggingSession? _debuggingSession;
        private EditSession? _editSession;

        internal EditAndContinueWorkspaceService(
            Func<Project, CompilationOutputs>? testCompilationOutputsProvider = null,
            Action<DebuggingSessionTelemetry.Data>? testReportTelemetry = null)
        {
            _debuggingSessionTelemetry = new DebuggingSessionTelemetry();
            _editSessionTelemetry = new EditSessionTelemetry();
            _compilationOutputsProvider = testCompilationOutputsProvider ?? GetCompilationOutputs;
            _reportTelemetry = testReportTelemetry ?? ReportTelemetry;
        }

        // test only:
        internal DebuggingSession? Test_GetDebuggingSession() => _debuggingSession;
        internal EditSession? Test_GetEditSession() => _editSession;

        private static CompilationOutputs GetCompilationOutputs(Project project)
        {
            // The Project System doesn't always indicate whether we emit PDB, what kind of PDB we emit nor the path of the PDB.
            // To work around we look for the PDB on the path specified in the PDB debug directory.
            // https://github.com/dotnet/roslyn/issues/35065
            return new CompilationOutputFilesWithImplicitPdbPath(project.CompilationOutputInfo.AssemblyPath);
        }

        public async ValueTask OnSourceFileUpdatedAsync(Document document, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession != null)
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(debuggingSession.CancellationToken, cancellationToken);
                await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(document, linkedTokenSource.Token).ConfigureAwait(false);
            }
        }

        public async ValueTask StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            var initialDocumentStates =
                captureMatchingDocuments ? await CommittedSolution.GetMatchingDocumentsAsync(solution, _compilationOutputsProvider, cancellationToken).ConfigureAwait(false) :
                SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>();

            var previousSession = Interlocked.CompareExchange(ref _debuggingSession, new DebuggingSession(solution, debuggerService, _compilationOutputsProvider, initialDocumentStates), null);
            Contract.ThrowIfFalse(previousSession == null, "New debugging session can't be started until the existing one has ended.");

            StartEditSession(inBreakState: false);
        }

        private void StartEditSession(bool inBreakState)
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession, "Edit session can only be started during debugging session");

            var newSession = new EditSession(debuggingSession, _editSessionTelemetry, inBreakState);

            var previousSession = Interlocked.CompareExchange(ref _editSession, newSession, null);
            Contract.ThrowIfFalse(previousSession == null, "New edit session can't be started until the existing one has ended.");
        }

        private void EndEditSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            // first, publish null session:
            var session = Interlocked.Exchange(ref _editSession, null);
            Contract.ThrowIfNull(session, "Edit session has not started.");

            // then cancel all ongoing work bound to the session:
            session.Cancel();

            // clear all reported rude edits:
            documentsToReanalyze = session.GetDocumentsWithReportedDiagnostics();

            // TODO: report a separate telemetry data for hot reload sessions to preserve the semantics of the current telemetry data
            if (session.InBreakState)
            {
                _debuggingSessionTelemetry.LogEditSession(_editSessionTelemetry.GetDataAndClear());
            }

            session.Dispose();
        }

        public void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            EndEditSession(out documentsToReanalyze);

            var debuggingSession = Interlocked.Exchange(ref _debuggingSession, null);
            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            // cancel all ongoing work bound to the session:
            debuggingSession.Cancel();

            _reportTelemetry(_debuggingSessionTelemetry.GetDataAndClear());

            debuggingSession.Dispose();
        }

        public void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            // Document analyses must be recalculated to account for active statements.
            EndEditSession(out documentsToReanalyze);
            StartEditSession(inBreakState: true);
        }

        internal static bool SupportsEditAndContinue(Project project)
            => project.LanguageServices.GetService<IEditAndContinueAnalyzer>() != null;

        // Note: source generated files have relative paths: https://github.com/dotnet/roslyn/issues/51998
        internal static bool SupportsEditAndContinue(DocumentState documentState)
            => !documentState.Attributes.DesignTimeOnly && documentState.SupportsSyntaxTree &&
               (PathUtilities.IsAbsolute(documentState.FilePath) || documentState is SourceGeneratedDocumentState);

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            try
            {
                var debuggingSession = _debuggingSession;
                if (debuggingSession == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Not a C# or VB project.
                var project = document.Project;
                if (!SupportsEditAndContinue(project))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Document does not compile to the assembly (e.g. cshtml files, .g.cs files generated for completion only)
                if (!SupportsEditAndContinue(document.DocumentState))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Do not analyze documents (and report diagnostics) of projects that have not been built.
                // Allow user to make any changes in these documents, they won't be applied within the current debugging session.
                // Do not report the file read error - it might be an intermittent issue. The error will be reported when the
                // change is attempted to be applied.
                var (mvid, _) = await debuggingSession.GetProjectModuleIdAsync(project, cancellationToken).ConfigureAwait(false);
                if (mvid == Guid.Empty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var (oldDocument, oldDocumentState) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(document.Id, document, cancellationToken).ConfigureAwait(false);
                if (oldDocumentState == CommittedSolution.DocumentState.OutOfSync ||
                    oldDocumentState == CommittedSolution.DocumentState.Indeterminate ||
                    oldDocumentState == CommittedSolution.DocumentState.DesignTimeOnly)
                {
                    // Do not report diagnostics for existing out-of-sync documents or design-time-only documents.
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var editSession = _editSession;
                Contract.ThrowIfNull(editSession);

                var oldProject = oldDocument?.Project ?? debuggingSession.LastCommittedSolution.GetProject(project.Id);
                if (oldProject == null)
                {
                    // TODO https://github.com/dotnet/roslyn/issues/1204:
                    // Project deleted (shouldn't happen since Project System does not allow removing projects while debugging) or
                    // was not loaded when the debugging session started.
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var documentActiveStatementSpans = await activeStatementSpanProvider(cancellationToken).ConfigureAwait(false);
                var analysis = await editSession.Analyses.GetDocumentAnalysisAsync(oldProject, document, documentActiveStatementSpans, cancellationToken).ConfigureAwait(false);
                if (analysis.HasChanges)
                {
                    // Once we detected a change in a document let the debugger know that the corresponding loaded module
                    // is about to be updated, so that it can start initializing it for EnC update, reducing the amount of time applying
                    // the change blocks the UI when the user "continues".
                    if (debuggingSession.AddModulePreparedForUpdate(mvid))
                    {
                        // fire and forget:
                        _ = Task.Run(() => debuggingSession.DebuggerService.PrepareModuleForUpdateAsync(mvid, cancellationToken), cancellationToken);
                    }
                }

                if (analysis.RudeEditErrors.IsEmpty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                editSession.Telemetry.LogRudeEditDiagnostics(analysis.RudeEditErrors);

                // track the document, so that we can refresh or clean diagnostics at the end of edit session:
                editSession.TrackDocumentWithReportedDiagnostics(document.Id);

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return analysis.RudeEditErrors.SelectAsArray((e, t) => e.ToDiagnostic(t), tree);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        /// <summary>
        /// Determine whether the updates made to projects containing the specified file (or all projects that are built,
        /// if <paramref name="sourceFilePath"/> is null) are ready to be applied and the debugger should attempt to apply
        /// them on "continue".
        /// </summary>
        /// <returns>
        /// Returns <see cref="ManagedModuleUpdateStatus.Blocked"/> if there are rude edits or other errors
        /// that block the application of the updates. Might return <see cref="ManagedModuleUpdateStatus.Ready"/> even if there are
        /// errors in the code that will block the application of the updates. E.g. emit diagnostics can't be determined until
        /// emit is actually performed. Therefore, this method only serves as an optimization to avoid unnecessary emit attempts,
        /// but does not provide a definitive answer. Only <see cref="EmitSolutionUpdateAsync"/> can definitively determine whether
        /// the update is valid or not.
        /// </returns>
        public ValueTask<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider solutionActiveStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            // GetStatusAsync is called outside of edit session when the debugger is determining
            // whether a source file checksum matches the one in PDB.
            // The debugger expects no changes in this case.
            var editSession = _editSession;
            if (editSession == null)
            {
                return default;
            }

            return editSession.HasChangesAsync(solution, solutionActiveStatementSpanProvider, sourceFilePath, cancellationToken);
        }

        public async ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
            Solution solution,
            SolutionActiveStatementSpanProvider activeStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            var editSession = _editSession;
            if (editSession == null)
            {
                return EmitSolutionUpdateResults.Empty;
            }

            var solutionUpdate = await editSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            if (solutionUpdate.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                editSession.StorePendingUpdate(solution, solutionUpdate);
            }

            // Note that we may return empty deltas if all updates have been deferred.
            // The debugger will still call commit or discard on the update batch.
            return new EmitSolutionUpdateResults(solutionUpdate.ModuleUpdates, solutionUpdate.Diagnostics, solutionUpdate.DocumentsWithRudeEdits);
        }

        public void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var editSession = _editSession;
            Contract.ThrowIfNull(editSession);

            var pendingUpdate = editSession.RetrievePendingUpdate();
            editSession.DebuggingSession.CommitSolutionUpdate(pendingUpdate);

            // restart edit session with no active statements (switching to run mode):
            EndEditSession(out documentsToReanalyze);
            StartEditSession(inBreakState: false);
        }

        public void DiscardSolutionUpdate()
        {
            var editSession = _editSession;
            Contract.ThrowIfNull(editSession);

            _ = editSession.RetrievePendingUpdate();
        }

        public async ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var editSession = _editSession;
            if (editSession == null || !editSession.InBreakState)
            {
                return default;
            }

            var lastCommittedSolution = editSession.DebuggingSession.LastCommittedSolution;
            var baseActiveStatements = await editSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>.GetInstance(out var spans);

            foreach (var documentId in documentIds)
            {
                if (baseActiveStatements.DocumentMap.TryGetValue(documentId, out var documentBaseActiveStatements))
                {
                    var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                    var (baseDocument, _) = await lastCommittedSolution.GetDocumentAndStateAsync(documentId, document, cancellationToken).ConfigureAwait(false);
                    if (baseDocument != null && document != null)
                    {
                        // Calculate diff with no prior knowledge of active statement spans.
                        // Translates active statements in the last committed solution snapshot to the current solution.
                        // Do not cache this result, it's only needed when entering a break mode.
                        // Optimize for the common case of the identical document.
                        if (baseDocument == document)
                        {
                            spans.Add(documentBaseActiveStatements.SelectAsArray(s => (s.Span, s.Flags)));
                            continue;
                        }

                        var analyzer = document.Project.LanguageServices.GetRequiredService<IEditAndContinueAnalyzer>();

                        var analysis = await analyzer.AnalyzeDocumentAsync(
                            baseDocument.Project,
                            documentBaseActiveStatements,
                            document,
                            newActiveStatementSpans: ImmutableArray<TextSpan>.Empty,
                            cancellationToken).ConfigureAwait(false);

                        if (!analysis.ActiveStatements.IsDefault)
                        {
                            spans.Add(analysis.ActiveStatements.SelectAsArray(s => (s.Span, s.Flags)));
                            continue;
                        }
                    }
                }

                // Document contains no active statements, or the document is not C#/VB document,
                // it has been added, is out-of-sync or a design-time-only document.
                spans.Add(ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>.Empty);
            }

            return spans.ToImmutable();
        }

        public async ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var editSession = _editSession;
            if (editSession == null || !editSession.InBreakState)
            {
                return default;
            }

            if (!SupportsEditAndContinue(document.Project))
            {
                return default;
            }

            var lastCommittedSolution = editSession.DebuggingSession.LastCommittedSolution;
            var (baseDocument, _) = await lastCommittedSolution.GetDocumentAndStateAsync(document.Id, document, cancellationToken).ConfigureAwait(false);
            if (baseDocument == null)
            {
                return default;
            }

            var documentActiveStatementSpans = await activeStatementSpanProvider(cancellationToken).ConfigureAwait(false);
            var activeStatements = await editSession.Analyses.GetActiveStatementsAsync(baseDocument, document, documentActiveStatementSpans, cancellationToken).ConfigureAwait(false);
            if (activeStatements.IsDefault)
            {
                return default;
            }

            return activeStatements.SelectAsArray(s => (s.Span, s.Flags));
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                // It is allowed to call this method before entering or after exiting break mode. In fact, the VS debugger does so.
                // We return null since there the concept of active statement only makes sense during break mode.
                var editSession = _editSession;
                if (editSession == null || !editSession.InBreakState)
                {
                    return null;
                }

                // TODO: Avoid enumerating active statements for unchanged documents.
                // We would need to add a document path parameter to be able to find the document we need to check for changes.
                // https://github.com/dotnet/roslyn/issues/24324
                var baseActiveStatements = await editSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var primaryDocument = await solution.GetDocumentAsync(baseActiveStatement.PrimaryDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                if (primaryDocument == null)
                {
                    // The document has been deleted.
                    return null;
                }

                var (oldPrimaryDocument, _) = await editSession.DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(baseActiveStatement.PrimaryDocumentId, primaryDocument, cancellationToken).ConfigureAwait(false);
                if (oldPrimaryDocument == null)
                {
                    // Can't determine position of an active statement if the document is out-of-sync with loaded module debug information.
                    return null;
                }

                var activeStatementSpans = await activeStatementSpanProvider(primaryDocument.Id, cancellationToken).ConfigureAwait(false);
                var currentActiveStatements = await editSession.Analyses.GetActiveStatementsAsync(oldPrimaryDocument, primaryDocument, activeStatementSpans, cancellationToken).ConfigureAwait(false);
                if (currentActiveStatements.IsDefault)
                {
                    // The document has syntax errors.
                    return null;
                }

                return currentActiveStatements[baseActiveStatement.PrimaryDocumentOrdinal].Span;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        /// <summary>
        /// Called by the debugger to determine whether an active statement is in an exception region,
        /// so it can determine whether the active statement can be remapped. This only happens when the EnC is about to apply changes.
        /// If the debugger determines we can remap active statements, the application of changes proceeds.
        /// </summary>
        /// <returns>
        /// True if the instruction is located within an exception region, false if it is not, null if the instruction isn't an active statement
        /// or the exception regions can't be determined.
        /// </returns>
        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                var editSession = _editSession;
                if (editSession == null || !editSession.InBreakState)
                {
                    return null;
                }

                // This method is only called when the EnC is about to apply changes, at which point all active statements and
                // their exception regions will be needed. Hence it's not necessary to scope this query down to just the instruction
                // the debugger is interested at this point while not calculating the others.

                var baseActiveStatements = await editSession.BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (!baseActiveStatements.InstructionMap.TryGetValue(instructionId, out var baseActiveStatement))
                {
                    return null;
                }

                var baseExceptionRegions = (await editSession.GetBaseActiveExceptionRegionsAsync(solution, cancellationToken).ConfigureAwait(false))[baseActiveStatement.Ordinal];

                // If the document is out-of-sync the exception regions can't be determined.
                return baseExceptionRegions.Spans.IsDefault ? (bool?)null : baseExceptionRegions.IsActiveStatementCovered;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        private static void ReportTelemetry(DebuggingSessionTelemetry.Data data)
        {
            // report telemetry (fire and forget):
            _ = Task.Run(() => LogDebuggingSessionTelemetry(data, Logger.Log, LogAggregator.GetNextId));
        }

        // internal for testing
        internal static void LogDebuggingSessionTelemetry(DebuggingSessionTelemetry.Data debugSessionData, Action<FunctionId, LogMessage> log, Func<int> getNextId)
        {
            const string SessionId = nameof(SessionId);
            const string EditSessionId = nameof(EditSessionId);

            var debugSessionId = getNextId();

            log(FunctionId.Debugging_EncSession, KeyValueLogMessage.Create(map =>
            {
                map[SessionId] = debugSessionId;
                map["SessionCount"] = debugSessionData.EditSessionData.Length;
                map["EmptySessionCount"] = debugSessionData.EmptyEditSessionCount;
            }));

            foreach (var editSessionData in debugSessionData.EditSessionData)
            {
                var editSessionId = getNextId();

                log(FunctionId.Debugging_EncSession_EditSession, KeyValueLogMessage.Create(map =>
                {
                    map[SessionId] = debugSessionId;
                    map[EditSessionId] = editSessionId;

                    map["HadCompilationErrors"] = editSessionData.HadCompilationErrors;
                    map["HadRudeEdits"] = editSessionData.HadRudeEdits;
                    map["HadValidChanges"] = editSessionData.HadValidChanges;
                    map["HadValidInsignificantChanges"] = editSessionData.HadValidInsignificantChanges;

                    map["RudeEditsCount"] = editSessionData.RudeEdits.Length;
                    map["EmitDeltaErrorIdCount"] = editSessionData.EmitErrorIds.Length;
                }));

                foreach (var errorId in editSessionData.EmitErrorIds)
                {
                    log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(map =>
                    {
                        map[SessionId] = debugSessionId;
                        map[EditSessionId] = editSessionId;
                        map["ErrorId"] = errorId;
                    }));
                }

                foreach (var (editKind, syntaxKind) in editSessionData.RudeEdits)
                {
                    log(FunctionId.Debugging_EncSession_EditSession_RudeEdit, KeyValueLogMessage.Create(map =>
                    {
                        map[SessionId] = debugSessionId;
                        map[EditSessionId] = editSessionId;

                        map["RudeEditKind"] = editKind;
                        map["RudeEditSyntaxKind"] = syntaxKind;
                        map["RudeEditBlocking"] = editSessionData.HadRudeEdits;
                    }));
                }
            }
        }
    }
}
