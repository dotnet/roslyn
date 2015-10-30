// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class DiagnosticsUpdatedArgs : UpdatedEventArgs
    {
        public DiagnosticsUpdatedKind Kind { get; }
        public Solution Solution { get; }
        public ImmutableArray<DiagnosticData> Diagnostics { get; }

        private DiagnosticsUpdatedArgs(
            object id,
            Workspace workspace,
            Solution solution,
            ProjectId projectId,
            DocumentId documentId,
            ImmutableArray<DiagnosticData> diagnostics,
            DiagnosticsUpdatedKind kind)
                : base(id, workspace, projectId, documentId)
        {
            Solution = solution;
            Diagnostics = diagnostics;
            Kind = kind;

            if (kind == DiagnosticsUpdatedKind.DiagnosticsRemoved)
            {
                Debug.Assert(diagnostics.IsEmpty);
            }
        }

        public static DiagnosticsUpdatedArgs DiagnosticsCreated(
            object id,
            Workspace workspace,
            Solution solution,
            ProjectId projectId,
            DocumentId documentId,
            ImmutableArray<DiagnosticData> diagnostics)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, diagnostics, DiagnosticsUpdatedKind.DiagnosticsCreated);
        }

        public static DiagnosticsUpdatedArgs DiagnosticsRemoved(
            object id,
            Workspace workspace,
            Solution solution,
            ProjectId projectId,
            DocumentId documentId)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, ImmutableArray<DiagnosticData>.Empty, DiagnosticsUpdatedKind.DiagnosticsCreated);
        }
    }

    internal enum DiagnosticsUpdatedKind
    {
        /// <summary>
        /// Called when the diagnostic analyzer engine decides to remove existing diagnostics.
        /// For example, this can happen when a document is removed from a solution.  In that
        /// case the analyzer engine will delete all diagnostics associated with that document.
        /// Any layers caching diagnostics should listen for these events to know when to 
        /// delete their cached items entirely.
        /// </summary>
        DiagnosticsRemoved,

        /// <summary>
        /// Called when a new set of (possibly empty) diagnostics have been produced.  This
        /// happens through normal editing and processing of files as diagnostic analyzers
        /// produce new diagnostics for documents and projects.
        /// </summary>
        DiagnosticsCreated,
    }
}
