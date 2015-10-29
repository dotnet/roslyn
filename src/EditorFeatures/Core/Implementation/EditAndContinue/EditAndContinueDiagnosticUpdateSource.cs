// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Export(typeof(EditAndContinueDiagnosticUpdateSource))]
    [Shared]
    internal sealed class EditAndContinueDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        internal static object DebuggerErrorId = new object();
        internal static object EmitErrorId = new object();

        [ImportingConstructor]
        public EditAndContinueDiagnosticUpdateSource(IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            registrationService.Register(this);
        }

        public bool SupportGetDiagnostics { get { return false; } }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }

        public void ClearDiagnostics(DebuggingSession session, Workspace workspace, object kind, ProjectId projectId, ImmutableArray<DocumentId> documentIds)
        {
            if (documentIds.IsDefault)
            {
                return;
            }

            foreach (var documentId in documentIds)
            {
                ClearDiagnostics(session, workspace, kind, projectId, documentId);
            }
        }

        public void ClearDiagnostics(DebuggingSession session, Workspace workspace, object errorId, ProjectId projectId, DocumentId documentId)
        {
            RaiseDiagnosticsUpdated(MakeArgs(session, workspace, errorId, projectId, documentId, ImmutableArray.Create<DiagnosticData>(),
                DiagnosticsUpdatedKind.DiagnosticsRemoved));
        }

        public ImmutableArray<DocumentId> ReportDiagnostics(DebuggingSession session, object errorId, ProjectId projectId, Solution solution, IEnumerable<Diagnostic> diagnostics)
        {
            var argsByDocument = ImmutableArray.CreateRange(
                from diagnostic in diagnostics
                let document = solution.GetDocument(diagnostic.Location.SourceTree, projectId)
                where document != null
                let item = MakeDiagnosticData(projectId, document, solution, diagnostic)
                group item by document.Id into itemsByDocumentId
                select MakeArgs(session, errorId, solution.Workspace, solution, projectId, itemsByDocumentId.Key, ImmutableArray.CreateRange(itemsByDocumentId), DiagnosticsUpdatedKind.DiagnosticsCreated));

            foreach (var args in argsByDocument)
            {
                RaiseDiagnosticsUpdated(args);
            }

            return argsByDocument.SelectAsArray(args => args.DocumentId);
        }

        private static DiagnosticData MakeDiagnosticData(ProjectId projectId, Document document, Solution solution, Diagnostic d)
        {
            if (document != null)
            {
                return DiagnosticData.Create(document, d);
            }
            else
            {
                var project = solution.GetProject(projectId);
                Debug.Assert(project != null);
                return DiagnosticData.Create(project, d);
            }
        }

        private DiagnosticsUpdatedArgs MakeArgs(
            DebuggingSession session, Workspace workspace, object errorId, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> items,
            DiagnosticsUpdatedKind kind)
        {
            return MakeArgs(session, errorId, workspace, solution: null, projectId: projectId, documentId: documentId, items: items, kind: kind);
        }

        private DiagnosticsUpdatedArgs MakeArgs(
            DebuggingSession session, object errorId, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> items,
            DiagnosticsUpdatedKind kind)
        {
            return new DiagnosticsUpdatedArgs(
                id: new EnCId(session, errorId),
                workspace: workspace,
                solution: solution,
                projectId: projectId,
                documentId: documentId,
                diagnostics: items,
                kind: kind);
        }

        private void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
        {
            var updated = this.DiagnosticsUpdated;
            if (updated != null)
            {
                updated(this, args);
            }
        }

        private class EnCId : BuildToolId.Base<DebuggingSession, object>
        {
            public EnCId(DebuggingSession session, object errorId) :
                base(session, errorId)
            {
            }

            public override string BuildTool
            {
                get { return PredefinedBuildTools.EnC; }
            }
        }
    }
}
