// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class EditAndContinueWorkspaceService : IEditAndContinueWorkspaceService
    {
        private readonly IDiagnosticAnalyzerService diagnosticService;
        private DebuggingSession debuggingSession;
        private EditSession editSession;

        internal EditAndContinueWorkspaceService(IDiagnosticAnalyzerService diagnosticService)
        {
            Debug.Assert(diagnosticService != null);
            this.diagnosticService = diagnosticService;
        }

        public DebuggingSession DebuggingSession
        {
            get
            {
                return debuggingSession;
            }
        }

        public EditSession EditSession
        {
            get
            {
                return editSession;
            }
        }

        public void StartDebuggingSession(Solution currentSolution)
        {
            Debug.Assert(debuggingSession == null && editSession == null);

            Interlocked.CompareExchange(ref this.debuggingSession, new DebuggingSession(currentSolution), null);

            // TODO(tomat): allow changing documents
        }

        public void StartEditSession(
            Solution currentSolution,
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatementSpan>> activeStatements,
            ImmutableDictionary<ProjectId, ProjectReadOnlyReason> projects,
            bool stoppedAtException)
        {
            Debug.Assert(this.debuggingSession != null && this.editSession == null);

            var newSession = new EditSession(currentSolution, activeStatements, this.debuggingSession, projects, stoppedAtException);

            Interlocked.CompareExchange(ref this.editSession, newSession, null);

            // TODO(tomat): allow changing documents
            // TODO(tomat): document added
        }

        public void EndEditSession()
        {
            Debug.Assert(this.debuggingSession != null && this.editSession != null);

            var session = this.editSession;

            // first, publish null session:
            this.editSession = null;

            // then cancel all ongoing work bound to the session:
            session.Cancellation.Cancel();

            // then clear all reported rude edits:
            diagnosticService.Reanalyze(debuggingSession.InitialSolution.Workspace, documentIds: session.GetDocumentsWithReportedRudeEdits());

            // TODO(tomat): allow changing documents
        }

        public void EndDebuggingSession()
        {
            Debug.Assert(this.debuggingSession != null && this.editSession == null);
            this.debuggingSession = null;
        }

        public bool IsProjectReadOnly(string projectName, out SessionReadOnlyReason sessionReason, out ProjectReadOnlyReason projectReason)
        {
            if (this.debuggingSession == null)
            {
                projectReason = ProjectReadOnlyReason.None;
                sessionReason = SessionReadOnlyReason.None;
                return false;
            }

            // run mode - all documents that belong to the workspace shall be read-only:
            var editSession = this.editSession;
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
            if (editSession.TryGetProjectState(projectName, out projectReason))
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
