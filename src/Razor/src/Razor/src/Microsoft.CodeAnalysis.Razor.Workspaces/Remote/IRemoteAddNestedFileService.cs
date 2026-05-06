// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.NestedFiles;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteAddNestedFileService : IRemoteJsonService
{
    /// <summary>
    /// Gets an edit to create a nested file (CSS, C# code-behind, or JavaScript) for a Razor file.
    /// Returns a <see cref="WorkspaceEdit"/> containing CreateFile + TextDocumentEdit operations,
    /// or null if the operation could not be completed.
    /// </summary>
    ValueTask<WorkspaceEdit?> GetNewNestedFileWorkspaceEditAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        NestedFileKind fileKind,
        CancellationToken cancellationToken);
}
