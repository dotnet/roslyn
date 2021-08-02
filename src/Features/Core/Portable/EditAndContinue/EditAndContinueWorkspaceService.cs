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

        private Func<Project, CompilationOutputs> _compilationOutputsProvider;

        /// <summary>
        /// List of active debugging sessions (small number of simoultaneously active sessions is expected).
        /// </summary>
        private readonly List<DebuggingSession> _debuggingSessions = new();
        private static int s_debuggingSessionId;

        internal EditAndContinueWorkspaceService()
        {
            _compilationOutputsProvider = GetCompilationOutputs;
        }

        private static CompilationOutputs GetCompilationOutputs(Project project)
        {
            // The Project System doesn't always indicate whether we emit PDB, what kind of PDB we emit nor the path of the PDB.
            // To work around we look for the PDB on the path specified in the PDB debug directory.
            // https://github.com/dotnet/roslyn/issues/35065
            return new CompilationOutputFilesWithImplicitPdbPath(project.CompilationOutputInfo.AssemblyPath);
        }

        private DebuggingSession? TryGetDebuggingSession(DebuggingSessionId sessionId)
        {
            lock (_debuggingSessions)
            {
                return _debuggingSessions.SingleOrDefault(s => s.Id == sessionId);
            }
        }

        private ImmutableArray<DebuggingSession> GetActiveDebuggingSessions()
        {
            lock (_debuggingSessions)
            {
                return _debuggingSessions.ToImmutableArray();
            }
        }

        private ImmutableArray<DebuggingSession> GetDiagnosticReportingDebuggingSessions()
        {
            lock (_debuggingSessions)
            {
                return _debuggingSessions.Where(s => s.ReportDiagnostics).ToImmutableArray();
            }
        }

        public void OnSourceFileUpdated(Document document)
        {
            // notify all active debugging sessions
            foreach (var debuggingSession in GetActiveDebuggingSessions())
            {
                // fire and forget
                _ = Task.Run(() => debuggingSession.OnSourceFileUpdatedAsync(document)).ReportNonFatalErrorAsync();
            }
        }

        public async ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(
            Solution solution,
            IManagedEditAndContinueDebuggerService debuggerService,
            ImmutableArray<DocumentId> captureMatchingDocuments,
            bool captureAllMatchingDocuments,
            bool reportDiagnostics,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(captureAllMatchingDocuments && !captureMatchingDocuments.IsEmpty);

            IEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>> initialDocumentStates;

            if (captureAllMatchingDocuments || !captureMatchingDocuments.IsEmpty)
            {
                var documentsByProject = captureAllMatchingDocuments ?
                    solution.Projects.Select(project => (project, project.State.DocumentStates.States.Values)) :
                    GetDocumentStatesGroupedByProject(solution, captureMatchingDocuments);

                initialDocumentStates = await CommittedSolution.GetMatchingDocumentsAsync(documentsByProject, _compilationOutputsProvider, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                initialDocumentStates = SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>();
            }

            var runtimeCapabilities = await debuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var capabilities = ParseCapabilities(runtimeCapabilities);

            // For now, runtimes aren't returning capabilities, we just fall back to a known set.
            if (capabilities == EditAndContinueCapabilities.None)
            {
                capabilities = EditAndContinueCapabilities.Baseline | EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.NewTypeDefinition;
            }

            var sessionId = new DebuggingSessionId(Interlocked.Increment(ref s_debuggingSessionId));
            var session = new DebuggingSession(sessionId, solution, debuggerService, capabilities, _compilationOutputsProvider, initialDocumentStates, reportDiagnostics);

            lock (_debuggingSessions)
            {
                _debuggingSessions.Add(session);
            }

            return sessionId;
        }

        private static IEnumerable<(Project, IEnumerable<DocumentState>)> GetDocumentStatesGroupedByProject(Solution solution, ImmutableArray<DocumentId> documentIds)
            => from documentId in documentIds
               where solution.ContainsDocument(documentId)
               group documentId by documentId.ProjectId into projectDocumentIds
               let project = solution.GetRequiredProject(projectDocumentIds.Key)
               select (project, from documentId in projectDocumentIds select project.State.DocumentStates.GetState(documentId));

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

        public void EndDebuggingSession(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            DebuggingSession? debuggingSession;
            lock (_debuggingSessions)
            {
                _debuggingSessions.TryRemoveFirst((s, sessionId) => s.Id == sessionId, sessionId, out debuggingSession);
            }

            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            debuggingSession.EndSession(out documentsToReanalyze, out var telemetryData);
        }

        public void BreakStateEntered(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            Contract.ThrowIfNull(debuggingSession);
            debuggingSession.BreakStateEntered(out documentsToReanalyze);
        }

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            return GetDiagnosticReportingDebuggingSessions().SelectManyAsArrayAsync(
                (s, arg, cancellationToken) => s.GetDocumentDiagnosticsAsync(arg.document, arg.activeStatementSpanProvider, cancellationToken),
                (document, activeStatementSpanProvider),
                cancellationToken);
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
        public ValueTask<bool> HasChangesAsync(
            DebuggingSessionId sessionId,
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            string? sourceFilePath,
            CancellationToken cancellationToken)
        {
            // GetStatusAsync is called outside of edit session when the debugger is determining
            // whether a source file checksum matches the one in PDB.
            // The debugger expects no changes in this case.
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return default;
            }

            return debuggingSession.EditSession.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken);
        }

        public ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
            DebuggingSessionId sessionId,
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            CancellationToken cancellationToken)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult(EmitSolutionUpdateResults.Empty);
            }

            return debuggingSession.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken);
        }

        public void CommitSolutionUpdate(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            Contract.ThrowIfNull(debuggingSession);

            debuggingSession.CommitSolutionUpdate(out documentsToReanalyze);
        }

        public void DiscardSolutionUpdate(DebuggingSessionId sessionId)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            Contract.ThrowIfNull(debuggingSession);

            debuggingSession.DiscardSolutionUpdate();
        }

        public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(DebuggingSessionId sessionId, Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return default;
            }

            return debuggingSession.GetBaseActiveStatementSpansAsync(solution, documentIds, cancellationToken);
        }

        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(DebuggingSessionId sessionId, TextDocument mappedDocument, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);
            }

            return debuggingSession.GetAdjustedActiveStatementSpansAsync(mappedDocument, activeStatementSpanProvider, cancellationToken);
        }

        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(DebuggingSessionId sessionId, Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            // It is allowed to call this method before entering or after exiting break mode. In fact, the VS debugger does so.
            // We return null since there the concept of active statement only makes sense during break mode.
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult<LinePositionSpan?>(null);
            }

            return debuggingSession.GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken);
        }

        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(DebuggingSessionId sessionId, Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            if (debuggingSession == null)
            {
                return ValueTaskFactory.FromResult<bool?>(null);
            }

            return debuggingSession.IsActiveStatementInExceptionRegionAsync(solution, instructionId, cancellationToken);
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

            public void SetOutputProvider(Func<Project, CompilationOutputs> value)
                => _service._compilationOutputsProvider = value;

            public DebuggingSession GetDebuggingSession(DebuggingSessionId id)
                => _service.TryGetDebuggingSession(id) ?? throw ExceptionUtilities.UnexpectedValue(id);

            public ImmutableArray<DebuggingSession> GetActiveDebuggingSessions()
                => _service.GetActiveDebuggingSessions();

        }
    }
}
