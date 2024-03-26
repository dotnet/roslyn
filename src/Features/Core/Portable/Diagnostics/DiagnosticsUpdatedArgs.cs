// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class DiagnosticsUpdatedArgs
{
    public readonly DiagnosticsUpdatedKind Kind;
    public readonly Solution? Solution;

    /// <summary>
    /// <see cref="ProjectId"/> this update is associated with, or <see langword="null"/>.
    /// </summary>
    public readonly ProjectId? ProjectId;

    /// <summary>
    /// <see cref="DocumentId"/> this update is associated with, or <see langword="null"/>.
    /// </summary>
    public readonly DocumentId? DocumentId;
    public readonly ImmutableArray<DiagnosticData> Diagnostics;

    private DiagnosticsUpdatedArgs(
        Solution? solution,
        ProjectId? projectId,
        DocumentId? documentId,
        ImmutableArray<DiagnosticData> diagnostics,
        DiagnosticsUpdatedKind kind)
    {
        Debug.Assert(diagnostics.All(d => d.ProjectId == projectId && d.DocumentId == documentId));
        Debug.Assert(kind != DiagnosticsUpdatedKind.DiagnosticsRemoved || diagnostics.IsEmpty);

        Solution = solution;
        ProjectId = projectId;
        DocumentId = documentId;
        Kind = kind;
        Diagnostics = diagnostics;
    }

    public static DiagnosticsUpdatedArgs DiagnosticsCreated(
        Solution solution,
        ProjectId? projectId,
        DocumentId? documentId,
        ImmutableArray<DiagnosticData> diagnostics)
    {
        return new DiagnosticsUpdatedArgs(solution, projectId, documentId, diagnostics, DiagnosticsUpdatedKind.DiagnosticsCreated);
    }

    public static DiagnosticsUpdatedArgs DiagnosticsRemoved(
        Solution? solution,
        ProjectId? projectId,
        DocumentId? documentId)
    {
        return new DiagnosticsUpdatedArgs(solution, projectId, documentId, [], DiagnosticsUpdatedKind.DiagnosticsRemoved);
    }
}
