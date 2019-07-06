// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticsUpdatedArgs : UpdatedEventArgs
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
            // TODO: This assert fails for EditAndContinueDiagnosticUpdateSource. See https://github.com/dotnet/roslyn/issues/36246.
            // Debug.Assert(diagnostics.All(d => d.ProjectId == projectId && d.DocumentId == documentId));

            Debug.Assert(kind != DiagnosticsUpdatedKind.DiagnosticsRemoved || diagnostics.IsEmpty);

            Solution = solution;
            Diagnostics = diagnostics;
            Kind = kind;
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
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, ImmutableArray<DiagnosticData>.Empty, DiagnosticsUpdatedKind.DiagnosticsRemoved);
        }
    }
}
