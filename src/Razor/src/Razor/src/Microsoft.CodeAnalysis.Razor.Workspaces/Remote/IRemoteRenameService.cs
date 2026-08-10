// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteRenameService : IRemoteJsonService
{
    ValueTask<RemoteResponse<LspRange?>> GetPrepareRenameRangeAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId documentId, Position position, CancellationToken cancellationToken);
    ValueTask<RemoteResponse<WorkspaceEdit?>> GetRenameEditAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId documentId, Position position, string newName, CancellationToken cancellationToken);
    ValueTask<WorkspaceEdit?> GetFileRenameEditAsync(JsonSerializableRazorSolutionWrapper solutionInfo, RenameFilesParams fileRenameRequest, CancellationToken cancellationToken);
}
