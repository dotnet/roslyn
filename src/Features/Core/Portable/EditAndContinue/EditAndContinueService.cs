// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Implements core of Edit and Continue orchestration: management of edit sessions and connecting EnC related services.
    /// </summary>
    /// <remarks>
    /// Although the service itself is host agnotic, some of the services it consumes are only available in particular hosts (like Visual Studio).
    /// Therefore this service doesn't export <see cref="IEditAndContinueService"/> on its own. Each host that supports EnC shall implement
    /// a subclass that exports <see cref="IEditAndContinueService"/>.
    /// </remarks>
    internal class EditAndContinueService : IEditAndContinueService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private DebuggingSession _debuggingSession;
        private EditSession _editSession;

        public EditAndContinueService(IDiagnosticAnalyzerService diagnosticService)
        {
            Debug.Assert(diagnosticService != null);
            _diagnosticService = diagnosticService;
        }

        public DebuggingSession DebuggingSession => _debuggingSession;

        public EditSession EditSession => _editSession;

        public void StartDebuggingSession(Solution currentSolution)
        {
            Debug.Assert(_debuggingSession == null && _editSession == null);

            Interlocked.CompareExchange(ref _debuggingSession, new DebuggingSession(currentSolution), null);

            // TODO(tomat): allow changing documents
        }

        public void StartEditSession(
            Solution currentSolution,
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> activeStatements,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            bool stoppedAtException)
        {
            Debug.Assert(_debuggingSession != null && _editSession == null);

            var newSession = new EditSession(currentSolution, activeStatements, _debuggingSession, projects, stoppedAtException);

            Interlocked.CompareExchange(ref _editSession, newSession, null);

            // TODO(tomat): allow changing documents
            // TODO(tomat): document added
        }

        public void EndEditSession()
        {
            Debug.Assert(_debuggingSession != null && _editSession != null);

            var session = _editSession;

            // first, publish null session:
            _editSession = null;

            // then cancel all ongoing work bound to the session:
            session.Cancellation.Cancel();

            // then clear all reported rude edits:
            _diagnosticService.Reanalyze(_debuggingSession.InitialSolution.Workspace, documentIds: session.GetDocumentsWithReportedRudeEdits());

            // TODO(tomat): allow changing documents
        }

        public void EndDebuggingSession()
        {
            Debug.Assert(_debuggingSession != null && _editSession == null);
            _debuggingSession = null;
        }

        public bool IsProjectReadOnly(ProjectId id, out SessionReadOnlyReason sessionReason, out ProjectReadOnlyReason projectReason)
        {
            if (_debuggingSession == null)
            {
                projectReason = ProjectReadOnlyReason.None;
                sessionReason = SessionReadOnlyReason.None;
                return false;
            }

            // run mode - all documents that belong to the workspace shall be read-only:
            var editSession = _editSession;
            if (editSession == null)
            {
                projectReason = ProjectReadOnlyReason.None;
                sessionReason = SessionReadOnlyReason.Running;
                return true;
            }

            // break mode and stopped at exception - all documents shall be read-only:
            if (editSession.StoppedAtException)
            {
                projectReason = ProjectReadOnlyReason.None;
                sessionReason = SessionReadOnlyReason.StoppedAtException;
                return true;
            }

            // normal break mode - if the document belongs to a project that hasn't entered the edit session it shall be read-only:
            if (editSession.Projects.TryGetValue(id, out projectReason))
            {
                sessionReason = SessionReadOnlyReason.None;
                return projectReason != ProjectReadOnlyReason.None;
            }

            sessionReason = SessionReadOnlyReason.None;
            projectReason = ProjectReadOnlyReason.MetadataNotAvailable;
            return true;
        }
    }
}
