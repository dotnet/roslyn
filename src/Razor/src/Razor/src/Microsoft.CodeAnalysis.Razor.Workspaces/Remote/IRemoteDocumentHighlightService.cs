// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteDocumentHighlightService
{
    ValueTask<RemoteResponse<RemoteDocumentHighlight[]?>> GetHighlightsAsync(RazorPinnedSolutionInfoWrapper solutionInfo, DocumentId documentId, LinePosition position, CancellationToken cancellationToken);
}
