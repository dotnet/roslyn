// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    [ExportWorkspaceService(typeof(IEditAndContinueWorkspaceService)), Shared]
    internal sealed class EditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        internal static readonly TraceLog Log = new(2048, "EnC", GetLogDirectory());

        private Func<Project, CompilationOutputs> _compilationOutputsProvider;

        /// <summary>
        /// List of active debugging sessions (small number of simoultaneously active sessions is expected).
        /// </summary>
        private readonly List<DebuggingSession> _debuggingSessions = new();
        private static int s_debuggingSessionId;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueWorkspaceService()
        {
            _compilationOutputsProvider = GetCompilationOutputs;
        }

        private static string? GetLogDirectory()
        {
            try
            {
                var path = Environment.GetEnvironmentVariable("Microsoft_CodeAnalysis_EditAndContinue_LogDir");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                Directory.CreateDirectory(path);
                return path;
            }
            catch
            {
                return null;
            }
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
            IManagedHotReloadService debuggerService,
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

            var sessionId = new DebuggingSessionId(Interlocked.Increment(ref s_debuggingSessionId));
            var session = new DebuggingSession(sessionId, solution, debuggerService, _compilationOutputsProvider, initialDocumentStates, reportDiagnostics);

            lock (_debuggingSessions)
            {
                _debuggingSessions.Add(session);
            }

            Log.Write("Session #{0} started.", sessionId.Ordinal);
            return sessionId;
        }

        private static IEnumerable<(Project, IEnumerable<DocumentState>)> GetDocumentStatesGroupedByProject(Solution solution, ImmutableArray<DocumentId> documentIds)
            => from documentId in documentIds
               where solution.ContainsDocument(documentId)
               group documentId by documentId.ProjectId into projectDocumentIds
               let project = solution.GetRequiredProject(projectDocumentIds.Key)
               select (project, from documentId in projectDocumentIds select project.State.DocumentStates.GetState(documentId));

        public void EndDebuggingSession(DebuggingSessionId sessionId, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            DebuggingSession? debuggingSession;
            lock (_debuggingSessions)
            {
                _debuggingSessions.TryRemoveFirst((s, sessionId) => s.Id == sessionId, sessionId, out debuggingSession);
            }

            Contract.ThrowIfNull(debuggingSession, "Debugging session has not started.");

            debuggingSession.EndSession(out documentsToReanalyze, out var telemetryData);

            Log.Write("Session #{0} ended.", debuggingSession.Id.Ordinal);
        }

        public void BreakStateOrCapabilitiesChanged(DebuggingSessionId sessionId, bool? inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze)
        {
            var debuggingSession = TryGetDebuggingSession(sessionId);
            Contract.ThrowIfNull(debuggingSession);
            debuggingSession.BreakStateOrCapabilitiesChanged(inBreakState, out documentsToReanalyze);
        }

        public ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            return GetDiagnosticReportingDebuggingSessions().SelectManyAsArrayAsync(
                (s, arg, cancellationToken) => s.GetDocumentDiagnosticsAsync(arg.document, arg.activeStatementSpanProvider, cancellationToken),
                (document, activeStatementSpanProvider),
                cancellationToken);
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
