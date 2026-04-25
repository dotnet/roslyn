// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteSignatureHelpService : IRemoteJsonService
{
    ValueTask<LspSignatureHelp?> GetSignatureHelpAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId documentId, Position linePosition, CancellationToken cancellationToken);
}
