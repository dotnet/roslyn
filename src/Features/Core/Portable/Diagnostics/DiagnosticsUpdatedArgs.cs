// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticsUpdatedArgs : UpdatedEventArgs
    {
        public DiagnosticsUpdatedKind Kind { get; }
        public Solution? Solution { get; }

        private readonly ImmutableArray<DiagnosticData> _diagnostics;

        private DiagnosticsUpdatedArgs(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId,
            ImmutableArray<DiagnosticData> diagnostics,
            DiagnosticsUpdatedKind kind)
            : base(id, workspace, projectId, documentId)
        {
            Debug.Assert(diagnostics.All(d => d.ProjectId == projectId && d.DocumentId == documentId));
            Debug.Assert(kind != DiagnosticsUpdatedKind.DiagnosticsRemoved || diagnostics.IsEmpty);

            Solution = solution;
            Kind = kind;
            _diagnostics = diagnostics;
        }

        public ImmutableArray<DiagnosticData> Diagnostics => _diagnostics;

        public static DiagnosticsUpdatedArgs DiagnosticsCreated(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId,
            ImmutableArray<DiagnosticData> diagnostics)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, diagnostics, DiagnosticsUpdatedKind.DiagnosticsCreated);
        }

        public static DiagnosticsUpdatedArgs DiagnosticsRemoved(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, ImmutableArray<DiagnosticData>.Empty, DiagnosticsUpdatedKind.DiagnosticsRemoved);
        }
    }
}
