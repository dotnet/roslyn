// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    internal sealed class EditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        internal static readonly TraceLog Log = new TraceLog(2048, "EnC");

        private readonly Workspace _workspace;
        private readonly IActiveStatementTrackingService _trackingService;
        private readonly IActiveStatementProvider _activeStatementProvider;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IDebuggeeModuleMetadataProvider _debugeeModuleMetadataProvider;
        private readonly EditAndContinueDiagnosticUpdateSource _emitDiagnosticsUpdateSource;
        private readonly EditSessionTelemetry _editSessionTelemetry;
        private readonly DebuggingSessionTelemetry _debuggingSessionTelemetry;
        private readonly ICompilationOutputsProviderService _compilationOutputsProvider;
        private readonly Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

        /// <summary>
        /// A document id is added whenever a diagnostic is reported while in run mode.
        /// These diagnostics are cleared as soon as we enter break mode or the debugging session terminates.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithReportedDiagnosticsDuringRunMode;
        private readonly object _documentsWithReportedDiagnosticsDuringRunModeGuard = new object();

        private DebuggingSession? _debuggingSession;
        private EditSession? _editSession;
        private PendingSolutionUpdate? _pendingUpdate;

        internal EditAndContinueWorkspaceService(
            Workspace workspace,
            IActiveStatementTrackingService activeStatementTrackingService,
            ICompilationOutputsProviderService compilationOutputsProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            IActiveStatementProvider activeStatementProvider,
            IDebuggeeModuleMetadataProvider debugeeModuleMetadataProvider,
            Action<DebuggingSessionTelemetry.Data>? reportTelemetry = null)
        {
            _workspace = workspace;
            _diagnosticService = diagnosticService;
            _emitDiagnosticsUpdateSource = diagnosticUpdateSource;
            _activeStatementProvider = activeStatementProvider;
            _debugeeModuleMetadataProvider = debugeeModuleMetadataProvider;
            _trackingService = activeStatementTrackingService;
            _debuggingSessionTelemetry = new DebuggingSessionTelemetry();
            _editSessionTelemetry = new EditSessionTelemetry();
            _documentsWithReportedDiagnosticsDuringRunMode = new HashSet<DocumentId>();
            _compilationOutputsProvider = compilationOutputsProvider;
            _reportTelemetry = reportTelemetry ?? ReportTelemetry;
        }

        // test only:
        internal EditSession? Test_GetEditSession() => _editSession;
        internal PendingSolutionUpdate? Test_GetPendingSolutionUpdate() => _pendingUpdate;

        public bool IsDebuggingSessionInProgress
            => _debuggingSession != null;

        /// <summary>
        /// Invoked whenever a module instance is loaded to a process being debugged.
        /// </summary>
        public void OnManagedModuleInstanceLoaded(Guid mvid)
            => _editSession?.ModuleInstanceLoadedOrUnloaded(mvid);

        /// <summary>
        /// Invoked whenever a module instance is unloaded from a process being debugged.
        /// </summary>
        public void OnManagedModuleInstanceUnloaded(Guid mvid)
            => _editSession?.ModuleInstanceLoadedOrUnloaded(mvid);

        public void StartDebuggingSession()
        {
            var previousSession = Interlocked.CompareExchange(ref _debuggingSession, new DebuggingSession(_workspace, _debugeeModuleMetadataProvider, _activeStatementProvider, _compilationOutputsProvider), null);
            Contract.ThrowIfFalse(previousSession == null, "New debugging session can't be started until the existing one has ended.");
        }

        public void StartEditSession()
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession, "Edit session can only be started during debugging session");

            var newSession = new EditSession(debuggingSession, _editSessionTelemetry);

            var previousSession = Interlocked.CompareExchange(ref _editSession, newSession, null);
            Contract.ThrowIfFalse(previousSession == null, "New edit session can't be started until the existing one has ended.");

            _trackingService.StartTracking(newSession);

            // clear diagnostics reported during run mode:
            ClearReportedRunModeDiagnostics();
        }

        public void EndEditSession()
        {
            // first, publish null session:
            var session = Interlocked.Exchange(ref _editSession, null);
            Contract.ThrowIfNull(session, "Edit session has not started.");

            // then cancel all ongoing work bound to the session:
            session.Cancel();

            // then clear all reported rude edits:
            _diagnosticService.Reanalyze(_workspace, documentIds: session.GetDocumentsWithReportedDiagnostics());

            _trackingService.EndTracking();

            _debuggingSessionTelemetry.LogEditSession(_editSessionTelemetry.GetDataAndClear());

            session.Dispose();
        }

        public void EndDebuggingSession()
        {
            var debuggingSession = Interlocked.Exchange(ref _debuggingSession, null);
            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            _reportTelemetry(_debuggingSessionTelemetry.GetDataAndClear());

            // clear emit/apply diagnostics reported previously:
            _emitDiagnosticsUpdateSource.ClearDiagnostics();

            // clear diagnostics reported during run mode:
            ClearReportedRunModeDiagnostics();

            debuggingSession.Dispose();
        }

        internal static bool SupportsEditAndContinue(Project project)
            => project.LanguageServices.GetService<IEditAndContinueAnalyzer>() != null;

        internal static bool IsDesignTimeOnlyDocument(Document document)
            => document.Services.GetService<DocumentPropertiesService>()?.DesignTimeOnly == true;

        public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                var debuggingSession = _debuggingSession;
                if (debuggingSession == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // The document has not changed while the application is running since the last changes were committed:
                var oldDocument = debuggingSession.LastCommittedSolution.GetDocument(document.Id);
                var editSession = _editSession;
                if (editSession == null && document == oldDocument)
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
                if (IsDesignTimeOnlyDocument(document) || !document.SupportsSyntaxTree)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // Do not analyze documents (and report diagnostics) of projects that have not been built.
                // Allow user to make any changes in these documents, they won't be applied within the current debugging session.
                // Do not report the file read error - it might be an intermittent issue. The error will be reported when the 
                // change is attempted to be applied.
                var (mvid, _) = await debuggingSession.GetProjectModuleIdAsync(project.Id, cancellationToken).ConfigureAwait(false);
                if (mvid == Guid.Empty)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                if (editSession == null)
                {
                    // Any changes made in loaded, built projects outside of edit session are rude edits (the application is running):
                    var newSyntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(newSyntaxTree);

                    var changedSpans = await GetChangedSpansAsync(oldDocument, newSyntaxTree, cancellationToken).ConfigureAwait(false);
                    return GetRunModeDocumentDiagnostics(document, newSyntaxTree, changedSpans);
                }

                // No changes made in the entire workspace since the edit session started:
                if (editSession.BaseSolution.WorkspaceVersion == project.Solution.WorkspaceVersion)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var analysis = await editSession.GetDocumentAnalysis(document).GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (analysis.HasChanges)
                {
                    // Once we detected a change in a document let the debugger know that the corresponding loaded module
                    // is about to be updated, so that it can start initializing it for EnC update, reducing the amount of time applying
                    // the change blocks the UI when the user "continues".
                    debuggingSession.PrepareModuleForUpdate(mvid);

                    // Check if EnC is allowed for all loaded modules corresponding to the project.
                    var moduleDiagnostics = editSession.GetModuleDiagnostics(mvid, project.Name);

                    if (!moduleDiagnostics.IsEmpty)
                    {
                        // track the document, so that we can refresh or clean diagnostics at the end of edit session:
                        editSession.TrackDocumentWithReportedDiagnostics(document.Id);

                        var newSyntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        Contract.ThrowIfNull(newSyntaxTree);

                        var changedSpans = await GetChangedSpansAsync(oldDocument, newSyntaxTree, cancellationToken).ConfigureAwait(false);

                        var diagnosticsBuilder = ArrayBuilder<Diagnostic>.GetInstance();
                        foreach (var span in changedSpans)
                        {
                            var location = Location.Create(newSyntaxTree, span);

                            foreach (var diagnostic in moduleDiagnostics)
                            {
                                diagnosticsBuilder.Add(diagnostic.ToDiagnostic(location));
                            }
                        }

                        return diagnosticsBuilder.ToImmutableAndFree();
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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        private static async Task<IEnumerable<TextSpan>> GetChangedSpansAsync(Document? oldDocument, SyntaxTree newSyntaxTree, CancellationToken cancellationToken)
        {
            if (oldDocument != null)
            {
                var oldSyntaxTree = await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return GetSpansInNewDocument(await GetDocumentTextChangesAsync(oldSyntaxTree, newSyntaxTree, cancellationToken).ConfigureAwait(false));
            }

            var newRoot = await newSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            return SpecializedCollections.SingletonEnumerable(newRoot.FullSpan);
        }

        private ImmutableArray<Diagnostic> GetRunModeDocumentDiagnostics(Document newDocument, SyntaxTree newSyntaxTree, IEnumerable<TextSpan> changedSpans)
        {
            if (!changedSpans.Any())
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            lock (_documentsWithReportedDiagnosticsDuringRunModeGuard)
            {
                _documentsWithReportedDiagnosticsDuringRunMode.Add(newDocument.Id);
            }

            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ChangesNotAppliedWhileRunning);
            var args = new[] { newDocument.Project.Name };
            return changedSpans.SelectAsArray(span => Diagnostic.Create(descriptor, Location.Create(newSyntaxTree, span), args));
        }

        // internal for testing
        internal static async Task<IList<TextChange>> GetDocumentTextChangesAsync(SyntaxTree oldSyntaxTree, SyntaxTree newSyntaxTree, CancellationToken cancellationToken)
        {
            var list = newSyntaxTree.GetChanges(oldSyntaxTree);
            if (list.Count != 0)
            {
                return list;
            }

            var oldText = await oldSyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = await newSyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (oldText.ContentEquals(newText))
            {
                return Array.Empty<TextChange>();
            }

            var roList = newText.GetTextChanges(oldText);
            if (roList.Count != 0)
            {
                return roList.ToArray();
            }

            return Array.Empty<TextChange>();
        }

        // internal for testing
        internal static IEnumerable<TextSpan> GetSpansInNewDocument(IEnumerable<TextChange> changes)
        {
            int oldPosition = 0;
            int newPosition = 0;
            foreach (var change in changes)
            {
                if (change.Span.Start < oldPosition)
                {
                    Debug.Fail("Text changes not ordered");
                    yield break;
                }

                if (change.Span.Length == 0 && change.NewText.Length == 0)
                {
                    continue;
                }

                // skip unchanged text:
                newPosition += change.Span.Start - oldPosition;

                yield return new TextSpan(newPosition, change.NewText.Length);

                // apply change:
                oldPosition = change.Span.End;
                newPosition += change.NewText.Length;
            }
        }

        private void ClearReportedRunModeDiagnostics()
        {
            // clear diagnostics reported during run mode:
            ImmutableArray<DocumentId> documentsToReanalyze;
            lock (_documentsWithReportedDiagnosticsDuringRunModeGuard)
            {
                documentsToReanalyze = _documentsWithReportedDiagnosticsDuringRunMode.ToImmutableArray();
                _documentsWithReportedDiagnosticsDuringRunMode.Clear();
            }

            // clear all reported run mode diagnostics:
            _diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);
        }

        /// <summary>
        /// Determine whether the updates made to projects containing the specified file (or all projects that are built, 
        /// if <paramref name="sourceFilePath"/> is null) are ready to be applied and the debugger should attempt to apply
        /// them on "continue".
        /// </summary>
        /// <returns>
        /// Returns <see cref="SolutionUpdateStatus.Blocked"/> if there are rude edits or other errors 
        /// that block the application of the updates. Might return <see cref="SolutionUpdateStatus.Ready"/> even if there are 
        /// errors in the code that will block the application of the updates. E.g. emit diagnostics can't be determined until 
        /// emit is actually performed. Therefore, this method only serves as an optimization to avoid unnecessary emit attempts,
        /// but does not provide a definitive answer. Only <see cref="EmitSolutionUpdateAsync"/> can definitively determine whether
        /// the update is valid or not.
        /// </returns>
        public Task<SolutionUpdateStatus> GetSolutionUpdateStatusAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            // GetStatusAsync is called outside of edit session when the debugger is determining 
            // whether a source file checksum matches the one in PDB.
            // The debugger expects no changes in this case.
            var editSession = _editSession;
            if (editSession == null)
            {
                return Task.FromResult(SolutionUpdateStatus.None);
            }

            return editSession.GetSolutionUpdateStatusAsync(_workspace.CurrentSolution, sourceFilePath, cancellationToken);
        }

        public async Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var editSession = _editSession;
            if (editSession == null)
            {
                return (SolutionUpdateStatus.None, ImmutableArray<Deltas>.Empty);
            }

            var solution = _workspace.CurrentSolution;

            var solutionUpdate = await editSession.EmitSolutionUpdateAsync(solution, cancellationToken).ConfigureAwait(false);

            if (solutionUpdate.Summary == SolutionUpdateStatus.Ready)
            {
                var previousPendingUpdate = Interlocked.Exchange(ref _pendingUpdate, new PendingSolutionUpdate(
                    solution,
                    solutionUpdate.EmitBaselines,
                    solutionUpdate.Deltas,
                    solutionUpdate.ModuleReaders));

                // commit/discard was not called:
                Contract.ThrowIfFalse(previousPendingUpdate == null);
            }

            // clear emit/apply diagnostics reported previously:
            _emitDiagnosticsUpdateSource.ClearDiagnostics();

            // report emit/apply diagnostics:
            foreach (var (projectId, diagnostics) in solutionUpdate.Diagnostics)
            {
                _emitDiagnosticsUpdateSource.ReportDiagnostics(solution, projectId, diagnostics);
            }

            // Note that we may return empty deltas if all updates have been deferred.
            // The debugger will still call commit or discard on the update batch.
            return (solutionUpdate.Summary, solutionUpdate.Deltas);
        }

        public void CommitSolutionUpdate()
        {
            var editSession = _editSession;

            Contract.ThrowIfNull(editSession);

            var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
            Contract.ThrowIfNull(pendingUpdate);

            editSession.DebuggingSession.CommitSolutionUpdate(pendingUpdate);
            editSession.ChangesApplied();
        }

        public void DiscardSolutionUpdate()
        {
            Contract.ThrowIfNull(_editSession);

            var pendingUpdate = Interlocked.Exchange(ref _pendingUpdate, null);
            Contract.ThrowIfNull(pendingUpdate);

            foreach (var moduleReader in pendingUpdate.ModuleReaders)
            {
                moduleReader.Dispose();
            }
        }

        public async Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                // It is allowed to call this method before entering or after exiting break mode. In fact, the VS debugger does so. 
                // We return null since there the concept of active statement only makes sense during break mode.
                var editSession = _editSession;
                if (editSession == null)
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

                var primaryDocument = _workspace.CurrentSolution.GetDocument(baseActiveStatement.PrimaryDocumentId);
                var documentAnalysis = await editSession.GetDocumentAnalysis(primaryDocument).GetValueAsync(cancellationToken).ConfigureAwait(false);
                var currentActiveStatements = documentAnalysis.ActiveStatements;
                if (currentActiveStatements.IsDefault)
                {
                    // The document has syntax errors.
                    return null;
                }

                return currentActiveStatements[baseActiveStatement.PrimaryDocumentOrdinal].Span;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            try
            {
                var editSession = _editSession;
                if (editSession == null)
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

                // TODO: avoid waiting for ERs of all active statements to be calculated and just calculate the one we are interested in at this moment:
                // https://github.com/dotnet/roslyn/issues/24324
                var baseExceptionRegions = await editSession.BaseActiveExceptionRegions.GetValueAsync(cancellationToken).ConfigureAwait(false);
                return baseExceptionRegions[baseActiveStatement.Ordinal].IsActiveStatementCovered;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }

        public void ReportApplyChangesException(string message)
        {
            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);

            _emitDiagnosticsUpdateSource.ReportDiagnostics(
                _workspace.CurrentSolution,
                projectIdOpt: null,
                new[] { Diagnostic.Create(descriptor, Location.None, new[] { message }) });
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
