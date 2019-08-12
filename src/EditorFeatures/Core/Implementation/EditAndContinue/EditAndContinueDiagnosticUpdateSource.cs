// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class EncErrorId : BuildToolId.Base<DebuggingSession, object>
    {
        public EncErrorId(DebuggingSession session, object errorId)
            : base(session, errorId)
        {
        }

        public override string BuildTool => PredefinedBuildTools.EnC;
    }

    [Export(typeof(EditAndContinueDiagnosticUpdateSource))]
    [Shared]
    internal sealed class EditAndContinueDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        internal static readonly object InternalErrorId = new object();
        internal static readonly object EmitErrorId = new object();

        [ImportingConstructor]
        public EditAndContinueDiagnosticUpdateSource(IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            registrationService.Register(this);
        }

        public bool SupportGetDiagnostics => false;

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }

        public void ClearDiagnostics(EncErrorId errorId, Solution solution, ProjectId projectId, ImmutableArray<DocumentId> documentIds)
        {
            // clear project diagnostics:
            ClearDiagnostics(errorId, solution, projectId, null);

            // clear document diagnostics:
            foreach (var documentIdOpt in documentIds)
            {
                ClearDiagnostics(errorId, solution, projectId, documentIdOpt);
            }
        }

        public void ClearDiagnostics(EncErrorId errorId, Solution solution, ProjectId projectId, DocumentId documentIdOpt)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                errorId,
                solution.Workspace,
                solution: solution,
                projectId: projectId,
                documentId: documentIdOpt));
        }

        /// <summary>
        /// Reports diagnostics.
        /// </summary>
        /// <returns>Returns ids of documents that belong to <paramref name="projectId"/> and containing one or more diagnostics.</returns>
        public ImmutableArray<DocumentId> ReportDiagnostics(object errorId, Solution solution, ProjectId projectId, IEnumerable<Diagnostic> diagnostics)
        {
            Debug.Assert(errorId != null);
            Debug.Assert(solution != null);
            Debug.Assert(projectId != null);

            var updateEvent = DiagnosticsUpdated;
            using var documentIdsDisposer = PooledHashSet<DocumentId>.GetInstance(out var documentIds);
            using var documentDiagnosticDataDisposer = ArrayBuilder<DiagnosticData>.GetInstance(out var documentDiagnosticData);
            using var projectDiagnosticDataDisposer = ArrayBuilder<DiagnosticData>.GetInstance(out var projectDiagnosticData);

            foreach (var diagnostic in diagnostics)
            {
                var documentOpt = solution.GetDocument(diagnostic.Location.SourceTree);

                if (documentOpt != null)
                {
                    if (updateEvent != null)
                    {
                        documentDiagnosticData.Add(DiagnosticData.Create(documentOpt, diagnostic));
                    }

                    // only add documents from the current project:
                    if (documentOpt.Project.Id == projectId)
                    {
                        documentIds.Add(documentOpt.Id);
                    }
                }
                else if (updateEvent != null)
                {
                    projectDiagnosticData.Add(DiagnosticData.Create(solution.Workspace, diagnostic, projectId));
                }
            }

            foreach (var documentDiagnostics in documentDiagnosticData.ToDictionary(data => data.DocumentId))
            {
                updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    errorId,
                    solution.Workspace,
                    solution,
                    projectId,
                    documentId: documentDiagnostics.Key,
                    diagnostics: documentDiagnostics.Value));
            }

            if (projectDiagnosticData.Count > 0)
            {
                updateEvent(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    errorId,
                    solution.Workspace,
                    solution,
                    projectId,
                    documentId: null,
                    diagnostics: projectDiagnosticData.ToImmutable()));
            }

            return documentIds.AsImmutableOrEmpty();
        }

        internal ImmutableArray<DocumentId> ReportDiagnostics(DebuggingSession session, object errorId, ProjectId projectId, Solution solution, IEnumerable<Diagnostic> diagnostics)
        {
            return ReportDiagnostics(new EncErrorId(session, errorId), solution, projectId, diagnostics);
        }

        internal void ClearDiagnostics(DebuggingSession session, Workspace workspace, object errorId, ProjectId projectId, ImmutableArray<DocumentId> documentIds)
        {
            ClearDiagnostics(new EncErrorId(session, errorId), workspace.CurrentSolution, projectId, documentIds);
        }
    }
}
