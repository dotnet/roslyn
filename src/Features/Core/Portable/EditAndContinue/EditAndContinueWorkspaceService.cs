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
        private Func<Project, CompilationOutputs> _compilationOutputsProvider;
        private Action<DebuggingSessionTelemetry.Data> _reportTelemetry;

        private DebuggingSession? _debuggingSession;

        internal EditAndContinueWorkspaceService()
        {
            _debuggingSessionTelemetry = new DebuggingSessionTelemetry();
            _editSessionTelemetry = new EditSessionTelemetry();
            _compilationOutputsProvider = GetCompilationOutputs;
            _reportTelemetry = ReportTelemetry;
        }

        private static CompilationOutputs GetCompilationOutputs(Project project)
        {
            // The Project System doesn't always indicate whether we emit PDB, what kind of PDB we emit nor the path of the PDB.
            // To work around we look for the PDB on the path specified in the PDB debug directory.
            // https://github.com/dotnet/roslyn/issues/35065
            return new CompilationOutputFilesWithImplicitPdbPath(project.CompilationOutputInfo.AssemblyPath);
        }

        public void OnSourceFileUpdated(Document document)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession != null)
            {
                // fire and forget
                _ = Task.Run(() => debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(document, debuggingSession.CancellationToken));
            }
        }

        public async ValueTask StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            var initialDocumentStates =
                captureMatchingDocuments ? await CommittedSolution.GetMatchingDocumentsAsync(solution, _compilationOutputsProvider, cancellationToken).ConfigureAwait(false) :
                SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>();

            var runtimeCapabilities = await debuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);

            var capabilities = ParseCapabilities(runtimeCapabilities);

            // For now, runtimes aren't returning capabilities, we just fall back to a known set.
            if (capabilities == EditAndContinueCapabilities.None)
            {
                capabilities = EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.NewTypeDefinition;
            }

            var newSession = new DebuggingSession(solution, debuggerService, capabilities, _compilationOutputsProvider, initialDocumentStates, _debuggingSessionTelemetry, _editSessionTelemetry);
            var previousSession = Interlocked.CompareExchange(ref _debuggingSession, newSession, null);
            Contract.ThrowIfFalse(previousSession == null, "New debugging session can't be started until the existing one has ended.");
        }

        // internal for testing
        internal static EditAndContinueCapabilities ParseCapabilities(ImmutableArray<string> capabilities)
        {
            var caps = EditAndContinueCapabilities.None;

            foreach (var capability in capabilities)
            {
                caps |= capability switch
                {
                    "Baseline" => EditAndContinueCapabilities.Baseline,
                    "AddMethodToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType,
                    "AddStaticFieldToExistingType" => EditAndContinueCapabilities.AddStaticFieldToExistingType,
                    "AddInstanceFieldToExistingType" => EditAndContinueCapabilities.AddInstanceFieldToExistingType,
                    "NewTypeDefinition" => EditAndContinueCapabilities.NewTypeDefinition,
                    "ChangeCustomAttributes" => EditAndContinueCapabilities.ChangeCustomAttributes,

                    // To make it eaiser for  runtimes to specify more broad capabilities
                    "AddDefinitionToExistingType" => EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType,

                    _ => EditAndContinueCapabilities.None
                };
            }

            return caps;
        }

        public void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = Interlocked.Exchange(ref _debuggingSession, null);
            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            debuggingSession.EndSession(out documentsToReanalyze, out var telemetryData);

            _reportTelemetry(telemetryData);
        }

        public void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);
            debuggingSession.RestartEditSession(inBreakState: true, out documentsToReanalyze);
        }

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            return debuggingSession.GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken);
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
        public ValueTask<bool> HasChangesAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            // GetStatusAsync is called outside of edit session when the debugger is determining
            // whether a source file checksum matches the one in PDB.
            // The debugger expects no changes in this case.
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return default;
            }

            return debuggingSession.EditSession.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken);
        }

        public ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult(EmitSolutionUpdateResults.Empty);
            }

            return debuggingSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken);
        }

        public void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);

            debuggingSession.CommitSolutionUpdate(out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate()
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);

            debuggingSession.DiscardSolutionUpdate();
        }

        public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return default;
            }

            return debuggingSession.GetBaseActiveStatementSpansAsync(solution, documentIds, cancellationToken);
        }

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument mappedDocument, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);
            }

            return debuggingSession.GetAdjustedActiveStatementSpansAsync(mappedDocument, activeStatementSpanProvider, cancellationToken);
        }

        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            // It is allowed to call this method before entering or after exiting break mode. In fact, the VS debugger does so.
            // We return null since there the concept of active statement only makes sense during break mode.
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult<LinePositionSpan?>(null);
            }

            return debuggingSession.GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken);
        }

        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var debuggingSession = _debuggingSession;
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult<bool?>(null);
            }

            return debuggingSession.IsActiveStatementInExceptionRegionAsync(solution, instructionId, cancellationToken);
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

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly EditAndContinueWorkspaceService _service;

            public TestAccessor(EditAndContinueWorkspaceService service)
            {
                _service = service;
            }

            internal DebuggingSession? GetDebuggingSession() => _service._debuggingSession;
            internal EditSession? GetEditSession() => _service._debuggingSession?.EditSession;
            internal void SetOutputProvider(Func<Project, CompilationOutputs> value) => _service._compilationOutputsProvider = value;
            internal void SetReportTelemetry(Action<DebuggingSessionTelemetry.Data> value) => _service._reportTelemetry = value;
        }
    }
}
