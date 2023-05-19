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
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    [Export(typeof(IEditAndContinueService)), Shared]
    internal sealed class EditAndContinueService : IEditAndContinueService
    {
        [ExportWorkspaceService(typeof(IEditAndContinueWorkspaceService)), Shared]
        internal sealed class WorkspaceService : IEditAndContinueWorkspaceService
        {
            public IEditAndContinueService Service { get; }

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public WorkspaceService(IEditAndContinueService service)
                => Service = service;
        }

        internal static readonly TraceLog Log;
        internal static readonly TraceLog AnalysisLog;

        private Func<Project, CompilationOutputs> _compilationOutputsProvider;

        /// <summary>
        /// List of active debugging sessions (small number of simoultaneously active sessions is expected).
        /// </summary>
        private readonly List<DebuggingSession> _debuggingSessions = new();
        private static int s_debuggingSessionId;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueService()
        {
            _compilationOutputsProvider = GetCompilationOutputs;
        }

        static EditAndContinueService()
        {
            Log = new(2048, "EnC", "Trace.log");
            AnalysisLog = new(1024, "EnC", "Analysis.log");

            var logDir = GetLogDirectory();
            if (logDir != null)
            {
                Log.SetLogDirectory(logDir);
                AnalysisLog.SetLogDirectory(logDir);
            }
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

        public void SetFileLoggingDirectory(string? logDirectory)
        {
            Log.SetLogDirectory(logDirectory ?? GetLogDirectory());
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

        public async ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(
            Solution solution,
            IManagedHotReloadService debuggerService,
            IPdbMatchingSourceTextProvider sourceTextProvider,
            ImmutableArray<DocumentId> captureMatchingDocuments,
            bool captureAllMatchingDocuments,
            bool reportDiagnostics,
            CancellationToken cancellationToken)
        {
            try
            {
                Contract.ThrowIfTrue(captureAllMatchingDocuments && !captureMatchingDocuments.IsEmpty);

                IEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>> initialDocumentStates;

                if (captureAllMatchingDocuments || !captureMatchingDocuments.IsEmpty)
                {
                    var documentsByProject = captureAllMatchingDocuments
                        ? solution.Projects.Select(project => (project, project.State.DocumentStates.States.Values))
                        : GetDocumentStatesGroupedByProject(solution, captureMatchingDocuments);

                    initialDocumentStates = await CommittedSolution.GetMatchingDocumentsAsync(documentsByProject, _compilationOutputsProvider, sourceTextProvider, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    initialDocumentStates = SpecializedCollections.EmptyEnumerable<KeyValuePair<DocumentId, CommittedSolution.DocumentState>>();
                }

                var sessionId = new DebuggingSessionId(Interlocked.Increment(ref s_debuggingSessionId));
                var session = new DebuggingSession(sessionId, solution, debuggerService, _compilationOutputsProvider, sourceTextProvider, initialDocumentStates, reportDiagnostics);

                lock (_debuggingSessions)
                {
                    _debuggingSessions.Add(session);
                }

                Log.Write("Session #{0} started.", sessionId.Ordinal);
                return sessionId;

            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
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

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly EditAndContinueService _service;

            public TestAccessor(EditAndContinueService service)
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
