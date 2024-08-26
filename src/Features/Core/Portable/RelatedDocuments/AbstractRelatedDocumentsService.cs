// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.RelatedDocuments;

internal abstract class AbstractRelatedDocumentsService : IRelatedDocumentsService
{
    public async ValueTask GetRelatedDocumentIdsAsync(
        Document document, int position, Func<DocumentId, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var remoteCallback = new RemoteRelatedDocumentsServiceCallback(
                async (documentIds, cancellationToken) =>
                {
                    foreach (var documentId in documentIds)
                        await callbackAsync(documentId, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken);

            var result = await client.TryInvokeAsync<IRemoteRelatedDocumentsService>(
                // We don't need to sync the entire solution (only the project) to ask for the related files for a
                // particular document.
                document.Project,
                (service, solutionChecksum, callbackId, cancellationToken) => service.GetRelatedDocumentIdsAsync(
                    solutionChecksum, document.Id, position, callbackId, cancellationToken),
                remoteCallback,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await GetRelatedDocumentIdsInCurrentProcessAsync(
                document, position, callbackAsync, cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract ValueTask GetRelatedDocumentIdsInCurrentProcessAsync(
        Document document, int position, Func<DocumentId, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken);
}
