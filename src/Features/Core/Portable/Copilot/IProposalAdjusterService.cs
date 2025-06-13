// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotProposalAdjusterService : IWorkspaceService
{
    ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken); 
}

internal interface IRemoteCopilotProposalAdjusterService
{
    ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken);
}

internal sealed class DefaultCopilotProposalAdjusterService : ICopilotProposalAdjusterService
{
    public async ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteCopilotProposalAdjusterService, ImmutableArray<TextChange>>(
                document.Project,
                (service, checksum, cancellationToken) => service.AdjustProposalAsync(checksum, document.Id, textChanges, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : [];
        }
        else
        {
            return await AdjustProposalInCurrentProcessAsync(
                document, textChanges, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ImmutableArray<TextChange>> AdjustProposalInCurrentProcessAsync(Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
